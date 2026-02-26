using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Control;

[NodeInfo("If Select", NodeCategories.Control, Description = "Select between two values based on a condition")]
public class IfSelectNode : BaseNode
{
    private InputPort<bool> _conditionInput = null!;
    private InputPort<object> _trueInput = null!;
    private InputPort<object> _falseInput = null!;
    private OutputPort<object> _resultOutput = null!;

    protected override void Setup()
    {
        _conditionInput = AddInput<bool>("Condition");
        _trueInput = AddInput<object>("True Value");
        _falseInput = AddInput<object>("False Value");
        _resultOutput = AddOutput<object>("Result");
    }

    public override void Process()
    {
        try
        {
            var condition = GetInputValue(_conditionInput);
            var trueConnected = _trueInput.IsConnected;
            var falseConnected = _falseInput.IsConnected;

            if (trueConnected && falseConnected)
            {
                // Both connected → select based on Condition
                var result = condition ? GetInputValue(_trueInput) : GetInputValue(_falseInput);
                SetOutputValue(_resultOutput, result);
            }
            else if (trueConnected && !falseConnected)
            {
                // True only → output True Value when condition is true, null (skip) when false
                SetOutputValue(_resultOutput, condition ? GetInputValue(_trueInput) : null);
            }
            else if (!trueConnected && falseConnected)
            {
                // False only → output False Value when condition is false, null (skip) when true
                SetOutputValue(_resultOutput, !condition ? GetInputValue(_falseInput) : null);
            }
            else
            {
                SetOutputValue(_resultOutput, (object?)null);
            }

            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"If Select error: {ex.Message}";
        }
    }
}
