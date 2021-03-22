CREATE FUNCTION [etlmanager].[GetExecutePackagesAsLogin]
(
	@ConnectionId UNIQUEIDENTIFIER
)
RETURNS NVARCHAR(128)
AS
BEGIN
	RETURN (
		SELECT ExecutePackagesAsLogin
		FROM etlmanager.Connection
		WHERE ConnectionId = @ConnectionId
	)
END
