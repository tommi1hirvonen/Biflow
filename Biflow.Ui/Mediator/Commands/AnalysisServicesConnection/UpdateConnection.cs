namespace Biflow.Ui;

public record UpdateAnalysisServicesConnectionCommand(AnalysisServicesConnection Connection) : IRequest;

internal class UpdateAnalysisServicesConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateAnalysisServicesConnectionCommand>
{
    public async Task Handle(UpdateAnalysisServicesConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Connection).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}