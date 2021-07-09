using EtlManagerDataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    public class ExecutionConfiguration
    {
        public EtlManagerContext DbContext { get; init; }
        public string ConnectionString { get; init; }
        public string ExecutionId { get; init; }
        public string? EncryptionKey { get; init; }
        public string Username { get; set; }
        public int MaxParallelSteps { get; init; }
        public int PollingIntervalMs { get; init; }
        public Job Job { get; init; }
        public bool Notify { get; init; }
        public ExecutionConfiguration(EtlManagerContext dbContext, string connectionString, string executionId, string? encryptionKey,
            int maxParallelSteps, int pollingIntervalMs, Job job, bool notify, string username)
        {
            DbContext = dbContext;
            ConnectionString = connectionString;
            ExecutionId = executionId;
            EncryptionKey = encryptionKey;
            Username = username;
            MaxParallelSteps = maxParallelSteps;
            PollingIntervalMs = pollingIntervalMs;
            Job = job;
            Notify = notify;
        }
    }
}
