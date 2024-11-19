namespace Biflow.Ui.Core;

public record UpdateFunctionAppCommand(FunctionApp FunctionApp) : IRequest;

internal class UpdateFunctionAppCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateFunctionAppCommand>
{
    public async Task Handle(UpdateFunctionAppCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var functionApp = await context.FunctionApps
            .FirstOrDefaultAsync(f => f.FunctionAppId == request.FunctionApp.FunctionAppId, cancellationToken)
            ?? throw new NotFoundException<FunctionApp>(request.FunctionApp.FunctionAppId);
        context.Entry(functionApp).CurrentValues.SetValues(request.FunctionApp);
        await context.SaveChangesAsync(cancellationToken);
    }
}