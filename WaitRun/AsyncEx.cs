using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AhDung.WinForm
{
    //本部分用于.net40+
    public static partial class Async
    {
        //static readonly Dictionary<Task,>

        //指定调度器避免任务运行在UI线程
        static readonly Lazy<TaskFactory> _taskFactoryLazy = new Lazy<TaskFactory>(() => new TaskFactory(TaskScheduler.Default));
        /// <summary>
        /// 本类使用的TaskFactory
        /// </summary>
        static TaskFactory TaskFactory => _taskFactoryLazy.Value;

        static void RunTask(Func<Task> taskFunc, Action before, Action<bool, Exception> after, bool force)
        {
            var method = taskFunc.Method;

            if (!force)
            {
                lock (ThisType)
                {
                    if (_inRunningMethods.ContainsKey(method))
                    {
                        return;
                    }
                }
            }

            lock (ThisType)
            {
                _inRunningMethods.TryGetValue(method, out var count);
                _inRunningMethods[method] = count + 1;
            }

            before?.Invoke();
            var task = TaskFactory.StartNew(taskFunc).Unwrap();
            task.ContinueWith(t =>
            {

            });

            if (after != null)
            {
                task.ContinueWith(t => after(t.IsCanceled, t.Exception?.InnerException), TaskScheduler.FromCurrentSynchronizationContext());
            }
        }


    }
}
