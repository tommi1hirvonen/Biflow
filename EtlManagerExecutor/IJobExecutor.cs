namespace EtlManagerExecutor
{
    interface IJobExecutor
    {
        void Run(string executionId, bool notify);
    }
}