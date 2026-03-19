# Unity Networking

## Netcode for GameObjects (NGO)

### Setup
```csharp
// Install package: com.unity.netcode.gameobjects
// Requires Unity Transport (com.unity.transport)
```

### Network Manager
```csharp
// Singleton pattern
NetworkManager networkManager = NetworkManager.Singleton;

// Start as host (server + client)
networkManager.StartHost();

// Start as server only
networkManager.StartServer();

// Start as client
networkManager.StartClient();

// Stop
networkManager.Shutdown();

// Connection events
networkManager.OnClientConnectedCallback += OnClientConnected;
networkManager.OnClientDisconnectCallback += OnClientDisconnected;

void OnClientConnected(ulong clientId) {
    Debug.Log($"Client {clientId} connected");
}
```

### NetworkObject
```csharp
// Mark object for networking
public class PlayerController : NetworkBehaviour {
    
    void Update() {
        // Only run on owner
        if (!IsOwner) return;
        
        // Local input
        float horizontal = Input.GetAxis("Horizontal");
        Move(horizontal);
    }
    
    void Move(float direction) {
        // Call server RPC
        MoveServerRpc(direction);
    }
    
    [ServerRpc]
    void MoveServerRpc(float direction) {
        // Runs on server
        transform.position += Vector3.right * direction * speed * Time.deltaTime;
        
        // Server updates position for all clients
        UpdatePositionClientRpc(transform.position);
    }
    
    [ClientRpc]
    void UpdatePositionClientRpc(Vector3 newPosition) {
        // Runs on all clients
        transform.position = newPosition;
    }
}
```

### Network Variables
```csharp
public class PlayerStats : NetworkBehaviour {
    // Synced variable
    public NetworkVariable<int> health = new NetworkVariable<int>(100);
    public NetworkVariable<float> speed = new NetworkVariable<float>(5f);
    
    void Start() {
        // Subscribe to changes
        health.OnValueChanged += OnHealthChanged;
    }
    
    void OnHealthChanged(int previous, int current) {
        Debug.Log($"Health changed from {previous} to {current}");
    }
    
    void TakeDamage(int damage) {
        // Only server can modify
        if (IsServer) {
            health.Value -= damage;
        }
    }
}
```

### Network Transform
```csharp
// Add NetworkTransform component
// Automatically syncs position, rotation, scale

// Configure interpolation
NetworkTransform netTransform = GetComponent<NetworkTransform>();
netTransform.Interpolate = true;

// Client authority (for player)
netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
```

## Mirror (Third-party)

### Setup
```csharp
// Install from Asset Store or Package Manager
// Mirror is a popular third-party networking solution
```

### Network Manager
```csharp
using Mirror;

public class MyNetworkManager : NetworkManager {
    public override void OnStartServer() {
        base.OnStartServer();
        Debug.Log("Server started");
    }
    
    public override void OnClientConnect() {
        base.OnClientConnect();
        Debug.Log("Connected to server");
    }
}
```

### Network Behaviour
```csharp
using Mirror;

public class Player : NetworkBehaviour {
    [SyncVar]
    public int health = 100;
    
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;
    
    void OnNameChanged(string oldName, string newName) {
        Debug.Log($"Name changed from {oldName} to {newName}");
    }
    
    void Update() {
        // Check if local player
        if (!isLocalPlayer) return;
        
        // Handle input
        if (Input.GetKeyDown(KeyCode.Space)) {
            CmdJump();
        }
    }
    
    [Command]
    void CmdJump() {
        // Runs on server
        RpcJump();
    }
    
    [ClientRpc]
    void RpcJump() {
        // Runs on all clients
        GetComponent<Rigidbody>().AddForce(Vector3.up * 10f, ForceMode.Impulse);
    }
    
    [TargetRpc]
    void TargetPrivateMessage(NetworkConnection target, string message) {
        // Runs only on specific client
        Debug.Log(message);
    }
}
```

## Save/Load Systems

### PlayerPrefs (Simple)
```csharp
// Save
PlayerPrefs.SetInt("HighScore", 1000);
PlayerPrefs.SetFloat("Volume", 0.8f);
PlayerPrefs.SetString("PlayerName", "Jimmy");
PlayerPrefs.Save(); // Write to disk

// Load
int score = PlayerPrefs.GetInt("HighScore", 0);
float volume = PlayerPrefs.GetFloat("Volume", 1f);
string name = PlayerPrefs.GetString("PlayerName", "Player");

// Check if exists
bool hasKey = PlayerPrefs.HasKey("HighScore");

// Delete
PlayerPrefs.DeleteKey("HighScore");
PlayerPrefs.DeleteAll();
```

