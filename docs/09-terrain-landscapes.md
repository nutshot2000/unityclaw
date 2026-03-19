# Unity Terrain & Landscapes

## Terrain Component

### Creating Terrain
```csharp
// Create new terrain
TerrainData terrainData = new TerrainData();
terrainData.size = new Vector3(1000, 600, 1000); // Width, Height, Depth
terrainData.heightmapResolution = 513; // Must be power of 2 + 1

GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
Terrain terrain = terrainObject.GetComponent<Terrain>();
```

### Terrain Settings
```csharp
Terrain terrain = GetComponent<Terrain>();
TerrainData td = terrain.terrainData;

// Size
td.size = new Vector3(1000, 600, 1000);

// Heightmap resolution
td.heightmapResolution = 513; // 513x513 vertices

// Detail resolution (grass, small objects)
td.SetDetailResolution(1024, 32); // Resolution, patches per tile

// Base texture resolution
td.baseMapResolution = 1024;
```

## Height Manipulation

### Setting Heights
```csharp
TerrainData td = terrain.terrainData;
int resolution = td.heightmapResolution;

// Get current heights
float[,] heights = td.GetHeights(0, 0, resolution, resolution);

// Modify heights
for (int x = 0; x < resolution; x++) {
    for (int y = 0; y < resolution; y++) {
        // Create a hill
        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(resolution/2, resolution/2));
        heights[x, y] = Mathf.Max(0, 1 - distance / 100) * 0.5f;
    }
}

// Apply heights
td.SetHeights(0, 0, heights);
```

### Perlin Noise Terrain
```csharp
void GeneratePerlinTerrain(TerrainData td, float scale, float heightMultiplier) {
    int resolution = td.heightmapResolution;
    float[,] heights = new float[resolution, resolution];
    
    for (int x = 0; x < resolution; x++) {
        for (int y = 0; y < resolution; y++) {
            float xCoord = (float)x / resolution * scale;
            float yCoord = (float)y / resolution * scale;
            heights[x, y] = Mathf.PerlinNoise(xCoord, yCoord) * heightMultiplier;
        }
    }
    
    td.SetHeights(0, 0, heights);
}
```

### Smooth Terrain
```csharp
void SmoothTerrain(TerrainData td, int iterations = 1) {
    int resolution = td.heightmapResolution;
    float[,] heights = td.GetHeights(0, 0, resolution, resolution);
    
    for (int i = 0; i < iterations; i++) {
        float[,] newHeights = new float[resolution, resolution];
        
        for (int x = 1; x < resolution - 1; x++) {
            for (int y = 1; y < resolution - 1; y++) {
                float avg = (heights[x-1, y] + heights[x+1, y] + 
                            heights[x, y-1] + heights[x, y+1]) / 4f;
                newHeights[x, y] = avg;
            }
        }
        heights = newHeights;
    }
    
    td.SetHeights(0, 0, heights);
}
```

## Terrain Textures (Splat Maps)

### Painting Textures
```csharp
TerrainData td = terrain.terrainData;

// Get alphamaps (splat maps)
int layers = td.alphamapLayers;
int width = td.alphamapWidth;
int height = td.alphamapHeight;
float[,,] alphamaps = td.GetAlphamaps(0, 0, width, height);

// Paint grass texture at center
for (int x = width/2 - 50; x < width/2 + 50; x++) {
    for (int y = height/2 - 50; y < height/2 + 50; y++) {
        alphamaps[x, y, 0] = 0; // Dirt
        alphamaps[x, y, 1] = 1; // Grass
    }
}

// Apply
td.SetAlphamaps(0, 0, alphamaps);
```

### Adding Terrain Layers
```csharp
TerrainLayer grassLayer = new TerrainLayer();
grassLayer.diffuseTexture = grassTexture;
grassLayer.tileSize = new Vector2(15, 15);
grassLayer.tileOffset = Vector2.zero;

TerrainLayer dirtLayer = new TerrainLayer();
dirtLayer.diffuseTexture = dirtTexture;
dirtLayer.tileSize = new Vector2(10, 10);

// Set layers
TerrainData td = terrain.terrainData;
td.terrainLayers = new TerrainLayer[] { dirtLayer, grassLayer };
```

