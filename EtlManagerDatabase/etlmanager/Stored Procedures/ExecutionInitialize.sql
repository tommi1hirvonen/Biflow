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
DECLARE @EtlManagerExecutionIdString NVARCHAR(36) = CONVERT(NVARCHAR(36), @EtlManagerExecutionId)

DECLARE @JobIdString NVARCHAR(36) = CONVERT(NVARCHAR(36), @JobId)


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


-- Insert placeholders for steps.
INSERT INTO etlmanager.Execution (
	ExecutionId,
	JobId,
	JobName,
	StepId,
	StepName,
	CreatedDateTime,
	StartDateTime,
	EndDateTime,
	ExecutionStatus,
	RetryAttemptIndex,
	RetryAttempts,
	RetryIntervalMinutes,
	ExecutionPhase,
	DependencyMode,
	StepType,
	SqlStatement,
	ServerName,
	FolderName,
	ProjectName,
	PackageName,
	ExecuteIn32BitMode,
	CreatedBy,
	ScheduleId
)
SELECT
	ExecutionId = @EtlManagerExecutionId,
	JobId = a.JobId,
	JobName = b.JobName,
	StepId = a.StepId,
	StepName = a.StepName,
	CreatedDateTime = GETDATE(),
	StartDateTime = NULL,
	EndDateTime = NULL,
	ExecutionStatus = 'NOT STARTED',
	RetryAttempt = 0,
	RetryAttempts = a.RetryAttempts,
	RetryIntervalMinutes = a.RetryIntervalMinutes,
	ExecutionPhase = a.ExecutionPhase,
	DependencyMode = b.UseDependencyMode,
	StepType = a.StepType,
	SqlStatement = a.SqlStatement,
	ServerName = a.ServerName,
	FolderName = a.FolderName,
	ProjectName = a.ProjectName,
	PackageName = a.PackageName,
	ExecuteIn32BitMode = a.ExecuteIn32BitMode,
	CreatedBy = @Username,
	ScheduleId = @ScheduleId
FROM etlmanager.Step AS a
	JOIN etlmanager.Job AS b ON a.JobId = b.JobId
WHERE a.JobId = @JobId 
	AND (
		EXISTS (SELECT * FROM #StepIds AS x WHERE a.StepId = x.StepId) -- If a list of steps was provided, disregard IsEnabled
		OR NOT EXISTS (SELECT * FROM #StepIds) AND a.IsEnabled = 1 -- If no list was provided, check IsEnabled
	)

SELECT @EtlManagerExecutionId

END;