### JSON Serialization
```csharp
using System.IO;

[System.Serializable]
public class SaveData {
    public int level;
    public int health;
    public float[] position;
    public List<string> inventory;
}

public class SaveSystem : MonoBehaviour {
    string savePath;
    
    void Start() {
        savePath = Path.Combine(Application.persistentDataPath, "save.json");
    }
    
    public void SaveGame() {
        SaveData data = new SaveData {
            level = currentLevel,
            health = playerHealth,
            position = new float[] { transform.position.x, transform.position.y, transform.position.z },
            inventory = inventoryItems
        };
        
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Saved to: {savePath}");
    }
    
    public void LoadGame() {
        if (File.Exists(savePath)) {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            
            currentLevel = data.level;
            playerHealth = data.health;
            transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
            inventoryItems = data.inventory;
        }
    }
}
```

### Binary Serialization
```csharp
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public void SaveBinary() {
    SaveData data = GetSaveData();
    
    BinaryFormatter formatter = new BinaryFormatter();
    FileStream stream = new FileStream(savePath, FileMode.Create);
    formatter.Serialize(stream, data);
    stream.Close();
}

public void LoadBinary() {
    if (File.Exists(savePath)) {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath, FileMode.Open);
        SaveData data = formatter.Deserialize(stream) as SaveData;
        stream.Close();
        
        ApplySaveData(data);
    }
}
```

## Object Pooling

### Simple Pool
```csharp
public class ObjectPool : MonoBehaviour {
    [SerializeField] private GameObject prefab;
    [SerializeField] private int poolSize = 50;
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    
    void Start() {
        // Pre-instantiate
        for (int i = 0; i < poolSize; i++) {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    
    public GameObject Get() {
        if (pool.Count > 0) {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        
        // Pool empty, create new
        return Instantiate(prefab);
    }
    
    public void Return(GameObject obj) {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}

// Usage
public class BulletSpawner : MonoBehaviour {
    [SerializeField] private ObjectPool bulletPool;
    
    void Fire() {
        GameObject bullet = bulletPool.Get();
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;
        
        // Return after delay
        StartCoroutine(ReturnBullet(bullet, 2f));
    }
    
    IEnumerator ReturnBullet(GameObject bullet, float delay) {
        yield return new WaitForSeconds(delay);
        bulletPool.Return(bullet);
    }
}
```

### Generic Pool
```csharp
public class GenericPool<T> where T : MonoBehaviour {
    private T prefab;
    private Queue<T> pool = new Queue<T>();
    
    public GenericPool(T prefab, int size) {
        this.prefab = prefab;
        
        for (int i = 0; i < size; i++) {
            T obj = Object.Instantiate(prefab);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    
    public T Get() {
        if (pool.Count > 0) {
            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        return Object.Instantiate(prefab);
    }
    
    public void Return(T obj) {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}

// Usage
GenericPool<Bullet> bulletPool = new GenericPool<Bullet>(bulletPrefab, 50);
Bullet bullet = bulletPool.Get();
bulletPool.Return(bullet);
```

## Coroutines vs Async

### Coroutines
```csharp
// Start coroutine
StartCoroutine(MyCoroutine());
StartCoroutine(MyCoroutine(param));

// Definition
IEnumerator MyCoroutine() {
    yield return null; // Wait one frame
    yield return new WaitForSeconds(2f);
    yield return new WaitForFixedUpdate();
    yield return new WaitForEndOfFrame();
    yield return new WaitUntil(() => condition);
    yield return new WaitWhile(() => condition);
    yield return StartCoroutine(OtherCoroutine());
}

// Stop
StopCoroutine(MyCoroutine());
StopAllCoroutines();
```

### Async/Await
```csharp
using System.Threading.Tasks;

async void Start() {
    await Task.Delay(2000); // Wait 2 seconds
    Debug.Log("Done");
}

async Task<int> GetResultAsync() {
    await Task.Delay(1000);
    return 42;
}

// On main thread
async void LoadData() {
    var result = await GetResultAsync();
    // Back on main thread
}

// Background thread
async void ProcessData() {
    await Task.Run(() => {
        // Runs on background thread
        HeavyCalculation();
    });
    // Back on main thread
}

// Cancellation
CancellationTokenSource cts = new CancellationTokenSource();

async Task LongOperation(CancellationToken token) {
    while (!token.IsCancellationRequested) {
        await Task.Delay(100);
    }
}

void Cancel() {
    cts.Cancel();
}
```

