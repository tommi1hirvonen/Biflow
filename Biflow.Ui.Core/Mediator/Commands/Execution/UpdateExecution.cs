namespace Biflow.Ui.Core;

public record UpdateExecutionCommand(Execution Execution) : IRequest;

internal class UpdateExecutionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateExecutionCommand>
{
    public async Task Handle(UpdateExecutionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Execution).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}