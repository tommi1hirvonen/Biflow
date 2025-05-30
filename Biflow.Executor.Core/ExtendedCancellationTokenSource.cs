﻿namespace Biflow.Executor.Core;

public class ExtendedCancellationTokenSource : CancellationTokenSource
{
    public string Username { get; private set; } = "timeout";

    public void Cancel(string username)
    {
        Username = username;
        Cancel();
    }
}
