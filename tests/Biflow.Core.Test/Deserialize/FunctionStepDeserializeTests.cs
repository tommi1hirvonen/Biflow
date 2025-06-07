using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class FunctionStepDeserializeTests
{
    private static readonly FunctionStep step = CreateStep();

    [Fact]
    public void Parameters_NotEmpty() => Assert.NotEmpty(step.StepParameters);

    [Fact]
    public void FunctionAppId_NotEmptyGuid() => Assert.NotEqual(step.FunctionAppId, Guid.Empty);

    [Fact]
    public void FunctionKey_Empty() => Assert.Empty(step.FunctionKey ?? "");

    private static FunctionStep CreateStep()
    {
        var step = new FunctionStep
        {
            FunctionAppId = Guid.NewGuid(),
            FunctionKey = "function_key"
        };
        step.StepParameters.Add(new FunctionStepParameter());
        return step.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
