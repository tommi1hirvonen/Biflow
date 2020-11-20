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
	CreatedDateTime,
	LastModifiedDateTime,
	UseDependencyMode,
	IsEnabled,
	CreatedBy
)
SELECT @JobIdNew,
	JobName + ' - Copy',
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
	Monday,
	Tuesday,
	Wednesday,
	Thursday,
	Friday,
	Saturday,
	Sunday,
	TimeHours,
	TimeMinutes,
	CreatedBy,
	CreatedDateTime
)
SELECT NEWID(),
	@JobIdNew,
	Monday,
	Tuesday,
	Wednesday,
	Thursday,
	Friday,
	Saturday,
	Sunday,
	TimeHours,
	TimeMinutes,
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
SELECT @JobIdNew,
	B.StepIdNew,
	A.StepName,
	A.ExecutionPhase,
	A.StepType,
	A.SqlStatement,
	A.ConnectionId,
	A.PackageFolderName,
	A.PackageProjectName,
	A.PackageName,
	A.ExecuteIn32BitMode,
	A.DataFactoryId,
	A.PipelineName,
	A.JobToExecuteId,
	A.JobExecuteSynchronized,
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

-- Copy parameters
INSERT INTO etlmanager.Parameter (
	ParameterId,
	StepId,
	ParameterName,
	ParameterValue
)
SELECT NEWID(),
	B.StepIdNew,
	A.ParameterName,
	A.ParameterValue
FROM etlmanager.Parameter AS A
	INNER JOIN #StepIdMapping AS B ON A.StepId = B.StepId

COMMIT TRANSACTION