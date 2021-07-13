CREATE PROCEDURE [etlmanager].[ExecutionInitialize]
	@JobId UNIQUEIDENTIFIER,
    @Username NVARCHAR(250) = NULL,
	@StepIds VARCHAR(MAX) = NULL, -- null if all steps should be executed
	@ScheduleId UNIQUEIDENTIFIER = NULL
AS
BEGIN

SET NOCOUNT ON

-- Create an execution id for ETL Manager.
DECLARE @EtlManagerExecutionId UNIQUEIDENTIFIER = NEWID()

-- Split the list of step ids to be executed into a temporary table.
DECLARE @Delimiter CHAR(1) = ',';

WITH Split AS(
	-- Calculate delimiter positions...
    SELECT
		0 AS StartPos,
		CHARINDEX(@Delimiter, @StepIds) AS EndPos
	WHERE @StepIds IS NOT NULL
    UNION ALL
    SELECT
		CONVERT(INT, EndPos + 1),
		CHARINDEX(@Delimiter, @StepIds, EndPos + 1)
    FROM Split
    WHERE EndPos > 0
)
-- ...and use them to split the @StepIds comma delimited list of steps.
SELECT StepId = CONVERT(UNIQUEIDENTIFIER, SUBSTRING(@StepIds, StartPos, ISNULL(NULLIF(EndPos, 0), LEN(@StepIds) + 1) - StartPos))
INTO #StepIds
FROM Split
OPTION (MAXRECURSION 10000) -- max 10000 steps


SELECT
	a.StepId,
	a.StepName,
	a.RetryAttempts,
	a.RetryIntervalMinutes,
	a.TimeoutMinutes,
	a.ExecutionPhase,
	a.StepType,
	a.SqlStatement,
	a.ConnectionId,
	a.PackageFolderName,
	a.PackageProjectName,
	a.PackageName,
	a.ExecuteIn32BitMode,
	a.ExecuteAsLogin,
	a.DataFactoryId,
	a.PipelineName,
	a.JobToExecuteId,
	a.JobExecuteSynchronized,
	a.ExeFileName,
	a.ExeArguments,
	a.ExeWorkingDirectory,
	a.ExeSuccessExitCode,
	a.PowerBIServiceId,
	a.DatasetGroupId,
	a.DatasetId
INTO #Steps
FROM etlmanager.Step AS a
WHERE a.JobId = @JobId 
	AND (
		EXISTS (SELECT * FROM #StepIds AS x WHERE a.StepId = x.StepId) -- If a list of steps was provided, disregard IsEnabled
		OR NOT EXISTS (SELECT * FROM #StepIds) AND a.IsEnabled = 1 -- If no list was provided, check IsEnabled
	)


IF (SELECT COUNT(*) FROM #Steps) = 0
	RETURN



-- Insert job execution
INSERT INTO etlmanager.Execution (
	ExecutionId,
	JobId,
	JobName,
	CreatedDateTime,
	StartDateTime,
	EndDateTime,
	ExecutionStatus,
	DependencyMode,
	CreatedBy,
	ScheduleId
)
SELECT
	ExecutionId = @EtlManagerExecutionId,
	JobId = a.JobId,
	JobName = a.JobName,
	CreatedDateTime = GETDATE(),
	StartDateTime = NULL,
	EndDateTime = NULL,
	ExecutionStatus = 'NOT STARTED',
	DependencyMode = a.UseDependencyMode,
	CreatedBy = @Username,
	ScheduleId = @ScheduleId
FROM etlmanager.Job AS a
WHERE a.JobId = @JobId


-- Insert placeholders for steps.
INSERT INTO etlmanager.ExecutionStep (
	ExecutionId,
	StepId,
	StepName,
	RetryAttempts,
	RetryIntervalMinutes,
	TimeoutMinutes,
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
	DatasetId
)
SELECT
	ExecutionId = @EtlManagerExecutionId,
	StepId = a.StepId,
	StepName = a.StepName,
	RetryAttempts = a.RetryAttempts,
	RetryIntervalMinutes = a.RetryIntervalMinutes,
	TimeoutMinutes = a.TimeoutMinutes,
	ExecutionPhase = a.ExecutionPhase,
	StepType = a.StepType,
	SqlStatement = a.SqlStatement,
	ConnectionId = a.ConnectionId,
	PackageFolderName = a.PackageFolderName,
	PackageProjectName = a.PackageProjectName,
	PackageName = a.PackageName,
	ExecuteIn32BitMode = a.ExecuteIn32BitMode,
	a.ExecuteAsLogin,
	a.DataFactoryId,
	a.PipelineName,
	JobToExecuteId = a.JobToExecuteId,
	JobExecuteSynchronized = a.JobExecuteSynchronized,
	a.ExeFileName,
	a.ExeArguments,
	a.ExeWorkingDirectory,
	a.ExeSuccessExitCode,
	a.PowerBIServiceId,
	a.DatasetGroupId,
	a.DatasetId
FROM #Steps AS a


-- Insert placeholders for steps.
INSERT INTO etlmanager.ExecutionStepAttempt (
	ExecutionId,
	StepId,
	StartDateTime,
	EndDateTime,
	ExecutionStatus,
	RetryAttemptIndex,
	StepType
)
SELECT
	ExecutionId = @EtlManagerExecutionId,
	StepId,
	StartDateTime = NULL,
	EndDateTime = NULL,
	ExecutionStatus = 'NOT STARTED',
	RetryAttempt = 0,
	StepType
FROM #Steps


-- Store and historize package execution parameters.
INSERT INTO etlmanager.ExecutionStepParameter (
	ExecutionId,
	ParameterId,
	StepId,
	ParameterName,
	ParameterValue,
	ParameterLevel,
	ParameterType
)
SELECT
	a.ExecutionId,
	b.ParameterId,
	b.StepId,
	b.ParameterName,
	b.ParameterValue,
	b.ParameterLevel,
	b.ParameterType
FROM etlmanager.ExecutionStep AS a
	JOIN etlmanager.PackageParameter AS b ON b.StepId = a.StepId
WHERE a.ExecutionId = @EtlManagerExecutionId

-- Store and historize pipeline execution parameters.
INSERT INTO etlmanager.ExecutionStepParameter (
	ExecutionId,
	ParameterId,
	StepId,
	ParameterName,
	ParameterValue,
	ParameterLevel,
	ParameterType
)
SELECT
	a.ExecutionId,
	b.ParameterId,
	b.StepId,
	b.ParameterName,
	b.ParameterValue,
	'Pipeline' AS ParameterLevel,
	b.ParameterType
FROM etlmanager.ExecutionStep AS a
	JOIN etlmanager.PipelineParameter AS b ON b.StepId = a.StepId
WHERE a.ExecutionId = @EtlManagerExecutionId


-- Finally return the id of the initialized execution.
SELECT @EtlManagerExecutionId

END;