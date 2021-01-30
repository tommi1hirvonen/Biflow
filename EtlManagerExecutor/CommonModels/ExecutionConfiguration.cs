using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    public class ExecutionConfiguration : ConfigurationBase
    {
        public int MaxParallelSteps { get; init; }
        public int PollingIntervalMs { get; init; }
        public string JobId { get; init; }
        public bool Notify { get; init; }
        public ExecutionConfiguration(string connectionString, string executionId, string encryptionKey,
            int maxParallelSteps, int pollingIntervalMs, string jobId, bool notify, string username)
            : base(connectionString, executionId, encryptionKey, username)
        {
            MaxParallelSteps = maxParallelSteps;
            PollingIntervalMs = pollingIntervalMs;
            JobId = jobId;
            Notify = notify;
        }
    }
}
