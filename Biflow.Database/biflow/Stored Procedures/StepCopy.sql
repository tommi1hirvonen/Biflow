CREATE PROCEDURE biflow.StepCopy
	@StepId UNIQUEIDENTIFIER,
	@TargetJobId UNIQUEIDENTIFIER,
	@Username NVARCHAR(250),
	@NameSuffix NVARCHAR(50) = NULL
AS

SET XACT_ABORT ON

BEGIN TRANSACTION

DECLARE @StepIdNew UNIQUEIDENTIFIER = NEWID()


-- Copy step
INSERT INTO biflow.Step (
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
	PipelineClientId,
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
	AgentJobName,
	TabularModelName,
	TabularTableName,
	TabularPartitionName,
	EmailRecipients,
	EmailSubject,
	EmailBody,
	RetryAttempts,
	RetryIntervalMinutes,
	TimeoutMinutes,
	ExecutionConditionExpression,
	CreatedDateTime,
	LastModifiedDateTime,
	IsEnabled,
	CreatedBy
)
SELECT @TargetJobId,
	@StepIdNew,
	CONCAT(StepName, @NameSuffix),
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
	PipelineClientId,
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
	AgentJobName,
	TabularModelName,
	TabularTableName,
	TabularPartitionName,
	EmailRecipients,
	EmailSubject,
	EmailBody,
	RetryAttempts,
	RetryIntervalMinutes,
	TimeoutMinutes,
	ExecutionConditionExpression,
	GETUTCDATE(),
	GETUTCDATE(),
	IsEnabled,
	@Username
FROM biflow.Step
WHERE StepId = @StepId



-- Copy step parameters
INSERT INTO biflow.StepParameter (
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
	@StepIdNew,
	a.ParameterLevel,
	a.ParameterName,
	a.ParameterType,
	a.ParameterValueType,
	a.ParameterValue,
	JobParameterId = CASE WHEN b.JobId = @TargetJobId THEN a.JobParameterId END
FROM biflow.StepParameter AS a
	INNER JOIN biflow.Step AS b ON a.StepId = b.StepId
WHERE a.StepId = @StepId


-- Copy step execution condition parameters
INSERT INTO biflow.StepConditionParameter (
	ParameterId,
	StepId,
	ParameterName,
	ParameterValueType,
	ParameterValue,
	JobParameterId
)
SELECT NEWID(),
	@StepIdNew,
	a.ParameterName,
	a.ParameterValueType,
	a.ParameterValue,
	JobParameterId = CASE WHEN b.JobId = @TargetJobId THEN a.JobParameterId END
FROM biflow.StepConditionParameter AS a
	INNER JOIN biflow.Step AS b ON a.StepId = b.StepId
WHERE a.StepId = @StepId


-- Copy sources and targets
INSERT INTO biflow.StepSource (
	StepId,
	ObjectId
)
SELECT
	@StepIdNew,
	ObjectId
FROM biflow.StepSource
WHERE StepId = @StepId

INSERT INTO biflow.StepTarget (
	StepId,
	ObjectId
)
SELECT
	@StepIdNew,
	ObjectId
FROM biflow.StepTarget
WHERE StepId = @StepId


-- If the source and target job are the same, copy dependencies
IF (SELECT JobId FROM biflow.Step WHERE StepId = @StepId) = @TargetJobId
BEGIN

	INSERT INTO biflow.Dependency (
		StepId,
		DependantOnStepId,
		DependencyType,
		CreatedBy,
		CreatedDateTime
	)
	SELECT @StepIdNew,
		DependantOnStepId,
		DependencyType,
		@Username,
		GETUTCDATE()
	FROM biflow.Dependency
	WHERE StepId = @StepId

END

-- Copy tags
INSERT INTO biflow.StepTag (
	StepId,
	TagId
)
SELECT @StepIdNew,
	A.TagId
FROM biflow.StepTag AS A
WHERE A.StepId = @StepId


COMMIT TRANSACTION

SELECT @StepIdNew