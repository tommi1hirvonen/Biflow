using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record UpdateQlikCloudClientCommand(QlikCloudClient Client) : IRequest;

internal class UpdateQlikCloudClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateQlikCloudClientCommand>
{
    public async Task Handle(UpdateQlikCloudClientCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Client).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}