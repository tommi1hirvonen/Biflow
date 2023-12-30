using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record UpdateJobCommand(Job Job) : IRequest;

internal class UpdateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateJobCommand>
{
    public async Task Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Jobs.Attach(request.Job).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}