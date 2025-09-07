# Unity MCP Trae - Example Scene

This example scene demonstrates the capabilities of Unity MCP Trae plugin and how AI assistants can interact with Unity Editor through the MCP protocol.

## What's Included

### Scripts
- **McpExampleController.cs**: A comprehensive example script showing various Unity operations that can be controlled via MCP

### Scene Setup
The example scene should include:
- A GameObject with McpExampleController attached
- Multiple materials for material switching demonstration
- Audio clip for sound playback testing
- Particle system for visual effects
- Proper lighting setup

## How to Use

### 1. Import the Sample
1. In Unity Package Manager, find "Unity MCP Trae"
2. Expand the package details
3. Find "Samples" section
4. Click "Import" next to "Example Scene"

### 2. Open the Scene
1. Navigate to `Assets/Samples/Unity MCP Trae/[version]/Example Scene/`
2. Open `ExampleScene.unity`

### 3. Start MCP Server
1. Go to **Tools > CLH-MCP > MCP Server**
2. Click **Start Server**
3. Ensure server is running on `http://localhost:8080/mcp`

### 4. Test with AI Assistant

Once the MCP server is running, you can ask your AI assistant (like Trae AI) to:

#### Basic Object Manipulation
- "Move the example object to position (5, 0, 0)"
- "Scale the object to 2x size"
- "Reset the object to its original position"

#### Material and Visual Effects
- "Change the object's material to the second material"
- "Start the particle effects"
- "Stop the particle system"

#### Audio Control
- "Play the example sound effect"

#### Information Queries
- "Get the current status of the example object"
- "What's the current position of the object?"

## MCP Commands Demonstrated

The example controller showcases these MCP tool categories:

### GameObject Operations
- Position manipulation
- Scale changes
- Transform queries

### Component Interaction
- Renderer material changes
- AudioSource control
- ParticleSystem management

### State Management
- Object status tracking
- Property queries
- Debug logging

## Example MCP Calls

Here are some JSON-RPC calls you can make directly to the MCP server:

### Move Object
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "set_transform",
    "arguments": {
      "gameObject": "ExampleObject",
      "position": {"x": 3, "y": 1, "z": 0}
    }
  }
}
```

### Change Material
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "set_material_properties",
    "arguments": {
      "material": "ExampleMaterial",
      "properties": {
        "_Color": {"r": 1, "g": 0, "b": 0, "a": 1}
      }
    }
  }
}
```

### Play Audio
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "play_audio",
    "arguments": {
      "audioSource": "ExampleObject"
    }
  }
}
```

## Learning Objectives

This example teaches:

1. **MCP Integration**: How Unity objects can be controlled via MCP protocol
2. **AI Automation**: Practical examples of AI-driven Unity workflows
3. **Tool Usage**: Real-world application of MCP tools
4. **Best Practices**: Proper error handling and logging
5. **Extensibility**: How to create MCP-compatible scripts

## Customization

### Adding More Materials
1. Create additional materials in the project
2. Assign them to the `materials` array in McpExampleController
3. Test material switching via MCP

### Adding Audio Clips
1. Import audio files to the project
2. Assign to the `exampleSound` field
3. Test audio playback via MCP

### Extending Functionality
1. Add new public methods to McpExampleController
2. These methods can be called via MCP tools
3. Follow the existing pattern for logging and error handling

## Troubleshooting

### Object Not Responding
- Ensure MCP server is running
- Check Unity Console for error messages
- Verify object names match MCP commands

### Materials Not Changing
- Ensure materials are assigned in the inspector
- Check material array indices
- Verify Renderer component exists

### Audio Not Playing
- Ensure AudioSource component is attached
- Check audio clip is assigned
- Verify audio settings in Unity

### Particles Not Working
- Ensure ParticleSystem component exists
- Check particle system settings
- Verify the system is not already playing/stopped

## Next Steps

1. **Experiment**: Try different MCP commands with the example object
2. **Extend**: Add your own methods to the controller
3. **Create**: Build your own MCP-compatible scripts
4. **Integrate**: Use with your AI assistant for real projects

---

**This example demonstrates the power of AI-driven Unity development with MCP!** 🎮✨