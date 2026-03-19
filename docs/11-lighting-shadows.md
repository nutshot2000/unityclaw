# Unity Lighting & Shadows

## Light Types

### Directional Light
```csharp
Light light = GetComponent<Light>();
light.type = LightType.Directional;

// Properties
light.color = Color.white;
light.intensity = 1f;
light.shadows = LightShadows.Soft;
light.shadowStrength = 0.8f;
light.shadowBias = 0.05f;
light.shadowNormalBias = 0.4f;
light.shadowNearPlane = 0.2f;
```

### Point Light
```csharp
light.type = LightType.Point;
light.range = 10f; // Affects how far light reaches
light.intensity = 1f;
light.color = Color.yellow;
```

### Spot Light
```csharp
light.type = LightType.Spot;
light.spotAngle = 45f; // Cone angle
light.innerSpotAngle = 30f; // Inner cone (for soft edge)
light.range = 10f;
light.intensity = 1f;
```

### Area Light (Baked only)
```csharp
light.type = LightType.Area;
light.areaSize = new Vector2(10, 10); // Rectangle size
// Area lights only work with baked lighting
```

## Global Illumination

### Ambient Settings
```csharp
// Ambient mode
RenderSettings.ambientMode = AmbientMode.Skybox;
RenderSettings.ambientMode = AmbientMode.Trilight; // Gradient
RenderSettings.ambientMode = AmbientMode.Flat; // Single color

// Colors
RenderSettings.ambientSkyColor = Color.blue;
RenderSettings.ambientEquatorColor = Color.cyan;
RenderSettings.ambientGroundColor = Color.gray;
RenderSettings.ambientLight = Color.gray; // Flat mode

// Intensity
RenderSettings.ambientIntensity = 1f;
```

### Reflections
```csharp
// Reflection source
RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

// Custom reflection
RenderSettings.customReflection = myCubemap;
RenderSettings.reflectionIntensity = 1f;
RenderSettings.reflectionBounces = 1;
```

## Light Baking

### Light Settings
```csharp
Light light = GetComponent<Light>();

// Bake type
light.lightmapBakeType = LightmapBakeType.Realtime;
light.lightmapBakeType = LightmapBakeType.Baked;
light.lightmapBakeType = LightmapBakeType.Mixed;

// For baked/mixed
light.bakingOutput.isBaked = true;
```

### Mesh Renderer Lightmap
```csharp
MeshRenderer renderer = GetComponent<MeshRenderer>();

// Contribute to GI
renderer.receiveGI = ReceiveGI.Lightmaps;
renderer.receiveGI = ReceiveGI.LightProbes;

// Scale in lightmap
renderer.scaleInLightmap = 1f;

// Stitch seams
renderer.stitchLightmapSeams = true;
```

## Light Probes

### Setup
```csharp
// Light Probe Group component
LightProbeGroup probeGroup = GetComponent<LightProbeGroup>();

// Get probe positions
Vector3[] positions = probeGroup.probePositions;

// Set positions
probeGroup.probePositions = new Vector3[] {
    new Vector3(0, 0, 0),
    new Vector3(10, 0, 0),
    new Vector3(0, 10, 0)
};
```

### Using Light Probes
```csharp
// Renderer automatically uses light probes when set to "Blend Probes"
MeshRenderer renderer = GetComponent<MeshRenderer>();
renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
renderer.lightProbeUsage = LightProbeUsage.Off;
renderer.lightProbeUsage = LightProbeUsage.UseProxyVolume;

// For moving objects
Renderer renderer = GetComponent<Renderer>();
LightProbes.GetInterpolatedProbe(transform.position, renderer, out SphericalHarmonicsL2 probe);
```

## Reflection Probes

### Setup
```csharp
ReflectionProbe probe = GetComponent<ReflectionProbe>();

// Type
probe.mode = ReflectionProbeMode.Realtime;
probe.mode = ReflectionProbeMode.Baked;
probe.mode = ReflectionProbeMode.Custom;

// Box projection
probe.boxProjection = true;

// Bounds
probe.bounds = new Bounds(Vector3.zero, new Vector3(10, 10, 10));

// Resolution
probe.resolution = 128;

// Refresh
probe.RenderProbe(); // Manual refresh
probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
```

## Shadows

### Shadow Settings
```csharp
// Quality Settings (Edit -> Project Settings -> Quality)
QualitySettings.shadows = ShadowQuality.All;
QualitySettings.shadowResolution = ShadowResolution.High;
QualitySettings.shadowDistance = 150f;
QualitySettings.shadowCascadeCount = 4;
QualitySettings.shadowCascades = new Vector3(0.067f, 0.2f, 0.5f);
```

