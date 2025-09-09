# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

# Changelog

## [1.9.118] - 2025-01-13
### Fixed
- 优先尝试调用 GetEntryInternal(int, LogEntry/object/ref LogEntry) 双参重载，避免其它不兼容重载导致的参数数量不匹配
- 兼容不同Unity版本的字段命名（message/condition、instanceID/instanceId）并稳健提取
- 调整成功短路逻辑，避免重复尝试引发异常
- 增强日志：记录双参重载尝试与结果，便于后续诊断

## [1.9.117] - 2025-01-13
### Improved
- 增强UnityLogTools.cs中反射调用的调试信息
- 添加详细的方法签名和参数类型输出
- 改进参数创建和调用过程的日志记录
- 优化错误诊断和问题定位能力

## [1.9.116] - 2025-01-13
### Fixed
- 修复UnityLogTools.cs中引用参数初始化问题
- 改进ByRef参数的类型处理和默认值创建
- 增强参数类型检查，支持值类型的动态实例化
- 优化反射调用的参数兼容性

## [1.9.115] - 2025-01-13
### Fixed
- 修复UnityLogTools.cs中反射调用参数数量不匹配问题
- 改进GetEntryInternal方法的动态参数适配
- 增强参数类型检查和错误处理
- 优化日志获取的兼容性

## [1.9.114] - 2025-01-13
### Fixed
- 修复UnityLogTools.cs中变量声明顺序错误
- 解决'getEntryInternalMethod'变量在声明前被使用的编译错误
- 调整变量声明位置，确保作用域正确调整变量声明位置确保正确的作用域

## [1.9.113] - 2025-01-13
### Fixed
- 修复UnityLogTools.cs中变量名冲突编译错误
- 解决'parameters'变量在嵌套作用域中重复定义的问题

## [1.9.112] - 2025-01-13
### Fixed
- 修复get_unity_logs反射调用参数数量不匹配问题
- 改进GetEntryInternal方法的动态查找和调用逻辑
- 增强日志获取方法的兼容性和错误处理
- 添加详细的方法参数调试信息

## [1.9.111] - 2025-01-13
### Fixed
- 修复控制面板版本号显示问题
- 更新LoadVersion方法从package.json读取正确版本号
- 改进版本号获取路径逻辑

## [1.9.110] - 2025-01-13
### Fixed
- 进一步修复get_unity_logs日志内容获取问题
- 实现多种反射方法获取Unity日志内容
- 添加LogEntry结构体支持和ref参数调用
- 增强日志获取的兼容性和稳定性

## [1.9.109] - 2025-01-13
### Fixed
- 修复get_unity_logs工具返回空日志数组的问题
- 改进Unity Console日志获取逻辑，修复GetEntryInternal方法调用方式
- 添加自动生成测试日志功能，确保Console中有内容可供获取
- 增强日志获取的调试信息和错误处理机制
- 添加Console刷新和重试逻辑，提高日志获取成功率

## [1.9.108] - 2025-01-13
### Fixed
- 修复MCP服务器在播放模式切换时停止不自动重启的问题
- 添加服务器状态记录和恢复机制
- 实现带重试机制的服务器启动方法
- 改进OnEnable中的服务器自动启动逻辑
- 确保服务器在Unity编辑器状态变化时保持运行

## [1.9.107] - 2025-01-13
### Fixed
- 改进日志查询工具的调试功能
- 在UnityLogTools.GetUnityLogs方法中添加详细的调试日志
- 在McpServerWindow中添加"测试日志查询"按钮，方便诊断日志查询问题
- 优化空日志情况的处理，返回更友好的提示信息

## [1.9.106] - 2025-01-13
### Fixed
- 修复McpServerWindow中DrawClientConfigSection方法的空引用异常，添加了_server空值检查

## [1.9.105] - 2025-01-13
### Fixed
- 修复编辑器工具返回类型错误：RefreshEditor和GetEditorStatus方法现在直接返回McpToolResult

## [1.9.104] - 2025-01-13
### Fixed
- 修复编辑器工具参数传递错误：RefreshEditor和GetEditorStatus方法现在正确接收JObject参数

## [1.9.103] - 2025-01-13
### Added
- 新增编辑器工具分类，包含强制刷新Unity编辑器界面的功能
- 添加refresh_editor工具：支持强制刷新Scene视图、Hierarchy、Project窗口、Inspector等编辑器界面
- 添加get_editor_status工具：获取Unity编辑器状态和界面状态信息
- 在McpServerWindow中添加编辑器工具配置界面

