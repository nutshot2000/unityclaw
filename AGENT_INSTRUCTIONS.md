# Unity MCP Server - Agent Instructions

## Overview

This Unity MCP (Model Context Protocol) server allows AI agents to control Unity Editor remotely and query Unity documentation via RAG (Retrieval-Augmented Generation).

## System Components

### 1. Unity Bridge (C#)
- **Location:** `UnityMcpBridge.cs` → Copy to `Assets/Editor/` in Unity project
- **Port:** `http://localhost:7778`
- **Function:** HTTP server inside Unity that executes commands

### 2. MCP Server (TypeScript/Node.js)
- **Location:** `unity-mcp-server/` folder
- **Entry:** `dist/index.js`
- **Function:** Bridges OpenClaw to Unity via MCP protocol

### 3. RAG System (Documentation)
- **Database:** `unity-docs-db/` (Vectra vector database)
- **Documents:** 16 markdown files in `docs/`
- **Embeddings:** Local Ollama (nomic-embed-text)
- **Total:** 304 code examples and documentation chunks

## Available MCP Tools

### Unity Control Tools

| Tool | Description | Example |
|------|-------------|---------|
| `unity_get_hierarchy` | Get scene objects | `unity_get_hierarchy({})` |
| `unity_spawn_object` | Create GameObject | `unity_spawn_object({"name": "Cube", "primitive": "Cube", "position": {"x": 0, "y": 5, "z": 0}})` |
| `unity_modify_object` | Change properties | `unity_modify_object({"name": "Cube", "position": {"x": 10, "y": 0, "z": 0}})` |
| `unity_delete_object` | Remove object | `unity_delete_object({"name": "Cube"})` |
| `unity_add_component` | Add component | `unity_add_component({"objectName": "Player", "componentType": "Rigidbody"})` |
| `unity_get_component_properties` | Read component data | `unity_get_component_properties({"objectName": "Player", "componentType": "Rigidbody"})` |
| `unity_set_property` | Modify component | `unity_set_property({"objectName": "Player", "componentType": "Rigidbody", "property": "mass", "value": "5.0"})` |
| `unity_invoke_method` | Manipulate component | `unity_invoke_method({"objectName": "Player", "componentType": "Rigidbody", "method": "AddForce", "parameters": ["0", "15", "0"]})` |
| `unity_create_script` | Create and compile | `unity_create_script({"name": "PlayerFollower", "template": "MonoBehaviour"})` |
| `unity_get_scene_info` | Scene metadata | `unity_get_scene_info({})` |
| `unity_get_selection` | Selected objects | `unity_get_selection({})` |
| `unity_find_assets` | Search Assets by query | `unity_find_assets({"searchQuery": "Player", "typeFilter": "Prefab"})` |

### Documentation Tools

| Tool | Description | When to Use |
|------|-------------|-------------|
| `unity_search_docs` | Search Unity docs | **ALWAYS use before writing Unity code** |

## Critical Rule: Docs-First Coding

### ⚠️ MANDATORY: Query Docs Before Answering

**Before writing ANY Unity code, you MUST query the documentation:**

```javascript
// Step 1: Search docs
unity_search_docs({
  "query": "Rigidbody AddForce jump",
  "topK": 3
})

// Step 2: Use results to write accurate code
// Step 3: Cite the source document
```

### Why This Matters

| Without RAG | With RAG |
|-------------|----------|
| ❌ Hallucinate method names | ✅ Exact API signatures |
| ❌ Wrong parameter types | ✅ Correct parameter order |
| ❌ Outdated Unity versions | ✅ Unity 2022+ compatible |
| ❌ Miss edge cases | ✅ Full context from docs |

## Query Patterns

### Good Queries
```javascript
// Specific API
unity_search_docs({"query": "NavMeshAgent SetDestination pathfinding"})

// Pattern/Concept
unity_search_docs({"query": "2D Rigidbody jump platformer"})

// Component usage
unity_search_docs({"query": "AudioSource PlayOneShot multiple sounds"})

// System setup
unity_search_docs({"query": "XR Origin VR setup controller"})
```

