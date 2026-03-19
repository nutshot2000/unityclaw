# Unity ScriptableObjects

## Creating ScriptableObjects

### Definition
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item")]
public class ItemData : ScriptableObject {
    public string itemName;
    public string description;
    public Sprite icon;
    public int value;
    public int maxStackSize = 99;
    public ItemType type;
    public GameObject prefab;
    
    public enum ItemType {
        Weapon,
        Armor,
        Consumable,
        Material
    }
}
```

### Creating Instances
```csharp
// In Editor: Right-click -> Create -> Game -> Item
// Or programmatically:

ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
newItem.itemName = "Health Potion";
newItem.value = 50;

// Save to asset
#if UNITY_EDITOR
UnityEditor.AssetDatabase.CreateAsset(newItem, "Assets/Items/HealthPotion.asset");
UnityEditor.AssetDatabase.SaveAssets();
#endif
```

## Using ScriptableObjects

### Inventory System
```csharp
public class Inventory : MonoBehaviour {
    [SerializeField] private List<ItemStack> items = new List<ItemStack>();
    
    public void AddItem(ItemData item, int amount = 1) {
        // Check if item already exists
        var existing = items.Find(i => i.item == item);
        if (existing != null) {
            existing.amount += amount;
        } else {
            items.Add(new ItemStack { item = item, amount = amount });
        }
    }
    
    [System.Serializable]
    public class ItemStack {
        public ItemData item;
        public int amount;
    }
}
```

### Game Configuration
```csharp
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Configuration")]
public class GameConfig : ScriptableObject {
    [Header("Player")]
    public float playerMoveSpeed = 5f;
    public float playerJumpForce = 10f;
    public int playerMaxHealth = 100;
    
    [Header("Combat")]
    public float attackCooldown = 0.5f;
    public float damageMultiplier = 1f;
    
    [Header("Economy")]
    public int startingGold = 100;
    public float sellPriceMultiplier = 0.5f;
    
    [Header("References")]
    public GameObject playerPrefab;
    public AudioClip backgroundMusic;
}

// Usage
public class GameManager : MonoBehaviour {
    [SerializeField] private GameConfig config;
    
    void Start() {
        // Access configuration
        float speed = config.playerMoveSpeed;
        int health = config.playerMaxHealth;
    }
}
```

### Event System
```csharp
[CreateAssetMenu(fileName = "GameEvent", menuName = "Events/Game Event")]
public class GameEvent : ScriptableObject {
    private List<GameEventListener> listeners = new List<GameEventListener>();
    
    public void Raise() {
        for (int i = listeners.Count - 1; i >= 0; i--) {
            listeners[i].OnEventRaised();
        }
    }
    
    public void RegisterListener(GameEventListener listener) {
        if (!listeners.Contains(listener))
            listeners.Add(listener);
    }
    
    public void UnregisterListener(GameEventListener listener) {
        listeners.Remove(listener);
    }
}

// Listener
public class GameEventListener : MonoBehaviour {
    [SerializeField] private GameEvent gameEvent;
    [SerializeField] private UnityEvent response;
    
    void OnEnable() {
        gameEvent.RegisterListener(this);
    }
    
    void OnDisable() {
        gameEvent.UnregisterListener(this);
    }
    
    public void OnEventRaised() {
        response.Invoke();
    }
}

// Usage: Create GameEvent asset, reference in listeners, call event.Raise()
```

## Advanced Patterns

### Runtime Sets
```csharp
public abstract class RuntimeSet<T> : ScriptableObject {
    public List<T> items = new List<T>();
    
    public void Add(T item) {
        if (!items.Contains(item))
            items.Add(item);
    }
    
    public void Remove(T item) {
        items.Remove(item);
    }
}

[CreateAssetMenu(fileName = "EnemyRuntimeSet", menuName = "Sets/Enemy")]
public class EnemyRuntimeSet : RuntimeSet<Enemy> { }

// Enemy script
public class Enemy : MonoBehaviour {
    [SerializeField] private EnemyRuntimeSet enemySet;
    
