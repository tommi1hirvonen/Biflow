using Biflow.Core.Entities;
using System.Text.Json;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class PipelineStepDeserializeTests
{
    private static readonly PipelineStep step = GetDeserializedStep();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(step.StepParameters);
    }

    private static PipelineStep GetDeserializedStep()
    {
        var json = JsonSerializer.Serialize(CreateStep(), EnvironmentSnapshot.JsonSerializerOptions);
        var step = JsonSerializer.Deserialize<PipelineStep>(json, EnvironmentSnapshot.JsonSerializerOptions);
        ArgumentNullException.ThrowIfNull(step);
        return step;
    }

    private static PipelineStep CreateStep()
    {
        var step = new PipelineStep();
        step.StepParameters.Add(new PipelineStepParameter());
        return step;
    }
}
