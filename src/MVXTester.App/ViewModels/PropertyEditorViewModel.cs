using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using MVXTester.Core.Models;
using MVXTester.Core.UndoRedo;
using MVXTester.App.UndoRedo;
using MVXTester.App.Views;

namespace MVXTester.App.ViewModels;

public partial class DeviceOptionItem : ObservableObject
{
    private readonly PropertyItem _parent;
    public string Name { get; }
    public int Index { get; }
    [ObservableProperty] private bool _isSelected;

    public DeviceOptionItem(string name, int index, PropertyItem parent, bool isSelected = false)
    {
        Name = name;
        Index = index;
        _parent = parent;
        _isSelected = isSelected;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
            _parent.SelectDevice(Index);
    }
}

public partial class PropertyItem : ObservableObject
{
    private readonly NodeProperty _property;
    private readonly Action _onChanged;
    private readonly UndoRedoManager? _undoManager;
    private readonly EditorViewModel? _editor;
    private readonly string? _nodeId;
    private object? _previousValue;
    private bool _suppressUndo;
    private readonly INode? _ownerNode;

    public string Name => _property.DisplayName;
    public string PropertyName => _property.Name;
    public string? Description => _property.Description;
    public PropertyType PropertyType => _property.PropertyType;
    public object? MinValue => _property.MinValue;
    public object? MaxValue => _property.MaxValue;
    public Type? EnumType => _property.EnumType;

    [ObservableProperty] private object? _value;

    /// <summary>
    /// Controls visibility of this property in the UI.
    /// Synced from NodeProperty.IsVisible via VisibilityChanged event.
    /// </summary>
    [ObservableProperty] private bool _isVisible = true;

    /// <summary>
    /// Slider-friendly double property that converts to/from the actual Value (boxed int or double).
    /// WPF Slider.Value is double, but PropertyItem.Value can be boxed int — direct binding breaks positioning.
    /// </summary>
    public double SliderValue
    {
        get
        {
            try { return Convert.ToDouble(Value ?? 0); }
            catch { return 0.0; }
        }
        set
        {
            if (PropertyType == PropertyType.Integer)
                Value = (int)Math.Round(value);
            else
                Value = value;
        }
    }

    [ObservableProperty] private ObservableCollection<DeviceOptionItem> _deviceOptions = new();

    public PropertyItem(NodeProperty property, Action onChanged,
        UndoRedoManager? undoManager = null, EditorViewModel? editor = null, string? nodeId = null,
        INode? ownerNode = null)
    {
        _property = property;
        _onChanged = onChanged;
        _undoManager = undoManager;
        _editor = editor;
        _nodeId = nodeId;
        _ownerNode = ownerNode;
        _value = property.Value;
        _previousValue = property.Value;
        _isVisible = property.IsVisible;

        // Subscribe to visibility changes (e.g., CameraNode hides irrelevant properties)
        property.VisibilityChanged += () => IsVisible = property.IsVisible;

        if (property.PropertyType == PropertyType.DeviceList)
        {
            RefreshDeviceOptions();
            property.OptionsChanged += RefreshDeviceOptions;
        }
    }

    private void RefreshDeviceOptions()
    {
        var currentValue = _property.GetValue<int>();
        var items = new ObservableCollection<DeviceOptionItem>();
        foreach (var opt in _property.DeviceOptions)
            items.Add(new DeviceOptionItem(opt.Name, opt.Index, this, opt.Index == currentValue));
        DeviceOptions = items;
    }

    public void SelectDevice(int deviceIndex)
    {
        Value = deviceIndex;
        foreach (var opt in DeviceOptions)
            opt.IsSelected = opt.Index == deviceIndex;
    }

    [RelayCommand]
    private void RefreshDevices()
    {
        if (_ownerNode is MVXTester.Core.Models.IDeviceEnumerable enumerable)
            enumerable.EnumerateDevices();
    }

