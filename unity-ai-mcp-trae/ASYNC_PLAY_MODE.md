# 异步播放模式功能

## 概述

本次更新将Unity MCP服务器中的播放模式控制功能从同步执行改为异步执行，提高了响应性能和用户体验。

## 修改内容

### 1. 新增异步执行方法

在 `McpServer.cs` 中添加了 `ExecuteOnMainThreadAsync` 方法：

- **功能**: 将操作排队到主线程异步执行，不等待执行完成
- **返回**: 立即返回成功状态，表示命令已成功排队
- **优势**: 避免阻塞MCP客户端，提高响应速度

### 2. 更新播放模式工具注册

修改了以下MCP工具的执行方式：

- `play_mode_start`: 实现智能状态检查，先检测当前播放状态，如果已在播放则立即返回，如果未播放则异步启动
- `play_mode_stop`: 从 `ExecuteOnMainThread` 改为 `ExecuteOnMainThreadAsync`

### 3. 保持同步的工具

以下工具仍使用同步执行，因为需要立即返回状态信息：

- `get_play_mode_status`: 需要返回当前播放模式状态

## 使用效果

### 异步执行前
- MCP客户端调用播放模式切换时需要等待Unity完成切换
- 可能出现超时或阻塞情况
- 响应时间较长

### 异步执行后
- **play_mode_start**: 如果已在播放模式，立即返回"Play mode is already active"；如果未播放，立即返回"Command queued for async execution"并在后台启动
- **play_mode_stop**: MCP客户端立即收到"Command queued for async execution"响应，Unity在后台完成停止操作
- 响应时间大幅缩短
- 避免客户端超时问题
- 智能状态检查避免重复操作

## 技术实现

### 1. 智能状态检查 (play_mode_start)

```csharp
// 先检查当前播放模式状态
if (isMainThread)
{
    // 如果在主线程，直接检查状态
    if (EditorApplication.isPlaying)
    {
        return new McpToolResult
        {
            Content = new List<McpContent>
            {
                new McpContent { Type = "text", Text = "Play mode is already active" }
            }
        };
    }
    
    // 如果没有播放，异步启动
    return ExecuteOnMainThreadAsync(() => UnityTools.StartPlayMode(args));
}
```

### 2. 异步执行方法核心逻辑

```csharp
// 异步执行方法核心逻辑
dispatcher.Enqueue(() => {
    try
    {
        func(); // 执行实际操作
    }
    catch (Exception ex)
    {
        UnityEngine.Debug.LogError($"[McpServer] Async execution failed: {ex.Message}");
    }
});

// 立即返回，不等待执行完成
return new McpToolResult
{
    Content = new List<McpContent>
    {
        new McpContent { Type = "text", Text = "Command queued for async execution" }
    }
};
```

## 注意事项

1. **状态检查**: 如果需要确认播放模式是否已切换，应使用 `get_play_mode_status` 工具
2. **错误处理**: 异步执行中的错误会记录到Unity控制台，但不会返回给MCP客户端
3. **线程安全**: 所有操作仍在Unity主线程中执行，保证线程安全
4. **队列处理**: EditorMainThreadDispatcher已启用EditorApplication.update队列处理，确保异步任务能够及时执行，无需等待Unity界面激活

## 测试验证

在 `McpTestWindow.cs` 中更新了播放模式测试，添加了异步执行的说明和验证。测试时会显示：

- 当前播放模式状态
- 异步执行的提示信息
- 同步测试结果（用于验证底层功能正常）
- 异步功能实现确认

## 兼容性

- 此更改对现有MCP客户端完全兼容
- 客户端代码无需修改
- 只是改变了执行方式，API接口保持不变