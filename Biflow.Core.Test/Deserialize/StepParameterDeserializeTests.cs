using Biflow.Core.Entities;
using System.Text.Json;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class StepParameterDeserializeTests
{
    private readonly StepParameterBase parameter = GetDeserializedParameter();

    [Fact]
    public void ExpressionParameters_NotEmpty()
    {
        Assert.NotEmpty(parameter.ExpressionParameters);
    }

    private static StepParameterBase GetDeserializedParameter()
    {
        var json = JsonSerializer.Serialize(CreateParameter(), EnvironmentSnapshot.JsonSerializerOptions);
        var parameter = JsonSerializer.Deserialize<SqlStepParameter>(json, EnvironmentSnapshot.JsonSerializerOptions);
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter;
    }

    private static SqlStepParameter CreateParameter()
    {
        var parameter = new SqlStepParameter();
        parameter.AddExpressionParameter(new JobParameter());
        return parameter;
    }
}
