using Biflow.Core.Entities;
using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record UpdatePipelineClientCommand(PipelineClient Client) : IRequest;

internal class UpdatePipelineClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdatePipelineClientCommand>
{
    public async Task Handle(UpdatePipelineClientCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Client).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}