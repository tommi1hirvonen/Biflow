namespace Biflow.Ui.Core;

public record CreateAzureCredentialCommand(AzureCredential AzureCredential) : IRequest;

internal class CreateAzureCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateAzureCredentialCommand>
{
    public async Task Handle(CreateAzureCredentialCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.AzureCredentials.Add(request.AzureCredential);
        await context.SaveChangesAsync(cancellationToken);
    }
}