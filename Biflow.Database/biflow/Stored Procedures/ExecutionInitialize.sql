CREATE PROCEDURE [biflow].[ExecutionInitialize]
	@JobId UNIQUEIDENTIFIER,
    @Username NVARCHAR(250) = NULL,
	@StepIds VARCHAR(MAX) = NULL, -- null if all steps should be executed
	@ScheduleId UNIQUEIDENTIFIER = NULL,
	@Notify BIT = 0,
	@NotifyCaller VARCHAR(20) = NULL,
	@NotifyCallerOvertime BIT = 0
AS
BEGIN

SET NOCOUNT ON

-- Create an execution id.
DECLARE @BiflowExecutionId UNIQUEIDENTIFIER = NEWID()

-- Create a temporary table to hold a list of step ids to include in this execution.
CREATE TABLE #StepIds (
	StepId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY
)

-- If an explicit list of step ids was given as a parameter.
IF @StepIds IS NOT NULL
BEGIN

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
	INSERT INTO #StepIds (StepId)
	-- ...and use them to split the @StepIds comma delimited list of steps.
	SELECT StepId = CONVERT(UNIQUEIDENTIFIER, SUBSTRING(@StepIds, StartPos, ISNULL(NULLIF(EndPos, 0), LEN(@StepIds) + 1) - StartPos))
	FROM Split
	OPTION (MAXRECURSION 10000) -- max 10000 steps

END
-- The execution should be based on a specific schedule definition.
ELSE IF @ScheduleId IS NOT NULl
BEGIN

	INSERT INTO #StepIds (StepId)
	SELECT step.StepId
	FROM biflow.Step AS step
		INNER JOIN biflow.Schedule AS sched ON step.JobId = sched.JobId AND sched.ScheduleId = @ScheduleId
	WHERE step.IsEnabled = 1 AND ( -- For schedules, always check IsEnabled.
		NOT EXISTS ( -- Schedule has no tags
			SELECT *
			FROM biflow.ScheduleTag AS schedtag
			WHERE sched.ScheduleId = schedtag.ScheduleId
		) OR EXISTS ( -- Schedule has tags and the step has at least one matching tag.
			SELECT *
			FROM biflow.ScheduleTag AS schedtag
				INNER JOIN biflow.StepTag AS steptag ON schedtag.TagId = steptag.TagId
			WHERE sched.ScheduleId = schedtag.ScheduleId AND step.StepId = steptag.StepId
		)
	)

END


SELECT
	a.StepId,
	a.StepName,
	a.RetryAttempts,
	a.RetryIntervalMinutes,
	a.TimeoutMinutes,
	a.ExecutionConditionExpression,
	a.ExecutionPhase,
	a.StepType,
	a.SqlStatement,
	a.ResultCaptureJobParameterId,
	a.ConnectionId,
	a.PackageFolderName,
	a.PackageProjectName,
	a.PackageName,
	a.ExecuteIn32BitMode,
	a.ExecuteAsLogin,
	a.PipelineClientId,
	a.PipelineName,
	a.JobToExecuteId,
	a.JobExecuteSynchronized,
	a.ExeFileName,
	a.ExeArguments,
	a.ExeWorkingDirectory,
	a.ExeSuccessExitCode,
	a.AppRegistrationId,
	a.DatasetGroupId,
	a.DatasetId,
	a.FunctionAppId,
	a.FunctionUrl,
	a.FunctionInput,
	a.FunctionIsDurable,
	a.AgentJobName,
	a.TabularModelName,
	a.TabularTableName,
	a.TabularPartitionName,
	a.EmailRecipients,
	a.EmailSubject,
	a.EmailBody
