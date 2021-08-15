namespace EtlManagerExecutor
{
    public interface IExecutionConfiguration
    {
        public string ConnectionString { get; }

        public int MaxParallelSteps { get; }

        public int PollingIntervalMs { get; }

        public bool Notify { get; set; }
    }
}
