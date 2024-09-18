using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test;

public class ApiKeyTests
{
    [Fact]
    public void KeyLengthBetween0And100()
    {
        var key = new ApiKey();
        Assert.True(key.Value is { Length: > 0 and < 100 });
    }
}
