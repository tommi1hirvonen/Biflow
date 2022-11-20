using Dapper;
using Biflow.DataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Biflow.Ui.Core;

public class DbHelperService
{
    private readonly IConfiguration _configuration;
    private readonly IExecutorService _executor;

    public DbHelperService(IConfiguration configuration, IExecutorService executor)
    {
        _configuration = configuration;
        _executor = executor;
    }

    public async Task<Guid> StartExecutionAsync(
        Job job,
        string username,
        List<string>? stepIds = null,
        bool notify = false,
        SubscriptionType? notifyMe = null,
        bool notifyMeOvertime = false)
    {
        Guid executionId;
        using (var sqlConnection = new SqlConnection(_configuration.GetConnectionString("BiflowContext")))
        {
            CommandDefinition command;
            var parameters = new DynamicParameters();
            parameters.AddDynamicParams(new
            {
                JobId_ = job.JobId,
                Username_ = username,
                Notify_ = notify,
                NotifyCaller_ = notifyMe,
                NotifyCallerOvertime_ = notifyMeOvertime
            });
            if (stepIds is not null && stepIds.Count > 0)
            {
                parameters.Add("StepIds_", string.Join(',', stepIds));
                command = new CommandDefinition("""
                    EXEC [biflow].[ExecutionInitialize]
                        @JobId = @JobId_,
                        @Username = @Username_,
                        @StepIds = @StepIds_,
                        @Notify = @Notify_,
                        @NotifyCaller = @NotifyCaller_,
                        @NotifyCallerOvertime = @NotifyCallerOvertime_
                    """,
                    parameters);
            }
            else
            {
                command = new CommandDefinition("""
                    EXEC [biflow].[ExecutionInitialize]
                        @JobId = @JobId_,
                        @Username = @Username_,
                        @Notify = @Notify_,
                        @NotifyCaller = @NotifyCaller_,
                        @NotifyCallerOvertime = @NotifyCallerOvertime_
                    """,
                    parameters);
            }
            executionId = await sqlConnection.ExecuteScalarAsync<Guid>(command);
        }

        await _executor.StartExecutionAsync(executionId);

        return executionId;
    }

    public async Task<Guid> JobCopyAsync(Guid jobId, string username)
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("BiflowContext"));
        var createdJobId = await sqlConnection.ExecuteScalarAsync<Guid>(
            "EXEC [biflow].[JobCopy] @JobId = @JobId_, @Username = @Username_",
            new { JobId_ = jobId, Username_ = username });
        return createdJobId;
    }

    public async Task<Guid> StepCopyAsync(Guid stepId, Guid targetJobId, string username, string nameSuffix = "")
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("BiflowContext"));
        var createdStepId = await sqlConnection.ExecuteScalarAsync<Guid>(
            "EXEC [biflow].[StepCopy] @StepId = @StepId_, @TargetJobId = @TargetJobId_, @Username = @Username_, @NameSuffix = @NameSuffix_",
            new { StepId_ = stepId, TargetJobId_ = targetJobId, Username_ = username, NameSuffix_ = nameSuffix });
        return createdStepId;
    }

    public async Task<bool> UpdatePasswordAsync(string username, string password)
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("BiflowContext"));
        var result = await sqlConnection.ExecuteScalarAsync<int>(
            "EXEC [biflow].[UserUpdatePassword] @Username = @Username_, @Password = @Password_",
            new { Username_ = username, Password_ = password });

        if (result > 0) return true;
        else return false;
    }

    public async Task<bool> AddUserAsync(User user, string password)
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("BiflowContext"));
        var result = await sqlConnection.ExecuteScalarAsync<int>(
            "EXEC [biflow].[UserAdd] @Username = @Username_, @Password = @Password_, @Role = @Role_, @Email = @Email_",
            new { Username_ = user.Username, Password_ = password, Role_ = user.Role, Email_ = user.Email });

        if (result > 0) return true;
        else return false;
    }

    public AuthenticationResult AuthenticateUser(string username, string password)
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("BiflowContext"));
        var role = sqlConnection.ExecuteScalar<string?>(
            "EXEC [biflow].[UserAuthenticate] @Username = @Username_, @Password = @Password_",
            new { Username_ = username, Password_ = password });
        return new AuthenticationResult(role);
    }
}
