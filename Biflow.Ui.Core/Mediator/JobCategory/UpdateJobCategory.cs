using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record UpdateJobCategoryCommand(JobCategory Category) : IRequest;

internal class UpdateJobCategoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateJobCategoryCommand>
{
    public async Task Handle(UpdateJobCategoryCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.JobCategories.Attach(request.Category).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}