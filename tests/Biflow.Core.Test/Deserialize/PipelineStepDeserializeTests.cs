using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class PipelineStepDeserializeTests
{
    private static readonly PipelineStep step = CreateStep();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(step.StepParameters);
    }

    private static PipelineStep CreateStep()
    {
        var step = new PipelineStep();
        step.StepParameters.Add(new PipelineStepParameter());
        return step.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
