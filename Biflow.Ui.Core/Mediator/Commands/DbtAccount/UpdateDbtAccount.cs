namespace Biflow.Ui.Core;

/// <summary>
/// 
/// </summary>
/// <param name="DbtAccountId"></param>
/// <param name="DbtAccountName"></param>
/// <param name="ApiBaseUrl"></param>
/// <param name="AccountId"></param>
/// <param name="ApiToken">Pass null to retain the previous ApiToken value</param>
public record UpdateDbtAccountCommand(
    Guid DbtAccountId,
    string DbtAccountName,
    string ApiBaseUrl,
    string AccountId,
    string? ApiToken) : IRequest<DbtAccount>;

[UsedImplicitly]
internal class UpdateDbtAccountCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateDbtAccountCommand, DbtAccount>
{
    public async Task<DbtAccount> Handle(UpdateDbtAccountCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var account = await dbContext.DbtAccounts
            .FirstOrDefaultAsync(x => x.DbtAccountId == request.DbtAccountId, cancellationToken)
            ?? throw new NotFoundException<DbtAccount>(request.DbtAccountId);
        account.DbtAccountName = request.DbtAccountName;
        account.ApiBaseUrl = request.ApiBaseUrl;
        account.AccountId = request.AccountId;
        if (request.ApiToken is not null)
        {
            account.ApiToken = request.ApiToken;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }
}
