# Unity MCP Trae

一个Unity编辑器插件，实现了Model Context Protocol (MCP)服务器，专为Trae AI集成优化，允许外部AI工具通过标准化的API控制Unity编辑器。

## 功能特性

- **MCP协议兼容**: 完全符合Model Context Protocol规范
- **Trae AI优化**: 专为Trae AI工作流程优化的接口设计
- **HTTP API**: 提供RESTful接口，支持跨平台访问
- **主线程安全**: 所有Unity API调用都在主线程中执行
- **丰富的工具集**: 提供场景管理、GameObject操作、组件添加等功能
- **编辑器集成**: 提供友好的Unity编辑器窗口界面

## 系统要求

- **Unity版本**: 2023.2.20f1 或更高版本
- **平台**: Windows, macOS, Linux
- **依赖项**: 
  - `com.unity.nuget.newtonsoft-json`: 3.2.1

## 安装方法

### 方法1: 本地包安装
1. 将此包复制到Unity项目的`Packages`文件夹中
2. Unity会自动检测并导入包

### 方法2: Package Manager安装
1. 打开Unity Package Manager (Window > Package Manager)
2. 点击左上角的"+"按钮
3. 选择"Add package from disk..."
4. 选择包根目录下的`package.json`文件

### 验证安装
安装完成后，可以通过以下方式验证：
1. 在Unity编辑器菜单中查看 `Tools > CLH-MCP > 验证Unity 2023.2兼容性`
2. 运行兼容性检查确保所有功能正常

## 使用方法

### 启动服务器

1. 在Unity编辑器中，选择菜单 `Tools > CLH-MCP > MCP Server`
2. 在打开的窗口中点击"Start Server"
3. 服务器将在指定端口启动（默认8080）
4. 可以启用"Auto Start"选项在编辑器启动时自动启动服务器

### 连接到服务器

服务器启动后，MCP客户端可以连接到：
```
http://localhost:8080/mcp
```

## 可用工具

### 场景管理

#### `list_scenes`
列出项目中的所有场景
```json
{
  "method": "tools/call",
  "params": {
    "name": "list_scenes",
    "arguments": {}
  }
}
```

#### `open_scene`
打开指定的场景
```json
{
  "method": "tools/call",
  "params": {
    "name": "open_scene",
    "arguments": {
      "path": "Assets/Scenes/MainScene.unity"
    }
  }
}
```

### 播放模式控制

#### `play_mode_start`
启动Unity播放模式
```json
{
  "method": "tools/call",
  "params": {
    "name": "play_mode_start",
    "arguments": {}
  }
}
```

#### `play_mode_stop`
停止Unity播放模式
```json
{
  "method": "tools/call",
  "params": {
    "name": "play_mode_stop",
    "arguments": {}
  }
}
```

### GameObject操作

#### `create_gameobject`
创建新的GameObject
```json
{
  "method": "tools/call",
  "params": {
    "name": "create_gameobject",
    "arguments": {
      "name": "MyGameObject",
      "parent": "ParentObject" // 可选
    }
  }
}
```

#### `add_component`
为GameObject添加组件
```json
{
  "method": "tools/call",
  "params": {
    "name": "add_component",
    "arguments": {
      "gameObject": "MyGameObject",
      "component": "Rigidbody"
    }
  }
}
```

#### `set_transform`
设置GameObject的变换属性
```json
{
  "method": "tools/call",
  "params": {
    "name": "set_transform",
    "arguments": {
      "gameObject": "MyGameObject",
      "position": {"x": 0, "y": 1, "z": 0},
      "rotation": {"x": 0, "y": 90, "z": 0},
      "scale": {"x": 1, "y": 1, "z": 1}
    }
  }
}
```

### 资源管理

#### `import_asset`
导入资源到项目中
```json
{
  "method": "tools/call",
  "params": {
    "name": "import_asset",
    "arguments": {
      "path": "Assets/Models/character.fbx"
    }
  }
}
```

## MCP协议示例

### 初始化连接
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {
      "name": "My MCP Client",
      "version": "1.0.0"
    }
  }
}
```

### 获取工具列表
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/list",
  "params": {}
}
```

## 安全注意事项

- 服务器仅绑定到localhost，不接受外部网络连接
- 建议仅在受信任的环境中使用
- 所有操作都会记录到Unity控制台
- 支持Unity的Undo系统，可以撤销大部分操作

## Unity 2023.2 兼容性

本插件已针对Unity 2023.2.20f1进行了优化和测试：

### 新特性支持
- ✅ 支持Unity 2023.2的新版本Newtonsoft.Json (3.2.1)
- ✅ 兼容Unity 2023.2的编辑器API变更
- ✅ 支持新的程序集定义系统
- ✅ 优化了Burst编译器兼容性

### 测试验证
使用内置的兼容性检查工具验证安装：
```
Tools > CLH-MCP > 验证Unity 2023.2兼容性
Tools > CLH-MCP > 测试MCP服务器启动
```

### 已知问题
- 无已知兼容性问题
- 所有核心功能在Unity 2023.2中正常工作

## 故障排除

### 服务器无法启动
- 检查端口是否被其他程序占用
- 确保防火墙允许本地连接
- 运行兼容性检查确认环境配置

### 编译错误
- 确保Unity版本为2023.2或更高
- 检查Newtonsoft.Json包版本是否为3.2.1
- 运行 `Tools > CLH-MCP > 验证Unity 2023.2兼容性` 进行诊断
- 查看Unity控制台的错误信息

### 工具调用失败
- 确保在Unity编辑器中执行（某些功能仅在编辑器中可用）
- 检查参数格式是否正确
- 查看Unity控制台的详细错误信息

## 开发和扩展

要添加新的工具：

1. 在`UnityTools.cs`中实现工具逻辑
2. 在`McpServer.cs`的`RegisterDefaultTools()`方法中注册工具
3. 更新工具定义和输入模式

## 许可证

本项目采用MIT许可证。详见LICENSE文件。