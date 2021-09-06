CREATE PROCEDURE etlmanager.StepCopy
	@StepId UNIQUEIDENTIFIER,
	@TargetJobId UNIQUEIDENTIFIER,
	@Username NVARCHAR(250)
AS

SET XACT_ABORT ON

BEGIN TRANSACTION

DECLARE @StepIdNew UNIQUEIDENTIFIER = NEWID()


-- Copy step
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
SELECT @TargetJobId,
	@StepIdNew,
	StepName + ' - Copy',
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
	GETUTCDATE(),
	GETUTCDATE(),
	IsEnabled,
	@Username
FROM etlmanager.Step
WHERE StepId = @StepId


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
	@StepIdNew,
	ParameterLevel,
	ParameterName,
	ParameterType,
	ParameterValue
FROM etlmanager.PackageParameter
WHERE StepId = @StepId

-- Copy step parameters
INSERT INTO etlmanager.StepParameter (
	ParameterId,
	StepId,
	ParameterName,
	ParameterType,
	ParameterValue
)
SELECT NEWID(),
	@StepIdNew,
	ParameterName,
	ParameterType,
	ParameterValue
FROM etlmanager.StepParameter
WHERE StepId = @StepId


-- If the source and target job are the same, copy dependencies
IF (SELECT JobId FROM etlmanager.Step WHERE StepId = @StepId) = @TargetJobId
BEGIN

	INSERT INTO etlmanager.Dependency (
		StepId,
		DependantOnStepId,
		StrictDependency,
		CreatedBy,
		CreatedDateTime
	)
	SELECT @StepIdNew,
		DependantOnStepId,
		StrictDependency,
		@Username,
		GETUTCDATE()
	FROM etlmanager.Dependency
	WHERE StepId = @StepId

END

-- Copy tags
INSERT INTO etlmanager.StepTag (
	StepId,
	TagId
)
SELECT @StepIdNew,
	A.TagId
FROM etlmanager.StepTag AS A
WHERE A.StepId = @StepId


COMMIT TRANSACTION

SELECT @StepIdNew