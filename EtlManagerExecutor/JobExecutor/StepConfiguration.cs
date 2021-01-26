namespace EtlManagerExecutor
{
    internal abstract record StepConfiguration(
        string StepId,
        int RetryAttempts,
        int RetryIntervalMinutes,
        int TimeoutMinutes);

    internal record SqlStepConfiguration(
        string StepId,
        int RetryAttempts,
        int RetryIntervalMinutes,
        int TimeoutMinutes,
        string SqlStatement,
        string ConnectionString)
        : StepConfiguration(
            StepId,
            RetryAttempts,
            RetryIntervalMinutes,
            TimeoutMinutes);

    internal record PackageStepConfiguration(
        string StepId,
        int RetryAttempts,
        int RetryIntervalMinutes,
        int TimeoutMinutes,
        string ConnectionString,
        string FolderName,
        string ProjectName,
        string PackageName,
        bool ExecuteIn32BitMode)
        : StepConfiguration(
            StepId,
            RetryAttempts,
            RetryIntervalMinutes,
            TimeoutMinutes);

    internal record PipelineStepConfiguration(
        string StepId,
        int RetryAttempts,
        int RetryIntervalMinutes,
        int TimeoutMinutes,
        string DataFactoryId,
        string PipelineName)
        : StepConfiguration(
            StepId,
            RetryAttempts,
            RetryIntervalMinutes,
            TimeoutMinutes);

    internal record JobStepConfiguration(
        string StepId,
        int RetryAttempts,
        int RetryIntervalMinutes,
        int TimeoutMinutes,
        string JobToExecuteId,
        bool JobExecuteSynchronized)
        : StepConfiguration(
            StepId,
            RetryAttempts,
            RetryIntervalMinutes,
            TimeoutMinutes);
}