### Light Shadow Properties
```csharp
Light light = GetComponent<Light>();

// Shadow type
light.shadows = LightShadows.None;
light.shadows = LightShadows.Hard;
light.shadows = LightShadows.Soft;

// Shadow strength
light.shadowStrength = 1f; // 0 = no shadow, 1 = full shadow

// Shadow resolution
light.shadowResolution = LightShadowResolution.FromQualitySettings;
light.shadowResolution = LightShadowResolution.Low;
light.shadowResolution = LightShadowResolution.Medium;
light.shadowResolution = LightShadowResolution.High;
light.shadowResolution = LightShadowResolution.VeryHigh;

// Shadow bias (prevents shadow acne)
light.shadowBias = 0.05f;
light.shadowNormalBias = 0.4f;
light.shadowNearPlane = 0.2f;
```

### Renderer Shadow Settings
```csharp
MeshRenderer renderer = GetComponent<MeshRenderer>();

// Cast shadows
renderer.shadowCastingMode = ShadowCastingMode.On;
renderer.shadowCastingMode = ShadowCastingMode.Off;
renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

// Receive shadows
renderer.receiveShadows = true;
```

## Volumetric Lighting (HDRP/URP)

### URP Volumetric Fog
```csharp
// Requires URP with volumetric fog enabled
// Window -> Rendering -> URP Global Settings

Volume volume = GetComponent<Volume>();

// Fog
if (volume.profile.TryGet<Fog>(out Fog fog)) {
    fog.enabled.value = true;
    fog.color.value = Color.gray;
    fog.density.value = 0.5f;
    fog.start.value = 0f;
    fog.end.value = 100f;
}

// Volumetric Lighting
if (volume.profile.TryGet<VolumetricLighting>(out VolumetricLighting vol)) {
    vol.enabled.value = true;
    vol.scattering.value = 0.5f;
}
```

## Light Cookies

```csharp
Light light = GetComponent<Light>();

// Assign cookie texture (must be imported as Cookie)
light.cookie = cookieTexture;
light.cookieSize = 10f;

// For spot lights, cookie projects texture in cone
// For point lights, requires cubemap cookie
```

## Emission

### Material Emission
```csharp
Material mat = GetComponent<Renderer>().material;

// Enable emission
mat.EnableKeyword("_EMISSION");

// Set emission color
mat.SetColor("_EmissionColor", Color.white * 2f); // Multiplier for HDR

// Texture
mat.SetTexture("_EmissionMap", emissionTexture);

// Global Illumination
mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
```

## Light Layers (URP/HDRP)

```csharp
// Setup light layers
Light light = GetComponent<Light>();
light.lightLayers = LightLayer.Layer0 | LightLayer.Layer1;

// Setup mesh renderer
MeshRenderer renderer = GetComponent<MeshRenderer>();
renderer.renderingLayerMask = (uint)LightLayer.Layer0;
```

## Common Patterns

### Day/Night Cycle
```csharp
public class DayNightCycle : MonoBehaviour {
    [SerializeField] private Light sunLight;
    [SerializeField] private float dayLength = 120f; // seconds
    
    private float timeOfDay = 0f; // 0-1
    
    void Update() {
        timeOfDay += Time.deltaTime / dayLength;
        if (timeOfDay >= 1f) timeOfDay = 0f;
        
        // Rotate sun
        float sunAngle = timeOfDay * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 0, 0);
        
        // Change intensity
        float intensity = Mathf.Clamp01(Mathf.Sin(timeOfDay * Mathf.PI * 2f));
        sunLight.intensity = intensity;
        
        // Change color (warm at sunrise/sunset, white at noon)
        sunLight.color = Color.Lerp(Color.red, Color.white, intensity);
    }
}
```

### Flickering Light
```csharp
public class FlickeringLight : MonoBehaviour {
    [SerializeField] private Light light;
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 1.5f;
    [SerializeField] private float flickerSpeed = 0.1f;
    
    void Update() {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0);
        light.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
```

### Light Switch
```csharp
public class LightSwitch : MonoBehaviour {
    [SerializeField] private Light[] lights;
    private bool isOn = true;
    
    public void Toggle() {
        isOn = !isOn;
        foreach (Light light in lights) {
            light.enabled = isOn;
        }
    }
    
    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            Toggle();
        }
    }
}
```

### Flashlight
```csharp
public class Flashlight : MonoBehaviour {
    [SerializeField] private Light flashlight;
    [SerializeField] private KeyCode toggleKey = KeyCode.F;
    
    void Update() {
        if (Input.GetKeyDown(toggleKey)) {
            flashlight.enabled = !flashlight.enabled;
        }
        
        // Optional: Battery drain
        if (flashlight.enabled) {
            flashlight.intensity -= Time.deltaTime * 0.1f;
            if (flashlight.intensity <= 0) {
                flashlight.enabled = false;
            }
        }
    }
}
```

## Performance Tips

### Shadow Distance
```csharp
// Reduce shadow distance for performance
QualitySettings.shadowDistance = 100f; // Default is often 150

// Or per-camera
Camera.main.shadowDistance = 100f;
```

### Light Culling
```csharp
// Disable lights that are far from camera
void Update() {
    float distance = Vector3.Distance(transform.position, Camera.main.transform.position);
    light.enabled = distance < light.range * 2f;
}
```

### Batching Lights
```csharp
// Use fewer, stronger lights instead of many weak ones
// Combine nearby lights into one
```
