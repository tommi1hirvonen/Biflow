using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test;

public class ApiKeyTests
{
    [Fact]
    public void GenerateApiKey()
    {
        var key = new ApiKey();
        Assert.True(key.Value is { Length: > 0 and < 100 });
    }
}
