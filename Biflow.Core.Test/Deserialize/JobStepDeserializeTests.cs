﻿using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class JobStepDeserializeTests
{
    private static readonly JobStep step = CreateStep();

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

    private static JobStep CreateStep()
    {
        var step = new JobStep();
        step.StepParameters.Add(new JobStepParameter(Guid.Empty));
        step.TagFilters.Add(new StepTag("Test"));
        return step.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
