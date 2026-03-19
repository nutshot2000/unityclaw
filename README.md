# Unity MCP Server v2.0

A Model Context Protocol (MCP) server that allows AI agents to control Unity Editor remotely and query Unity documentation via RAG (Retrieval-Augmented Generation).

## 🎯 What's New in v2.0

**50+ MCP Tools** including:
- ✅ Scene Management (load, create, save, list scenes)
- ✅ Play Mode Control (enter, exit, pause, status)
- ✅ Asset Management (materials, prefabs, instantiate, import)
- ✅ Script Creation (MonoBehaviour, ScriptableObject, Editor templates)
- ✅ Inspector Properties (get/set ANY property, invoke methods)
- ✅ Console Logs (capture, clear, send logs)
- ✅ Animation Control (triggers, bools, floats, state info)
- ✅ Camera Control (position, orthographic, follow, screenshots)
- ✅ Physics Control (time scale, raycast, overlap sphere)
- ✅ Build System (player builds for all platforms)
- ✅ Undo/Redo support
- ✅ Selection Management (set, copy, paste, duplicate)
- ✅ Editor Windows (open Game, Scene, Console, Animator, etc.)
- ✅ Project Settings
- ✅ Documentation RAG (304 Unity doc chunks)

## 📦 Installation

### 1. Unity Setup

Copy `UnityMcpBridge.cs` to your Unity project:
```
YourProject/Assets/Editor/UnityMcpBridge.cs
```

Start Unity - the bridge auto-starts on `http://localhost:7778`

### 2. MCP Server Setup

```bash
# Clone or download this repo
cd unity-mcp-server

# Install dependencies
npm install

# Build TypeScript
npm run build

# Ingest Unity docs (creates vector database)
npm run ingest
```

### 3. Configure OpenClaw

Add to your `~/.openclaw/openclaw.json`:

```json
{
  "agents": {
    "mcpServers": {
      "unity_editor": {
        "command": "node",
        "args": ["/path/to/unity-mcp-server/dist/index.js"]
      }
    }
  }
}
```

Restart OpenClaw:
```bash
openclaw gateway restart
```

## 🎮 Usage Examples

### Scene Management
```
"Load the MainMenu scene"
"Create a new scene called TestLevel"
"Save the current scene"
```

### Play Mode
```
"Enter play mode"
"Pause the game"
"Exit play mode"
"Is the game currently playing?"
```

### Object Creation
```
"Create a red cube at position 0, 5, 0"
"Spawn a sphere named 'Enemy' at 10, 0, 10"
"Create an empty GameObject called 'SpawnPoint'"
```

### Components
```
"Add a Rigidbody to the Player"
"Set the Player's Rigidbody mass to 5"
"Get all properties on the Enemy's Animator"
"Invoke the 'TakeDamage' method on the Boss"
```

### Animation
```
"Set the 'IsRunning' bool to true on Player"
"Trigger the 'Jump' animation on Player"
"Set the 'Speed' float to 5.5 on Enemy"
"What's the current animation state on Player?"
```

### Camera
```
"Move the camera to position 0, 10, -10"
"Make the camera follow the Player"
"Set the camera to orthographic with size 5"
"Take a screenshot"
```

### Physics
```
"Set time scale to 0.5 for slow motion"
"Raycast from the camera to find what the player is looking at"
"Find all colliders within 5 units of the explosion point"
```

### Assets
```
"Create a red material called 'EnemyMaterial'"
"Create a prefab from the Player object"
"Instantiate the Enemy prefab at position 20, 0, 0"
```

### Scripts (Auto-Compiling)
```
"Create a new script called 'HealthManager'"
"Create a ScriptableObject called 'WeaponData'"
"Recompile all scripts"
```

### Build
```
"Build the game for Windows"
"Build for WebGL to Builds/WebGL/"
"What are the current build settings?"
```

## 📚 Documentation RAG

The system includes 304 chunks of Unity documentation covering:

