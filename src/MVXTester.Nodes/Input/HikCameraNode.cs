using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;
using System.Runtime.InteropServices;

namespace MVXTester.Nodes.Input;

public enum HikTriggerMode
{
    Continuous,
    Software,
    Hardware
}

[NodeInfo("HIK Camera", NodeCategories.Input, Description = "HIK GigE camera capture using MvCameraControl.Net SDK")]
public class HikCameraNode : BaseNode, IStreamingSource
{
    private InputPort<int> _triggerInput = null!;
    private OutputPort<Mat> _frameOutput = null!;
    private NodeProperty _deviceIndex = null!;
    private NodeProperty _triggerMode = null!;
    private NodeProperty _exposureTime = null!;
    private NodeProperty _gain = null!;
    private NodeProperty _width = null!;
    private NodeProperty _height = null!;

    private dynamic? _camera;
    private Type? _myCameraType;
    private bool _isOpen;
    private int _lastDeviceIndex = -1;

    protected override void Setup()
    {
        _triggerInput = AddInput<int>("Trigger");
        _frameOutput = AddOutput<Mat>("Frame");
        _deviceIndex = AddIntProperty("DeviceIndex", "Device Index", 0, 0, 16, "Camera device index");
        _triggerMode = AddEnumProperty("TriggerMode", "Trigger Mode", HikTriggerMode.Continuous, "Trigger mode");
        _exposureTime = AddDoubleProperty("ExposureTime", "Exposure Time (us)", 10000.0, 16.0, 10000000.0, "Exposure time in microseconds");
        _gain = AddDoubleProperty("Gain", "Gain (dB)", 0.0, 0.0, 20.0, "Analog gain in dB");
        _width = AddIntProperty("Width", "Width", 0, 0, 10000, "Image width (0 = max)");
        _height = AddIntProperty("Height", "Height", 0, 0, 10000, "Image height (0 = max)");
    }

    public override void Process()
    {
        try
        {
            var deviceIndex = _deviceIndex.GetValue<int>();

            if (!_isOpen || deviceIndex != _lastDeviceIndex)
            {
                CloseCamera();
                OpenCamera(deviceIndex);
                _lastDeviceIndex = deviceIndex;
            }

            if (!_isOpen || _camera == null)
            {
                Error = "Camera not opened";
                return;
            }

            // Set exposure
            var exposureTime = _exposureTime.GetValue<double>();
            InvokeMethod("MV_CC_SetFloatValue_NET", "ExposureTime", (float)exposureTime);

            // Set gain
            var gain = _gain.GetValue<double>();
            InvokeMethod("MV_CC_SetFloatValue_NET", "Gain", (float)gain);

            // Software trigger
            var triggerMode = _triggerMode.GetValue<HikTriggerMode>();
            if (triggerMode == HikTriggerMode.Software)
            {
                InvokeMethod("MV_CC_SetCommandValue_NET", "TriggerSoftware");
            }

            // Get frame using MV_CC_GetImageBuffer_NET
            var frameOutType = _myCameraType?.Assembly.GetType("MvCamCtrl.NET.MyCamera+MV_FRAME_OUT");
            if (frameOutType == null)
            {
                Error = "MV_FRAME_OUT type not found";
                return;
            }

            var frameOut = Activator.CreateInstance(frameOutType)!;
            var getImageMethod = _camera.GetType().GetMethod("MV_CC_GetImageBuffer_NET");
            if (getImageMethod == null)
            {
                Error = "MV_CC_GetImageBuffer_NET not found";
                return;
            }

            var args = new object[] { frameOut, 1000 };
            int ret = (int)(getImageMethod.Invoke(_camera, args) ?? -1);

            if (ret != 0)
            {
                Error = $"Get frame failed: 0x{ret:X8}";
                return;
            }

            frameOut = args[0];

            // Read frame info
            var stFrameInfo = frameOutType.GetField("stFrameInfo")?.GetValue(frameOut);
            if (stFrameInfo == null)
            {
                Error = "Cannot read frame info";
                FreeImageBuffer(frameOut);
                return;
            }

            var frameInfoType = stFrameInfo.GetType();
            uint w = (uint)(frameInfoType.GetField("nWidth")?.GetValue(stFrameInfo) ?? 0u);
            uint h = (uint)(frameInfoType.GetField("nHeight")?.GetValue(stFrameInfo) ?? 0u);
            uint frameLen = (uint)(frameInfoType.GetField("nFrameLen")?.GetValue(stFrameInfo) ?? 0u);

            if (w == 0 || h == 0)
            {
                Error = "Invalid frame dimensions";
                FreeImageBuffer(frameOut);
                return;
            }

            // Get buffer pointer
            IntPtr pBufAddr = (IntPtr)(frameOutType.GetField("pBufAddr")?.GetValue(frameOut) ?? IntPtr.Zero);
            if (pBufAddr == IntPtr.Zero)
            {
                Error = "Invalid buffer address";
                FreeImageBuffer(frameOut);
                return;
            }

            // Determine pixel format
            var pixelType = frameInfoType.GetField("enPixelType")?.GetValue(stFrameInfo);
            int pixelTypeInt = pixelType != null ? (int)Convert.ChangeType(pixelType, typeof(int)) : 0;
            bool isMono = (pixelTypeInt & 0x01000000) != 0;

            Mat frame;
            if (isMono)
            {
                int size = (int)w * (int)h;
                byte[] data = new byte[size];
                Marshal.Copy(pBufAddr, data, 0, size);
                frame = new Mat((int)h, (int)w, MatType.CV_8UC1);
                Marshal.Copy(data, 0, frame.Data, size);
            }
            else
            {
                // RGB/BGR - check if 3 channel or needs conversion
                int expectedLen = (int)w * (int)h * 3;
                if (frameLen >= (uint)expectedLen)
                {
                    byte[] data = new byte[expectedLen];
                    Marshal.Copy(pBufAddr, data, 0, expectedLen);
                    frame = new Mat((int)h, (int)w, MatType.CV_8UC3);
                    Marshal.Copy(data, 0, frame.Data, expectedLen);
                    // HIK outputs RGB, OpenCV needs BGR
                    Cv2.CvtColor(frame, frame, ColorConversionCodes.RGB2BGR);
                }
                else
                {
                    // Bayer or other format - try mono first
                    int size = (int)w * (int)h;
                    byte[] data = new byte[size];
                    Marshal.Copy(pBufAddr, data, 0, size);
                    frame = new Mat((int)h, (int)w, MatType.CV_8UC1);
                    Marshal.Copy(data, 0, frame.Data, size);
                    Cv2.CvtColor(frame, frame, ColorConversionCodes.BayerRG2BGR);
                }
            }

            // Free the SDK buffer
            FreeImageBuffer(frameOut);

            SetOutputValue(_frameOutput, frame);
            SetPreview(frame);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"HIK Camera error: {ex.Message}";
        }
    }

