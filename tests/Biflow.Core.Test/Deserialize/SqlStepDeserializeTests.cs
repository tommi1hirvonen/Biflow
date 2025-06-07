using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class SqlStepDeserializeTests
{
    private static readonly SqlStep step = CreateStep();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(step.StepParameters);
    }

    private static SqlStep CreateStep()
    {
        var step = new SqlStep();
        step.StepParameters.Add(new SqlStepParameter());
        return step.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
