using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class SqlStepTests(SqlStepFixture fixture) : IClassFixture<SqlStepFixture>
{
    private SqlStep SqlStep { get; } = fixture.SqlStep;

    [Fact] public void Connection_NotNull() => Assert.NotNull(SqlStep.Connection);

    [Fact] public void Parameters_NotEmpty() => Assert.NotEmpty(SqlStep.StepParameters);
}

public class SqlStepFixture(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = fixture.DbContextFactory;

    public SqlStep SqlStep { get; private set; } = null!;

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        SqlStep = (SqlStep)await context.Steps
            .Include($"{nameof(IHasConnection.Connection)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}")
            .FirstAsync(step => step.StepName == "Test step 4");
    }
}