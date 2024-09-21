using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class StepDeserializeTests
{
    private static readonly Step step = CreateStep();

    [Fact]
    public void Dependencies_NotEmpty()
    {
        Assert.NotEmpty(step.Dependencies);
    }

    [Fact]
    public void DataObjects_NotEmpty()
    {
        Assert.NotEmpty(step.DataObjects);
    }

    [Fact]
    public void Tags_NotEmpty()
    {
        Assert.NotEmpty(step.Tags);
    }

    [Fact]
    public void ExecutionConditionParameters_NotEmpty()
    {
        Assert.NotEmpty(step.ExecutionConditionParameters);
    }

    private static SqlStep CreateStep()
    {
        var step = new SqlStep();
        step.Dependencies.Add(new Dependency());
        step.DataObjects.Add(new StepDataObject());
        step.Tags.Add(new StepTag("Test"));
        step.ExecutionConditionParameters.Add(new ExecutionConditionParameter());
        return step.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
