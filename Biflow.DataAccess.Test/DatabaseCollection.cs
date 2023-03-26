using Xunit;

namespace Biflow.DataAccess.Test;

[CollectionDefinition(nameof(DatabaseCollection))]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }