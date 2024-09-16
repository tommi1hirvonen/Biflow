using Biflow.Core.Entities;
using System.Text.Json;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class EmailStepDeserializeTests
{
    private static readonly EmailStep step = GetDeserializedStep();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(step.StepParameters);
    }

    private static EmailStep GetDeserializedStep()
    {
        var json = JsonSerializer.Serialize(CreateStep(), EnvironmentSnapshot.JsonSerializerOptions);
        var step = JsonSerializer.Deserialize<EmailStep>(json, EnvironmentSnapshot.JsonSerializerOptions);
        ArgumentNullException.ThrowIfNull(step);
        return step;
    }

    private static EmailStep CreateStep()
    {
        var step = new EmailStep();
        step.StepParameters.Add(new EmailStepParameter());
        return step;
    }
}
