using Biflow.Core.Entities;
using System.Text.Json;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class FunctionStepDeserializeTests
{
    private static readonly FunctionStep step = GetDeserializedStep();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(step.StepParameters);
    }

    private static FunctionStep GetDeserializedStep()
    {
        var json = JsonSerializer.Serialize(CreateStep(), EnvironmentSnapshot.JsonSerializerOptions);
        var step = JsonSerializer.Deserialize<FunctionStep>(json, EnvironmentSnapshot.JsonSerializerOptions);
        ArgumentNullException.ThrowIfNull(step);
        return step;
    }

    private static FunctionStep CreateStep()
    {
        var step = new FunctionStep();
        step.StepParameters.Add(new FunctionStepParameter());
        return step;
    }
}
