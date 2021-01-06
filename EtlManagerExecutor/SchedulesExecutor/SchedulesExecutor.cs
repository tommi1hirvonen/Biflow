using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class SchedulesExecutor : ISchedulesExecutor
    {
        private readonly IConfiguration configuration;
        public SchedulesExecutor(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task RunAsync(int hours, int minutes)
        {
            string etlManagerConnectionString = configuration.GetValue<string>("EtlManagerConnectionString");

            DayOfWeek weekday = DateTime.Now.DayOfWeek;

            StringBuilder commandBuilder = new();

            commandBuilder.Append(
                @"SELECT A.ScheduleId, A.JobId
                FROM etlmanager.Schedule AS A
                    INNER JOIN etlmanager.Job AS B ON A.JobId = B.JobId
                WHERE A.IsEnabled = 1 AND B.IsEnabled = 1 "
            );
            commandBuilder.AppendFormat("AND A.TimeHours = '{0}' AND A.TimeMinutes = '{1}' ", hours, minutes);

            switch (weekday)
            {
                case DayOfWeek.Sunday:
                    commandBuilder.Append("AND A.Sunday = 1 ");
                    break;
                case DayOfWeek.Monday:
                    commandBuilder.Append("AND A.Monday = 1 ");
                    break;
                case DayOfWeek.Tuesday:
                    commandBuilder.Append("AND A.Tuesday = 1 ");
                    break;
                case DayOfWeek.Wednesday:
                    commandBuilder.Append("AND A.Wednesday = 1 ");
                    break;
                case DayOfWeek.Thursday:
                    commandBuilder.Append("AND A.Thursday = 1 ");
                    break;
                case DayOfWeek.Friday:
                    commandBuilder.Append("AND A.Friday = 1 ");
                    break;
                case DayOfWeek.Saturday:
                    commandBuilder.Append("AND A.Saturday = 1 ");
                    break;
            }

            using SqlConnection sqlConnection = new(etlManagerConnectionString);

            try
            {
                sqlConnection.Open();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening SQL Server connection");
                return;
            }

            SqlCommand sqlCommand = new(commandBuilder.ToString(), sqlConnection);

            List<KeyValuePair<string, string>> jobIds = new();

            try
            {
                using var reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    var jobId = reader["JobId"].ToString();
                    var scheduleId = reader["ScheduleId"].ToString();
                    jobIds.Add(new KeyValuePair<string, string>(jobId, scheduleId));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting list of jobs to execute");
                return;
            }

            if (jobIds.Count == 0)
            {
                Log.Information("No active jobs to execute on weekday: {weekday}, hours: {hours}, minutes: {minutes}", weekday, hours, minutes);
                return;
            }

            string executorFilePath = Process.GetCurrentProcess().MainModule.FileName;

            foreach (var pair in jobIds)
            {
                SqlCommand initCommand = new(
                    "EXEC etlmanager.ExecutionInitialize @JobId = @JobId_, @ScheduleId = @ScheduleId_"
                    , sqlConnection);
                initCommand.Parameters.AddWithValue("@JobId_", pair.Key);
                initCommand.Parameters.AddWithValue("@ScheduleId_", pair.Value);

                string executionId;
                try
                {
                    executionId = (await initCommand.ExecuteScalarAsync()).ToString();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error initializing execution for job {jobId}", pair.Key);
                    continue;
                }

                ProcessStartInfo executionInfo = new()
                {
                    FileName = executorFilePath,
                    ArgumentList = {
                        "execute",
                        "--id",
                        executionId.ToString(),
                        "--notify"
                    },
                    // Set WorkingDirectory for the EtlManagerExecutor executable.
                    // This way it reads the configuration file (appsettings.json) from the correct folder.
                    WorkingDirectory = Path.GetDirectoryName(executorFilePath),
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process executorProcess = new() { StartInfo = executionInfo };
                try
                {
                    executorProcess.Start();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error starting executor process for execution {executionId}", executionId);
                    continue;
                }

                SqlCommand processIdCmd = new(
                "UPDATE etlmanager.Execution SET ExecutorProcessId = @ProcessId WHERE ExecutionId = @ExecutionId", sqlConnection);
                processIdCmd.Parameters.AddWithValue("@ProcessId", executorProcess.Id);
                processIdCmd.Parameters.AddWithValue("@ExecutionId", executionId);

                try
                {
                    await processIdCmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating executor process id for execution {executionId}", executionId);
                }
            }
        }
    }
}
