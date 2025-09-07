# Unity MCP Trae - Quick Start Guide

## What is Unity MCP Trae?

Unity MCP Trae is a powerful Unity Editor plugin that implements the Model Context Protocol (MCP) server, enabling external AI tools (especially Trae AI) to control Unity Editor through a standardized API.

## Key Benefits

- **AI-Powered Automation**: Let AI assistants help you build Unity projects
- **64+ Built-in Tools**: Comprehensive coverage of Unity Editor functionality
- **Safe & Secure**: All operations run in Unity Editor with proper threading
- **Easy Integration**: Simple HTTP API with JSON-RPC protocol
- **Trae AI Optimized**: Specially designed for Trae AI workflows

## Quick Installation

### Step 1: Install the Package

1. Download the Unity MCP Trae package
2. In Unity, open **Window > Package Manager**
3. Click the **+** button and select **Add package from disk...**
4. Navigate to the package folder and select `package.json`
5. Unity will automatically import the package

### Step 2: Verify Installation

1. Check that **Tools > CLH-MCP** menu appears in Unity
2. Run **Tools > CLH-MCP > 验证Unity 2023.2兼容性** to verify compatibility
3. All checks should pass ✅

## First Steps

### 1. Start the MCP Server

1. Open **Tools > CLH-MCP > MCP Server**
2. Click **Start Server** button
3. Server will start on `http://localhost:8080/mcp`
4. Enable **Auto Start** for automatic startup

### 2. Test Basic Functionality

1. Open **Tools > CLH-MCP > MCP Test Window**
2. Try the "List Scenes" test
3. You should see your project scenes listed

### 3. Configure Tool Settings

1. In the MCP Server window, click **显示配置**
2. Enable/disable specific tools as needed
3. All 64+ tools are enabled by default

## Common Use Cases

### Scene Setup Automation

```json
// Create a new GameObject
{
  "method": "tools/call",
  "params": {
    "name": "create_gameobject",
    "arguments": {
      "name": "Player"
    }
  }
}

// Add components
{
  "method": "tools/call",
  "params": {
    "name": "add_component",
    "arguments": {
      "gameObject": "Player",
      "component": "Rigidbody"
    }
  }
}

// Set transform
{
  "method": "tools/call",
  "params": {
    "name": "set_transform",
    "arguments": {
      "gameObject": "Player",
      "position": {"x": 0, "y": 1, "z": 0}
    }
  }
}
```

### Material Creation

```json
// Create material
{
  "method": "tools/call",
  "params": {
    "name": "create_material",
    "arguments": {
      "name": "PlayerMaterial",
      "shader": "Standard"
    }
  }
}

// Set material properties
{
  "method": "tools/call",
  "params": {
    "name": "set_material_properties",
    "arguments": {
      "material": "PlayerMaterial",
      "properties": {
        "_Color": {"r": 1, "g": 0, "b": 0, "a": 1}
      }
    }
  }
}
```

### Play Mode Control

```json
// Start play mode
{
  "method": "tools/call",
  "params": {
    "name": "play_mode_start",
    "arguments": {}
  }
}

// Check status
{
  "method": "tools/call",
  "params": {
    "name": "get_play_mode_status",
    "arguments": {}
  }
}

// Stop play mode
{
  "method": "tools/call",
  "params": {
    "name": "play_mode_stop",
    "arguments": {}
  }
}
```

## Integration with Trae AI

### 1. Configure Trae AI

1. In MCP Server window, click **推送到Trae AI**
2. This automatically configures Trae AI to use the MCP server
3. Restart Trae AI to apply configuration

### 2. Use with Trae AI

Once configured, you can ask Trae AI to:

- "Create a player character with Rigidbody and Collider"
- "Set up a basic scene with lighting"
- "Create a UI canvas with buttons"
- "Import and configure materials"
- "Set up particle effects"

## Available Tool Categories

| Category | Tools | Description |
|----------|-------|-------------|
| **Scene Management** | 3 tools | List, open, load scenes |
| **Play Mode** | 3 tools | Start, stop, status |
| **GameObjects** | 7 tools | Create, find, delete, transform |
| **Components** | 5 tools | Add, remove, configure |
| **Materials** | 4 tools | Create, configure, assign |
| **Physics** | 4 tools | Rigidbody, colliders, forces |
| **Audio** | 3 tools | Play, stop, configure |
| **Lighting** | 2 tools | Create, configure lights |
| **Scripts** | 4 tools | Create, modify, compile |
| **UI System** | 4 tools | Canvas, elements, events |
| **Animation** | 5 tools | Animator, clips, playback |
| **Input** | 4 tools | Actions, events, simulation |
| **Particles** | 4 tools | Systems, effects, properties |
| **Assets** | 1 tool | Import assets |
| **Logging** | 3 tools | Get, clear, stats |
| **Debug** | 2 tools | Scene info, stack traces |

**Total: 64+ tools across 16 categories**

## Best Practices

### 1. Tool Configuration
- Disable unused tools to improve performance
- Use tool categories to organize functionality
- Test tools individually before complex workflows

### 2. Error Handling
- Always check tool responses for errors
- Use Unity Console for detailed error messages
- Enable logging for debugging

### 3. Performance
- Batch related operations when possible
- Use async operations for long-running tasks
- Monitor Unity Editor performance

### 4. Security
- Server only binds to localhost
- Use in trusted environments only
- Review tool permissions regularly

## Troubleshooting

### Server Won't Start
- Check if port 8080 is available
- Verify Unity 2023.2+ compatibility
- Run compatibility check tool

### Tools Not Working
- Ensure Unity Editor is active
- Check tool is enabled in configuration
- Verify parameter formats

### Performance Issues
- Disable unused tools
- Check Unity Console for errors
- Monitor system resources

## Next Steps

1. **Explore Tools**: Try different tool categories
2. **Read API Reference**: Detailed documentation for all tools
3. **Build Workflows**: Create custom automation sequences
4. **Integrate with AI**: Use with Trae AI or other MCP clients
5. **Extend Functionality**: Add custom tools if needed

## Support

- Check Unity Console for error messages
- Review documentation in `/Documentation` folder
- Use built-in compatibility and test tools
- Refer to troubleshooting section in README

---

**Ready to automate your Unity workflow with AI? Start the MCP server and begin exploring!** 🚀