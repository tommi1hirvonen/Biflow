namespace Biflow.Ui.Core;

public record UpdateAzureCredentialCommand(AzureCredential AzureCredential) : IRequest;

internal class UpdateAzureCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateAzureCredentialCommand>
{
    public async Task Handle(UpdateAzureCredentialCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.AzureCredential).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}