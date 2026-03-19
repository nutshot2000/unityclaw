# Unity Rendering & Materials

## Materials

### Creating Materials
```csharp
// Create new material
Material mat = new Material(Shader.Find("Standard"));
Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

// From existing
Material mat = new Material(existingMaterial);

// Assign to renderer
GetComponent<Renderer>().material = mat;
GetComponent<Renderer>().sharedMaterial = mat; // Shared across instances
```

### Setting Properties
```csharp
Material mat = GetComponent<Renderer>().material;

// Colors
mat.SetColor("_Color", Color.red);
mat.SetColor("_EmissionColor", Color.white * 2f);

// Floats
mat.SetFloat("_Metallic", 0.5f);
mat.SetFloat("_Smoothness", 0.8f);

// Textures
mat.SetTexture("_MainTex", myTexture);
mat.SetTexture("_BumpMap", normalMap);
mat.SetTexture("_EmissionMap", emissionMap);

// Vectors
mat.SetVector("_SomeVector", new Vector4(1, 2, 3, 4));

// Keywords
mat.EnableKeyword("_EMISSION");
mat.DisableKeyword("_EMISSION");

// Shader pass
mat.SetShaderPassEnabled("ShadowCaster", true);
```

### Material Property Blocks (Efficient)
```csharp
// Use for changing properties without creating new material instances
MaterialPropertyBlock props = new MaterialPropertyBlock();
Renderer renderer = GetComponent<Renderer>();

// Set properties
props.SetColor("_Color", Color.red);
props.SetFloat("_Metallic", 0.5f);
props.SetTexture("_MainTex", texture);

// Apply
renderer.SetPropertyBlock(props);

// Clear
renderer.SetPropertyBlock(null);
```

## Shaders

### Finding Shaders
```csharp
Shader standard = Shader.Find("Standard");
Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
Shader unlit = Shader.Find("Unlit/Texture");
Shader particles = Shader.Find("Particles/Standard Unlit");
```

### Shader Properties
```csharp
Shader shader = mat.shader;

// Get property count
int propertyCount = shader.GetPropertyCount();

// Get property info
for (int i = 0; i < propertyCount; i++) {
    string name = shader.GetPropertyName(i);
    ShaderPropertyType type = shader.GetPropertyType(i);
    string description = shader.GetPropertyDescription(i);
}
```

## Textures

### Loading Textures
```csharp
// From Resources
Texture2D tex = Resources.Load<Texture2D>("Textures/MyTexture");

// Create programmatically
Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);

// Set pixels
Color[] pixels = new Color[256 * 256];
for (int i = 0; i < pixels.Length; i++) {
    pixels[i] = Color.red;
}
tex.SetPixels(pixels);
tex.Apply(); // Upload to GPU

// Load from file
byte[] bytes = File.ReadAllBytes("path/to/image.png");
Texture2D tex = new Texture2D(2, 2);
tex.LoadImage(bytes);
```

### Texture Settings
```csharp
Texture2D tex = ...;

tex.wrapMode = TextureWrapMode.Repeat;
tex.wrapMode = TextureWrapMode.Clamp;
tex.filterMode = FilterMode.Point; // Pixelated
tex.filterMode = FilterMode.Bilinear; // Smooth
tex.filterMode = FilterMode.Trilinear; // Smooth with mipmaps

tex.anisoLevel = 9; // Anisotropic filtering

// Mipmaps
tex.mipMapBias = 0;
tex.autoGenerateMips = true;
```

### Render Textures
```csharp
// Create render texture
RenderTexture rt = new RenderTexture(1920, 1080, 24);
rt.Create();

// Use with camera
Camera cam = GetComponent<Camera>();
cam.targetTexture = rt;

// Use as texture
Material mat = GetComponent<Renderer>().material;
mat.SetTexture("_MainTex", rt);

// Release
rt.Release();
Destroy(rt);
```

## Lighting

