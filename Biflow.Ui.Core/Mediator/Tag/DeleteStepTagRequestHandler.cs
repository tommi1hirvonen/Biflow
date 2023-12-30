using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class DeleteStepTagRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteStepTagRequest>
{
    public async Task Handle(DeleteStepTagRequest request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await context.Tags
            .Include(t => t.Steps.Where(s => s.StepId == request.StepId))
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken);
        if (tag?.Steps.FirstOrDefault(s => s.StepId == request.StepId) is Step step)
        {
            tag.Steps.Remove(step);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
