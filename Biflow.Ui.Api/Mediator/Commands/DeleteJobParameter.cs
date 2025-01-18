namespace Biflow.Ui.Api.Mediator.Commands;

internal record DeleteJobParameterCommand(Guid ParameterId) : IRequest;

[UsedImplicitly]
internal class DeleteJobParameterCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) 
    : IRequestHandler<DeleteJobParameterCommand>
{
    public async Task Handle(DeleteJobParameterCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var jobParameter = await dbContext.Set<JobParameter>()
            .FirstOrDefaultAsync(p => p.ParameterId == request.ParameterId, cancellationToken)
            ?? throw new NotFoundException<JobParameter>(request.ParameterId);
        dbContext.Remove(jobParameter);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}