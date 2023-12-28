namespace Biflow.Ui.SqlServer;

public record SqlReference(
    string ReferencingSchema,
    string ReferencingName,
    string ReferencingType,
    string? ReferencedDatabase,
    string ReferencedSchema,
    string ReferencedName,
    string ReferencedType);