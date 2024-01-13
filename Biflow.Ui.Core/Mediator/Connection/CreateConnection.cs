using Biflow.Core.Entities;
using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record CreateConnectionCommand(ConnectionInfoBase Connection) : IRequest;

internal class CreateConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<CreateConnectionCommand>
{
    public async Task Handle(CreateConnectionCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Connections.Add(request.Connection);
        await context.SaveChangesAsync(cancellationToken);
    }
}