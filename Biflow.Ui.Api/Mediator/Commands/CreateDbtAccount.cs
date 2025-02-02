namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateDbtAccountCommand(
    string DbtAccountName,
    string ApiBaseUrl,
    string AccountId,
    string ApiToken) : IRequest<DbtAccount>;

[UsedImplicitly]
internal class CreateDbtAccountCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateDbtAccountCommand, DbtAccount>
{
    public async Task<DbtAccount> Handle(CreateDbtAccountCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var account = new DbtAccount
        {
            DbtAccountName = request.DbtAccountName,
            ApiBaseUrl = request.ApiBaseUrl,
            AccountId = request.AccountId,
            ApiToken = request.ApiToken
        };
        account.EnsureDataAnnotationsValidated();
        dbContext.DbtAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }
}
