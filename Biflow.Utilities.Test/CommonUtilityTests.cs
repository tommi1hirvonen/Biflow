using Xunit;

namespace Biflow.Utilities.Test;

public class CommonUtilityTests
{
    [Theory]
    [InlineData(18000.5, "5 h 0 min 0 s")]
    [InlineData(172800.5, "2 d 0 h 0 min 0 s")]
    [InlineData(96001, "1 d 2 h 40 min 1 s")]
    [InlineData(65.5, "1 min 5 s")]
    [InlineData(45, "45 s")]
    [InlineData(0.5, "0 s")]
    public void SecondsToReadableFormatTest(double seconds, string expectedOutput)
    {
        string result = seconds.SecondsToReadableFormat();
        Assert.Equal(expectedOutput, result);
    }
}