CREATE PROCEDURE [etlmanager].[ExecutionStepCopy]
	@ExecutionId UNIQUEIDENTIFIER,
	@StepId UNIQUEIDENTIFIER,
    @RetryAttemptIndex INT,
    @Status VARCHAR(50) = 'RUNNING'
AS

INSERT INTO etlmanager.ExecutionStepAttempt(
	[ExecutionId],
    [StepId],
    [RetryAttemptIndex],
    [StartDateTime],
    [ExecutionStatus],
    [StepType]
)
SELECT
	[ExecutionId],
    [StepId],
    [RetryAttemptIndex] = @RetryAttemptIndex,
    [StartDateTime] = GETDATE(),
    [ExecutionStatus] = @Status,
    [StepType]
FROM etlmanager.ExecutionStepAttempt
WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = 0