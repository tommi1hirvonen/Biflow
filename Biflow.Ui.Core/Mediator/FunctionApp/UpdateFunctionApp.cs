using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record UpdateFunctionAppCommand(FunctionApp FunctionApp) : IRequest;

internal class UpdateFunctionAppCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateFunctionAppCommand>
{
    public async Task Handle(UpdateFunctionAppCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.FunctionApp).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}