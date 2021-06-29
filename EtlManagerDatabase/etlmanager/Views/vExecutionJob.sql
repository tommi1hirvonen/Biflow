
CREATE VIEW [etlmanager].[vExecutionJob] AS

SELECT
	A.ExecutionId,
	A.JobId,
	JobName = ISNULL(B.JobName, A.JobName),
	A.CreatedDateTime,
	CreatedBy = MAX(A.CreatedBy),
	StartDateTime = MIN(A.StartDateTime),
	A.ScheduleId,
	EndDateTime =
		CASE
			WHEN EXISTS (SELECT * FROM etlmanager.Execution AS X WHERE A.ExecutionId = X.ExecutionId AND X.ExecutionStatus IN ('RUNNING')) THEN NULL
			ELSE MAX(A.EndDateTime)
		END,
	
	ExecutionInSeconds =
		CASE
			-- If the job is running, compare StartDateTime to current time
			WHEN EXISTS (SELECT * FROM etlmanager.Execution AS X WHERE A.ExecutionId = X.ExecutionId AND X.ExecutionStatus IN ('RUNNING'))
				THEN DATEDIFF(SECOND, MIN(A.StartDateTime), GETDATE())
			-- Otherwise compare to EndDateTime
			ELSE DATEDIFF(SECOND, MIN(A.StartDateTime), MAX(A.EndDateTime))
		END,
	
	ExecutionInMinutes =
		CASE
			-- If the job is running, compare StartDateTime to current time
			WHEN EXISTS (SELECT * FROM etlmanager.Execution AS X WHERE A.ExecutionId = X.ExecutionId AND X.ExecutionStatus IN ('RUNNING'))
				THEN DATEDIFF(MINUTE, MIN(A.StartDateTime), GETDATE())
			-- Otherwise compare to EndDateTime
			ELSE DATEDIFF(MINUTE, MIN(A.StartDateTime), MAX(A.EndDateTime))
		END,
	
	DependencyMode = CONVERT(BIT, MAX(CONVERT(INT, A.DependencyMode))),
	
	ExecutionStatus =
		CASE
			WHEN EXISTS (SELECT * FROM etlmanager.Execution AS X WHERE A.ExecutionId = X.ExecutionId AND X.ExecutionStatus = 'RUNNING') THEN 'RUNNING'
			WHEN EXISTS (SELECT * FROM etlmanager.Execution AS X WHERE A.ExecutionId = X.ExecutionId AND X.ExecutionStatus = 'FAILED') THEN 'FAILED'
			WHEN EXISTS (SELECT * FROM etlmanager.Execution AS X WHERE A.ExecutionId = X.ExecutionId AND X.ExecutionStatus IN ('AWAIT RETRY','DUPLICATE')) THEN 'WARNING'
			WHEN NOT EXISTS (SELECT * FROM etlmanager.Execution AS X WHERE A.ExecutionId = X.ExecutionId AND X.ExecutionStatus <> 'NOT STARTED') THEN 'NOT STARTED'
			WHEN EXISTS (SELECT * FROM etlmanager.Execution AS X WHERE A.ExecutionId = X.ExecutionId AND X.ExecutionStatus = 'STOPPED') THEN 'STOPPED'
			WHEN EXISTS (SELECT * FROM etlmanager.Execution AS X WHERE A.ExecutionId = X.ExecutionId AND X.ExecutionStatus = 'NOT STARTED') THEN 'SUSPENDED'
			ELSE 'SUCCEEDED'
		END,

	NumberOfSteps = COUNT(DISTINCT A.StepId),
	SuccessPercent = COUNT(CASE WHEN A.ExecutionStatus = 'SUCCEEDED' THEN 1 ELSE NULL END) * 100.0 / COUNT(DISTINCT A.StepId)

FROM etlmanager.Execution AS A
	LEFT JOIN etlmanager.Job AS B ON A.JobId = B.JobId
GROUP BY
	A.ExecutionId,
	A.JobId,
	ISNULL(B.JobName, A.JobName),
	A.CreatedDateTime,
	A.ScheduleId