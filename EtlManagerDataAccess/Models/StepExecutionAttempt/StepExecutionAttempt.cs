using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.IO.Pipes;
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

    }
}
