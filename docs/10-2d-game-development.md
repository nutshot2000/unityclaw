# Unity 2D Game Development

## Sprite Renderer

### Basic Setup
```csharp
SpriteRenderer sr = GetComponent<SpriteRenderer>();

// Set sprite
sr.sprite = mySprite;

// Color tint
sr.color = Color.white;
sr.color = new Color(1, 1, 1, 0.5f); // Half transparent

// Flip
sr.flipX = true;
sr.flipY = false;

// Sorting
sr.sortingLayerName = "Foreground";
sr.sortingOrder = 10; // Higher = in front

// Draw mode
sr.drawMode = SpriteDrawMode.Simple;
sr.drawMode = SpriteDrawMode.Sliced; // 9-slice
sr.drawMode = SpriteDrawMode.Tiled;

// Size (for sliced/tiled)
sr.size = new Vector2(10, 5);
```

## 2D Physics (Rigidbody2D)

### Setup
```csharp
Rigidbody2D rb = GetComponent<Rigidbody2D>();

// Body type
rb.bodyType = RigidbodyType2D.Dynamic; // Affected by forces
rb.bodyType = RigidbodyType2D.Kinematic; // Controlled by script
rb.bodyType = RigidbodyType2D.Static; // Doesn't move

// Mass
rb.mass = 1f;

// Drag
rb.drag = 0.5f; // Linear drag
rb.angularDrag = 0.05f;

// Gravity
rb.gravityScale = 1f;
rb.gravityScale = 0f; // No gravity

// Constraints
rb.constraints = RigidbodyConstraints2D.FreezeRotation;
rb.constraints = RigidbodyConstraints2D.FreezePositionX;
```

### Movement
```csharp
// Velocity
rb.velocity = new Vector2(5, rb.velocity.y);

// Add force
rb.AddForce(Vector2.right * 10f);
rb.AddForce(Vector2.up * 500f, ForceMode2D.Impulse); // Jump

// Move position
rb.MovePosition(rb.position + Vector2.right * speed * Time.fixedDeltaTime);

// Rotation
rb.MoveRotation(90f);
rb.AddTorque(100f);
```

## 2D Colliders

### Types
```csharp
// Box Collider
BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
box.size = new Vector2(1, 1);
box.offset = new Vector2(0, 0);

// Circle Collider
CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
circle.radius = 0.5f;

// Capsule Collider
CapsuleCollider2D capsule = gameObject.AddComponent<CapsuleCollider2D>();
capsule.size = new Vector2(1, 2);

// Polygon Collider (custom shape)
PolygonCollider2D poly = gameObject.AddComponent<PolygonCollider2D>();
Vector2[] points = new Vector2[] {
    new Vector2(0, 0),
    new Vector2(1, 0),
    new Vector2(0.5f, 1)
};
poly.SetPath(0, points);

// Edge Collider (lines)
EdgeCollider2D edge = gameObject.AddComponent<EdgeCollider2D>();
edge.points = new Vector2[] {
    new Vector2(-5, 0),
    new Vector2(5, 0)
};

// Composite Collider (combines multiple)
CompositeCollider2D composite = gameObject.AddComponent<CompositeCollider2D>();
```

### Collision Detection
```csharp
void OnCollisionEnter2D(Collision2D collision) {
    Debug.Log($"Collided with {collision.gameObject.name}");
    Debug.Log($"Contact point: {collision.contacts[0].point}");
    Debug.Log($"Normal: {collision.contacts[0].normal}");
}

void OnCollisionStay2D(Collision2D collision) { }
void OnCollisionExit2D(Collision2D collision) { }

// Triggers
void OnTriggerEnter2D(Collider2D other) {
    if (other.CompareTag("Coin")) {
        CollectCoin(other.gameObject);
    }
}
void OnTriggerStay2D(Collider2D other) { }
void OnTriggerExit2D(Collider2D other) { }
```