    private void FreeImageBuffer(object frameOut)
    {
        try
        {
            var freeMethod = _camera?.GetType().GetMethod("MV_CC_FreeImageBuffer_NET");
            if (freeMethod != null)
            {
                var args = new object[] { frameOut };
                freeMethod.Invoke(_camera, args);
            }
        }
        catch { }
    }

    private int InvokeMethod(string methodName, params object[] args)
    {
        try
        {
            var method = _camera?.GetType().GetMethod(methodName);
            if (method != null)
            {
                var result = method.Invoke(_camera, args);
                return result is int r ? r : -1;
            }
        }
        catch { }
        return -1;
    }

    private void OpenCamera(int deviceIndex)
    {
        try
        {
            // Load the SDK type
            _myCameraType = Type.GetType("MvCamCtrl.NET.MyCamera, MvCameraControl.Net");
            if (_myCameraType == null)
            {
                // Try loading the assembly explicitly
                try
                {
                    var asm = System.Reflection.Assembly.LoadFrom(
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MvCameraControl.Net.dll"));
                    _myCameraType = asm.GetType("MvCamCtrl.NET.MyCamera");
                }
                catch { }
            }

            if (_myCameraType == null)
            {
                Error = "MvCameraControl.Net SDK not found. Ensure MvCameraControl.Net.dll is available.";
                return;
            }

            // Initialize SDK
            var initMethod = _myCameraType.GetMethod("MV_CC_Initialize_NET",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            initMethod?.Invoke(null, null);

            // Enumerate devices
            var deviceListType = _myCameraType.Assembly.GetType("MvCamCtrl.NET.MyCamera+MV_CC_DEVICE_INFO_LIST");
            if (deviceListType == null)
            {
                Error = "Device list type not found in SDK";
                return;
            }

            var deviceList = Activator.CreateInstance(deviceListType)!;

            // MV_GIGE_DEVICE=1, MV_USB_DEVICE=4
            var enumMethod = _myCameraType.GetMethod("MV_CC_EnumDevices_NET",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (enumMethod == null)
            {
                Error = "MV_CC_EnumDevices_NET method not found";
                return;
            }

            var enumArgs = new object[] { (uint)(0x1 | 0x4), deviceList };
            int ret = (int)(enumMethod.Invoke(null, enumArgs) ?? -1);
            deviceList = enumArgs[1];

            if (ret != 0)
            {
                Error = $"Enumerate devices failed: 0x{ret:X8}";
                return;
            }

            var deviceCountField = deviceListType.GetField("nDeviceNum");
            uint deviceCount = (uint)(deviceCountField?.GetValue(deviceList) ?? 0u);
            if (deviceCount == 0)
            {
                Error = "No HIK cameras found";
                return;
            }

            if (deviceIndex >= (int)deviceCount)
            {
                Error = $"Device index {deviceIndex} out of range (found {deviceCount})";
                return;
            }

            // Get device info
            var pDeviceInfoField = deviceListType.GetField("pDeviceInfo");
            var pDeviceInfoArray = pDeviceInfoField?.GetValue(deviceList) as IntPtr[];
            if (pDeviceInfoArray == null || pDeviceInfoArray.Length <= deviceIndex)
            {
                Error = "Failed to get device info array";
                return;
            }

            var deviceInfoType = _myCameraType.Assembly.GetType("MvCamCtrl.NET.MyCamera+MV_CC_DEVICE_INFO");
            if (deviceInfoType == null)
            {
                Error = "MV_CC_DEVICE_INFO type not found";
                return;
            }

            var deviceInfo = Marshal.PtrToStructure(pDeviceInfoArray[deviceIndex], deviceInfoType)!;

            // Create camera instance
            _camera = Activator.CreateInstance(_myCameraType);
            if (_camera == null)
            {
                Error = "Failed to create MyCamera instance";
                return;
            }

            // Create device
            var createMethod = _camera.GetType().GetMethod("MV_CC_CreateDevice_NET");
            if (createMethod != null)
            {
                var createArgs = new object[] { deviceInfo };
                ret = (int)(createMethod.Invoke(_camera, createArgs) ?? -1);
                if (ret != 0)
                {
                    Error = $"Create device failed: 0x{ret:X8}";
                    return;
                }
            }

            // Open device
            ret = InvokeMethod("MV_CC_OpenDevice_NET");
            if (ret != 0)
            {
                Error = $"Open device failed: 0x{ret:X8}";
                return;
            }

            // Check if GigE and set optimal packet size
            var nTLayerField = deviceInfoType.GetField("nTLayerType");
            uint tLayerType = (uint)(nTLayerField?.GetValue(deviceInfo) ?? 0u);
            if (tLayerType == 0x1) // MV_GIGE_DEVICE
            {
                var getPacketMethod = _camera.GetType().GetMethod("MV_CC_GetOptimalPacketSize_NET");
                if (getPacketMethod != null)
                {
                    int packetSize = (int)(getPacketMethod.Invoke(_camera, null) ?? 0);
                    if (packetSize > 0)
                    {
                        InvokeMethod("MV_CC_SetIntValueEx_NET", "GevSCPSPacketSize", (long)packetSize);
                    }
                }
            }

            // Set trigger mode
            var triggerMode = _triggerMode.GetValue<HikTriggerMode>();
            if (triggerMode == HikTriggerMode.Continuous)
            {
                InvokeMethod("MV_CC_SetEnumValue_NET", "TriggerMode", 0u);
            }
            else
            {
                InvokeMethod("MV_CC_SetEnumValue_NET", "TriggerMode", 1u);
                if (triggerMode == HikTriggerMode.Software)
                    InvokeMethod("MV_CC_SetEnumValue_NET", "TriggerSource", 7u);
                else
                    InvokeMethod("MV_CC_SetEnumValue_NET", "TriggerSource", 0u);
            }

            // Set ROI if specified
            var width = _width.GetValue<int>();
            var height = _height.GetValue<int>();
            if (width > 0)
                InvokeMethod("MV_CC_SetIntValueEx_NET", "Width", (long)width);
            if (height > 0)
                InvokeMethod("MV_CC_SetIntValueEx_NET", "Height", (long)height);

            // Set initial exposure and gain
            var exposureTime = _exposureTime.GetValue<double>();
            InvokeMethod("MV_CC_SetFloatValue_NET", "ExposureTime", (float)exposureTime);
            var gain = _gain.GetValue<double>();
            InvokeMethod("MV_CC_SetFloatValue_NET", "Gain", (float)gain);

            // Start grabbing
            ret = InvokeMethod("MV_CC_StartGrabbing_NET");
            if (ret != 0)
            {
                Error = $"Start grabbing failed: 0x{ret:X8}";
                return;
            }

            _isOpen = true;
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Failed to open HIK camera: {ex.Message}";
            _isOpen = false;
        }
    }

    private void CloseCamera()
    {
        try
        {
            if (_camera != null && _isOpen)
            {
                InvokeMethod("MV_CC_StopGrabbing_NET");
                InvokeMethod("MV_CC_CloseDevice_NET");
                InvokeMethod("MV_CC_DestroyDevice_NET");
            }
        }
        catch { }
        finally
        {
            _camera = null;
            _isOpen = false;
        }
    }

    public override void Cleanup()
    {
        CloseCamera();
        // Finalize SDK
        try
        {
            var finalizeMethod = _myCameraType?.GetMethod("MV_CC_Finalize_NET",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            finalizeMethod?.Invoke(null, null);
        }
        catch { }
        base.Cleanup();
    }
}