### Light Component
```csharp
Light light = GetComponent<Light>();

// Type
light.type = LightType.Directional;
light.type = LightType.Point;
light.type = LightType.Spot;
light.type = LightType.Area;

// Color and intensity
light.color = Color.white;
light.intensity = 1f;
light.range = 10f; // For point/spot
light.spotAngle = 45f; // For spot

// Shadows
light.shadows = LightShadows.None;
light.shadows = LightShadows.Hard;
light.shadows = LightShadows.Soft;
light.shadowStrength = 0.8f;
light.shadowBias = 0.05f;

// Baking
light.lightmapBakeType = LightmapBakeType.Realtime;
light.lightmapBakeType = LightmapBakeType.Baked;
light.lightmapBakeType = LightmapBakeType.Mixed;
```

### Global Illumination
```csharp
// Ambient
RenderSettings.ambientMode = AmbientMode.Skybox;
RenderSettings.ambientMode = AmbientMode.Trilight;
RenderSettings.ambientMode = AmbientMode.Flat;
RenderSettings.ambientLight = Color.gray;
RenderSettings.ambientIntensity = 1f;

// Reflections
RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
RenderSettings.customReflection = myCubemap;
RenderSettings.reflectionIntensity = 1f;
RenderSettings.reflectionBounces = 1;
```

## Camera

### Camera Properties
```csharp
Camera cam = Camera.main;

// Projection
cam.orthographic = false; // Perspective
cam.orthographic = true; // Orthographic
cam.orthographicSize = 5f; // Half-height in world units

// Field of view
cam.fieldOfView = 60f;

// Clipping planes
cam.nearClipPlane = 0.3f;
cam.farClipPlane = 1000f;

// Viewport rect (split screen)
cam.rect = new Rect(0, 0, 0.5f, 1); // Left half
cam.rect = new Rect(0.5f, 0, 0.5f, 1); // Right half

// Background
cam.clearFlags = CameraClearFlags.Skybox;
cam.clearFlags = CameraClearFlags.SolidColor;
cam.clearFlags = CameraClearFlags.Depth;
cam.backgroundColor = Color.black;

// Culling mask
cam.cullingMask = LayerMask.GetMask("Default", "Enemies");
cam.cullingMask = ~LayerMask.GetMask("UI"); // Exclude UI
```

### Camera Effects
```csharp
// Depth texture
cam.depthTextureMode = DepthTextureMode.Depth;
cam.depthTextureMode = DepthTextureMode.DepthNormals;

// Occlusion culling
cam.useOcclusionCulling = true;

// HDR
cam.allowHDR = true;

// MSAA
cam.allowMSAA = true;

// Dynamic resolution
cam.allowDynamicResolution = true;
```

### Camera Transforms
```csharp
// World to screen
Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
Vector3 viewportPos = cam.WorldToViewportPoint(worldPos);

// Screen to world
Ray ray = cam.ScreenPointToRay(Input.mousePosition);
Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mouseX, mouseY, 10f));

// Viewport to world
Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Center
```

## Post-Processing

### Built-in (Legacy)
```csharp
// Add Post Process Layer component to camera
// Add Post Process Volume to scene

// Control via script
PostProcessVolume volume = GetComponent<PostProcessVolume>();
volume.weight = 0.5f;
volume.isGlobal = true;

// Modify profile
PostProcessProfile profile = volume.profile;

// Get effects
if (profile.TryGetSettings(out ColorGrading colorGrading)) {
    colorGrading.saturation.value = 50f;
}

if (profile.TryGetSettings(out Bloom bloom)) {
    bloom.intensity.value = 5f;
    bloom.threshold.value = 0.8f;
}

if (profile.TryGetSettings(out DepthOfField dof)) {
    dof.focusDistance.value = 10f;
    dof.aperture.value = 5.6f;
}
```

