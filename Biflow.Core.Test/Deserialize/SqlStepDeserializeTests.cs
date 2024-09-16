using Biflow.Core.Entities;
using System.Text.Json;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class SqlStepDeserializeTests
{
    private static readonly SqlStep step = GetDeserializedStep();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(step.StepParameters);
    }

    private static SqlStep GetDeserializedStep()
    {
        var json = JsonSerializer.Serialize(CreateStep(), EnvironmentSnapshot.JsonSerializerOptions);
        var step = JsonSerializer.Deserialize<SqlStep>(json, EnvironmentSnapshot.JsonSerializerOptions);
        ArgumentNullException.ThrowIfNull(step);
        return step;
    }

    private static SqlStep CreateStep()
    {
        var step = new SqlStep();
        step.StepParameters.Add(new SqlStepParameter());
        return step;
    }
}
