namespace Biflow.Ui.Core;

public record UpdateAppRegistrationCommand(AppRegistration AppRegistration) : IRequest;

internal class UpdateAppRegistrationCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateAppRegistrationCommand>
{
    public async Task Handle(UpdateAppRegistrationCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.AppRegistration).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}