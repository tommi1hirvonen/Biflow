using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EtlManagerExecutor;

public class ExecutionConfiguration : IExecutionConfiguration
{
    private readonly IConfiguration _configuration;

    public string ConnectionString => _configuration.GetConnectionString("EtlManagerContext");
    public int MaxParallelSteps => _configuration.GetValue<int>("MaximumParallelSteps");
    public int PollingIntervalMs => _configuration.GetValue<int>("PollingIntervalMs");
    public bool Notify { get; set; } = false;

    public ExecutionConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }
}
