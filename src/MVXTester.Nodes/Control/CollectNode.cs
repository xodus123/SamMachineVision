using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Control;

/// <summary>
/// Collects values from loop iterations into an array.
/// Place at the end of a loop body as the boundary.
/// Nodes downstream of Collect execute AFTER the loop completes.
///
/// Example: For(0..10) → Process → Collect → Display
///   - Process executes 10 times
///   - Collect gathers each result
///   - Display executes once with the collected array
/// </summary>
[NodeInfo("Collect", NodeCategories.Control, Description = "Collect loop iteration results into an array")]
public class CollectNode : BaseNode, ILoopCollector
{
    private InputPort<object> _valueInput = null!;
    private OutputPort<object[]> _resultOutput = null!;
    private OutputPort<int> _countOutput = null!;

    private readonly List<object> _collected = new();

    protected override void Setup()
    {
        _valueInput = AddInput<object>("Value");
        _resultOutput = AddOutput<object[]>("Result");
        _countOutput = AddOutput<int>("Count");
    }

    public void ClearCollection()
    {
        _collected.Clear();
    }

    public void CollectIteration()
    {
        var value = _valueInput.GetValue();
        if (value != null)
            _collected.Add(value);
    }

    public void FinalizeCollection()
    {
        SetOutputValue(_resultOutput, _collected.ToArray());
        SetOutputValue(_countOutput, _collected.Count);
        Error = null;
    }

    public override void Process()
    {
        // Non-loop context: pass single value through as 1-element array
        var value = _valueInput.GetValue();
        if (value != null)
        {
            SetOutputValue(_resultOutput, new[] { value });
            SetOutputValue(_countOutput, 1);
        }
        else
        {
            SetOutputValue(_resultOutput, Array.Empty<object>());
            SetOutputValue(_countOutput, 0);
        }
        Error = null;
    }
}
