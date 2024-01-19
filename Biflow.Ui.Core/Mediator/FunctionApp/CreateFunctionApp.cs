namespace Biflow.Ui.Core;

public record CreateFunctionAppCommand(FunctionApp FunctionApp) : IRequest;

internal class CreateFunctionAppCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<CreateFunctionAppCommand>
{
    public async Task Handle(CreateFunctionAppCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.FunctionApps.Add(request.FunctionApp);
        await context.SaveChangesAsync(cancellationToken);
    }
}