CREATE PROCEDURE [etlmanager].[GetNotificationMessageBody]
	@ExecutionId UNIQUEIDENTIFIER
AS

DECLARE
	@JobName NVARCHAR(250),
	@JobStatus VARCHAR(50),
	@JobStartDateTime DATETIME2,
	@JobEndDateTime DATETIME2,
	@JobExecutionInSeconds INT

SELECT
	@JobName = [JobName],
	@JobStatus = [ExecutionStatus],
	@JobStartDateTime = [StartDateTime],
	@JobEndDateTime = [EndDateTime],
	@JobExecutionInSeconds = datediff(second,[StartDateTime],isnull([EndDateTime],getdate()))
FROM [etlmanager].[Execution]
WHERE [ExecutionId] = @ExecutionId

DECLARE @Color VARCHAR(100) = CASE @JobStatus
	WHEN 'SUCCEEDED' THEN '#00b400' -- green
	WHEN 'FAILED' THEN '#dc0000' -- red
	ELSE '#ffc800' -- orange
	END

DECLARE @JobDuration VARCHAR(100) = CONVERT(VARCHAR(100), DATEADD(SECOND, @JobExecutionInSeconds, 0), 108)

DECLARE @Body NVARCHAR(MAX)

SET @Body = CONCAT(N'
<h3>', @JobName, N'</h3>
<table>
<tbody>
<tr>
<td><strong>Status:</strong></td>
<td><span style="color: ', @Color,N';"><strong>', @JobStatus, N'</strong></span></td>
</tr>
<tr>
<td>Start time:</td>
<td>', @JobStartDateTime, N'</td>
</tr>
<tr>
<td>End time:</td>
<td>', @JobEndDateTime, N'</td>
</tr>
<tr>
<td>Duration:</td>
<td>', @JobDuration, N'</td>
</tr>
</tbody>
</table>
<h4>Failed steps</h4>
<table border="1">
<thead>
<tr>
<td><strong>Step name</strong></td>
<td><strong>Start time</strong></td>
<td><strong>End Time</strong></td>
<td><strong>Duration</strong></td>
<td><strong>Status</strong></td>
<td><strong>Error message</strong></td>
</tr>
</thead>
<tbody>'
)


DECLARE
	@StepName NVARCHAR(250),
	@StepStartDateTime DATETIME2,
	@StepEndDateTime DATETIME2,
	@StepExecutionInSeconds INT,
	@StepExecutionStatus VARCHAR(50),
	@StepErrorMessage NVARCHAR(MAX),
	@StepDuration VARCHAR(100)

DECLARE StepCursor CURSOR LOCAL FAST_FORWARD FOR
SELECT
	a.StepName,
	b.StartDateTime,
	b.EndDateTime,
	ExecutionInSeconds = datediff(second,b.[StartDateTime],isnull(b.[EndDateTime],getdate())),
	b.ExecutionStatus,
	b.ErrorMessage
FROM etlmanager.ExecutionStep AS a
	INNER JOIN etlmanager.ExecutionStepAttempt AS b ON a.ExecutionId = b.ExecutionId AND a.StepId = b.StepId
WHERE a.ExecutionId = @ExecutionId AND b.ExecutionStatus <> 'SUCCEEDED'
ORDER BY StartDateTime

OPEN StepCursor

FETCH StepCursor INTO
	@StepName,
	@StepStartDateTime,
	@StepEndDateTime,
	@StepExecutionInSeconds,
	@StepExecutionStatus,
	@StepErrorMessage


WHILE @@FETCH_STATUS = 0
BEGIN

	SET @StepDuration = CONVERT(VARCHAR(100), DATEADD(SECOND, @StepExecutionInSeconds, 0), 108)

	SET @Body += CONCAT(N'<tr>
	<td>', @StepName, N'</td>
	<td>', @StepStartDateTime, N'</td>
	<td>', @StepEndDateTime, N'</td>
	<td>', @StepDuration, N'</td>
	<td>', @StepExecutionStatus, N'</td>
	<td>', @StepErrorMessage, N'</td>
	</tr>'
	)

	FETCH StepCursor INTO
		@StepName,
		@StepStartDateTime,
		@StepEndDateTime,
		@StepExecutionInSeconds,
		@StepExecutionStatus,
		@StepErrorMessage

END

SET @Body += N'</tbody>'

CLOSE StepCursor
DEALLOCATE StepCursor

SELECT @Body