### URP/HDRP Post-Processing
```csharp
// Volume component with overrides
Volume volume = GetComponent<Volume>();

// Access profile
VolumeProfile profile = volume.profile;

// Get override
if (profile.TryGet<Bloom>(out Bloom bloom)) {
    bloom.intensity.value = 1f;
    bloom.scatter.value = 0.7f;
}

if (profile.TryGet<ColorAdjustments>(out ColorAdjustments colorAdj)) {
    colorAdj.saturation.value = 20f;
    colorAdj.contrast.value = 10f;
}

if (profile.TryGet<Vignette>(out Vignette vignette)) {
    vignette.intensity.value = 0.4f;
}
```

## Particle Systems

### Basic Control
```csharp
ParticleSystem ps = GetComponent<ParticleSystem>();

// Play/Stop
ps.Play();
ps.Stop();
ps.Pause();
ps.Clear();

// Emit burst
ps.Emit(100);

// Check if playing
bool isPlaying = ps.isPlaying;
bool isEmitting = ps.isEmitting;

// Duration
float duration = ps.main.duration;
```

### Main Module
```csharp
var main = ps.main;

main.startLifetime = 5f;
main.startSpeed = 10f;
main.startSize = 1f;
main.startColor = Color.red;
main.startRotation = 0f;

main.duration = 2f;
main.loop = true;
main.prewarm = false;

main.maxParticles = 1000;
main.gravityModifier = 0.5f;
main.simulationSpace = ParticleSystemSimulationSpace.Local;
main.simulationSpace = ParticleSystemSimulationSpace.World;
```

### Emission Module
```csharp
var emission = ps.emission;

emission.enabled = true;
emission.rateOverTime = 50f;
emission.rateOverDistance = 10f;

// Bursts
emission.SetBursts(new ParticleSystem.Burst[] {
    new ParticleSystem.Burst(0f, 100),
    new ParticleSystem.Burst(1f, 50)
});
```

### Shape Module
```csharp
var shape = ps.shape;

shape.shapeType = ParticleSystemShapeType.Cone;
shape.shapeType = ParticleSystemShapeType.Sphere;
shape.shapeType = ParticleSystemShapeType.Box;
shape.shapeType = ParticleSystemShapeType.Circle;

shape.scale = new Vector3(1, 1, 1);
shape.position = Vector3.zero;
shape.rotation = Vector3.zero;
```

### Other Modules
```csharp
// Velocity over lifetime
var velocity = ps.velocityOverLifetime;
velocity.enabled = true;
velocity.space = ParticleSystemSimulationSpace.Local;
velocity.x = new ParticleSystem.MinMaxCurve(0, 10);

// Color over lifetime
var color = ps.colorOverLifetime;
color.enabled = true;
color.color = new ParticleSystem.MinMaxGradient(Color.red, Color.blue);

// Size over lifetime
var size = ps.sizeOverLifetime;
size.enabled = true;
size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
    new Keyframe(0, 0),
    new Keyframe(0.5f, 1),
    new Keyframe(1, 0)
));

// Collision
var collision = ps.collision;
collision.enabled = true;
collision.type = ParticleSystemCollisionType.World;
collision.mode = ParticleSystemCollisionMode.Collision3D;
```

## Line Renderer

```csharp
LineRenderer lr = GetComponent<LineRenderer>();

// Set points
lr.positionCount = 2;
lr.SetPosition(0, startPos);
lr.SetPosition(1, endPos);

// Multiple points
lr.positionCount = points.Length;
lr.SetPositions(points);

// Appearance
lr.startWidth = 0.1f;
lr.endWidth = 0.1f;
lr.startColor = Color.red;
lr.endColor = Color.blue;
lr.material = lineMaterial;

// Texture
lr.textureMode = LineTextureMode.Tile;
lr.textureMode = LineTextureMode.Stretch;
```

## Trail Renderer

```csharp
TrailRenderer trail = GetComponent<TrailRenderer>();

// Time and width
trail.time = 1f; // How long trail lasts
trail.startWidth = 0.5f;
trail.endWidth = 0f;

// Colors
trail.startColor = Color.white;
trail.endColor = Color.clear;

// Material
trail.material = trailMaterial;

// Min vertex distance
trail.minVertexDistance = 0.1f;
```
