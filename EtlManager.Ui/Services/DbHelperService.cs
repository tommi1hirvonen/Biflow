using Dapper;
using EtlManager.DataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Ui;

public class DbHelperService
{
    private readonly IConfiguration _configuration;

    public DbHelperService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<Guid> StartExecutionAsync(Job job, string username, List<string>? stepIds = null, bool notify = false)
    {
        Guid executionId;
        using (var sqlConnection = new SqlConnection(_configuration.GetConnectionString("EtlManagerContext")))
        {
            CommandDefinition command;
            var parameters = new DynamicParameters();
            parameters.AddDynamicParams(new { JobId_ = job.JobId, Username_ = username });
            if (stepIds is not null && stepIds.Count > 0)
            {
                parameters.Add("StepIds_", string.Join(',', stepIds));
                command = new CommandDefinition("EXEC [etlmanager].[ExecutionInitialize] @JobId = @JobId_, @Username = @Username_, @StepIds = @StepIds_",
                    parameters);
            }
            else
            {
                command = new CommandDefinition("EXEC [etlmanager].[ExecutionInitialize] @JobId = @JobId_, @Username = @Username_",
                    parameters);
            }
            executionId = await sqlConnection.ExecuteScalarAsync<Guid>(command);
        }

        string executorPath = _configuration.GetValue<string>("EtlManagerExecutorPath");

        var executionInfo = new ProcessStartInfo()
        {
            // The installation folder should be included in the Path variable, so no path required here.
            FileName = executorPath,
            ArgumentList = {
                    "execute",
                    "--id",
                    executionId.ToString(),
                    notify ? "--notify" : ""
                },
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        var executorProcess = new Process() { StartInfo = executionInfo };
        executorProcess.Start();

        return executionId;
    }

    public async Task<Guid> JobCopyAsync(Guid jobId, string username)
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("EtlManagerContext"));
        var createdJobId = await sqlConnection.ExecuteScalarAsync<Guid>(
            "EXEC [etlmanager].[JobCopy] @JobId = @JobId_, @Username = @Username_",
            new { JobId_ = jobId, Username_ = username });
        return createdJobId;
    }

    public async Task<Guid> StepCopyAsync(Guid stepId, Guid targetJobId, string username)
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("EtlManagerContext"));
        var createdStepId = await sqlConnection.ExecuteScalarAsync<Guid>(
            "EXEC [etlmanager].[StepCopy] @StepId = @StepId_, @TargetJobId = @TargetJobId_, @Username = @Username_",
            new { StepId_ = stepId, TargetJobId_ = targetJobId, Username_ = username });
        return createdStepId;
    }

    public async Task<bool> UpdatePasswordAsync(string username, string password)
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("EtlManagerContext"));
        var result = await sqlConnection.ExecuteScalarAsync<int>(
            "EXEC [etlmanager].[UserUpdatePassword] @Username = @Username_, @Password = @Password_",
            new { Username_ = username, Password_ = password });

        if (result > 0) return true;
        else return false;
    }

    public async Task<bool> AddUserAsync(User user, string password)
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("EtlManagerContext"));
        var result = await sqlConnection.ExecuteScalarAsync<int>(
            "EXEC [etlmanager].[UserAdd] @Username = @Username_, @Password = @Password_, @Role = @Role_, @Email = @Email_",
            new { Username_ = user.Username, Password_ = password, Role_ = user.Role, Email_ = user.Email });

        if (result > 0) return true;
        else return false;
    }

    public AuthenticationResult AuthenticateUser(string username, string password)
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("EtlManagerContext"));
        var role = sqlConnection.ExecuteScalar<string?>(
            "EXEC [etlmanager].[UserAuthenticate] @Username = @Username_, @Password = @Password_",
            new { Username_ = username, Password_ = password });
        return new AuthenticationResult(role);
    }
}
