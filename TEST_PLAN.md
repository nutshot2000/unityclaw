# Unity MCP Test Plan

## Test Order (Start Simple → Complex)

---

## ✅ TEST 1: Basic Connectivity

**What to test:** Bridge is running and responding

**Test:**
```
"Get the current scene info"
```

**What to look for in Unity:**
- Console should show: `[MCP Bridge] GET /api/scene-info`
- Response should include scene name, path, rootCount

**Expected result:** ✅ JSON response with scene data

---

## ✅ TEST 2: Scene Management

### 2a. Get Scene List
**Test:**
```
"List all loaded scenes"
```

**What to look for:**
- Response shows array of scenes
- Each scene has: name, path, isLoaded, buildIndex

### 2b. Create New Scene
**Test:**
```
"Create a new scene called TestScene"
```

**What to look for in Unity:**
- Scene view goes empty (new scene)
- Hierarchy shows only default objects (Main Camera, Directional Light)
- Scene tab shows "Untitled" or new name

### 2c. Save Scene
**Test:**
```
"Save the current scene to Assets/Scenes/TestScene.unity"
```

**What to look for:**
- File appears in Project window under Assets/Scenes/
- Scene name updates in Unity

### 2d. Load Scene
**Test:**
```
"Load the SampleScene"
```

**What to look for:**
- Unity loads your original scene
- Previous objects reappear

---

## ✅ TEST 3: Object Spawning

### 3a. Spawn Primitive
**Test:**
```
"Create a cube named 'TestCube' at position 0, 2, 0"
```

**What to look for in Unity:**
- Hierarchy shows "TestCube"
- Scene view shows white cube at position 0, 2, 0
- Inspector shows Transform with Y = 2

### 3b. Spawn with Scale
**Test:**
```
"Create a sphere named 'GiantSphere' at 5, 1, 0 with scale 3, 3, 3"
```

**What to look for:**
- Large sphere appears at X=5
- Transform scale shows 3, 3, 3

### 3c. Spawn with Rotation
**Test:**
```
"Create a cylinder named 'RotatedCyl' at 0, 1, 5 rotated 45, 0, 0"
```

**What to look for:**
- Cylinder is tilted at 45 degrees on X axis

---

## ✅ TEST 4: Object Modification

### 4a. Change Position
**Test:**
```
"Move TestCube to position 10, 2, 0"
```

**What to look for:**
- Cube moves to X=10 in Scene view
- Transform updates in Inspector

