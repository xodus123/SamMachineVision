using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Control;

/// <summary>
/// While/Repeat loop node that repeatedly executes downstream body nodes
/// up to MaxIterations times. Use a BreakIf node in the body to stop early.
///
/// Example: While(Max=100) → Process → BreakIf(condition)
/// The body executes until BreakIf triggers or MaxIterations is reached.
/// </summary>
[NodeInfo("While", NodeCategories.Control, Description = "Repeat body execution (use BreakIf to stop early)")]
public class WhileNode : BaseNode, ILoopNode
{
    private OutputPort<int> _indexOutput = null!;
    private OutputPort<bool> _isRunningOutput = null!;

    private NodeProperty _maxIterProp = null!;

    private int _currentIndex;

    public int MaxIterations => _maxIterProp.GetValue<int>();

    protected override void Setup()
    {
        _indexOutput = AddOutput<int>("Index");
        _isRunningOutput = AddOutput<bool>("IsRunning");

        _maxIterProp = AddIntProperty("MaxIterations", "Max Iterations", 100, min: 1, max: 1000000,
            description: "Maximum loop iterations (use BreakIf node to stop earlier)");
    }

    public void InitializeLoop()
    {
        _currentIndex = -1;
    }

    public bool MoveNext()
    {
        _currentIndex++;
        if (_currentIndex >= MaxIterations) return false;

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
        // Fallback for non-loop context
        SetOutputValue(_indexOutput, 0);
        SetOutputValue(_isRunningOutput, false);
    }
}
