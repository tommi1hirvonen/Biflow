using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class VmStepExecutor(
    IServiceProvider serviceProvider,
    VmStepExecution step,
    VmStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly ILogger<VmStepExecutor> _logger = serviceProvider
        .GetRequiredService<ILogger<VmStepExecutor>>();

    private readonly ArmClient _armClient = new(
        (step.GetAzureCredential() ?? throw new ArgumentNullException(message: "Azure credential was null", innerException: null))
        .GetTokenServiceCredential(serviceProvider.GetRequiredService<ITokenService>()));

    public async Task<Result> ExecuteAsync(OrchestrationContext context, CancellationContext cancellationContext)
    {
        var cancellationToken = cancellationContext.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        ResourceIdentifier vmResourceId;
        try
        {
            vmResourceId = new ResourceIdentifier(step.VirtualMachineResourceId);
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Invalid virtual machine resource id");
            return Result.Failure;
        }

        try
        {
            var vm = _armClient.GetVirtualMachineResource(vmResourceId);
            var powerState = await GetPowerStateAsync(vm, cancellationToken);

            if (step.Operation == VmStepOperation.EnsureRunning)
            {
                if (IsRunning(powerState))
                {
                    attempt.AddOutput($"VM is already running (state: {powerState ?? "unknown"})");
                    return Result.Success;
                }

                attempt.AddOutput($"Powering on VM '{vmResourceId.Name}'");
                await vm.PowerOnAsync(WaitUntil.Completed, cancellationToken: cancellationToken);
                attempt.AddOutput("VM is running");
                return Result.Success;
            }

            if (IsStopped(powerState))
            {
                attempt.AddOutput($"VM is already stopped (state: {powerState ?? "unknown"})");
                return Result.Success;
            }

            attempt.AddOutput($"Deallocating VM '{vmResourceId.Name}'");
            await vm.DeallocateAsync(WaitUntil.Completed, cancellationToken: cancellationToken);
            attempt.AddOutput("VM is stopped");
            return Result.Success;
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationContext.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }

            attempt.AddError(ex, "VM operation was canceled unexpectedly");
            return Result.Failure;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} VM operation failed", step.ExecutionId, step);
            attempt.AddError(ex, "Azure VM operation failed");
            return Result.Failure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error executing VM step", step.ExecutionId, step);
            attempt.AddError(ex, "Error executing VM step");
            return Result.Failure;
        }
    }

    private static async Task<string?> GetPowerStateAsync(VirtualMachineResource vm, CancellationToken cancellationToken)
    {
        var instanceView = await vm.InstanceViewAsync(cancellationToken);
        var powerStatus = instanceView.Value.Statuses
            .FirstOrDefault(s => s.Code?.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase) == true);
        return powerStatus?.Code;
    }

    private static bool IsRunning(string? powerState) => powerState is "PowerState/running" or "PowerState/starting";

    private static bool IsStopped(string? powerState) => powerState is
        "PowerState/stopped" or
        "PowerState/stopping" or
        "PowerState/deallocated" or
        "PowerState/deallocating";
}
