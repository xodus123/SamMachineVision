using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Control;

/// <summary>
/// ForEach loop node that iterates over a collection (array/list).
/// Outputs one element at a time to the body nodes.
///
/// Example: FindContours → ForEach → ContourArea → Collect
/// Each contour is processed individually, results collected into an array.
///
/// Supported input types: any array (T[]), IEnumerable, or single object (treated as 1-element).
/// </summary>
[NodeInfo("ForEach", NodeCategories.Control, Description = "Iterate over each element in a collection")]
public class ForEachNode : BaseNode, ILoopNode
{
    private InputPort<object> _collectionInput = null!;
    private OutputPort<object> _elementOutput = null!;
    private OutputPort<int> _indexOutput = null!;
    private OutputPort<int> _countOutput = null!;
    private OutputPort<bool> _isRunningOutput = null!;

    private NodeProperty _maxIterProp = null!;

    private object[]? _items;
    private int _currentIndex;

    public int MaxIterations => Math.Min(_items?.Length ?? 0, _maxIterProp.GetValue<int>());

    protected override void Setup()
    {
        _collectionInput = AddInput<object>("Collection");

        _elementOutput = AddOutput<object>("Element");
        _indexOutput = AddOutput<int>("Index");
        _countOutput = AddOutput<int>("Count");
        _isRunningOutput = AddOutput<bool>("IsRunning");

        _maxIterProp = AddIntProperty("MaxIterations", "Max Iterations", 10000, min: 1, max: 1000000,
            description: "Safety limit for maximum iterations");
    }

    public void InitializeLoop()
    {
        var input = _collectionInput.GetValue();
        _items = ConvertToArray(input);
        _currentIndex = -1;

        SetOutputValue(_countOutput, _items?.Length ?? 0);
    }

    public bool MoveNext()
    {
        _currentIndex++;
        if (_items == null || _currentIndex >= _items.Length) return false;

        SetOutputValue(_elementOutput, _items[_currentIndex]);
        SetOutputValue(_indexOutput, _currentIndex);
        SetOutputValue(_isRunningOutput, true);
        return true;
    }

    public void EndLoop()
    {
        SetOutputValue(_isRunningOutput, false);
    }

    public override void Process()
    {
        // Fallback for non-loop context: output first element
        var input = _collectionInput.GetValue();
        _items = ConvertToArray(input);
        SetOutputValue(_countOutput, _items?.Length ?? 0);
        if (_items != null && _items.Length > 0)
        {
            SetOutputValue(_elementOutput, _items[0]);
            SetOutputValue(_indexOutput, 0);
        }
        else
        {
            SetOutputValue(_elementOutput, null);
            SetOutputValue(_indexOutput, 0);
        }
        SetOutputValue(_isRunningOutput, false);
    }

    /// <summary>
    /// Convert any collection/array type to object[].
    /// Handles: Array (T[]), IEnumerable, single object.
    /// </summary>
    private static object[]? ConvertToArray(object? input)
    {
        if (input == null) return null;

        if (input is Array arr)
        {
            var result = new object[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                result[i] = arr.GetValue(i)!;
            return result;
        }

        if (input is System.Collections.IEnumerable enumerable and not string)
        {
            var list = new System.Collections.Generic.List<object>();
            foreach (var item in enumerable)
                list.Add(item);
            return list.ToArray();
        }

        // Single value → 1-element array
        return new[] { input };
    }
}
