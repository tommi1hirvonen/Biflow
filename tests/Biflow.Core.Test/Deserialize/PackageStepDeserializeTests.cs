using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class PackageStepDeserializeTests
{
    private static readonly PackageStep step = CreateStep();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(step.StepParameters);
    }

    private static PackageStep CreateStep()
    {
        var step = new PackageStep();
        step.StepParameters.Add(new PackageStepParameter());
        return step.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
