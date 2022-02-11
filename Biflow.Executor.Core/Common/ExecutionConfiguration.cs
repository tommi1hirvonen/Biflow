using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Biflow.Executor.Core.Common;

internal class ExecutionConfiguration : IExecutionConfiguration
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationSection? _baseSection;

    public string ConnectionString => _configuration.GetConnectionString("BiflowContext");
    public int MaxParallelSteps => (_baseSection ?? _configuration).GetValue<int>("MaximumParallelSteps");
    public int PollingIntervalMs => (_baseSection ?? _configuration).GetValue<int>("PollingIntervalMs");

    public ExecutionConfiguration(IConfiguration configuration, IConfigurationSection? baseSection = null)
    {
        _configuration = configuration;
        _baseSection = baseSection;
    }
}
