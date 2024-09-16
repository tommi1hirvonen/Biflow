using Biflow.Core.Entities;
using System.Text.Json;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class JobStepDeserializeTests
{
    private static readonly JobStep step = GetDeserializedStep();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(step.StepParameters);
    }

    [Fact]
    public void TagFilters_NotEmpty()
    {
        Assert.NotEmpty(step.TagFilters);
    }

    private static JobStep GetDeserializedStep()
    {
        var json = JsonSerializer.Serialize(CreateStep(), EnvironmentSnapshot.JsonSerializerOptions);
        var step = JsonSerializer.Deserialize<JobStep>(json, EnvironmentSnapshot.JsonSerializerOptions);
        ArgumentNullException.ThrowIfNull(step);
        return step;
    }

    private static JobStep CreateStep()
    {
        var step = new JobStep();
        step.StepParameters.Add(new JobStepParameter(Guid.Empty));
        step.TagFilters.Add(new StepTag("Test"));
        return step;
    }
}
