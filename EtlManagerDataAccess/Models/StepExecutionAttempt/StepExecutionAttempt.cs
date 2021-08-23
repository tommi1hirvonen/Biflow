using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EtlManagerUtils;

namespace EtlManagerDataAccess.Models
{
    public abstract record StepExecutionAttempt
    {
        public StepExecutionAttempt(StepExecutionStatus executionStatus, StepType stepType)
        {
            ExecutionStatus = executionStatus;
            StepType = stepType;
        }

        public Guid ExecutionId { get; set; }
        
        public Guid StepId { get; set; }
        
        public int RetryAttemptIndex { get; set; }
        
        public DateTimeOffset? StartDateTime { get; set; }
        
        public DateTimeOffset? EndDateTime { get; set; }
        
        public StepExecutionStatus ExecutionStatus { get; set; }

        public StepType StepType { get; private init; }

        [Display(Name = "Error message")]
        public string? ErrorMessage { get; set; }

        [Display(Name = "Info message")]
        public string? InfoMessage { get; set; }

        [Display(Name = "Stopped by")]
        public string? StoppedBy { get; set; }

        public StepExecution StepExecution { get; set; } = null!;

        [NotMapped]
        public string UniqueId => string.Concat(ExecutionId, StepId, RetryAttemptIndex);

        [NotMapped]
        public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;

        public string? GetDurationInReadableFormat() => ExecutionInSeconds?.SecondsToReadableFormat();

        public virtual void Reset()
        {
            ErrorMessage = null;
            InfoMessage = null;
        }

        public async Task StopExecutionAsync(string username)
        {
            // Connect to the pipe server set up by the executor process.
            using var pipeClient = new NamedPipeClientStream(".", ExecutionId.ToString().ToLower(), PipeDirection.Out); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
            using var streamWriter = new StreamWriter(pipeClient);
            // Send cancel command.
            var username_ = string.IsNullOrWhiteSpace(username) ? "unknown" : username;
            var cancelCommand = new CancelCommand(StepId, username);
            var json = JsonSerializer.Serialize(cancelCommand);
            streamWriter.WriteLine(json);
        }

        public (int Offset, int Width) GetGanttGraphDimensions()
        {
            var allAttempts = StepExecution.Execution.StepExecutions
                .SelectMany(e => e.StepExecutionAttempts)
                .Where(e => e.StartDateTime != null);
            
            if (!allAttempts.Any())
                return (0, 0);

            var minTime = allAttempts.Min(e => e.StartDateTime?.LocalDateTime) ?? DateTime.Now;
            var maxTime = allAttempts.Max(e => e.EndDateTime?.LocalDateTime ?? DateTime.Now);

            var minTicks = minTime.Ticks;
            var maxTicks = maxTime.Ticks;

            if (minTicks == maxTicks)
                return (0, 0);

            var startTicks = (StartDateTime?.LocalDateTime ?? DateTime.Now).Ticks;
            var endTicks = (EndDateTime?.LocalDateTime ?? DateTime.Now).Ticks;

            var start = (double)(startTicks - minTicks) / (maxTicks - minTicks) * 100;
            var end = (double)(endTicks - minTicks) / (maxTicks - minTicks) * 100;
            var width = end - start;
            width = width < 1 ? 1 : width; // check that width is not 0
            start = start > 99 ? 99 : start; // check that start is not 100

            return ((int)Math.Round(start, 0), (int)Math.Round(width, 0));
        }

    }
}
