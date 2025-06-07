global using ExeTasksRunner =
    Biflow.ExecutorProxy.WebApp.TasksRunner<
        Biflow.ExecutorProxy.WebApp.ProxyTasks.ExeProxyTask,
        Biflow.ExecutorProxy.Core.ExeTaskRunningResponse,
        Biflow.ExecutorProxy.Core.ExeTaskCompletedResponse>;