## Trees & Details

### Planting Trees
```csharp
TerrainData td = terrain.terrainData;

// Add tree prototypes
TreePrototype[] prototypes = new TreePrototype[2];
prototypes[0] = new TreePrototype();
prototypes[0].prefab = oakTreePrefab;
prototypes[1] = new TreePrototype();
prototypes[1].prefab = pineTreePrefab;
td.treePrototypes = prototypes;

// Plant trees
List<TreeInstance> trees = new List<TreeInstance>();
for (int i = 0; i < 100; i++) {
    TreeInstance tree = new TreeInstance();
    tree.position = new Vector3(Random.value, 0, Random.value); // Normalized 0-1
    tree.prototypeIndex = Random.Range(0, prototypes.Length);
    tree.widthScale = Random.Range(0.8f, 1.2f);
    tree.heightScale = Random.Range(0.8f, 1.2f);
    tree.color = Color.white;
    tree.lightmapColor = Color.white;
    trees.Add(tree);
}
td.SetTreeInstances(trees.ToArray(), true);
```

### Grass/Details
```csharp
TerrainData td = terrain.terrainData;

// Add detail prototypes
DetailPrototype[] details = new DetailPrototype[1];
details[0] = new DetailPrototype();
details[0].prototypeTexture = grassTexture;
details[0].renderMode = DetailRenderMode.GrassBillboard;
details[0].healthyColor = new Color(0.3f, 0.6f, 0.3f);
details[0].dryColor = new Color(0.6f, 0.5f, 0.3f);
td.detailPrototypes = details;

// Paint grass
int[,] detailMap = new int[td.detailWidth, td.detailHeight];
for (int x = 0; x < td.detailWidth; x++) {
    for (int y = 0; y < td.detailHeight; y++) {
        // Random grass density
        detailMap[x, y] = Random.value > 0.7f ? Random.Range(1, 3) : 0;
    }
}
td.SetDetailLayer(0, 0, 0, detailMap);
```

## Terrain Tools

### Flatten Area
```csharp
void FlattenArea(TerrainData td, Vector3 worldPos, float radius, float height) {
    Vector3 terrainPos = terrain.transform.position;
    Vector3 relativePos = worldPos - terrainPos;
    
    int x = (int)(relativePos.x / td.size.x * td.heightmapResolution);
    int y = (int)(relativePos.z / td.size.z * td.heightmapResolution);
    int r = (int)(radius / td.size.x * td.heightmapResolution);
    
    float[,] heights = td.GetHeights(x - r, y - r, r * 2, r * 2);
    
    for (int i = 0; i < r * 2; i++) {
        for (int j = 0; j < r * 2; j++) {
            float dist = Vector2.Distance(new Vector2(i, j), new Vector2(r, r));
            if (dist < r) {
                heights[i, j] = height / td.size.y;
            }
        }
    }
    
    td.SetHeights(x - r, y - r, heights);
}
```

### Raise/Lower Terrain
```csharp
void ModifyTerrainHeight(TerrainData td, Vector3 worldPos, float radius, float amount) {
    Vector3 terrainPos = terrain.transform.position;
    Vector3 relativePos = worldPos - terrainPos;
    
    int resolution = td.heightmapResolution;
    int x = (int)(relativePos.x / td.size.x * resolution);
    int y = (int)(relativePos.z / td.size.z * resolution);
    int r = (int)(radius / td.size.x * resolution);
    
    float[,] heights = td.GetHeights(Mathf.Max(0, x - r), Mathf.Max(0, y - r), 
                                     Mathf.Min(r * 2, resolution - x), 
                                     Mathf.Min(r * 2, resolution - y));
    
    for (int i = 0; i < heights.GetLength(0); i++) {
        for (int j = 0; j < heights.GetLength(1); j++) {
            float dist = Vector2.Distance(new Vector2(i, j), 
                new Vector2(heights.GetLength(0)/2, heights.GetLength(1)/2));
            float factor = 1 - (dist / r);
            if (factor > 0) {
                heights[i, j] += (amount / td.size.y) * factor;
            }
        }
    }
    
    td.SetHeights(Mathf.Max(0, x - r), Mathf.Max(0, y - r), heights);
}
```

