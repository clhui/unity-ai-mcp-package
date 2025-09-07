using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Unity.MCP.Editor
{
    // 主线程调度器接口
    public interface IMainThreadDispatcher
    {
        void Enqueue(Action action);
        T EnqueueAndWait<T>(Func<T> func);
        void EnqueueAndWait(Action action);
    }

    // 主线程调度器统一入口
    public class MainThreadDispatcher
    {
        private static IMainThreadDispatcher _instance;
        private static readonly object _lock = new object();
        
        public static IMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            InitializeOnMainThread();
                            
                            // 如果在非主线程且实例仍为null，创建临时实例
                            if (_instance == null && !UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                            {
                                _instance = new TemporaryMainThreadDispatcher();
                                McpLogger.LogWarning("使用临时调度器，等待主线程初始化完成");
                            }
                        }
                    }
                }
                return _instance;
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnMainThread()
        {
            if (_instance != null) return; // 防止重复初始化
            
            // 检查是否在主线程
            if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
            {
                // 在主线程，直接创建
                CreateEditorDispatcher();
            }
            else
            {
                // 打印当前线程ID
                McpLogger.LogDebug($"初始化延迟调用CreateEditorDispatcher - 当前线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                // 在非主线程，延迟到主线程创建
                EditorApplication.delayCall += () => {
                    if (_instance == null)
                    {
                        CreateEditorDispatcher();
                    }
                };
            }
        }
        
        private static void CreateEditorDispatcher()
        {
            try
            {
                // 打印当前线程ID
                McpLogger.LogDebug($"CreateEditorDispatcher 调用 - 当前线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                var editorDispatcher = ScriptableObject.CreateInstance<EditorMainThreadDispatcher>();
                editorDispatcher.Initialize();
                
                // 设置锁定超时为3秒
                if (!Monitor.TryEnter(_lock, TimeSpan.FromSeconds(3)))
                {
                    throw new TimeoutException("[MainThreadDispatcher] 获取锁超时");
                }
                try
                {
                    var wasTemporary = _instance is TemporaryMainThreadDispatcher;
                    _instance = editorDispatcher;
                    
                    if (wasTemporary)
                    {
                        McpLogger.LogDebug("已从临时调度器切换到EditorMainThreadDispatcher");
                    }
                    else
                    {
                        McpLogger.LogDebug("EditorMainThreadDispatcher初始化完成");
                    }
                }
                finally
                {
                    Monitor.Exit(_lock);
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "初始化失败");
                throw;
            }
        }
        
        public static bool IsMainThread()
        {
            // 检查是否在主线程
            return UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread();
        }
        
        // 内部方法，供临时调度器检查实例状态
        internal static IMainThreadDispatcher GetCurrentInstance()
        {
            return _instance;
        }
    }

    // 临时主线程调度器，用于非主线程访问时的临时处理
    public class TemporaryMainThreadDispatcher : IMainThreadDispatcher
    {
        public void Enqueue(Action action)
        {
            if (action == null) return;
            

            bool completed = false;
            // 同时使用update回调作为备用机制
            EditorApplication.CallbackFunction updateCallback = null;
            updateCallback = () => {
                if (!completed)
                {
                    try
                    {
                        McpLogger.LogDebug($"🔁 Enqueue  Update回调执行 - 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                        action();
                        completed = true;
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogException(ex, "🔁 Enqueue  Update回调执行失败");
                        completed = true;
                    }
                }
                EditorApplication.update -= updateCallback;
            };
            EditorApplication.update += updateCallback;
        }
        
        public T EnqueueAndWait<T>(Func<T> func)
        {
            if (func == null) return default(T);
            
            // 检查是否在主线程
            if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
            {
                McpLogger.LogDebug($"✅ 主线程直接执行 - 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                // 在主线程直接执行
                return func();
            }
            
            // 非主线程时，使用改进的等待机制
            T result = default(T);
            Exception exception = null;
            bool completed = false;
            
            // 使用多种机制确保及时执行
            EditorApplication.delayCall += () => {
                try
                {
                    McpLogger.LogDebug($"🔄 DelayCall执行 - 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                    result = func();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    completed = true;
                }
            };
            
            // 同时使用update回调作为备用机制
            EditorApplication.CallbackFunction updateCallback = null;
            updateCallback = () => {
                if (!completed)
                {
                    try
                    {
                        McpLogger.LogDebug($"🔁 Update回调执行 - 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                        result = func();
                        completed = true;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        completed = true;
                    }
                }
                EditorApplication.update -= updateCallback;
            };
            EditorApplication.update += updateCallback;
            
            // 等待完成，减少到10秒但增加检查频率
            var timeout = DateTime.Now.AddSeconds(10);
            while (!completed && DateTime.Now < timeout)
            {
                System.Threading.Thread.Sleep(10); // 减少睡眠时间，增加响应性
                
                // 检查是否已经切换到正式调度器
                var currentInstance = MainThreadDispatcher.GetCurrentInstance();
                if (currentInstance != null && !(currentInstance is TemporaryMainThreadDispatcher))
                {
                    // 清理回调
                    EditorApplication.update -= updateCallback;
                    McpLogger.LogDebug($"🔄 切换到正式调度器 - 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                    // 已经有正式调度器，重新执行
                    return currentInstance.EnqueueAndWait(func);
                }
            }
            
            // 清理回调
            EditorApplication.update -= updateCallback;
            
            if (!completed)
            {
                throw new TimeoutException("TemporaryMainThreadDispatcher操作超时10秒，主线程可能繁忙或初始化失败");
            }
            
            if (exception != null)
            {
                throw exception;
            }
            
            return result;
        }
        
        public void EnqueueAndWait(Action action)
        {
            if (action == null) return;
            
            // 检查是否在主线程
            if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
            {
                // 在主线程直接执行
                action();
                return;
            }
            
            // 非主线程时，使用改进的等待机制
            Exception exception = null;
            bool completed = false;
            
            // 使用多种机制确保及时执行
            EditorApplication.delayCall += () => {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    completed = true;
                }
            };
            
            // 同时使用update回调作为备用机制
             EditorApplication.CallbackFunction updateCallback = null;
             updateCallback = () => {
                 if (!completed)
                 {
                     try
                     {
                         action();
                         completed = true;
                     }
                     catch (Exception ex)
                     {
                         exception = ex;
                         completed = true;
                     }
                 }
                 EditorApplication.update -= updateCallback;
             };
             EditorApplication.update += updateCallback;
            
            // 等待完成，减少到10秒但增加检查频率
            var timeout = DateTime.Now.AddSeconds(10);
            while (!completed && DateTime.Now < timeout)
            {
                System.Threading.Thread.Sleep(10); // 减少睡眠时间，增加响应性
                
                // 检查是否已经切换到正式调度器
                var currentInstance = MainThreadDispatcher.GetCurrentInstance();
                if (currentInstance != null && !(currentInstance is TemporaryMainThreadDispatcher))
                {
                    // 清理回调
                    EditorApplication.update -= updateCallback;
                    // 已经有正式调度器，重新执行
                    currentInstance.EnqueueAndWait(action);
                    return;
                }
            }
            
            // 清理回调
            EditorApplication.update -= updateCallback;
            
            if (!completed)
            {
                throw new TimeoutException("TemporaryMainThreadDispatcher操作超时10秒，主线程可能繁忙或初始化失败");
            }
            
            if (exception != null)
            {
                throw exception;
            }
        }
    }

    // 编辑器专用的MainThreadDispatcher，继承自ScriptableObject而不是MonoBehaviour
    public class EditorMainThreadDispatcher : ScriptableObject, IMainThreadDispatcher
    {
        private readonly ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();
        private readonly object _lockObject = new object();
        private static int _mainThreadId;
        private volatile bool _isProcessing = false;
        private volatile bool _isInitialized = false;
        public void Initialize()
        {
            if (_isInitialized) return;
            
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            
            // 注册EditorApplication.update作为队列处理机制
            //打印日志
            McpLogger.LogDebug($"🔄 初始化 EditorApplication.update += ProcessQueue; - 主线程ID: {_mainThreadId}");
            EditorApplication.update += ProcessQueue;
            _isInitialized = true;
            //启动server
            McpServerWindow.StartServerStatic();
        }
        
        public void Enqueue(Action action)
        {
            if (action == null) return;
            
            // 将任务加入队列
            _actionQueue.Enqueue(action);
            McpLogger.LogDebug($"📝 任务已入队 - 队列长度: {_actionQueue.Count}, 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            
            // 立即触发播放器循环更新，强制处理队列
        
            // 触发更新回调
            try
            {
                EditorApplication.update -= ProcessQueue;
                EditorApplication.update += ProcessQueue;
                // EditorApplication.update?.Invoke();
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "🔁 Enqueue  触发更新回调失败");
                
                // bool completed = false;
                // // 同时使用update回调作为备用机制
                // EditorApplication.CallbackFunction updateCallback = null;
                // updateCallback = () => {
                //     if (!completed)
                //     {
                //         try
                //         {
                //             UnityEngine.Debug.Log($"[TemporaryMainThreadDispatcher] 🔁 Enqueue  Update回调执行 - 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                //             // action();
                //             completed = true;
                //         }
                //         catch (Exception ex)
                //         {
                //             UnityEngine.Debug.LogError($"[TemporaryMainThreadDispatcher] 🔁 Enqueue  Update回调执行失败: {ex.Message}");
                //             completed = true;
                //         }
                //     }
                //     EditorApplication.update -= updateCallback;
                // };
                // EditorApplication.update += updateCallback;
            }
            // //打印update队列长度
            // UnityEngine.Debug.Log($"[EditorMainThreadDispatcher] 🔁 Enqueue  Update回调已添加 - 队列长度: {_actionQueue.Count}, 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        }
        
        private void OnDestroy()
        {
            // 移除EditorApplication回调
            EditorApplication.update -= ProcessQueue;
            
            McpLogger.LogDebug("🧹 资源已清理");
        }
        
        public void ProcessQueue()
        {
            // 防止重入
            if (_isProcessing) return;
            //打印日志 
            // UnityEngine.Debug.Log($"[EditorMainThreadDispatcher] 🔁 ProcessQueue 开始处理队列 - 队列长度: {_actionQueue.Count}, 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            try
            {
                _isProcessing = true;
                
                // 执行队列中的所有操作，限制每帧处理的操作数量
                const int maxActionsPerFrame = 100; // 增加处理数量
                int actionsProcessed = 0;
                
                while (_actionQueue.TryDequeue(out Action action) && actionsProcessed < maxActionsPerFrame)
                {
                    try
                    {
                        //打印日志记录执行的任务剩余队列和线程id
                        McpLogger.LogDebug($"📋 队列处理中 - 剩余任务: {_actionQueue.Count}, 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogException(ex, "执行队列操作时发生异常");
                    }
                    actionsProcessed++;
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }
        
        // 强制处理队列，用于EnqueueAndWait
        private void ForceProcessQueue()
        {
            if (!UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                return;
                
            ProcessQueue();
        }
        
        public T EnqueueAndWait<T>(Func<T> func)
        {
            if (func == null) return default(T);
            
            var result = ExecuteWithQueue(() => func());
            return (T)result.Result;
        }
        
        public void EnqueueAndWait(Action action)
        {
            if (action == null) return;
            
            ExecuteWithQueue(() => 
            {
                action();
                return null;
            });
        }
        
        /// <summary>
        /// 执行结果类型
        /// </summary>
        private class ExecutionResult
        {
            public object Result { get; set; }
            public Exception Exception { get; set; }
            public bool IsCompleted { get; set; }
        }
        
        /// <summary>
        /// 执行队列任务的通用方法，提取公共逻辑
        /// </summary>
        private ExecutionResult ExecuteWithQueue(Func<object> func)
        {
            var executionResult = new ExecutionResult();
            
            // 使用多重检查机制确保主线程检测的可靠性
            bool isMainThread = false;
            try
            {
                // 首先尝试使用Unity的主线程检查API
                try
                {
                    isMainThread = UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread();
                }
                catch (Exception)
                {
                    // 如果Unity API失败，回退到线程ID比较
                    isMainThread = System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;
                }
            }
            catch (Exception ex)
            {
                isMainThread = false;
            }
            
            // 注意：即使在主线程，某些Unity API在特定时机（如场景加载）也不能直接调用
            // 为了确保线程安全和API调用的正确性，统一使用队列机制
            McpLogger.LogDebug($"⚡ 队列执行 - 主线程: {isMainThread}, 队列任务: {_actionQueue.Count}, 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            
            Enqueue(() =>
            {
                try
                {
                    McpLogger.LogDebug($"🎯 队列任务执行 - 待处理: {_actionQueue.Count}, 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                    executionResult.Result = func();
                }
                catch (Exception ex)
                {
                    executionResult.Exception = ex;
                }
                finally
                {
                    executionResult.IsCompleted = true;
                }
            });
            
            // 使用混合等待策略：先强制处理队列，再轮询等待
            int waitTime = 0;
            const int maxWaitTime = 10000; // 10秒超时
            const int sleepInterval = 5; // 5毫秒间隔
            const int forceProcessInterval = 100; // 每100ms强制处理一次队列
            
            while (!executionResult.IsCompleted && waitTime < maxWaitTime)
            {
                // 定期强制处理队列
                if (waitTime % forceProcessInterval == 0)
                {
                    try
                    {
                        EditorApplication.QueuePlayerLoopUpdate();
                        // 强制触发update回调来处理队列
                        if (EditorApplication.update != null)
                        {
                            EditorApplication.update.Invoke();
                        }
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogWarning($"强制处理队列时发生异常: {ex.Message}");
                    }
                }
                
                SafeSleep(sleepInterval);
                waitTime += sleepInterval;
            }
            
            if (!executionResult.IsCompleted)
            {
                McpLogger.LogError($"ExecuteWithQueue 超时，队列中还有 {_actionQueue.Count} 个待处理项目");
                throw new TimeoutException($"Operation timed out after {maxWaitTime}ms, queue count: {_actionQueue.Count}");
            }
            
            if (executionResult.Exception != null)
            {
                throw executionResult.Exception;
            }
            
            return executionResult;
        }
        
        /// <summary>
        /// 安全的Sleep方法，可以在线程中止时快速退出
        /// </summary>
        private static void SafeSleep(int milliseconds)
        {
            try
            {
                // 将长时间的Sleep分解为多个短时间的Sleep，以便快速响应中止
                const int chunkSize = 100; // 每次Sleep 100ms
                int remaining = milliseconds;
                
                while (remaining > 0)
                {
                    int sleepTime = Math.Min(remaining, chunkSize);
                    System.Threading.Thread.Sleep(sleepTime);
                    remaining -= sleepTime;
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                // 线程被中止时，直接退出
                throw;
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"SafeSleep异常: {ex.Message}");
            }
        }
    }
}