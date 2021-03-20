CREATE VIEW [etlmanager].[vExecutionParameter] AS

SELECT
    StepExecutionParameterId = CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT(b.ExecutionId, a.[ParameterId])), 2),
    StepExecutionId = CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT(b.[ExecutionId], b.[StepId], b.[RetryAttemptIndex])), 2),
    a.ParameterName,
    a.ParameterValue,
    a.ParameterLevel,
    a.ParameterType
FROM etlmanager.ExecutionParameter AS a
    INNER JOIN etlmanager.Execution AS b ON a.ExecutionId = b.ExecutionId AND a.StepId = b.StepId