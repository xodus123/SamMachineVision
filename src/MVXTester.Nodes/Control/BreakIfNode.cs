using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Control;

/// <summary>
/// Signals early termination of a loop when the condition is true.
/// Place inside a loop body (between a loop node and Collect).
///
/// Example: While(Max=1000) → Process → BreakIf(error &lt; threshold)
///   The loop stops as soon as the condition becomes true.
/// </summary>
[NodeInfo("BreakIf", NodeCategories.Control, Description = "Break out of a loop when condition is true")]
public class BreakIfNode : BaseNode, IBreakSignal
{
    private InputPort<bool> _conditionInput = null!;
    private InputPort<object> _passThrough = null!;
    private OutputPort<object> _output = null!;

    public bool ShouldBreak { get; private set; }

    protected override void Setup()
    {
        _conditionInput = AddInput<bool>("Condition");
        _passThrough = AddInput<object>("Value");
        _output = AddOutput<object>("Value");
    }

    public void ResetBreak()
    {
        ShouldBreak = false;
    }

    public override void Process()
    {
        try
        {
            var condition = GetInputValue(_conditionInput);
            ShouldBreak = condition;

            // Pass through value regardless of break signal
            // (the loop executor checks ShouldBreak after body execution)
            var value = _passThrough.GetValue();
            SetOutputValue(_output, value);

            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"BreakIf error: {ex.Message}";
        }
    }
}
