# MCP Desktop Client - Unity MCP服务器测试工具

这是一个用于测试Unity MCP服务器的桌面客户端应用程序，可以方便地调用和测试各种MCP工具。

## 功能特性

- **连接管理**: 连接到Unity MCP服务器
- **工具发现**: 自动获取服务器上可用的工具列表
- **参数配置**: 通过JSON格式配置工具参数
- **预设管理**: 保存和加载常用的工具调用预设
- **结果显示**: 清晰显示工具执行结果

## 使用方法

### 1. 启动应用程序

```bash
dotnet run
```

或者构建后运行可执行文件：

```bash
dotnet build
.\bin\Debug\net6.0-windows\McpDesktopClient.exe
```

### 2. 连接到MCP服务器

1. 在"服务器地址"输入框中输入Unity MCP服务器的地址（默认：`http://localhost:9123/mcp`）
2. 点击"连接"按钮
3. 连接成功后会自动获取可用工具列表

### 3. 调用工具

1. 在"可用工具"列表中选择要调用的工具
2. 在"参数"文本框中输入JSON格式的参数
3. 点击"调用工具"按钮
4. 在"执行结果"区域查看返回结果

### 4. 使用预设功能

#### 加载预设
- 在参数文本框上右键点击
- 选择"加载预设"
- 从列表中选择要使用的预设

#### 保存预设
- 配置好工具和参数后
- 在参数文本框上右键点击
- 选择"保存为预设"
- 输入预设名称

#### 管理预设
- 在参数文本框上右键点击
- 选择"显示所有预设"
- 可以编辑、删除现有预设

## 内置预设

应用程序包含以下预设的Unity工具测试用例：

- **获取场景信息**: 获取当前Unity场景的基本信息
- **列出场景对象**: 列出场景中的所有游戏对象
- **创建场景对象**: 在场景中创建新的游戏对象
- **修改对象属性**: 修改游戏对象的位置、旋转等属性
- **添加组件**: 为游戏对象添加组件（如Rigidbody）
- **删除场景对象**: 从场景中删除指定对象
- **创建材质**: 创建新的材质资源
- **应用材质**: 将材质应用到游戏对象
- **保存场景**: 保存当前场景
- **加载场景**: 加载指定场景
- **执行脚本**: 在Unity编辑器中执行C#代码
- **获取项目设置**: 获取Unity项目设置

## 参数格式示例

### 创建游戏对象
```json
{
  "name": "TestCube",
  "position": {
    "x": 0,
    "y": 0,
    "z": 0
  },
  "rotation": {
    "x": 0,
    "y": 0,
    "z": 0
  },
  "scale": {
    "x": 1,
    "y": 1,
    "z": 1
  }
}
```

### 修改对象属性
```json
{
  "object_name": "TestCube",
  "properties": {
    "position": {
      "x": 5,
      "y": 2,
      "z": 0
    },
    "active": true
  }
}
```

### 添加组件
```json
{
  "object_name": "TestCube",
  "component_type": "Rigidbody",
  "properties": {
    "mass": 1.0,
    "useGravity": true
  }
}
```

## 技术架构

- **框架**: .NET 6.0 Windows Forms
- **通信协议**: JSON-RPC over HTTP
- **JSON处理**: Newtonsoft.Json
- **预设存储**: 本地JSON文件

## 项目结构

```
McpDesktopClient/
├── Models/
│   └── McpModels.cs          # MCP数据模型
├── Services/
│   ├── McpClientService.cs   # MCP通信服务
│   └── TestPresetManager.cs  # 预设管理服务
├── Forms/
│   ├── PresetSelectionForm.cs    # 预设选择窗体
│   └── PresetManagementForm.cs   # 预设管理窗体
├── TestPresets/
│   └── UnityMcpTestPresets.json  # 预设配置文件
├── MainForm.cs               # 主窗体
├── Program.cs                # 程序入口
└── McpDesktopClient.csproj   # 项目文件
```

## 故障排除

### 连接失败
- 确保Unity MCP服务器正在运行
- 检查服务器地址是否正确
- 确认防火墙设置允许连接

### 工具调用失败
- 检查参数JSON格式是否正确
- 确认工具名称拼写正确
- 查看错误信息了解具体原因

### 预设加载失败
- 检查预设文件是否存在
- 确认JSON格式正确
- 重新创建预设文件

## 开发说明

如需扩展功能，可以：

1. 在`Models`目录添加新的数据模型
2. 在`Services`目录添加新的服务类
3. 创建新的窗体处理特定功能
4. 修改`TestPresets`配置文件添加新预设

## 许可证

本项目遵循MIT许可证。