    partial void OnValueChanged(object? value)
    {
        // Keep slider in sync whenever Value changes (undo/redo, text box, etc.)
        OnPropertyChanged(nameof(SliderValue));

        if (_suppressUndo)
        {
            _property.SetValue(value);
            _onChanged();
            return;
        }

        var oldValue = _previousValue;
        _property.SetValue(value);
        _previousValue = value;

        if (_undoManager != null && _editor != null && _nodeId != null && !_undoManager.IsExecutingUndoRedo)
        {
            var lastAction = _undoManager.PeekUndo() as ChangePropertyAction;
            if (lastAction != null && lastAction.TryMerge(_nodeId, _property.Name, value))
            {
                // Merged
            }
            else
            {
                var action = new ChangePropertyAction(_editor, _nodeId, _property.Name, oldValue, value);
                _undoManager.PushAction(action);
            }
        }

        _onChanged();
    }

    public void SetValueSilently(object? value)
    {
        _suppressUndo = true;
        Value = value;
        _previousValue = value;
        _suppressUndo = false;
    }

    public Array? EnumValues => EnumType != null ? Enum.GetValues(EnumType) : null;

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.tif|Video Files|*.mp4;*.avi;*.mov;*.mkv|Cascade Files|*.xml|CSV Files|*.csv|All Files|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            Value = dialog.FileName;
        }
    }
}

public partial class PropertyEditorViewModel : ObservableObject
{
    [ObservableProperty] private NodeViewModel? _selectedNode;
    [ObservableProperty] private ObservableCollection<PropertyItem> _properties = new();
    [ObservableProperty] private string _nodeName = "";
    [ObservableProperty] private string _nodeDescription = "";
    [ObservableProperty] private string _nodeCategory = "";
    [ObservableProperty] private WriteableBitmap? _resultImage;
    [ObservableProperty] private string _resultImageInfo = "";
    [ObservableProperty] private bool _isFunctionNode;

    // 마우스 좌표 표시
    [ObservableProperty] private string _cursorInfo = "";

    // 드래그 ROI 오버레이 (표시 좌표 기준)
    [ObservableProperty] private double _roiLeft;
    [ObservableProperty] private double _roiTop;
    [ObservableProperty] private double _roiWidth;
    [ObservableProperty] private double _roiHeight;
    [ObservableProperty] private bool _isRoiVisible;

    // 원본 이미지 크기 (좌표 변환용)
    private int _originalImageWidth;
    private int _originalImageHeight;

    private bool _isDragging;
    private System.Windows.Point _dragStart;          // 표시 좌표
    private System.Windows.Point _dragStartOriginal;  // 원본 좌표

    private Action? _onPropertyChanged;
    private UndoRedoManager? _undoManager;
    private EditorViewModel? _editor;

    public void Initialize(UndoRedoManager undoManager, EditorViewModel editor)
    {
        _undoManager = undoManager;
        _editor = editor;
    }

    public void SetSelectedNode(NodeViewModel? node, Action? onPropertyChanged)
    {
        _onPropertyChanged = onPropertyChanged;
        SelectedNode = node;
        Properties.Clear();
        ResultImage = null;
        ResultImageInfo = "";
        IsFunctionNode = node?.Model is FunctionNode;

        if (node == null)
        {
            NodeName = "";
            NodeDescription = "";
            NodeCategory = "";
            return;
        }

        NodeName = node.Model.Name;
        NodeDescription = node.Model.Description;
        NodeCategory = node.Model.Category;

        foreach (var prop in node.Model.Properties)
        {
            Properties.Add(new PropertyItem(prop, () => _onPropertyChanged?.Invoke(),
                _undoManager, _editor, node.Model.Id, node.Model));
        }

        UpdateResultImage();
    }

    public void UpdateResultImage()
    {
        if (SelectedNode == null)
        {
            ResultImage = null;
            ResultImageInfo = "";
            return;
        }

        try
        {
            // 좌표/ROI 속성이 있는 노드는 입력(원본) 이미지를 표시하여
            // 사용자가 전체 이미지 위에서 영역을 선택할 수 있게 함
            using var snapshot = HasCoordinateProperties()
                ? (CloneInputImage() ?? SelectedNode.Model.ClonePreview())
                : SelectedNode.Model.ClonePreview();
            if (snapshot != null)
            {
                _originalImageWidth = snapshot.Width;
                _originalImageHeight = snapshot.Height;

                Mat display;
                var maxW = 240.0;
                var maxH = 240.0;
                var scale = Math.Min(maxW / snapshot.Width, maxH / snapshot.Height);
                if (scale < 1.0)
                {
                    display = new Mat();
                    Cv2.Resize(snapshot, display, new OpenCvSharp.Size(0, 0), scale, scale);
                }
                else
                {
                    display = snapshot.Clone();
                }

                if (display.Channels() == 1)
                {
                    var bgr = new Mat();
                    Cv2.CvtColor(display, bgr, ColorConversionCodes.GRAY2BGR);
                    display.Dispose();
                    display = bgr;
                }

                ResultImage = display.ToWriteableBitmap();
                ResultImageInfo = $"{snapshot.Width} x {snapshot.Height}  |  {snapshot.Channels()}ch  |  {snapshot.Type()}";
                display.Dispose();
            }
            else
            {
                ResultImage = null;
                ResultImageInfo = "";
            }
        }
        catch
        {
            ResultImage = null;
            ResultImageInfo = "";
        }
    }