### Bad Queries
```javascript
// Too vague
unity_search_docs({"query": "how to make game"})

// Not Unity-specific
unity_search_docs({"query": "C# programming basics"})

// Wrong terminology
unity_search_docs({"query": "Unity physics engine gravity"}) // Use "Rigidbody"
```

## Response Format

### When Answering Unity Questions:

1. **Query docs first** (silent, don't mention unless relevant)
2. **Provide code** based on doc results
3. **Cite sources** when helpful

**Example Response:**
```
Based on the Unity documentation, here's how to make a NavMesh agent follow the player:

```csharp
public class FollowAI : MonoBehaviour {
    [SerializeField] private Transform target;
    private NavMeshAgent agent;
    
    void Start() {
        agent = GetComponent<NavMeshAgent>();
    }
    
    void Update() {
        if (target != null) {
            agent.SetDestination(target.position);
        }
    }
}
```

This uses `NavMeshAgent.SetDestination()` from the AI Navigation system.
```

## Document Coverage

The RAG database includes:

- **Core:** GameObject, Component, MonoBehaviour, Coroutines
- **Physics:** Rigidbody, Colliders, Raycasting, 2D Physics
- **Input:** Legacy Input, New Input System, Touch, XR Input
- **UI:** Canvas, UI Elements, Layout Groups, TextMeshPro
- **Animation:** Animator, Animation Clips, Blend Trees, Root Motion
- **Audio:** AudioSource, AudioMixer, Microphone
- **Rendering:** Materials, Lighting, Camera, Particles
- **Data:** ScriptableObjects, Events, Runtime Sets
- **Terrain:** Landscapes, Heightmaps, Trees, Grass, Water
- **2D:** Sprite Renderer, 2D Physics, Tilemap
- **Lighting:** Shadows, Global Illumination, Light Probes
- **Shaders:** Shader Graph, ShaderLab, Compute Shaders
- **AI:** NavMesh, Pathfinding, Obstacles, Off-mesh Links
- **Networking:** Netcode for GameObjects, Mirror, Save Systems
- **XR/VR:** XR Toolkit, VR Input, Hand Tracking, Passthrough

## Troubleshooting

### Unity Bridge Not Responding
```bash
# Check if Unity is running with bridge
# Look for console message: "[MCP Bridge] Server started on http://localhost:7778/"

# If not running:
# 1. Ensure UnityMcpBridge.cs is in Assets/Editor/
# 2. Check Unity Console for errors
# 3. Restart Unity if needed
```

### RAG Returns Wrong Results
```javascript
// Try more specific queries
// Instead of: "physics"
// Use: "Rigidbody AddForce ForceMode"

// Instead of: "UI"
// Use: "Canvas world space camera"
```

### MCP Server Not Found
```bash
# Check if configured in openclaw.json:
"agents": {
  "mcpServers": {
    "unity_editor": {
      "command": "node",
      "args": ["C:\\path\\to\\unity-mcp-server\\dist\\index.js"]
    }
  }
}
```

## Best Practices

1. **Always query docs** before writing Unity code
2. **Use specific terms** in queries (component names, method names)
3. **Check multiple results** if first isn't clear
4. **Cite sources** when providing complex solutions
5. **Read Components directly** if confused, via `unity_get_component_properties`
6. **Use Scripts for Logic** via `unity_create_script` instead of arbitrary C# execution.

## For Future LLM Models

If you're a different AI model reading this:

1. **You have access to Unity docs** via `unity_search_docs`
2. **You can control Unity** via the MCP tools
3. **You MUST query docs first** to avoid hallucinations
4. **The database has 304 chunks** of Unity documentation
5. **Always cite your sources** when using doc results

## Quick Reference Card

```javascript
// Before answering ANY Unity question:
const docs = await unity_search_docs({
  query: "your specific query here",
  topK: 3
});

// Use docs.results to write accurate code
// docs.results[0].content - the code/documentation
// docs.results[0].source - which file it came from
```

---

**Remember: Query docs first, write code second, cite sources always!** 🔥🦞🎮
