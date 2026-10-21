using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Camefor.Tools.Cache
{
    /// <summary>
    /// 以反射方式触发缓存代理方法，并等待异步返回值完成。
    /// </summary>
    internal static class MemoryCacheInvoker
    {
        internal static async Task InvokeAsync(
            object proxy,
            MethodInfo method,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            object result;
            try
            {
                // 通过接口方法调用生成的代理实例，确保请求经过 MemoryCacheDispatchProxy。
                result = method.Invoke(proxy, Array.Empty<object>());
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }

            var returnType = method.ReturnType;
            if (typeof(Task).GetTypeInfo().IsAssignableFrom(returnType.GetTypeInfo()))
            {
                await AwaitWithCancellationAsync((Task)result, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (returnType.GetTypeInfo().IsGenericType &&
                returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var asTaskMethod = returnType.GetTypeInfo().GetDeclaredMethod("AsTask");
                var task = (Task)asTaskMethod.Invoke(result, null);
                await AwaitWithCancellationAsync(task, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task AwaitWithCancellationAsync(Task task, CancellationToken cancellationToken)
        {
            if (task == null)
            {
                throw new InvalidOperationException("缓存方法返回了空的 Task。");
            }

            if (task.IsCompleted || !cancellationToken.CanBeCanceled)
            {
                await task.ConfigureAwait(false);
                return;
            }

            var cancellationSource = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                state => ((TaskCompletionSource<bool>)state).TrySetResult(true),
                cancellationSource))
            {
                var completedTask = await Task.WhenAny(task, cancellationSource.Task).ConfigureAwait(false);
                if (completedTask != task)
                {
                    // 取消后原任务仍会继续执行，观察其异常避免 UnobservedTaskException。
                    ObserveFault(task);
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            await task.ConfigureAwait(false);
        }

        private static void ObserveFault(Task task)
        {
            task.ContinueWith(
                completed => { var ignored = completed.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
    }
}