## 2D Raycasting
```csharp
// Simple raycast
RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right, 10f);
if (hit.collider != null) {
    Debug.Log($"Hit: {hit.collider.name}");
}

// With layer mask
int layerMask = LayerMask.GetMask("Enemies");
RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right, 10f, layerMask);

// Circle cast
RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.5f, Vector2.right, 10f);

// Box cast
RaycastHit2D hit = Physics2D.BoxCast(transform.position, Vector2.one, 0, Vector2.right, 10f);

// Raycast all
RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.right, 10f);

// Linecast
RaycastHit2D hit = Physics2D.Linecast(startPos, endPos);
```

## 2D Character Controller

### Basic Movement
```csharp
public class Player2D : MonoBehaviour {
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    
    private Rigidbody2D rb;
    private bool isGrounded;
    
    void Start() {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update() {
        // Movement
        float horizontal = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        
        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded) {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        
        // Flip sprite
        if (horizontal != 0) {
            transform.localScale = new Vector3(Mathf.Sign(horizontal), 1, 1);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision) {
        // Ground check
        if (collision.contacts[0].normal.y > 0.5f) {
            isGrounded = true;
        }
    }
    
    void OnCollisionExit2D(Collision2D collision) {
        isGrounded = false;
    }
}
```

### Better Ground Check
```csharp
bool IsGrounded() {
    float extraHeight = 0.1f;
    RaycastHit2D hit = Physics2D.Raycast(
        boxCollider.bounds.center, 
        Vector2.down, 
        boxCollider.bounds.extents.y + extraHeight,
        groundLayer
    );
    return hit.collider != null;
}
```

## Tilemap System

### Creating Tilemaps
```csharp
// In Editor: GameObject -> 2D Object -> Tilemap
// Creates Grid with Tilemap child
```

### Tilemap Scripting
```csharp
Tilemap tilemap = GetComponent<Tilemap>();

// Set tile
tilemap.SetTile(new Vector3Int(0, 0, 0), myTile);

// Get tile
TileBase tile = tilemap.GetTile(new Vector3Int(0, 0, 0));

// Remove tile
tilemap.SetTile(new Vector3Int(0, 0, 0), null);

// Clear all
tilemap.ClearAllTiles();

// World to cell
Vector3Int cellPos = tilemap.WorldToCell(worldPosition);

// Cell to world
Vector3 worldPos = tilemap.CellToWorld(cellPos);

// Get bounds
BoundsInt bounds = tilemap.cellBounds;

// Iterate over tiles
foreach (Vector3Int pos in bounds.allPositionsWithin) {
    TileBase tile = tilemap.GetTile(pos);
    if (tile != null) {
        // Process tile
    }
}
```

### Animated Tiles
```csharp
// Create animated tile asset
AnimatedTile animatedTile = ScriptableObject.CreateInstance<AnimatedTile>();
animatedTile.m_AnimatedSprites = new Sprite[] { frame1, frame2, frame3 };
animatedTile.m_MinSpeed = 1;
animatedTile.m_MaxSpeed = 1;
animatedTile.m_AnimationStartTime = 0;

// Place on tilemap
tilemap.SetTile(position, animatedTile);
```

## 2D Animation

### Animator Setup
```csharp
Animator animator = GetComponent<Animator>();

// Set parameters
animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
animator.SetBool("IsGrounded", isGrounded);
animator.SetBool("IsRunning", Mathf.Abs(rb.velocity.x) > 0.1f);
animator.SetTrigger("Attack");

// Check state
if (animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Jump")) {
    // In jump state
}
```

### Sprite Animation (Legacy)
```csharp
public Sprite[] walkSprites;
private SpriteRenderer sr;
private int currentFrame;
private float timer;

void Update() {
    timer += Time.deltaTime;
    if (timer >= 0.1f) { // 10 fps
        timer = 0;
        currentFrame = (currentFrame + 1) % walkSprites.Length;
        sr.sprite = walkSprites[currentFrame];
    }
}
```