### 4b. Change Scale
**Test:**
"Scale GiantSphere to 5, 5, 5"
```

**What to look for:**
- Sphere gets even bigger
- Scale values update

### 4c. Change Name
**Test:**
```
"Rename TestCube to PlayerCube"
```

**What to look for:**
- Hierarchy updates to show "PlayerCube"

### 4d. Change Active State
**Test:**
```
"Set GiantSphere inactive"
```

**What to look for:**
- Sphere disappears from Scene view
- Checkbox unchecked in Inspector
- Still visible in Hierarchy (grayed out)

**Then test:**
```
"Set GiantSphere active"
```

**What to look for:**
- Sphere reappears

---

## ✅ TEST 5: Components

### 5a. Add Component
**Test:**
```
"Add a Rigidbody to PlayerCube"
```

**What to look for in Unity:**
- Inspector shows "Rigidbody" component added
- Component has default values (mass=1, drag=0, etc.)

### 5b. Get Component Properties
**Test:**
```
"Get all properties on PlayerCube's Rigidbody"
```

**What to look for:**
- Response shows list of properties:
  - mass (float)
  - drag (float)
  - angularDrag (float)
  - useGravity (bool)
  - isKinematic (bool)
  - And more...

### 5c. Set Property
**Test:**
```
"Set PlayerCube's Rigidbody mass to 5"
```

**What to look for:**
- Inspector shows Mass = 5

### 5d. Set Bool Property
**Test:**
```
"Set PlayerCube's Rigidbody useGravity to false"
```

**What to look for:**
- "Use Gravity" checkbox unchecked in Inspector

### 5e. Remove Component
**Test:**
```
"Remove the Rigidbody from PlayerCube"
```

**What to look for:**
- Rigidbody component disappears from Inspector

---

## ✅ TEST 6: Play Mode

### 6a. Enter Play Mode
**Test:**
```
"Enter play mode"
```

**What to look for in Unity:**
- Play button (▶) in toolbar becomes highlighted/active
- Scene view may change to Game view
- "[MCP Bridge] Server started" message might reappear

### 6b. Check Status
**Test:**
```
"Get the play mode status"
```

**What to look for:**
- Response shows: `isPlaying: true`, `isPaused: false`

### 6c. Pause
**Test:**
```
"Pause play mode"
```

**What to look for:**
- Pause button (⏸) highlighted
- Editor frozen/paused

### 6d. Unpause
**Test:**
```
"Unpause play mode"
```

**What to look for:**
- Editor resumes

### 6e. Exit Play Mode
**Test:**
```
"Exit play mode"
```

**What to look for:**
- Play button no longer highlighted
- Returns to edit mode
- Any runtime changes reset

---

## ✅ TEST 7: Selection

### 7a. Set Selection
**Test:**
```
"Select PlayerCube and GiantSphere"
```

**What to look for in Unity:**
- Both objects highlighted in Hierarchy
- Inspector shows "2 GameObjects selected"
- Both have orange outline in Scene view

### 7b. Duplicate
**Test:**
```
"Duplicate the selection"
```

**What to look for:**
- New objects appear: "PlayerCube (1)" and "GiantSphere (1)"
- Slightly offset from originals

---

## ✅ TEST 8: Assets

### 8a. Create Material
**Test:**
```
"Create a red material called 'EnemyMaterial'"
```

**What to look for in Unity:**
- File appears in Project window: Assets/Materials/EnemyMaterial.mat
- Material is red color

### 8b. Create Prefab
**Test:**
```
"Create a prefab from PlayerCube at Assets/Prefabs/Player.prefab"
```

**What to look for:**
- File appears: Assets/Prefabs/Player.prefab
- PlayerCube in Hierarchy gets blue icon (prefab instance)

### 8c. Instantiate Prefab
**Test:**
```
"Instantiate the Player prefab at position -5, 0, 0"
```

**What to look for:**
- New "Player" appears in Hierarchy
- At position X=-5
- Has blue prefab icon

---

## ✅ TEST 9: Animation (if you have an Animator setup)

**Prerequisite:** Need a GameObject with Animator component

### 9a. Set Trigger
**Test:**
```
"Set the 'Jump' trigger on Player"
```

**What to look for:**
- Animation triggers (if Animator is set up)

### 9b. Set Bool
**Test:**
```
"Set the 'IsRunning' bool to true on Player"
```

### 9c. Set Float
**Test:**
```
"Set the 'Speed' float to 5.5 on Player"
```

---

## ✅ TEST 10: Camera

### 10a. Move Camera
**Test:**
```
"Move the main camera to position 0, 20, -20 and rotation 45, 0, 0"
```

**What to look for in Unity:**
- Scene view camera moves (if in Scene view)
- Main Camera GameObject's Transform updates
- Camera preview changes

### 10b. Set Orthographic
**Test:**
```
"Set the main camera to orthographic with size 10"
```

**What to look for:**
- Camera projection changes to orthographic
- Size field shows 10
- View becomes isometric

### 10c. Take Screenshot
**Test:**
```
"Take a screenshot"
```

**What to look for:**
- File created: Assets/Screenshots/screenshot_[timestamp].png
- Image shows current Game view

---

## ✅ TEST 11: Physics

### 11a. Time Scale
**Test:**
```
"Set time scale to 0.5"
```

**What to look for:**
- If in Play Mode: time moves slower
- Physics simulations slow down

**Then:**
```
"Set time scale to 1"
```

### 11b. Raycast
**Test:**
```
"Raycast from origin 0, 5, 0 in direction 0, -1, 0 with max distance 10"
```

**What to look for:**
- Response shows hit info if ray hits something
- Includes: point, normal, distance, collider name

### 11c. Overlap Sphere
**Test:**
```
"Find all colliders within sphere at 0, 0, 0 with radius 10"
```

**What to look for:**
- Response lists all colliders in range
- Shows names and tags

---

## ✅ TEST 12: Console

### 12a. Send Log
**Test:**
```
"Send a debug log saying 'Hello from MCP!'"
```

**What to look for in Unity:**
- Console window shows: "Hello from MCP!"
- Log type: Log

### 12b. Send Warning
**Test:**
```
"Send a warning log saying 'This is a test warning'"
```

**What to look for:**
- Yellow warning icon in Console

### 12c. Send Error
**Test:**
```
"Send an error log saying 'Test error message'"
```

**What to look for:**
- Red error icon in Console

### 12d. Get Logs
**Test:**
```
"Get the recent console logs"
```

**What to look for:**
- Response shows array of recent logs
- Includes our test messages
- Shows type, message, timestamp

### 12e. Clear Console
**Test:**
```
"Clear the console"
```

**What to look for:**
- Console window is empty
- All previous logs cleared

---

## ✅ TEST 13: Editor Windows

### 13a. Open Game Window
**Test:**
```
"Open the Game window"
```

**What to look for:**
- Game view tab opens/focuses
- Shows what camera sees

### 13b. Open Console
**Test:**
```
"Open the Console window"
```

**What to look for:**
- Console window opens
- Shows logs

---

## ✅ TEST 14: Undo/Redo

### 14a. Undo
**Test:**
```
"Undo the last action"
```

**What to look for:**
- Last change reverts (e.g., deleted object reappears)

### 14b. Redo
**Test:**
```
"Redo the last undone action"
```

**What to look for:**
- Change reapplies

---

## ✅ TEST 15: Documentation RAG

### 15a. Search Docs
**Test:**
```
"Search Unity docs for 'Rigidbody.AddForce'"
```

**What to look for:**
- Response shows relevant documentation
- Includes code examples
- Shows source document

### 15b. Complex Query
**Test:**
```
"Search Unity docs for 'NavMeshAgent pathfinding'"
```

**What to look for:**
- Results about AI navigation
- Code examples for SetDestination

---

## ✅ TEST 16: Scripts (Advanced)

### 16a. Create Script
**Test:**
```
"Create a script called 'HealthManager'"
```

**What to look for:**
- File created: Assets/Scripts/HealthManager.cs
- Contains MonoBehaviour template

### 16b. Create ScriptableObject
**Test:**
```
"Create a ScriptableObject called 'WeaponData'"
```

**What to look for:**
- File created with ScriptableObject template
- Has CreateAssetMenu attribute

### 16c. Compile
**Test:**
```
"Compile all scripts"
```

**What to look for:**
- Unity recompiles
- May show progress bar
- Any errors appear in Console

---

## ✅ TEST 17: Build (Advanced)

**Note:** This takes time and requires proper setup

**Test:**
```
"Build the player for Windows to Builds/Windows/MyGame.exe"
```

**What to look for:**
- Build process starts
- Progress in Console
- Build folder created with executable

---

## 🎯 QUICK TEST SEQUENCE (5 minutes)

If you want a quick validation, run these in order:

1. `"Get scene info"` - Basic connectivity
2. `"Create a cube at 0, 2, 0"` - Spawning
3. `"Add a Rigidbody to the cube"` - Components
4. `"Enter play mode"` - Play mode
5. `"Exit play mode"` - Exit play
6. `"Delete the cube"` - Cleanup

**All should complete without errors!**

---

## 🐛 Troubleshooting

### If a test fails:

1. **Check Unity Console** for error messages
2. **Verify bridge is running** - look for `[MCP Bridge] Server started`
3. **Check MCP Server** - should show "Unity MCP Server v2.0.0 running"
4. **Try simpler test** - start with basic connectivity
5. **Restart Unity** if needed

### Common Issues:

| Issue | Solution |
|-------|----------|
| "Connection refused" | Unity not running or bridge not started |
| "Object not found" | Check exact name in Hierarchy |
| "Component not found" | Component type name is case-sensitive |
| "Method not found" | Method name must match exactly |
| No response | Check Unity Console for errors |

---

**Ready to start testing?** Pick a category and let's go! 🔥🦞🎮
