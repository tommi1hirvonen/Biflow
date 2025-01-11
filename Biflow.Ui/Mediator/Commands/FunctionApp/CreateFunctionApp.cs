namespace Biflow.Ui;

public record CreateFunctionAppCommand(FunctionApp FunctionApp) : IRequest;

internal class CreateFunctionAppCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateFunctionAppCommand>
{
    public async Task Handle(CreateFunctionAppCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.FunctionApps.Add(request.FunctionApp);
        context.Entry(request.FunctionApp.AzureCredential).State = EntityState.Unchanged;
        await context.SaveChangesAsync(cancellationToken);
    }
}