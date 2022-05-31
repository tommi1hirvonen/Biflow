CREATE TABLE [biflow].[ExecutionParameter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
	[ParameterValueType] VARCHAR(20) NOT NULL, 
    CONSTRAINT [PK_ExecutionParameter] PRIMARY KEY CLUSTERED ([ExecutionId], [ParameterId]),
	CONSTRAINT [FK_ExecutionParameter_Execution] FOREIGN KEY ([ExecutionId]) REFERENCES [biflow].[Execution] ([ExecutionId]),
	CONSTRAINT [CK_ExecutionParameter_ParameterValueType] CHECK (
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
)

GO

CREATE TRIGGER [biflow].[Trigger_ExecutionParameter]
    ON [biflow].[ExecutionParameter]
    INSTEAD OF DELETE
    AS
    BEGIN
        SET NOCOUNT ON

        UPDATE biflow.ExecutionStep
        SET ResultCaptureJobParameterId = NULL
        WHERE EXISTS (
            SELECT *
            FROM deleted
            WHERE ExecutionStep.ExecutionId = deleted.ExecutionId AND ExecutionStep.ResultCaptureJobParameterId = deleted.ParameterId
        )

        UPDATE biflow.ExecutionStepParameter
        SET ExecutionParameterId = NULL
        WHERE EXISTS (
            SELECT *
            FROM deleted
            WHERE ExecutionStepParameter.ExecutionId = deleted.ExecutionId AND ExecutionStepParameter.ExecutionParameterId = deleted.ParameterId
        )

        UPDATE biflow.ExecutionStepConditionParameter
        SET ExecutionParameterId = NULL
        WHERE EXISTS (
            SELECT *
            FROM deleted
            WHERE ExecutionStepConditionParameter.ExecutionId = deleted.ExecutionId
                AND ExecutionStepConditionParameter.ExecutionParameterId = deleted.ParameterId
        )

        DELETE FROM biflow.ExecutionParameter
        WHERE EXISTS (
            SELECT *
            FROM deleted
            WHERE ExecutionParameter.ExecutionId = deleted.ExecutionId AND ExecutionParameter.ParameterId = deleted.ParameterId
        )

    END