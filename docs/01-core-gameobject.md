# Unity GameObject & Component System

## GameObject

### Creating GameObjects
```csharp
// Create empty GameObject
GameObject go = new GameObject("MyObject");

// Create with components
GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

// Instantiate from prefab
GameObject instance = Instantiate(prefab, position, rotation);
GameObject instance = Instantiate(prefab, position, rotation, parent);
```

### Finding GameObjects
```csharp
// By name
GameObject player = GameObject.Find("Player");

// By tag
GameObject enemy = GameObject.FindWithTag("Enemy");
GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

// By type
MyScript script = FindObjectOfType<MyScript>();
MyScript[] scripts = FindObjectsOfType<MyScript>();

// With specific component
PlayerController pc = FindObjectOfType<PlayerController>();
```

### GameObject Properties
```csharp
gameObject.name = "NewName";
gameObject.tag = "Player";
gameObject.layer = LayerMask.NameToLayer("Enemies");
gameObject.SetActive(true);
gameObject.activeInHierarchy; // Is active considering parents
gameObject.activeSelf; // Is active ignoring parents
gameObject.transform;
gameObject.scene;
gameObject.isStatic;
```

### Managing Components
```csharp
// Add component
Rigidbody rb = gameObject.AddComponent<Rigidbody>();
BoxCollider collider = gameObject.AddComponent<BoxCollider>();

// Get component
Rigidbody rb = GetComponent<Rigidbody>();
Rigidbody rb = GetComponentInChildren<Rigidbody>();
Rigidbody rb = GetComponentInParent<Rigidbody>();

// Get all components
Rigidbody[] rbs = GetComponents<Rigidbody>();
Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();

// Remove component
Destroy(rb);
Destroy(GetComponent<Rigidbody>());

// Check if has component
bool hasRigidbody = TryGetComponent<Rigidbody>(out Rigidbody rb);
```

## Component Base Class

### Lifecycle Methods
```csharp
void Awake() { } // Called when script instance is loaded
void OnEnable() { } // Called when object becomes enabled/active
void Start() { } // Called before first frame update
void Update() { } // Called once per frame
void FixedUpdate() { } // Called at fixed time intervals (physics)
void LateUpdate() { } // Called after all Updates
void OnDisable() { } // Called when object becomes disabled/inactive
void OnDestroy() { } // Called when object is destroyed
```

### Component Properties
```csharp
component.gameObject;
component.transform;
component.enabled = true;
component.name; // Same as gameObject.name
component.tag; // Same as gameObject.tag
```

## MonoBehaviour

### Coroutines
```csharp
// Start coroutine
StartCoroutine(MyCoroutine());
StartCoroutine(MyCoroutine(param));

// Stop coroutine
StopCoroutine(MyCoroutine());
StopAllCoroutines();

// Coroutine definition
IEnumerator MyCoroutine() {
    yield return null; // Wait one frame
    yield return new WaitForSeconds(2f); // Wait 2 seconds
    yield return new WaitForFixedUpdate(); // Wait for next FixedUpdate
    yield return new WaitForEndOfFrame(); // Wait until end of frame
    yield return StartCoroutine(AnotherCoroutine()); // Wait for other coroutine
}

// Coroutine with parameter
IEnumerator MyCoroutine(float delay) {
    yield return new WaitForSeconds(delay);
}
```

### Instantiation & Destruction
```csharp
// Instantiate
GameObject obj = Instantiate(prefab);
GameObject obj = Instantiate(prefab, position, rotation);
GameObject obj = Instantiate(prefab, position, rotation, parent);

// Destroy
Destroy(gameObject);
Destroy(component);
Destroy(gameObject, 5f); // Destroy after 5 seconds

// Don't destroy on scene load
DontDestroyOnLoad(gameObject);
```

### Invoke Methods
```csharp
// Call method after delay
Invoke("MethodName", 2f);

// Call method repeatedly
InvokeRepeating("MethodName", initialDelay, repeatRate);

// Cancel
CancelInvoke();
CancelInvoke("MethodName");
bool isInvoked = IsInvoking("MethodName");
```

### Context Menu
```csharp
[ContextMenu("Do Something")]
void DoSomething() {
    // This appears in component's context menu
}
```

## Execution Order

### Default Order
1. Awake()
2. OnEnable()
3. Start()
4. FixedUpdate() (physics)
5. Update()
6. LateUpdate()

### Custom Execution Order
```csharp
[DefaultExecutionOrder(100)]
public class MyScript : MonoBehaviour { }
```