## Terrain Optimization

### LOD Settings
```csharp
Terrain terrain = GetComponent<Terrain>();

// Base map distance (when to show base texture)
terrain.basemapDistance = 1000f;

// Tree distance
terrain.treeDistance = 2000f;
terrain.treeBillboardDistance = 100f;
terrain.treeCrossFadeLength = 25f;

// Detail distance
terrain.detailObjectDistance = 80f;

// Heightmap pixel error
terrain.heightmapPixelError = 5;

// Draw instanced (better performance)
terrain.drawInstanced = true;
```

### Terrain Neighbors (Seamless)
```csharp
Terrain leftTerrain = leftTerrainObject.GetComponent<Terrain>();
Terrain rightTerrain = rightTerrainObject.GetComponent<Terrain>();
Terrain topTerrain = topTerrainObject.GetComponent<Terrain>();
Terrain bottomTerrain = bottomTerrainObject.GetComponent<Terrain>();

terrain.SetNeighbors(leftTerrain, topTerrain, rightTerrain, bottomTerrain);
```

## Water

### Simple Water Plane
```csharp
// Create water plane
GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
water.name = "Water";
water.transform.position = new Vector3(0, 50, 0); // Water level
water.transform.localScale = new Vector3(100, 1, 100);

// Add water material
Renderer renderer = water.GetComponent<Renderer>();
renderer.material = waterMaterial;
```

### Water4 (Legacy)
```csharp
// Add Water4 component (if using legacy water)
WaterBase water = waterObject.AddComponent<WaterBase>();
water.sharedMaterial = waterMaterial;

// Or use the newer Water system from Package Manager
```

## Terrain Collider

```csharp
TerrainCollider tc = terrainObject.GetComponent<TerrainCollider>();

// The collider automatically uses the terrain data
// No additional setup needed

// Raycast against terrain
if (Physics.Raycast(ray, out RaycastHit hit)) {
    float terrainHeight = hit.point.y;
}
```

## Common Patterns

### Get Terrain Height at Position
```csharp
float GetTerrainHeight(Vector3 worldPos) {
    Terrain terrain = Terrain.activeTerrain;
    if (terrain == null) return 0;
    
    return terrain.SampleHeight(worldPos);
}

// Usage
float height = GetTerrainHeight(playerPosition);
playerPosition.y = height + 1f; // Place player 1 unit above terrain
```

### Get Normal at Position
```csharp
Vector3 GetTerrainNormal(Vector3 worldPos) {
    Terrain terrain = Terrain.activeTerrain;
    if (terrain == null) return Vector3.up;
    
    Vector3 terrainPos = terrain.transform.position;
    float x = (worldPos.x - terrainPos.x) / terrain.terrainData.size.x;
    float z = (worldPos.z - terrainPos.z) / terrain.terrainData.size.z;
    
    return terrain.terrainData.GetInterpolatedNormal(x, z);
}
```

### Procedural Terrain Generation
```csharp
void GenerateTerrain(TerrainData td, int octaves, float persistence, float scale) {
    int resolution = td.heightmapResolution;
    float[,] heights = new float[resolution, resolution];
    
    for (int x = 0; x < resolution; x++) {
        for (int y = 0; y < resolution; y++) {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;
            
            for (int i = 0; i < octaves; i++) {
                float xCoord = (float)x / resolution * scale * frequency;
                float yCoord = (float)y / resolution * scale * frequency;
                noiseHeight += Mathf.PerlinNoise(xCoord, yCoord) * amplitude;
                
                amplitude *= persistence;
                frequency *= 2;
            }
            
            heights[x, y] = noiseHeight;
        }
    }
    
    td.SetHeights(0, 0, heights);
}
```
