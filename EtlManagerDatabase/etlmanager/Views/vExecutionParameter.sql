CREATE VIEW [etlmanager].[vExecutionParameter] AS

SELECT
    StepExecutionParameterId = CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT(b.ExecutionId, a.[ParameterId])), 2),
    b.StepExecutionId,
    a.ParameterName,
    a.ParameterValue
FROM etlmanager.ExecutionParameter AS a
    INNER JOIN etlmanager.vExecution AS b ON a.ExecutionId = b.ExecutionId AND a.StepId = b.StepId