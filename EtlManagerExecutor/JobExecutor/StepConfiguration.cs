namespace EtlManagerExecutor
{
    internal abstract record StepConfiguration(
        string StepId,
        int RetryAttempts,
        int RetryIntervalMinutes)
    { }

    internal record SqlStepConfiguration(
        string StepId,
        int RetryAttempts,
        int RetryIntervalMinutes,
        string SqlStatement,
        string ConnectionString)
        : StepConfiguration(
            StepId,
            RetryAttempts,
            RetryIntervalMinutes)
    { }

    internal record PackageStepConfiguration(
        string StepId,
        int RetryAttempts,
        int RetryIntervalMinutes,
        string ConnectionString,
        string FolderName,
        string ProjectName,
        string PackageName,
        bool ExecuteIn32BitMode)
        : StepConfiguration(
            StepId,
            RetryAttempts,
            RetryIntervalMinutes)
    { }

    internal record PipelineStepConfiguration(
        string StepId,
        int RetryAttempts,
        int RetryIntervalMinutes,
        string DataFactoryId,
        string PipelineName)
        : StepConfiguration(
            StepId,
            RetryAttempts,
            RetryIntervalMinutes)
    { }

    internal record JobStepConfiguration(
        string StepId,
        int RetryAttempts,
        int RetryIntervalMinutes,
        string JobToExecuteId,
        bool JobExecuteSynchronized)
        : StepConfiguration(
            StepId,
            RetryAttempts,
            RetryIntervalMinutes)
    { }

}
