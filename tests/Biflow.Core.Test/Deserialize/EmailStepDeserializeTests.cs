using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class EmailStepDeserializeTests
{
    private static readonly EmailStep step = CreateStep();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(step.StepParameters);
    }

    private static EmailStep CreateStep()
    {
        var step = new EmailStep();
        step.StepParameters.Add(new EmailStepParameter());
        return step.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
