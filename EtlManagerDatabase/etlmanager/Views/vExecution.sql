


CREATE VIEW [etlmanager].[vExecution] AS

SELECT
	[StepExecutionId] = CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT(a.[ExecutionId], a.[StepId], a.[RetryAttemptIndex])), 2)
	,a.[ExecutionId]
	,a.[JobId]
	,JobName = ISNULL(b.JobName, a.[JobName])
	,a.[StepId]
	,StepName = ISNULL(c.StepName, a.[StepName])
	,a.[CreatedDateTime]
	,a.[CreatedBy]
	,a.[StartDateTime]
	,a.[EndDateTime]
	,a.[RetryAttemptIndex]
	,a.[RetryAttempts]
	,a.[RetryIntervalMinutes]
	,a.[ExecutionStatus]
	,a.[ExecutionInSeconds]
	,a.[ExecutionInMinutes]
	,a.[ExecutionPhase]
	,a.[DependencyMode]
	,a.[StepType]
	,a.[SqlStatement]
	,a.[ConnectionId]
	,[PackagePath] = NULLIF(CONCAT(a.[PackageFolderName], '\', a.[PackageProjectName], '\', a.[PackageName]), '\\')
	,a.[PackageFolderName]
	,a.[PackageProjectName]
	,a.[PackageName]
	,a.[ExecuteIn32BitMode]
	,a.[ExecuteAsLogin]
	,a.[ErrorMessage]
	,a.[InfoMessage]
	,a.[PackageOperationId]
	,a.[JobToExecuteId]
	,a.[JobExecuteSynchronized]
	,a.[PipelineName]
	,a.[DataFactoryId]
	,a.[PipelineRunId]
	,a.[ScheduleId]
	,a.[ExeFileName]
	,a.[ExeArguments]
	,a.[ExeWorkingDirectory]
	,a.[ExeSuccessExitCode]
	,a.[StoppedBy]
	,a.[ExecutorProcessId]
FROM [etlmanager].[Execution] AS a
	LEFT JOIN etlmanager.Job AS b ON a.JobId = b.JobId
	LEFT JOIN etlmanager.Step AS c ON a.StepId = c.StepId