using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace EtlManagerExecutor
{
    class SchedulesExecutor : ISchedulesExecutor
    {
        private readonly IConfiguration configuration;
        public SchedulesExecutor(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void Run(int hours, int minutes)
        {
            string etlManagerConnectionString = configuration.GetValue<string>("EtlManagerConnectionString");

            DayOfWeek weekday = DateTime.Now.DayOfWeek;

            StringBuilder commandBuilder = new StringBuilder();

            commandBuilder.Append("SELECT ScheduleId, JobId FROM etlmanager.Schedule WHERE IsEnabled = 1 ");
            commandBuilder.AppendFormat("AND TimeHours = '{0}' AND TimeMinutes = '{1}' ", hours, minutes);

            switch (weekday)
            {
                case DayOfWeek.Sunday:
                    commandBuilder.Append("AND Sunday = 1 ");
                    break;
                case DayOfWeek.Monday:
                    commandBuilder.Append("AND Monday = 1 ");
                    break;
                case DayOfWeek.Tuesday:
                    commandBuilder.Append("AND Tuesday = 1 ");
                    break;
                case DayOfWeek.Wednesday:
                    commandBuilder.Append("AND Wednesday = 1 ");
                    break;
                case DayOfWeek.Thursday:
                    commandBuilder.Append("AND Thursday = 1 ");
                    break;
                case DayOfWeek.Friday:
                    commandBuilder.Append("AND Friday = 1 ");
                    break;
                case DayOfWeek.Saturday:
                    commandBuilder.Append("AND Saturday = 1 ");
                    break;
            }

            using SqlConnection sqlConnection = new SqlConnection(etlManagerConnectionString);

            try
            {
                sqlConnection.Open();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening SQL Server connection");
                return;
            }

            SqlCommand sqlCommand = new SqlCommand(commandBuilder.ToString(), sqlConnection);

            List<KeyValuePair<string, string>> jobIds = new List<KeyValuePair<string, string>>();

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
                return;
            }

            string executorFilePath = Process.GetCurrentProcess().MainModule.FileName;

            foreach (var pair in jobIds)
            {
                SqlCommand initCommand = new SqlCommand(
                    "EXEC etlmanager.ExecutionInitialize @JobId = @JobId_, @ScheduleId = @ScheduleId_"
                    , sqlConnection);
                initCommand.Parameters.AddWithValue("@JobId_", pair.Key);
                initCommand.Parameters.AddWithValue("@ScheduleId_", pair.Value);

                string executionId;
                try
                {
                    executionId = initCommand.ExecuteScalar().ToString();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error initializing execution for job {jobId}", pair.Key);
                    continue;
                }

                ProcessStartInfo executionInfo = new ProcessStartInfo()
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

                Process executorProcess = new Process() { StartInfo = executionInfo };
                try
                {
                    executorProcess.Start();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error starting executor process for execution {executionId}", executionId);
                    continue;
                }

                SqlCommand processIdCmd = new SqlCommand(
                "UPDATE etlmanager.Execution SET ExecutorProcessId = @ProcessId WHERE ExecutionId = @ExecutionId", sqlConnection);
                processIdCmd.Parameters.AddWithValue("@ProcessId", executorProcess.Id);
                processIdCmd.Parameters.AddWithValue("@ExecutionId", executionId);

                try
                {
                    processIdCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating executor process id for execution {executionId}", executionId);
                }
            }
        }
    }
}
