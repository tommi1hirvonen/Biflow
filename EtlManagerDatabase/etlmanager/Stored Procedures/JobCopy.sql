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
	StopOnFirstError,
	OvertimeNotificationLimitMinutes,
	MaxParallelSteps,
	IsEnabled,
	CreatedBy
)
SELECT @JobIdNew,
	JobName + ' - Copy',
	JobDescription,
	GETUTCDATE(),
	GETUTCDATE(),
	UseDependencyMode,
	StopOnFirstError,
	OvertimeNotificationLimitMinutes,
	MaxParallelSteps,
	IsEnabled,
	@Username
FROM etlmanager.Job
WHERE JobId = @JobId

-- Copy job parameters

-- First create mapping table
SELECT NEWID() AS ParameterIdNew,
	ParameterId
INTO #ParameterIdMapping
FROM etlmanager.JobParameter
WHERE JobId = @JobId

ALTER TABLE #ParameterIdMapping ADD CONSTRAINT PK_ParameterIdMapping PRIMARY KEY (ParameterId)

INSERT INTO etlmanager.JobParameter (
	ParameterId,
	JobId,
	ParameterName,
	ParameterValueType,
	ParameterValue
)
SELECT B.ParameterIdNew,
	@JobIdNew,
	A.ParameterName,
	A.ParameterValueType,
	A.ParameterValue
FROM etlmanager.JobParameter AS A
	INNER JOIN #ParameterIdMapping AS B ON A.ParameterId = B.ParameterId
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
	ResultCaptureJobParameterId,
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
	AppRegistrationId,
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
	C.ParameterIdNew,
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
	A.AppRegistrationId,
	A.DatasetGroupId,
	A.DatasetId,
	A.FunctionAppId,
	A.FunctionUrl,
	A.FunctionInput,
	A.FunctionIsDurable,
	A.FunctionIsDurable,
	A.RetryAttempts,
	A.RetryIntervalMinutes,
	GETUTCDATE(),
	GETUTCDATE(),
	A.IsEnabled,
	@Username
FROM etlmanager.Step AS A
	INNER JOIN #StepIdMapping AS B ON A.StepId = B.StepId
	LEFT JOIN #ParameterIdMapping AS C ON A.ResultCaptureJobParameterId = C.ParameterId
WHERE A.JobId = @JobId

-- Copy dependencies
INSERT INTO etlmanager.Dependency (
	StepId,
	DependantOnStepId,
	StrictDependency,
	CreatedBy,
	CreatedDateTime
)
SELECT B.StepIdNew,
	C.StepIdNew,
	A.StrictDependency,
	@Username,
	GETUTCDATE()
FROM etlmanager.Dependency AS A
	INNER JOIN #StepIdMapping AS B ON A.StepId = B.StepId
	INNER JOIN #StepIdMapping AS C ON A.DependantOnStepId = C.StepId


-- Copy step parameters
INSERT INTO etlmanager.StepParameter (
	ParameterId,
	StepId,
	ParameterLevel,
	ParameterName,
	ParameterType,
	ParameterValueType,
	ParameterValue,
	JobParameterId
)
SELECT NEWID(),
	B.StepIdNew,
	A.ParameterLevel,
	A.ParameterName,
	A.ParameterType,
	A.ParameterValueType,
	A.ParameterValue,
	C.ParameterIdNew
FROM etlmanager.StepParameter AS A
	INNER JOIN #StepIdMapping AS B ON A.StepId = B.StepId
	LEFT JOIN #ParameterIdMapping AS C ON A.JobParameterId = C.ParameterId

-- Copy tags
INSERT INTO etlmanager.StepTag (
	StepId,
	TagId
)
SELECT B.StepIdNew,
	A.TagId
FROM etlmanager.StepTag AS A
	INNER JOIN #StepIdMapping AS B ON a.StepId = B.StepId

COMMIT TRANSACTION

SELECT @JobIdNew