## [1.9.102] - 2025-01-08
### Fixed
- 修复McpServerWindow中GUI布局错误，移除脚本管理工具中重复的compile_scripts工具
- compile_scripts工具现在仅在资源管理工具分类中显示，避免重复GUI元素导致的布局错误

## [1.9.101] - 2025-01-08
### Fixed
- 修复UnityToolsMain.cs中重复的CompileScripts方法定义编译错误
- 保留资源管理区域的CompileScripts方法，移除脚本管理区域的重复定义

## [1.9.100] - 2025-01-13
### Fixed
- 修复 McpServerWindow 中的 GUI 布局错误 "Invalid GUILayout state"
- 移除 OnEnable 方法中重复的 StartServer() 调用
- 更新资源管理工具配置，包含新增的 refresh_assets、compile_scripts、wait_for_compilation 工具
- 添加新工具的中文名称映射

## [1.9.99] - 2025-01-13
### Added
- 新增 refresh_assets 工具来强制刷新Unity资源数据库
- 新增 compile_scripts 工具来触发Unity脚本编译
- 新增 wait_for_compilation 工具来等待编译完成

### Improved
- 改进 add_component 工具的错误处理和重试机制
- 添加多种游戏对象查找方式（按名称、路径、标签）
- 增强组件类型解析，支持常见组件名称映射
- 添加重试参数支持（maxRetries 和 retryDelay）

## [1.9.97] - 2025-01-07
### Fixed
- 修复 McpServerWindow 中的 GUI 布局错误
- 解决 "Invalid GUILayout state" 错误，确保所有 Begin/End 调用匹配
- 清理多余的空行和格式问题

## [1.9.96] - 2025-01-07
### Fixed
- 修复 CHANGELOG.md.meta 文件的 YAML 格式问题
- 确保 assetBundleVariant 字段有正确的空值格式

## [1.9.95] - 2024-12-19

### Changed
- Renamed package directory from `unityMcp-Trae` to `unity-ai-mcp-trae` for better naming convention
- Updated package name from `com.unity.mcp-server` to `com.unity.ai-mcp-trae`

## [1.9.94] - 2024-12-19

### Added
- Comprehensive API Reference documentation
- Quick Start Guide for new users
- Example scene with McpExampleController script
- Sample scripts demonstrating MCP integration
- Complete documentation package for Asset Store submission

### Improved
- Enhanced documentation structure
- Better user onboarding experience
- More comprehensive examples and tutorials

## [1.9.93] - 2024-12-19

### Added
- Complete MCP (Model Context Protocol) server implementation
- 64+ Unity automation tools covering 13 major modules
- HTTP API with RESTful interface
- Main thread dispatcher for Unity API safety
- Tool configuration system with enable/disable functionality
- Comprehensive logging and error handling
- Unity 2023.2+ compatibility
- Documentation and Tests directories
- Third-party notices and licensing information

### Features
- **Scene Management**: List, open, and load scenes
- **Play Mode Control**: Start/stop play mode with status monitoring
- **GameObject Operations**: Create, find, delete, duplicate, and transform objects
- **Component Management**: Add, remove, and configure components
- **Material & Rendering**: Create materials and configure renderer properties
- **Physics System**: Rigidbody, collider, and force management
- **Audio System**: Audio playback and property configuration
- **Lighting System**: Light creation and property management
- **Script Management**: Script creation, modification, and compilation
- **UI System**: Canvas and UI element creation with event binding
- **Animation System**: Animator setup and animation control
- **Input System**: Input action setup and event binding
- **Particle System**: Particle effect creation and management
- **Asset Management**: Asset import and management
- **Logging System**: Unity log access and management

### Technical
- Unity 2023.2.20f1 compatibility
- Newtonsoft.Json 3.2.1 integration
- Assembly definition files for proper module separation
- Editor-only execution for security
- Async operation support with proper threading

### Documentation
- Comprehensive README with usage examples
- API documentation for all 64+ tools
- Installation and setup instructions
- Troubleshooting guide
- MCP protocol examples

## [1.0.0] - 2024-12-01

### Added
- Initial release
- Basic MCP server functionality
- Core Unity automation tools
- HTTP API foundation