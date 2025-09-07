# Unity MCP Trae - API Reference

## Overview

This document provides detailed API reference for all available tools in Unity MCP Trae plugin.

## Tool Categories

### Scene Management Tools

#### list_scenes
**Description**: Lists all scenes in the project
**Parameters**: None
**Returns**: Array of scene information

#### open_scene
**Description**: Opens a specific scene
**Parameters**:
- `path` (string): Path to the scene file

#### load_scene
**Description**: Loads a scene additively or single
**Parameters**:
- `path` (string): Path to the scene file
- `mode` (string): "Single" or "Additive"

### Play Mode Control

#### play_mode_start
**Description**: Starts Unity play mode
**Parameters**: None

#### play_mode_stop
**Description**: Stops Unity play mode
**Parameters**: None

#### get_play_mode_status
**Description**: Gets current play mode status
**Parameters**: None
**Returns**: Current play mode state

### GameObject Operations

#### create_gameobject
**Description**: Creates a new GameObject
**Parameters**:
- `name` (string): Name of the GameObject
- `parent` (string, optional): Parent GameObject name

#### find_gameobject
**Description**: Finds GameObject by name
**Parameters**:
- `name` (string): Name to search for

#### delete_gameobject
**Description**: Deletes a GameObject
**Parameters**:
- `name` (string): Name of GameObject to delete

#### duplicate_gameobject
**Description**: Duplicates a GameObject
**Parameters**:
- `name` (string): Name of GameObject to duplicate

#### set_parent
**Description**: Sets parent-child relationship
**Parameters**:
- `child` (string): Child GameObject name
- `parent` (string): Parent GameObject name

#### get_gameobject_info
**Description**: Gets detailed GameObject information
**Parameters**:
- `name` (string): GameObject name

#### set_transform
**Description**: Sets GameObject transform properties
**Parameters**:
- `gameObject` (string): GameObject name
- `position` (Vector3, optional): Position coordinates
- `rotation` (Vector3, optional): Rotation angles
- `scale` (Vector3, optional): Scale values

### Component Management

#### add_component
**Description**: Adds component to GameObject
**Parameters**:
- `gameObject` (string): Target GameObject name
- `component` (string): Component type name

#### remove_component
**Description**: Removes component from GameObject
**Parameters**:
- `gameObject` (string): Target GameObject name
- `component` (string): Component type name

#### get_component_properties
**Description**: Gets component properties
**Parameters**:
- `gameObject` (string): GameObject name
- `component` (string): Component type

#### set_component_properties
**Description**: Sets component properties
**Parameters**:
- `gameObject` (string): GameObject name
- `component` (string): Component type
- `properties` (object): Property values

#### list_components
**Description**: Lists all components on GameObject
**Parameters**:
- `gameObject` (string): GameObject name

### Material and Rendering

#### create_material
**Description**: Creates a new material
**Parameters**:
- `name` (string): Material name
- `shader` (string, optional): Shader name

#### set_material_properties
**Description**: Sets material properties
**Parameters**:
- `material` (string): Material name
- `properties` (object): Property values

#### assign_material
**Description**: Assigns material to renderer
**Parameters**:
- `gameObject` (string): GameObject name
- `material` (string): Material name

#### set_renderer_properties
**Description**: Sets renderer properties
**Parameters**:
- `gameObject` (string): GameObject name
- `properties` (object): Renderer properties

### Physics System

#### set_rigidbody_properties
**Description**: Sets Rigidbody properties
**Parameters**:
- `gameObject` (string): GameObject name
- `properties` (object): Rigidbody properties

#### add_force
**Description**: Adds force to Rigidbody
**Parameters**:
- `gameObject` (string): GameObject name
- `force` (Vector3): Force vector
- `mode` (string, optional): Force mode

#### set_collider_properties
**Description**: Sets collider properties
**Parameters**:
- `gameObject` (string): GameObject name
- `properties` (object): Collider properties

#### raycast
**Description**: Performs raycast
**Parameters**:
- `origin` (Vector3): Ray origin
- `direction` (Vector3): Ray direction
- `distance` (float, optional): Max distance

### Audio System

#### play_audio
**Description**: Plays audio clip
**Parameters**:
- `gameObject` (string): GameObject with AudioSource
- `clip` (string, optional): Audio clip name

#### stop_audio
**Description**: Stops audio playback
**Parameters**:
- `gameObject` (string): GameObject with AudioSource