- Core (GameObject, Component, MonoBehaviour)
- Physics (Rigidbody, Colliders, Raycasting, 2D Physics)
- Input (Legacy, New Input System, Touch, XR)
- UI (Canvas, UI Elements, TextMeshPro)
- Animation (Animator, Clips, Blend Trees)
- Audio (AudioSource, Mixer)
- Rendering (Materials, Lighting, Camera)
- ScriptableObjects
- Terrain
- 2D Development
- Lighting & Shadows
- Shaders & Shader Graph
- AI & Navigation
- Networking
- XR/VR

**The AI automatically queries docs before writing code** to prevent hallucinations!

## 🔧 Available Tools (50+)

### Scene Management
- `unity_get_hierarchy` - Get scene objects
- `unity_get_scene_info` - Scene metadata
- `unity_load_scene` - Load a scene
- `unity_create_scene` - Create new scene
- `unity_save_scene` - Save current scene
- `unity_get_scene_list` - List loaded scenes

### Play Mode
- `unity_enter_play_mode` - Start playing
- `unity_exit_play_mode` - Stop playing
- `unity_pause_play_mode` - Pause/unpause
- `unity_get_play_mode_status` - Check status

### Objects
- `unity_spawn_object` - Create GameObject
- `unity_modify_object` - Modify properties
- `unity_delete_object` - Delete object
- `unity_get_selection` - Get selected objects
- `unity_set_selection` - Set selection
- `unity_duplicate_selection` - Duplicate selected

### Components (Reflection-Based)
- `unity_add_component` - Add component
- `unity_remove_component` - Remove component
- `unity_get_component_properties` - Get all properties statically/dynamically via Reflection
- `unity_set_property` - Set property/field value
- `unity_invoke_method` - Call any C# method on Component

### Assets
- `unity_create_material` - Create material
- `unity_create_prefab` - Create prefab
- `unity_instantiate_prefab` - Instantiate prefab
- `unity_find_assets` - Find assets by name or type

### Scripts
- `unity_create_script` - Create C# script
- `unity_compile_scripts` - Force recompile

### Animation
- `unity_set_animation_trigger` - Set trigger
- `unity_set_animation_bool` - Set bool
- `unity_set_animation_float` - Set float
- `unity_get_animation_state` - Get state

### Camera
- `unity_set_camera_position` - Move camera
- `unity_set_camera_orthographic` - Set ortho/perspective
- `unity_camera_follow_target` - Follow target
- `unity_take_screenshot` - Capture screen

### Physics
- `unity_set_time_scale` - Time scale
- `unity_physics_raycast` - Raycast
- `unity_physics_overlap_sphere` - Overlap sphere

### Build
- `unity_build_player` - Build game
- `unity_get_build_settings` - Build settings

### Editor
- `unity_undo` / `unity_redo` - Undo/redo
- `unity_open_window` - Open editor window
- `unity_get_console_logs` - Get logs
- `unity_clear_console` - Clear console
- `unity_send_debug_log` - Send log
- `unity_get_project_settings` - Project info

### Documentation
- `unity_search_docs` - Search Unity docs

## 🛡️ Anti-Hallucination

The system prevents AI hallucinations by:

1. **Querying docs first** - Before writing Unity code
2. **Using exact APIs** - Method names from official docs
3. **Citing sources** - Referencing which doc the code came from

## 📝 For AI Agents

See:
- `AGENT_INSTRUCTIONS.md` - Full usage guide
- `QUICK_REFERENCE.md` - Quick cheat sheet
- `SYSTEM_PROMPT.md` - System prompt for LLMs

## 🔧 Requirements

- Unity 2022.3+ (with .NET 4.x)
- Node.js 18+
- Ollama (for local embeddings)
- OpenClaw (for MCP integration)

## 📄 License

MIT - Feel free to use and modify!

## 🙏 Credits

Built with:
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [Unity](https://unity.com/)
- [Ollama](https://ollama.com/)
- [Vectra](https://github.com/Berkay-akbas/vectra)

---

**Made with 🔥🦞 by Firebat for Jimmy**