## 2D Camera

### Following Player
```csharp
public class CameraFollow2D : MonoBehaviour {
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset;
    
    void LateUpdate() {
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
```

### Camera with Bounds
```csharp
void LateUpdate() {
    Vector3 targetPos = target.position + offset;
    
    // Clamp to bounds
    targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x);
    targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y, maxBounds.y);
    
    transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
}
```

### Pixel Perfect Camera
```csharp
// Add Pixel Perfect Camera component
PixelPerfectCamera ppc = GetComponent<PixelPerfectCamera>();
ppc.assetsPPU = 32; // Pixels per unit
ppc.refResolutionX = 320;
ppc.refResolutionY = 180;
```

## 2D Lighting (URP)

### Setup
```csharp
// Requires Universal Render Pipeline with 2D Renderer
// Window -> Rendering -> URP 2D Renderer
```

### Light2D Component
```csharp
Light2D light2D = GetComponent<Light2D>();

// Types
light2D.lightType = Light2D.LightType.Global; // Affects everything
light2D.lightType = Light2D.LightType.Point; // Radial light
light2D.lightType = Light2D.LightType.Spot; // Directional cone

// Properties
light2D.color = Color.yellow;
light2D.intensity = 1f;
light2D.pointLightOuterRadius = 10f;
light2D.pointLightInnerRadius = 5f;
```

### Normal Maps for 2D
```csharp
// Assign normal map to sprite
SpriteRenderer sr = GetComponent<SpriteRenderer>();
sr.sprite = mySprite;

// Normal map must be imported as "Normal Map" in texture settings
```

## Sprite Shape (Spline-based)

```csharp
// GameObject -> 2D Object -> Sprite Shape

SpriteShapeController shape = GetComponent<SpriteShapeController>();

// Modify spline
Spline spline = shape.spline;
spline.InsertPointAt(0, new Vector3(0, 0, 0));
spline.InsertPointAt(1, new Vector3(5, 0, 0));
spline.InsertPointAt(2, new Vector3(5, 5, 0));
spline.InsertPointAt(3, new Vector3(0, 5, 0));
spline.SetPosition(0, new Vector3(0, 2, 0));
```

## 2D Pathfinding (NavMesh)

```csharp
// Requires AI Navigation package
// Window -> AI -> Navigation (Obsolete)
// Or use A* Pathfinding Project from Asset Store

// Simple waypoint system
public Transform[] waypoints;
private int currentWaypoint;

void Update() {
    Transform target = waypoints[currentWaypoint];
    Vector3 direction = (target.position - transform.position).normalized;
    transform.position += direction * speed * Time.deltaTime;
    
    if (Vector3.Distance(transform.position, target.position) < 0.1f) {
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
    }
}
```

## Common Patterns

### Platformer Controller
```csharp
public class PlatformerController : MonoBehaviour {
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    
    private Rigidbody2D rb;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isGrounded;
    
    void Update() {
        // Jump buffer
        if (Input.GetButtonDown("Jump")) {
            jumpBufferCounter = jumpBufferTime;
        } else {
            jumpBufferCounter -= Time.deltaTime;
        }
        
        // Jump
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0) {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferCounter = 0;
        }
        
        // Variable jump height
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0) {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }
    }
    
    void FixedUpdate() {
        // Movement
        float horizontal = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        
        // Coyote time
        if (isGrounded) {
            coyoteTimeCounter = coyoteTime;
        } else {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }
    }
}
```

### Top-Down Movement
```csharp
void Update() {
    float horizontal = Input.GetAxisRaw("Horizontal");
    float vertical = Input.GetAxisRaw("Vertical");
    
    Vector2 direction = new Vector2(horizontal, vertical).normalized;
    rb.velocity = direction * moveSpeed;
    
    // Face movement direction
    if (direction != Vector2.zero) {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
```
