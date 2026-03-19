# Unity MCP - Quick Reference

## ⚡ Golden Rule
**Query docs BEFORE writing Unity code!**

```javascript
unity_search_docs({"query": "your query", "topK": 3})
```

## 🔧 Common Tasks

### Spawn Object
```javascript
unity_spawn_object({
  "name": "MyCube",
  "primitive": "Cube",
  "position": {"x": 0, "y": 5, "z": 0},
  "scale": {"x": 2, "y": 2, "z": 2}
})
```

### Execute C# Code
```javascript
unity_execute_code({
  "code": "GameObject.Find(\"Player\").transform.position = Vector3.zero;"
})
```

### Get Scene Info
```javascript
unity_get_scene_info({})
unity_get_hierarchy({})
```

## 📚 Doc Search Examples

| What You Need | Query |
|---------------|-------|
| Player movement | `"Rigidbody velocity movement"` |
| Jump mechanics | `"Rigidbody AddForce jump ForceMode"` |
| AI pathfinding | `"NavMeshAgent SetDestination"` |
| UI button click | `"Button onClick AddListener"` |
| Animation trigger | `"Animator SetTrigger parameters"` |
| 2D physics | `"Rigidbody2D velocity movement"` |
| VR controller | `"XR controller input thumbstick"` |
| Save game | `"JSON serialization save load"` |
| Shader effect | `"Shader Graph dissolve effect"` |
| Terrain generation | `"Terrain Perlin noise heightmap"` |

## 🎯 Query Tips

✅ **Good:** `"NavMeshAgent stoppingDistance pathComplete"`
❌ **Bad:** `"how to make AI"`

✅ **Good:** `"Rigidbody AddForce ForceMode.Impulse"`
❌ **Bad:** `"physics"`

✅ **Good:** `"Canvas worldSpace renderMode camera"`
❌ **Bad:** `"UI setup"`

## 🚨 Troubleshooting

| Problem | Solution |
|---------|----------|
| Unity not responding | Check if UnityMcpBridge.cs is in Assets/Editor/ |
| Wrong doc results | Use more specific terms (component names) |
| No results | Try synonyms (e.g., "move" vs "velocity") |
| Outdated info | Query again with version-specific terms |

## 📖 Document Sources

When citing docs, reference these files:
- `01-core-gameobject.md` - GameObject basics
- `02-physics-rigidbody.md` - Physics & forces
- `03-input-system.md` - Input handling
- `04-ui-system.md` - UI & Canvas
- `05-animation.md` - Animator & clips
- `06-audio.md` - Audio systems
- `07-rendering.md` - Rendering & camera
- `08-scriptableobjects.md` - ScriptableObjects
- `09-terrain-landscapes.md` - Terrain
- `10-2d-game-development.md` - 2D games
- `11-lighting-shadows.md` - Lighting
- `12-shaders-graph.md` - Shaders
- `13-ai-navigation.md` - AI & NavMesh
- `14-networking-save-systems.md` - Networking
- `15-xr-vr-development.md` - VR/XR
- `transform.md` - Transform

## 💡 Pro Tips

1. **Always query first** - Prevents hallucinations
2. **Use component names** - "Rigidbody" not "physics"
3. **Include method names** - "SetDestination" not "move"
4. **Check multiple results** - First isn't always best
5. **Cite your sources** - Build trust with users

---

**Query → Results → Code → Cite** 🔥🦞🎮
