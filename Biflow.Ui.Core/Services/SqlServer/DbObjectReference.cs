using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core;

public record DbObjectReference(string ServerName, string DatabaseName, string SchemaName, string ObjectName, bool IsUnreliable) : IDataObject;