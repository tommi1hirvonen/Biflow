using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    public class ExecutionConfiguration
    {
        public IDbContextFactory<EtlManagerContext> DbContextFactory { get; init; }
        public string ConnectionString { get; init; }
        public Guid ExecutionId { get; init; }
        public string Username { get; set; }
        public int MaxParallelSteps { get; init; }
        public int PollingIntervalMs { get; init; }
        public Job Job { get; init; }
        public bool Notify { get; init; }
        public ExecutionConfiguration(IDbContextFactory<EtlManagerContext> dbContextFactory, string connectionString, Guid executionId,
            int maxParallelSteps, int pollingIntervalMs, Job job, bool notify, string username)
        {
            DbContextFactory = dbContextFactory;
            ConnectionString = connectionString;
            ExecutionId = executionId;
            Username = username;
            MaxParallelSteps = maxParallelSteps;
            PollingIntervalMs = pollingIntervalMs;
            Job = job;
            Notify = notify;
        }
    }
}
