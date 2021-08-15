using System;

namespace EtlManagerUtils
{
    public record CancelCommand(Guid? StepId, string Username);
}