## Jobs System (Burst)

### Simple Job
```csharp
using Unity.Jobs;
using Unity.Collections;

struct MyJob : IJob {
    public float deltaTime;
    public NativeArray<float> values;
    
    public void Execute() {
        for (int i = 0; i < values.Length; i++) {
            values[i] += deltaTime;
        }
    }
}

// Schedule
NativeArray<float> values = new NativeArray<float>(100, Allocator.TempJob);

MyJob job = new MyJob {
    deltaTime = Time.deltaTime,
    values = values
};

JobHandle handle = job.Schedule();
handle.Complete(); // Wait for completion

// Cleanup
values.Dispose();
```

### Parallel Job
```csharp
struct ParallelJob : IJobParallelFor {
    [ReadOnly] public NativeArray<float> input;
    [WriteOnly] public NativeArray<float> output;
    
    public void Execute(int index) {
        output[index] = input[index] * 2;
    }
}

// Schedule
ParallelJob job = new ParallelJob {
    input = inputArray,
    output = outputArray
};

JobHandle handle = job.Schedule(inputArray.Length, 32); // Batch size
handle.Complete();
```

## Addressables

### Setup
```csharp
// Install package: com.unity.addressables
// Window -> Asset Management -> Addressables
// Mark assets as Addressable
```

### Loading
```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// Load asset
AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>("PlayerPrefab");

// Callback
handle.Completed += OnAssetLoaded;

void OnAssetLoaded(AsyncOperationHandle<GameObject> obj) {
    if (obj.Status == AsyncOperationStatus.Succeeded) {
        Instantiate(obj.Result);
    }
}

// Or await
async void LoadAsset() {
    GameObject prefab = await Addressables.LoadAssetAsync<GameObject>("PlayerPrefab").Task;
    Instantiate(prefab);
}

// Release
Addressables.Release(handle);
```

### Scene Loading
```csharp
// Load scene
AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync("Level1", LoadSceneMode.Single);

// Unload
Addressables.UnloadSceneAsync(handle);
```

## Timeline/Cinemachine

### Cinemachine Virtual Camera
```csharp
using Cinemachine;

CinemachineVirtualCamera vcam = GetComponent<CinemachineVirtualCamera>();

// Follow target
vcam.Follow = playerTransform;

// Look at
vcam.LookAt = targetTransform;

// Get components
CinemachineTransposer transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
CinemachineComposer composer = vcam.GetCinemachineComponent<CinemachineComposer>();

// Change priority
vcam.Priority = 10; // Higher = active
```

### Timeline Control
```csharp
using UnityEngine.Playables;
using UnityEngine.Timeline;

PlayableDirector director = GetComponent<PlayableDirector>();

// Play
director.Play();
director.Play(timelineAsset);

// Pause/Resume
director.Pause();
director.Resume();

// Stop
director.Stop();

// Time control
director.time = 5f; // Jump to 5 seconds
director.time = director.duration; // End

// Speed
director.playableGraph.GetRootPlayable(0).SetSpeed(2f);

// Bindings
var binding = director.playableAsset.outputs.First();
director.SetGenericBinding(binding.sourceObject, targetGameObject);
```

## Analytics

### Unity Analytics
```csharp
// Install package: com.unity.analytics

// Custom event
Analytics.CustomEvent("game_start", new Dictionary<string, object> {
    { "level", 1 },
    { "difficulty", "normal" }
});

// Transaction
Analytics.Transaction("item_purchase", 0.99m, "USD", null, null);

// Progression
AnalyticsEvent.LevelStart(1);
AnalyticsEvent.LevelComplete(1, 100); // Level, score
AnalyticsEvent.LevelFail(1);
```

## Unity Cloud Services

### Remote Config
```csharp
// Install package: com.unity.remote-config

async void FetchConfig() {
    await RemoteConfigService.Instance.FetchConfigsAsync(new userAttributes(), new appAttributes());
    
    string welcomeMessage = RemoteConfigService.Instance.appConfig.GetString("welcome_message");
    int maxLives = RemoteConfigService.Instance.appConfig.GetInt("max_lives");
}
```

### Cloud Save
```csharp
// Install package: com.unity.services.cloudsave

async void SaveToCloud() {
    var data = new Dictionary<string, object> {
        { "level", 5 },
        { "coins", 1000 }
    };
    
    await CloudSaveService.Instance.Data.Player.SaveAsync(data);
}

async void LoadFromCloud() {
    var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "level", "coins" });
    int level = (int)data["level"];
}
```
