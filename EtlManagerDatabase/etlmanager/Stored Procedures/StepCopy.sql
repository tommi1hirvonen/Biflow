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
	DataFactoryId,
	PipelineName,
	JobToExecuteId,
	JobExecuteSynchronized,
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
	DataFactoryId,
	PipelineName,
	JobToExecuteId,
	JobExecuteSynchronized,
	RetryAttempts,
	RetryIntervalMinutes,
	GETDATE(),
	GETDATE(),
	IsEnabled,
	@Username
FROM etlmanager.Step
WHERE StepId = @StepId


-- Copy parameters
INSERT INTO etlmanager.Parameter (
	ParameterId,
	StepId,
	ParameterName,
	ParameterValue
)
SELECT NEWID(),
	@StepIdNew,
	ParameterName,
	ParameterValue
FROM etlmanager.Parameter
WHERE StepId = @StepId


-- If the source and target job are the same, copy dependencies
IF (SELECT JobId FROM etlmanager.Step WHERE StepId = @StepId) = @TargetJobId
BEGIN

	INSERT INTO etlmanager.Dependency (
		DependencyId,
		StepId,
		DependantOnStepId,
		StrictDependency,
		CreatedBy,
		CreatedDateTime
	)
	SELECT NEWID(),
		@StepIdNew,
		DependantOnStepId,
		StrictDependency,
		@Username,
		GETDATE()
	FROM etlmanager.Dependency
	WHERE StepId = @StepId

END

-- Copy tags
INSERT INTO etlmanager.StepTag (
	StepsStepId,
	TagsTagId
)
SELECT @StepIdNew,
	A.TagsTagId
FROM etlmanager.StepTag AS A
WHERE A.StepsStepId = @StepId


COMMIT TRANSACTION

SELECT @StepIdNew