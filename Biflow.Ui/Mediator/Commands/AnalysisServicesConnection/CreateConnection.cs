namespace Biflow.Ui;

public record CreateAnalysisServicesConnectionCommand(AnalysisServicesConnection Connection) : IRequest;

internal class CreateAnalysisServicesConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateAnalysisServicesConnectionCommand>
{
    public async Task Handle(CreateAnalysisServicesConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.AnalysisServicesConnections.Add(request.Connection);
        await context.SaveChangesAsync(cancellationToken);
    }
}