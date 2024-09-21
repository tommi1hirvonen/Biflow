using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class FunctionStepDeserializeTests
{
    private static readonly FunctionStep step = CreateStep();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(step.StepParameters);
    }

    private static FunctionStep CreateStep()
    {
        var step = new FunctionStep();
        step.StepParameters.Add(new FunctionStepParameter());
        return step.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
