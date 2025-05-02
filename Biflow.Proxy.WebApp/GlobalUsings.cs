global using ExeTasksRunner =
    Biflow.Proxy.WebApp.TasksRunner<
        Biflow.Proxy.WebApp.ProxyTasks.ExeProxyTask,
        Biflow.Proxy.Core.ExeTaskRunningResponse,
        Biflow.Proxy.Core.ExeTaskCompletedResponse>;