#### set_audio_properties
**Description**: Sets AudioSource properties
**Parameters**:
- `gameObject` (string): GameObject name
- `properties` (object): Audio properties

### Lighting System

#### create_light
**Description**: Creates a light source
**Parameters**:
- `name` (string): Light name
- `type` (string): Light type

#### set_light_properties
**Description**: Sets light properties
**Parameters**:
- `gameObject` (string): GameObject name
- `properties` (object): Light properties

### Script Management

#### create_script
**Description**: Creates a new script
**Parameters**:
- `name` (string): Script name
- `content` (string): Script content

#### modify_script
**Description**: Modifies existing script
**Parameters**:
- `path` (string): Script path
- `content` (string): New content

#### compile_scripts
**Description**: Compiles all scripts
**Parameters**: None

#### get_script_errors
**Description**: Gets compilation errors
**Parameters**: None

### UI System

#### create_canvas
**Description**: Creates UI Canvas
**Parameters**:
- `name` (string): Canvas name
- `renderMode` (string, optional): Render mode

#### create_ui_element
**Description**: Creates UI element
**Parameters**:
- `type` (string): UI element type
- `name` (string): Element name
- `parent` (string, optional): Parent canvas

#### set_ui_properties
**Description**: Sets UI element properties
**Parameters**:
- `gameObject` (string): UI element name
- `properties` (object): UI properties

#### bind_ui_events
**Description**: Binds UI events
**Parameters**:
- `gameObject` (string): UI element name
- `events` (object): Event bindings

### Animation System

#### create_animator
**Description**: Creates Animator component
**Parameters**:
- `gameObject` (string): GameObject name
- `controller` (string, optional): Animator controller

#### set_animation_clip
**Description**: Sets animation clip
**Parameters**:
- `gameObject` (string): GameObject name
- `clip` (string): Animation clip name

#### play_animation
**Description**: Plays animation
**Parameters**:
- `gameObject` (string): GameObject name
- `stateName` (string): Animation state name

#### set_animation_parameters
**Description**: Sets animation parameters
**Parameters**:
- `gameObject` (string): GameObject name
- `parameters` (object): Parameter values

#### create_animation_clip
**Description**: Creates animation clip
**Parameters**:
- `name` (string): Clip name
- `length` (float): Clip length

### Input System

#### setup_input_actions
**Description**: Sets up input actions
**Parameters**:
- `actions` (object): Input action definitions

#### bind_input_events
**Description**: Binds input events
**Parameters**:
- `bindings` (object): Event bindings

#### simulate_input
**Description**: Simulates input
**Parameters**:
- `input` (object): Input simulation data

#### create_input_mapping
**Description**: Creates input mapping
**Parameters**:
- `mappingName` (string): Mapping name
- `mappingData` (object): Mapping configuration

### Particle System

#### create_particle_system
**Description**: Creates particle system
**Parameters**:
- `name` (string): System name
- `preset` (string, optional): Preset name

#### set_particle_properties
**Description**: Sets particle properties
**Parameters**:
- `gameObject` (string): GameObject name
- `properties` (object): Particle properties

#### play_particle_effect
**Description**: Plays particle effect
**Parameters**:
- `gameObject` (string): GameObject name

#### create_particle_effect
**Description**: Creates particle effect
**Parameters**:
- `name` (string): Effect name
- `properties` (object): Effect properties

### Asset Management

#### import_asset
**Description**: Imports asset
**Parameters**:
- `path` (string): Asset path

### Logging System

#### get_unity_logs
**Description**: Gets Unity console logs
**Parameters**:
- `count` (int, optional): Number of logs
- `logType` (string, optional): Log type filter

#### clear_unity_logs
**Description**: Clears Unity console
**Parameters**: None

#### get_unity_log_stats
**Description**: Gets log statistics
**Parameters**: None

### Debug Tools

#### get_current_scene_info
**Description**: Gets current scene information
**Parameters**: None

#### get_thread_stack_info
**Description**: Gets thread stack information
**Parameters**: None

## Error Handling

All tools return standardized error responses when operations fail:

```json
{
  "error": {
    "code": -32000,
    "message": "Error description",
    "data": {
      "details": "Additional error details"
    }
  }
}
```

## Threading

All Unity API calls are automatically dispatched to the main thread for safety. Async operations are supported where appropriate.