using System.Collections.Immutable;

namespace Biflow.Executor.Core.Notification;

public record NotificationResponse(ICollection<string> Recipients, string Subject, string Body, bool IsBodyHtml)
{
    public static readonly NotificationResponse Empty = new(ImmutableArray<string>.Empty, "", "", false);
}