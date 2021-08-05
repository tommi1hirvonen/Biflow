CREATE PROCEDURE etlmanager.JobCopy
	@JobId UNIQUEIDENTIFIER,
	@Username NVARCHAR(250)
AS

SET XACT_ABORT ON

BEGIN TRANSACTION

DECLARE @JobIdNew UNIQUEIDENTIFIER = NEWID()

-- Copy job
INSERT INTO etlmanager.Job (
	JobId,
	JobName,
	JobDescription,
	CreatedDateTime,
	LastModifiedDateTime,
	UseDependencyMode,
	IsEnabled,
	CreatedBy
)
SELECT @JobIdNew,
	JobName + ' - Copy',
	JobDescription,
	GETDATE(),
	GETDATE(),
	UseDependencyMode,
	IsEnabled,
	@Username
FROM etlmanager.Job
WHERE JobId = @JobId

-- Copy schedules
INSERT INTO etlmanager.Schedule (
	ScheduleId,
	JobId,
	CronExpression,
	CreatedBy,
	CreatedDateTime
)
SELECT NEWID(),
	@JobIdNew,
	CronExpression,
	@Username,
	GETDATE()
FROM etlmanager.Schedule
WHERE JobId = @JobId

-- Copy steps

-- First create a mapping table for step id's
SELECT NEWID() AS StepIdNew,
	StepId
INTO #StepIdMapping
FROM etlmanager.Step
WHERE JobId = @JobId

ALTER TABLE #StepIdMapping ADD CONSTRAINT PK_StepIdMapping PRIMARY KEY (StepId)

-- Copy steps
INSERT INTO etlmanager.Step (
	JobId,
	StepId,
	StepName,
	StepDescription,
	ExecutionPhase,
	StepType,
	SqlStatement,
	ConnectionId,
	PackageFolderName,
	PackageProjectName,
	PackageName,
	ExecuteIn32BitMode,
	ExecuteAsLogin,
	DataFactoryId,
	PipelineName,
	JobToExecuteId,
	JobExecuteSynchronized,
	ExeFileName,
	ExeArguments,
	ExeWorkingDirectory,
	ExeSuccessExitCode,
	PowerBIServiceId,
	DatasetGroupId,
	DatasetId,
	FunctionAppId,
	FunctionUrl,
	FunctionInput,
	FunctionIsDurable,
	FunctionKey,
	RetryAttempts,
	RetryIntervalMinutes,
	CreatedDateTime,
	LastModifiedDateTime,
	IsEnabled,
	CreatedBy
)
SELECT @JobIdNew,
	B.StepIdNew,
	A.StepName,
	A.StepDescription,
	A.ExecutionPhase,
	A.StepType,
	A.SqlStatement,
	A.ConnectionId,
	A.PackageFolderName,
	A.PackageProjectName,
	A.PackageName,
	A.ExecuteIn32BitMode,
	A.ExecuteAsLogin,
	A.DataFactoryId,
	A.PipelineName,
	A.JobToExecuteId,
	A.JobExecuteSynchronized,
	A.ExeFileName,
	A.ExeArguments,
	A.ExeWorkingDirectory,
	A.ExeSuccessExitCode,
	A.PowerBIServiceId,
	A.DatasetGroupId,
	A.DatasetId,
	A.FunctionAppId,
	A.FunctionUrl,
	A.FunctionInput,
	A.FunctionIsDurable,
	A.FunctionIsDurable,
	A.RetryAttempts,
	A.RetryIntervalMinutes,
	GETDATE(),
	GETDATE(),
	A.IsEnabled,
	@Username
FROM etlmanager.Step AS A
	INNER JOIN #StepIdMapping AS B ON A.StepId = B.StepId
WHERE A.JobId = @JobId

-- Copy dependencies
INSERT INTO etlmanager.Dependency (
	DependencyId,
	StepId,
	DependantOnStepId,
	StrictDependency,
	CreatedBy,
	CreatedDateTime
)
SELECT NEWID(),
	B.StepIdNew,
	C.StepIdNew,
	A.StrictDependency,
	@Username,
	GETDATE()
FROM etlmanager.Dependency AS A
	INNER JOIN #StepIdMapping AS B ON A.StepId = B.StepId
	INNER JOIN #StepIdMapping AS C ON A.DependantOnStepId = C.StepId

-- Copy package parameters
INSERT INTO etlmanager.PackageParameter (
	ParameterId,
	StepId,
	ParameterLevel,
	ParameterName,
	ParameterType,
	ParameterValue
)
SELECT NEWID(),
	B.StepIdNew,
	A.ParameterLevel,
	A.ParameterName,
	A.ParameterType,
	A.ParameterValue
FROM etlmanager.PackageParameter AS A
	INNER JOIN #StepIdMapping AS B ON A.StepId = B.StepId

-- Copy pipeline parameters
INSERT INTO etlmanager.PipelineParameter (
	ParameterId,
	StepId,
	ParameterName,
	ParameterType,
	ParameterValue
)
SELECT NEWID(),
	B.StepIdNew,
	A.ParameterName,
	A.ParameterType,
	A.ParameterValue
FROM etlmanager.PipelineParameter AS A
	INNER JOIN #StepIdMapping AS B ON A.StepId = B.StepId

-- Copy tags
INSERT INTO etlmanager.StepTag (
	StepsStepId,
	TagsTagId
)
SELECT B.StepIdNew,
	A.TagsTagId
FROM etlmanager.StepTag AS A
	INNER JOIN #StepIdMapping AS B ON a.StepsStepId = B.StepId

COMMIT TRANSACTION

SELECT @JobIdNew