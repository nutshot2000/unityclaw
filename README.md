# Unity MCP Server

A Model Context Protocol (MCP) server that allows AI agents to control Unity Editor remotely and query Unity documentation via RAG (Retrieval-Augmented Generation).

## 🎯 Features

- **Remote Unity Control** - Spawn objects, modify scenes, execute C# code
- **Documentation RAG** - 304 chunks of Unity docs with vector search
- **Anti-Hallucination** - AI queries docs before writing code
- **Local Embeddings** - Uses Ollama (no API costs)
- **Real-time Feedback** - See changes in Unity instantly

## 📦 What's Included

```
unity-mcp-server/
├── UnityMcpBridge.cs          # C# script for Unity Editor
├── src/
│   ├── index.ts               # MCP server (15+ tools)
│   ├── rag.ts                 # Vector search with Ollama
│   └── ingest-docs.ts         # Document indexing
├── docs/                      # 16 Unity documentation files
│   ├── 01-core-gameobject.md
│   ├── 02-physics-rigidbody.md
│   ├── 03-input-system.md
│   ├── ... (13 more)
│   └── 15-xr-vr-development.md
├── AGENT_INSTRUCTIONS.md      # Full guide for AI agents
├── QUICK_REFERENCE.md         # Cheat sheet
├── SYSTEM_PROMPT.md           # LLM system prompt
└── README.md                  # This file
```

## 🚀 Quick Start

### 1. Unity Setup

Copy `UnityMcpBridge.cs` to your Unity project:
```
YourProject/Assets/Editor/UnityMcpBridge.cs
```

Start Unity - the bridge will auto-start on `http://localhost:7778`

### 2. MCP Server Setup

```bash
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

Restart OpenClaw gateway:
```bash
openclaw gateway restart
```

## 🎮 Usage

### Control Unity

Ask your AI assistant:
- "Create a red cube at position 0, 5, 0"
- "Add a Rigidbody to the Player object"
- "What's in the current scene?"

### Query Documentation

The AI automatically queries docs before writing code:

```javascript
// AI queries this first
unity_search_docs({
  "query": "NavMeshAgent follow player",
  "topK": 3
})

// Then gives accurate code based on results
```

## 📚 Documentation Coverage

| Category | Topics |
|----------|--------|
| Core | GameObject, Component, MonoBehaviour, Coroutines |
| Physics | Rigidbody, Colliders, Raycasting, 2D Physics |
| Input | Legacy Input, New Input System, Touch, XR |
| UI | Canvas, UI Elements, Layout Groups, TextMeshPro |
| Animation | Animator, Animation Clips, Blend Trees |
| Audio | AudioSource, AudioMixer, Microphone |
| Rendering | Materials, Lighting, Camera, Particles |
| Data | ScriptableObjects, Events, Runtime Sets |
| Terrain | Landscapes, Heightmaps, Trees, Water |
| 2D | Sprite Renderer, 2D Physics, Tilemap |
| Lighting | Shadows, Global Illumination, Light Probes |
| Shaders | Shader Graph, ShaderLab, Compute Shaders |
| AI | NavMesh, Pathfinding, Obstacles |
| Networking | Netcode, Mirror, Save Systems |
| XR/VR | XR Toolkit, VR Input, Hand Tracking |

**Total: 304 documentation chunks**

## 🔧 Available Tools

### Unity Control
- `unity_get_hierarchy` - Get scene objects
- `unity_spawn_object` - Create GameObjects
- `unity_modify_object` - Change properties
- `unity_delete_object` - Remove objects
- `unity_add_component` - Add components
- `unity_execute_code` - Run C# code
- `unity_get_scene_info` - Scene metadata
- `unity_get_selection` - Selected objects

### Documentation
- `unity_search_docs` - Search Unity docs via RAG

## 🛡️ Anti-Hallucination

This system prevents AI hallucinations by:

1. **Querying docs first** - Before writing any Unity code
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
- [Vectra](https://github.com/Berkay-akbas/vectra) (vector database)

---

**Made with 🔥🦞 by Firebat for Jimmy**
