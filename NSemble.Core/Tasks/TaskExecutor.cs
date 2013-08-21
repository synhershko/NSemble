using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Document;

namespace NSemble.Core.Tasks
{
    public static class TaskExecutor
    {
        private static readonly ThreadLocal<List<ExecutableTask>> tasksToExecute =
            new ThreadLocal<List<ExecutableTask>>(() => new List<ExecutableTask>());

        public static Action<Exception> ExceptionHandler { get; set; }

        public static void ExcuteLater(ExecutableTask task)
        {
            tasksToExecute.Value.Add(task);
        }

        public static void Discard()
        {
            tasksToExecute.Value.Clear();
        }

        public static void StartExecuting()
        {
            var value = tasksToExecute.Value;
            var copy = value.ToArray();
            value.Clear();

            if (copy.Length > 0)
            {
                Task.Factory.StartNew(() =>
                {
                    foreach (var backgroundTask in copy)
                    {
                        ExecuteTask(backgroundTask);
                    }
                }, TaskCreationOptions.LongRunning)
                    .ContinueWith(task =>
                    {
                        if (ExceptionHandler != null) ExceptionHandler(task.Exception);
                    }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public static void ExecuteTask(ExecutableTask task)
        {
            for (var i = 0; i < 10; i++)
            {
                switch (task.Run())
                {
                    case true:
                    case false:
                        return;
                    case null:
                        break;
                }
            }
        }
    }

}
