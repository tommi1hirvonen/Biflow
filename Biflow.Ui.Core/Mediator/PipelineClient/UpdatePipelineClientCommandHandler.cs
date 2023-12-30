using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

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