    void OnEnable() {
        enemySet.Add(this);
    }
    
    void OnDisable() {
        enemySet.Remove(this);
    }
}

// Usage: Get all enemies
foreach (Enemy enemy in enemySet.items) {
    // Do something with each enemy
}
```

### Variable System
```csharp
[CreateAssetMenu(fileName = "FloatVariable", menuName = "Variables/Float")]
public class FloatVariable : ScriptableObject {
    public float value;
    
    public void Set(float newValue) {
        value = newValue;
    }
    
    public void Add(float amount) {
        value += amount;
    }
}

[CreateAssetMenu(fileName = "IntVariable", menuName = "Variables/Int")]
public class IntVariable : ScriptableObject {
    public int value;
}

[CreateAssetMenu(fileName = "BoolVariable", menuName = "Variables/Bool")]
public class BoolVariable : ScriptableObject {
    public bool value;
}

// Reference in scripts
public class PlayerHealth : MonoBehaviour {
    [SerializeField] private IntVariable healthVariable;
    [SerializeField] private IntVariable maxHealthVariable;
    
    void Start() {
        healthVariable.value = maxHealthVariable.value;
    }
    
    void TakeDamage(int damage) {
        healthVariable.value -= damage;
    }
}
```

### State Machine with SO
```csharp
public abstract class State : ScriptableObject {
    public abstract void Enter(StateMachine machine);
    public abstract void Execute(StateMachine machine);
    public abstract void Exit(StateMachine machine);
}

[CreateAssetMenu(fileName = "IdleState", menuName = "States/Idle")]
public class IdleState : State {
    public override void Enter(StateMachine machine) {
        machine.animator.SetBool("IsIdle", true);
    }
    
    public override void Execute(StateMachine machine) {
        if (machine.input.magnitude > 0.1f) {
            machine.ChangeState(machine.moveState);
        }
    }
    
    public override void Exit(StateMachine machine) {
        machine.animator.SetBool("IsIdle", false);
    }
}

public class StateMachine : MonoBehaviour {
    [SerializeField] private State currentState;
    [SerializeField] public State idleState;
    [SerializeField] public State moveState;
    [SerializeField] public State attackState;
    
    [HideInInspector] public Animator animator;
    [HideInInspector] public Vector2 input;
    
    void Start() {
        animator = GetComponent<Animator>();
        ChangeState(idleState);
    }
    
    void Update() {
        currentState?.Execute(this);
    }
    
    public void ChangeState(State newState) {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }
}
```

## Best Practices

### Data Separation
```csharp
// Separate data from behavior
[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject {
    public string weaponName;
    public int damage;
    public float attackSpeed;
    public float range;
    public GameObject modelPrefab;
    public AudioClip attackSound;
    public ParticleSystem hitEffect;
}

// Behavior uses the data
public class Weapon : MonoBehaviour {
    [SerializeField] private WeaponData data;
    
    public void Attack() {
        // Use data.damage, data.attackSpeed, etc.
        PlaySound(data.attackSound);
        SpawnEffect(data.hitEffect);
    }
}
```

### Validation
```csharp
public class ItemData : ScriptableObject {
    public string itemName;
    public int value;
    
    void OnValidate() {
        // Ensure valid data
        if (value < 0) value = 0;
        if (string.IsNullOrEmpty(itemName)) {
            itemName = "New Item";
        }
    }
}
```

### Editor Extensions
```csharp
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor {
    public override void OnInspectorGUI() {
        ItemData item = (ItemData)target;
        
        EditorGUILayout.LabelField("Item Editor", EditorStyles.boldLabel);
        
        item.itemName = EditorGUILayout.TextField("Name", item.itemName);
        item.value = EditorGUILayout.IntField("Value", item.value);
        
        if (GUILayout.Button("Generate Random Stats")) {
            Undo.RecordObject(item, "Randomize Stats");
            item.value = Random.Range(1, 100);
            EditorUtility.SetDirty(item);
        }
        
        // Show default inspector too
        DrawDefaultInspector();
    }
}
#endif
```
