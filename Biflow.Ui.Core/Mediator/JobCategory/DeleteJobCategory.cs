using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record DeleteJobCategoryCommand(JobCategory Category) : IRequest;

internal class DeleteJobCategoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteJobCategoryCommand>
{
    public async Task Handle(DeleteJobCategoryCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.JobCategories.Remove(request.Category);
        await context.SaveChangesAsync(cancellationToken);
    }
}