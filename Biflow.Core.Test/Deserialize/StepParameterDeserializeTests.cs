using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class StepParameterDeserializeTests
{
    private readonly StepParameterBase _parameter = CreateParameter();

    [Fact]
    public void ExpressionParameters_NotEmpty()
    {
        Assert.NotEmpty(_parameter.ExpressionParameters);
    }

    private static SqlStepParameter CreateParameter()
    {
        var parameter = new SqlStepParameter();
        parameter.AddExpressionParameter(new JobParameter());
        return parameter.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