INTO #Steps
FROM biflow.Step AS a
WHERE a.JobId = @JobId 
	AND (
		EXISTS (SELECT * FROM #StepIds AS x WHERE a.StepId = x.StepId) -- If a list of steps was provided, disregard IsEnabled
		OR NOT EXISTS (SELECT * FROM #StepIds) AND a.IsEnabled = 1 -- If no list was provided, check IsEnabled
	)


IF (SELECT COUNT(*) FROM #Steps) = 0
	RETURN



-- Insert job execution
INSERT INTO biflow.Execution (
	ExecutionId,
	JobId,
	JobName,
	CreatedDateTime,
	StartDateTime,
	EndDateTime,
	ExecutionStatus,
	DependencyMode,
	StopOnFirstError,
	MaxParallelSteps,
	OvertimeNotificationLimitMinutes,
	CreatedBy,
	ScheduleId,
	Notify,
	NotifyCaller,
	NotifyCallerOvertime
)
SELECT
	ExecutionId = @BiflowExecutionId,
	JobId = a.JobId,
	JobName = a.JobName,
	CreatedDateTime = GETUTCDATE(),
	StartDateTime = NULL,
	EndDateTime = NULL,
	ExecutionStatus = 'NotStarted',
	DependencyMode = a.UseDependencyMode,
	StopOnFirstError = a.StopOnFirstError,
	MaxParallelSteps = a.MaxParallelSteps,
	OvertimeNotificationLimitMinutes = a.OvertimeNotificationLimitMinutes,
	CreatedBy = @Username,
	ScheduleId = @ScheduleId,
	Notify = @Notify,
	NotifyCaller = @NotifyCaller,
	NotifyCallerOvertime = @NotifyCallerOvertime
FROM biflow.Job AS a
WHERE a.JobId = @JobId


-- Insert job execution parameters
INSERT INTO biflow.ExecutionParameter (
	ExecutionId,
	ParameterId,
	ParameterName,
	ParameterValueType,
	ParameterValue
)
SELECT
	@BiflowExecutionId,
	ParameterId,
	ParameterName,
	ParameterValueType,
	ParameterValue
FROM biflow.JobParameter
WHERE JobId = @JobId


-- Insert job concurrency definitions for different step types.
INSERT INTO biflow.ExecutionConcurrency (
	ExecutionId,
	StepType,
	MaxParallelSteps
)
SELECT
	@BiflowExecutionId,
	StepType,
	MaxParallelSteps
FROM biflow.JobConcurrency
WHERE JobId = @JobId


-- Insert placeholders for steps.
INSERT INTO biflow.ExecutionStep (
	ExecutionId,
	StepId,
	StepName,
	RetryAttempts,
	RetryIntervalMinutes,
	TimeoutMinutes,
	ExecutionConditionExpression,
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
	AgentJobName,
	TabularModelName,
	TabularTableName,
	TabularPartitionName,
	EmailRecipients,
	EmailSubject,
	EmailBody
)
SELECT
	ExecutionId = @BiflowExecutionId,
	a.StepId,
	a.StepName,
	a.RetryAttempts,
	a.RetryIntervalMinutes,
	a.TimeoutMinutes,
	a.ExecutionConditionExpression,
	a.ExecutionPhase,
	a.StepType,
	a.SqlStatement,
	a.ResultCaptureJobParameterId,
	a.ConnectionId,
	a.PackageFolderName,
	a.PackageProjectName,
	a.PackageName,
	a.ExecuteIn32BitMode,
	a.ExecuteAsLogin,
	a.PipelineClientId,
	a.PipelineName,
	a.JobToExecuteId,
	a.JobExecuteSynchronized,
	a.ExeFileName,
	a.ExeArguments,
	a.ExeWorkingDirectory,
	a.ExeSuccessExitCode,
	a.AppRegistrationId,
	a.DatasetGroupId,
	a.DatasetId,
	a.FunctionAppId,
	a.FunctionUrl,
	a.FunctionInput,
	a.FunctionIsDurable,
	a.AgentJobName,
	a.TabularModelName,
	a.TabularTableName,
	a.TabularPartitionName,
	a.EmailRecipients,
	a.EmailSubject,
	a.EmailBody
FROM #Steps AS a


-- Insert placeholders for steps.
INSERT INTO biflow.ExecutionStepAttempt (
	ExecutionId,
	StepId,
	StartDateTime,
	EndDateTime,
	ExecutionStatus,
	RetryAttemptIndex,
	StepType
)
SELECT
	ExecutionId = @BiflowExecutionId,
	StepId,
	StartDateTime = NULL,
	EndDateTime = NULL,
	ExecutionStatus = 'NotStarted',
	RetryAttempt = 0,
	StepType
FROM #Steps


-- Store and historize step execution parameters.
INSERT INTO biflow.ExecutionStepParameter (
	ExecutionId,
	ParameterId,
	StepId,
	ParameterType,
	ParameterLevel,
	ParameterName,
	ParameterValue,
	ParameterValueType,
	InheritFromExecutionParameterId
)
SELECT
	a.ExecutionId,
	b.ParameterId,
	b.StepId,
	b.ParameterType,
	b.ParameterLevel,
	b.ParameterName,
	b.ParameterValue,
	b.ParameterValueType,
	b.InheritFromJobParameterId
FROM biflow.ExecutionStep AS a
	JOIN biflow.StepParameter AS b ON b.StepId = a.StepId
WHERE a.ExecutionId = @BiflowExecutionId

-- Store and historize execution condition parameters
INSERT INTO biflow.ExecutionStepConditionParameter (
	ExecutionId,
	ParameterId,
	StepId,
	ParameterName,
	ParameterValue,
	ParameterValueType,
	ExecutionParameterId
)
SELECT
	a.ExecutionId,
	b.ParameterId,
	b.StepId,
	b.ParameterName,
	b.ParameterValue,
	b.ParameterValueType,
	b.JobParameterId
FROM biflow.ExecutionStep AS a
	JOIN biflow.StepConditionParameter AS b ON b.StepId = a.StepId
WHERE a.ExecutionId = @BiflowExecutionId


-- Store and historize dependencies
INSERT INTO biflow.ExecutionDependency (
	ExecutionId,
	StepId,
	DependantOnStepId,
	DependencyType
)
SELECT @BiflowExecutionId,
	a.StepId,
	a.DependantOnStepId,
	a.DependencyType
FROM biflow.Dependency AS a
	INNER JOIN #Steps AS b ON a.StepId = b.StepId
	INNER JOIN #Steps AS c ON a.DependantOnStepId = c.StepId


-- Store and historize sources and targets
INSERT INTO biflow.ExecutionDataObject (
	ExecutionId,
	ObjectId,
	ServerName,
	DatabaseName,
	SchemaName,
	ObjectName,
	MaxConcurrentWrites
)
SELECT
	@BiflowExecutionId,
	ObjectId,
	ServerName,
	DatabaseName,
	SchemaName,
	ObjectName,
	MaxConcurrentWrites
FROM biflow.DataObject AS a
WHERE EXISTS (
	SELECT *
	FROM #Steps AS x
		INNER JOIN biflow.StepSource AS y ON x.StepId = y.StepId
	WHERE a.ObjectId = y.ObjectId
) OR EXISTS (
	SELECT *
	FROM #Steps AS x
		INNER JOIN biflow.StepTarget AS y ON x.StepId = y.StepId
	WHERE a.ObjectId = y.ObjectId
)

INSERT INTO biflow.ExecutionStepSource (
	ExecutionId,
	StepId,
	ObjectId
)
SELECT
	@BiflowExecutionId,
	a.StepId,
	b.ObjectId
FROM #Steps AS a
	INNER JOIN biflow.StepSource AS b ON a.StepId = b.StepId

INSERT INTO biflow.ExecutionStepTarget (
	ExecutionId,
	StepId,
	ObjectId
)
SELECT
	@BiflowExecutionId,
	a.StepId,
	b.ObjectId
FROM #Steps AS a
	INNER JOIN biflow.StepTarget AS b ON a.StepId = b.StepId



-- Finally return the id of the initialized execution.
SELECT @BiflowExecutionId

END;