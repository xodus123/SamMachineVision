using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Control;

/// <summary>
/// Real For loop node that repeatedly executes downstream "body" nodes.
/// Usage: Connect processing nodes to this node's outputs. Use a Collect node to gather results.
///
/// Example: For(0..10) → SomeProcessing → Collect
/// The body (SomeProcessing) executes 10 times with Index 0..9.
/// </summary>
[NodeInfo("For", NodeCategories.Control, Description = "Execute downstream body nodes for each index (Start to End)")]
public class ForNode : BaseNode, ILoopNode
{
    private InputPort<int> _startInput = null!;
    private InputPort<int> _endInput = null!;
    private InputPort<int> _stepInput = null!;
    private OutputPort<int> _indexOutput = null!;
    private OutputPort<int> _countOutput = null!;
    private OutputPort<bool> _isRunningOutput = null!;

    private NodeProperty _startProp = null!;
    private NodeProperty _endProp = null!;
    private NodeProperty _stepProp = null!;
    private NodeProperty _maxIterProp = null!;

    private int _start, _end, _step;
    private int _currentIndex;

    public int MaxIterations => _maxIterProp.GetValue<int>();

    protected override void Setup()
    {
        _startInput = AddInput<int>("Start");
        _endInput = AddInput<int>("End");
        _stepInput = AddInput<int>("Step");

        _indexOutput = AddOutput<int>("Index");
        _countOutput = AddOutput<int>("Count");
        _isRunningOutput = AddOutput<bool>("IsRunning");

        _startProp = AddIntProperty("Start", "Start", 0, description: "Loop start value (inclusive)");
        _endProp = AddIntProperty("End", "End", 10, description: "Loop end value (exclusive)");
        _stepProp = AddIntProperty("Step", "Step", 1, min: 1, description: "Loop step increment");
        _maxIterProp = AddIntProperty("MaxIterations", "Max Iterations", 10000, min: 1, max: 1000000,
            description: "Safety limit for maximum iterations");
    }

    public void InitializeLoop()
    {
        _start = GetPortOrProperty(_startInput, _startProp);
        _end = GetPortOrProperty(_endInput, _endProp);
        _step = GetPortOrProperty(_stepInput, _stepProp);
        if (_step <= 0) _step = 1;

        _currentIndex = _start - _step; // Will be incremented in MoveNext

        var count = _step > 0 ? Math.Max(0, (_end - _start + _step - 1) / _step) : 0;
        SetOutputValue(_countOutput, count);
    }

    public bool MoveNext()
    {
        _currentIndex += _step;
        if (_currentIndex >= _end) return false;

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
        // Fallback when no downstream body or used in non-loop context
        var start = GetPortOrProperty(_startInput, _startProp);
        var end = GetPortOrProperty(_endInput, _endProp);
        var step = GetPortOrProperty(_stepInput, _stepProp);
        if (step <= 0) step = 1;

        SetOutputValue(_indexOutput, start);
        SetOutputValue(_countOutput, Math.Max(0, (end - start + step - 1) / step));
        SetOutputValue(_isRunningOutput, false);
    }
}
