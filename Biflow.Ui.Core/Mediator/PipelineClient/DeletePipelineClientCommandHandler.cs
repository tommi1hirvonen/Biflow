using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class DeletePipelineClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeletePipelineClientCommand>
{
    public async Task Handle(DeletePipelineClientCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.PipelineClients
            .FirstOrDefaultAsync(p => p.PipelineClientId == request.PipelineClientId, cancellationToken);
        if (client is not null)
        {
            context.PipelineClients.Remove(client);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
