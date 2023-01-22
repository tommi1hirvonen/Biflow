CREATE TABLE [biflow].[JobParameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[JobId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValueType] VARCHAR(20) NOT NULL,
	[ParameterValue] SQL_VARIANT NULL,
    [UseExpression] BIT NOT NULL CONSTRAINT [DF_JobParameter_UseExpression] DEFAULT (0),
    [Expression] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_JobParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_JobParameter] UNIQUE ([JobId],[ParameterName]),
	CONSTRAINT [FK_JobParameter_Job] FOREIGN KEY ([JobId]) REFERENCES [biflow].[Job] ([JobId]),
	CONSTRAINT [CK_JobParameter_ParameterValueType] CHECK (
        [ParameterValueType] = 'Boolean' OR
        [ParameterValueType] = 'DateTime' OR
        [ParameterValueType] = 'Decimal' OR
        [ParameterValueType] = 'Double' OR
        [ParameterValueType] = 'Int16' OR
        [ParameterValueType] = 'Int32' OR
        [ParameterValueType] = 'Int64' OR
        [ParameterValueType] = 'Single' OR
        [ParameterValueType] = 'String'
        )
);

GO

CREATE TRIGGER [biflow].[Trigger_JobParameter] ON [biflow].[JobParameter] INSTEAD OF DELETE AS
BEGIN

    SET NOCOUNT ON
    -- Use instead of trigger to delete linking dependencies because of SQL Server limitation with multiple cascading paths.
    -- https://support.microsoft.com/en-us/help/321843/error-message-1785-occurs-when-you-create-a-foreign-key-constraint-tha

    DELETE FROM [biflow].[StepParameter]
    WHERE EXISTS (
        SELECT *
        FROM [deleted]
        WHERE [StepParameter].[AssignToJobParameterId] = [deleted].[ParameterId]
    )

    UPDATE [biflow].[StepParameter]
    SET [InheritFromJobParameterId] = NULL
    WHERE EXISTS (
        SELECT *
        FROM [deleted]
        WHERE [StepParameter].[InheritFromJobParameterId] = [deleted].[ParameterId]
    )

    DELETE FROM [biflow].[JobParameter]
    WHERE EXISTS (
        SELECT *
        FROM [deleted]
        WHERE [JobParameter].[ParameterId] = [deleted].ParameterId
    )

END