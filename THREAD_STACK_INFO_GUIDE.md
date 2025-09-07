# Unity线程栈信息工具使用指南

## 概述

`get_thread_stack_info` 是一个强大的Unity MCP工具，专门用于获取Unity编辑器的详细线程栈信息并检测潜在的死锁情况。该工具为Unity开发者提供了深入的线程分析能力，帮助诊断和解决多线程相关的问题。

## 功能特性

### 🔍 线程信息收集
- **活动线程枚举**: 获取所有当前活动的线程信息
- **线程状态分析**: 显示每个线程的运行状态（运行中、等待、阻塞等）
- **调用栈追踪**: 提供详细的线程调用栈信息
- **线程优先级**: 显示线程优先级设置

### ⚠️ 死锁检测
- **自动死锁分析**: 智能检测潜在的死锁情况
- **阻塞线程识别**: 识别长时间阻塞的线程
- **资源竞争分析**: 分析线程间的资源竞争情况
- **风险评估**: 提供死锁风险等级评估

### 🎯 Unity特定功能
- **主线程识别**: 特别标识Unity主线程
- **编辑器模式优化**: 在Unity编辑器中提供更详细的信息
- **性能影响最小**: 设计为低开销的诊断工具

## 使用方法

### 1. 通过桌面客户端使用

#### 快捷按钮方式
1. 启动Unity MCP桌面客户端
2. 连接到Unity MCP服务器
3. 点击 **"线程栈信息"** 按钮
4. 查看结果面板中的详细线程信息

#### 工具列表方式
1. 在工具列表中选择 `get_thread_stack_info`
2. 参数框保持为空（该工具无需参数）
3. 点击 **"调用工具"** 按钮
4. 查看详细的线程栈信息

### 2. 通过HTTP API使用

```bash
curl -X POST http://localhost:3000/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "get_thread_stack_info",
    "arguments": {}
  }'
```

### 3. 通过Python脚本使用

```python
import requests

response = requests.post(
    "http://localhost:3000/tools/call",
    json={
        "name": "get_thread_stack_info",
        "arguments": {}
    }
)

result = response.json()
print(result)
```

## 输出示例

### 正常情况输出
```
=== Unity线程栈信息分析 ===
分析时间: 2024-01-15 14:30:25

🧵 活动线程信息:

[线程 1] Unity主线程 (ID: 12345)
状态: Running
优先级: Normal
调用栈:
  at UnityEngine.Application.Update()
  at UnityEditor.EditorApplication.Internal_CallUpdateFunctions()
  at UnityEditor.EditorApplication.UpdateMainThread()

[线程 2] 后台工作线程 (ID: 12346)
状态: WaitSleepJoin
优先级: BelowNormal
调用栈:
  at System.Threading.Thread.Sleep(Int32)
  at UnityEditor.AssetDatabase.Refresh()

⚡ 死锁检测结果:
✅ 未检测到死锁风险
✅ 所有线程状态正常
✅ 无长时间阻塞的线程

📊 线程统计:
- 总线程数: 8
- 活动线程: 2
- 等待线程: 6
- 阻塞线程: 0
```

### 检测到问题时的输出
```
=== Unity线程栈信息分析 ===
分析时间: 2024-01-15 14:35:10

🧵 活动线程信息:

[线程 1] Unity主线程 (ID: 12345)
状态: WaitSleepJoin
优先级: Normal
⚠️ 警告: 主线程被阻塞超过5秒
调用栈:
  at System.Threading.Monitor.Wait(Object)
  at CustomScript.WaitForResource()
  at UnityEngine.MonoBehaviour.Update()

[线程 3] 资源加载线程 (ID: 12347)
状态: WaitSleepJoin
优先级: Normal
⚠️ 警告: 线程等待时间过长
调用栈:
  at System.Threading.Monitor.Wait(Object)
  at ResourceManager.LoadAsset(String)

🚨 死锁检测结果:
❌ 检测到潜在死锁风险!
❌ 线程 1 和线程 3 可能存在循环等待
⚠️ 建议检查资源锁定逻辑

📊 线程统计:
- 总线程数: 10
- 活动线程: 1
- 等待线程: 7
- 阻塞线程: 2
```

## 应用场景

### 🐛 性能调试
- 诊断Unity编辑器响应缓慢的问题
- 识别CPU密集型操作的线程
- 分析异步操作的执行情况

### 🔒 死锁排查
- 检测多线程代码中的死锁问题
- 分析资源竞争情况
- 预防潜在的线程阻塞

### 📈 性能优化
- 了解线程资源使用情况
- 优化线程池配置
- 改进多线程架构设计

### 🧪 开发测试
- 验证多线程代码的正确性
- 测试并发场景下的稳定性
- 监控长时间运行的后台任务

## 注意事项

### ⚡ 性能考虑
- 该工具会遍历所有活动线程，在线程数量很多时可能需要几秒钟
- 建议在需要时才使用，避免频繁调用
- 在生产环境中谨慎使用

### 🔒 权限要求
- 需要在Unity编辑器环境中运行
- 某些系统线程信息可能受到访问限制
- 在某些平台上可能无法获取完整的调用栈

### 🎯 准确性说明
- 线程状态是瞬时快照，可能在获取过程中发生变化
- 死锁检测基于启发式算法，可能存在误报或漏报
- 调用栈信息的详细程度取决于编译配置

## 故障排除

### 问题：工具调用失败
**可能原因**:
- Unity MCP服务器未运行
- 网络连接问题
- Unity编辑器未启动

**解决方案**:
1. 确认Unity编辑器正在运行
2. 检查MCP服务器状态
3. 验证网络连接

### 问题：获取的线程信息不完整
**可能原因**:
- 系统权限限制
- 平台兼容性问题
- Unity版本差异

**解决方案**:
1. 以管理员权限运行Unity
2. 检查Unity版本兼容性
3. 查看Unity控制台的错误信息

### 问题：死锁检测不准确
**可能原因**:
- 复杂的线程交互模式
- 动态创建的线程
- 第三方插件的影响

**解决方案**:
1. 多次运行工具进行对比
2. 结合Unity Profiler进行分析
3. 手动检查可疑的代码段

## 相关工具

- **get_thread_info**: 获取基础线程信息（更轻量级）
- **get_current_scene_info**: 获取当前场景信息
- **Unity Profiler**: Unity内置的性能分析工具
- **Visual Studio诊断工具**: 外部线程调试工具

## 更新日志

### v1.0.0 (2024-01-15)
- ✨ 初始版本发布
- 🔍 基础线程信息收集功能
- ⚠️ 死锁检测算法实现
- 🎯 Unity编辑器集成
- 📱 桌面客户端快捷按钮支持

---

**提示**: 如果您在使用过程中遇到问题或有改进建议，请查看Unity控制台的详细错误信息，或联系开发团队获取支持。