    public void RefreshValues()
    {
        foreach (var prop in Properties)
        {
            var modelProp = SelectedNode?.Model.Properties
                .FirstOrDefault(p => p.DisplayName == prop.Name);
            if (modelProp != null)
                prop.SetValueSilently(modelProp.Value);
        }
    }

    [RelayCommand]
    private void ViewFunctionDetail()
    {
        if (SelectedNode?.Model is not FunctionNode fn) return;
        if (fn.SubGraph == null) return;

        var vm = new FunctionDetailViewModel(fn);
        var dialog = new FunctionDetailDialog(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }

    /// <summary>
    /// 선택된 노드의 첫 번째 Mat 입력 포트에서 원본 이미지를 가져옴.
    /// ROI 인터랙션 시 출력(크롭 결과) 대신 입력(전체) 이미지를 보여주기 위함.
    /// </summary>
    private Mat? CloneInputImage()
    {
        if (SelectedNode?.Model == null) return null;
        var matInput = SelectedNode.Model.Inputs
            .FirstOrDefault(p => p.DataType == typeof(Mat) && p.IsConnected);
        if (matInput == null) return null;

        try
        {
            var mat = matInput.GetValue() as Mat;
            if (mat != null && !mat.IsDisposed && !mat.Empty())
                return mat.Clone();
        }
        catch { }
        return null;
    }

    // --- 이미지 인터랙티브 좌표/ROI ---

    public (int x, int y) ToOriginalCoord(double displayX, double displayY,
        double displayWidth, double displayHeight)
    {
        if (displayWidth <= 0 || displayHeight <= 0
            || _originalImageWidth <= 0 || _originalImageHeight <= 0)
            return (0, 0);

        // Stretch="Uniform" 레터박싱 보정
        double imgAspect = (double)_originalImageWidth / _originalImageHeight;
        double elemAspect = displayWidth / displayHeight;
        double imgW, imgH, offX, offY;

        if (elemAspect > imgAspect)
        {
            // 엘리먼트가 더 넓음 → 좌우 여백
            imgH = displayHeight;
            imgW = displayHeight * imgAspect;
            offX = (displayWidth - imgW) / 2;
            offY = 0;
        }
        else
        {
            // 엘리먼트가 더 높음 → 상하 여백
            imgW = displayWidth;
            imgH = displayWidth / imgAspect;
            offX = 0;
            offY = (displayHeight - imgH) / 2;
        }

        var x = (int)((displayX - offX) / imgW * _originalImageWidth);
        var y = (int)((displayY - offY) / imgH * _originalImageHeight);
        return (Math.Clamp(x, 0, Math.Max(_originalImageWidth - 1, 0)),
                Math.Clamp(y, 0, Math.Max(_originalImageHeight - 1, 0)));
    }

    public void OnImageMouseMove(double displayX, double displayY,
        double displayWidth, double displayHeight)
    {
        var (ox, oy) = ToOriginalCoord(displayX, displayY, displayWidth, displayHeight);
        CursorInfo = $"X: {ox}, Y: {oy}";

        if (_isDragging)
        {
            RoiLeft = Math.Min(_dragStart.X, displayX);
            RoiTop = Math.Min(_dragStart.Y, displayY);
            RoiWidth = Math.Abs(displayX - _dragStart.X);
            RoiHeight = Math.Abs(displayY - _dragStart.Y);
        }
    }

    public void OnImageMouseDown(double displayX, double displayY,
        double displayWidth, double displayHeight)
    {
        // 좌표/ROI 속성이 없는 노드에서는 마우스 인터랙션 무시
        if (!HasCoordinateProperties()) return;

        var (ox, oy) = ToOriginalCoord(displayX, displayY, displayWidth, displayHeight);
        _dragStart = new System.Windows.Point(displayX, displayY);
        _dragStartOriginal = new System.Windows.Point(ox, oy);
        _isDragging = true;
        IsRoiVisible = true;
        RoiLeft = displayX;
        RoiTop = displayY;
        RoiWidth = 0;
        RoiHeight = 0;
    }

    public void OnImageMouseUp(double displayX, double displayY,
        double displayWidth, double displayHeight)
    {
        if (!_isDragging) return;
        _isDragging = false;

        var (endX, endY) = ToOriginalCoord(displayX, displayY, displayWidth, displayHeight);
        var startX = (int)_dragStartOriginal.X;
        var startY = (int)_dragStartOriginal.Y;

        var dragDist = Math.Abs(endX - startX) + Math.Abs(endY - startY);

        if (dragDist < 3)
        {
            // 클릭: X, Y 속성만 설정
            SetPropertyByName("X", endX);
            SetPropertyByName("Y", endY);
            IsRoiVisible = false;
        }
        else
        {
            // 드래그: X, Y, Width, Height 설정
            SetPropertyByName("X", Math.Min(startX, endX));
            SetPropertyByName("Y", Math.Min(startY, endY));
            SetPropertyByName("Width", Math.Abs(endX - startX));
            SetPropertyByName("Height", Math.Abs(endY - startY));
            // ROI 오버레이 2초 후 자동 숨김
            _ = Task.Delay(2000).ContinueWith(_ =>
                Application.Current?.Dispatcher.BeginInvoke(
                    () => IsRoiVisible = false));
        }
    }

    public void OnImageMouseLeave()
    {
        CursorInfo = "";
        if (!_isDragging) IsRoiVisible = false;
    }

    /// <summary>
    /// Double-click on image: reset X, Y to 0 and Width, Height to full image size.
    /// </summary>
    public void OnImageDoubleClick()
    {
        _isDragging = false;
        IsRoiVisible = false;

        // 좌표/ROI 속성이 없는 노드에서는 무시
        if (!HasCoordinateProperties()) return;

        SetPropertyByName("X", 0);
        SetPropertyByName("Y", 0);
        if (_originalImageWidth > 0)
            SetPropertyByName("Width", _originalImageWidth);
        if (_originalImageHeight > 0)
            SetPropertyByName("Height", _originalImageHeight);
    }

    // 표준 이름 → 노드별 별칭 매핑 (ROI/좌표 속성)
    private static readonly Dictionary<string, string[]> PropertyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "X", new[] { "X", "RectX", "RoiX", "SeedX", "CenterX" } },
        { "Y", new[] { "Y", "RectY", "RoiY", "SeedY", "CenterY" } },
        { "Width", new[] { "Width", "W", "RectW", "RoiWidth" } },
        { "Height", new[] { "Height", "H", "RectH", "RoiHeight" } },
    };

    /// <summary>
    /// 현재 선택된 노드에 좌표/ROI 관련 속성이 있는지 확인.
    /// 없으면 마우스 인터랙션을 무시하여 의도치 않은 값 변경을 방지.
    /// </summary>
    private bool HasCoordinateProperties()
    {
        var allAliases = PropertyAliases.Values.SelectMany(a => a);
        return Properties.Any(p =>
            allAliases.Any(a => p.PropertyName.Equals(a, StringComparison.OrdinalIgnoreCase)) &&
            (p.PropertyType == PropertyType.Integer || p.PropertyType == PropertyType.Double));
    }

    private void SetPropertyByName(string name, int value)
    {
        if (!PropertyAliases.TryGetValue(name, out var aliases))
            aliases = new[] { name };

        var prop = Properties.FirstOrDefault(p =>
            aliases.Any(a => p.PropertyName.Equals(a, StringComparison.OrdinalIgnoreCase)) &&
            (p.PropertyType == PropertyType.Integer || p.PropertyType == PropertyType.Double));

        if (prop != null)
        {
            if (prop.PropertyType == PropertyType.Double)
                prop.Value = (double)value;
            else
                prop.Value = value;
        }
    }
}
