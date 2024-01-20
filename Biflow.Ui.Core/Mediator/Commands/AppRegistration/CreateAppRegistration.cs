namespace Biflow.Ui.Core;

public record CreateAppRegistrationCommand(AppRegistration AppRegistration) : IRequest;

internal class CreateAppRegistrationCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateAppRegistrationCommand>
{
    public async Task Handle(CreateAppRegistrationCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.AppRegistrations.Add(request.AppRegistration);
        await context.SaveChangesAsync(cancellationToken);
    }
}