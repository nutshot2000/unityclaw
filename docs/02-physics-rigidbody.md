# Unity Physics - Rigidbody

## Rigidbody Component

### Adding & Configuring
```csharp
Rigidbody rb = gameObject.AddComponent<Rigidbody>();
rb.mass = 5f;
rb.drag = 0.5f;
rb.angularDrag = 0.05f;
rb.useGravity = true;
rb.isKinematic = false;
rb.interpolation = RigidbodyInterpolation.Interpolate;
rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
```

### Movement
```csharp
// Set velocity directly
rb.velocity = new Vector3(0, 10, 0);
rb.angularVelocity = new Vector3(0, 90, 0);

// Move position (respects physics)
rb.MovePosition(transform.position + Vector3.forward * speed * Time.fixedDeltaTime);

// Move rotation
rb.MoveRotation(Quaternion.Euler(0, 90, 0));

// Add force
rb.AddForce(Vector3.up * 500f);
rb.AddForce(Vector3.up * 500f, ForceMode.Force); // Continuous force
rb.AddForce(Vector3.up * 500f, ForceMode.Impulse); // Instant force
rb.AddForce(Vector3.up * 500f, ForceMode.Acceleration); // Continuous acceleration
rb.AddForce(Vector3.up * 500f, ForceMode.VelocityChange); // Instant velocity change

// Add torque (rotation)
rb.AddTorque(Vector3.up * 100f);
rb.AddTorque(Vector3.up * 100f, ForceMode.Impulse);

// Add force at position (creates rotation)
rb.AddForceAtPosition(Vector3.up * 100f, transform.position + Vector3.right);

// Add explosion force
rb.AddExplosionForce(1000f, explosionPosition, 10f);
```

### Force Modes Explained
- **Force**: Continuous force using mass (default)
- **Acceleration**: Continuous force ignoring mass
- **Impulse**: Instant force using mass
- **VelocityChange**: Instant force ignoring mass

### Physics Properties
```csharp
rb.mass = 1f; // Kilograms
rb.drag = 0f; // Air resistance (linear)
rb.angularDrag = 0.05f; // Air resistance (rotation)
rb.useGravity = true;
rb.isKinematic = false; // Not affected by physics
rb.freezeRotation = false; // Prevent rotation from physics
rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationZ;
```

### Constraints
```csharp
// Freeze position
rb.constraints = RigidbodyConstraints.FreezePosition;
rb.constraints = RigidbodyConstraints.FreezePositionX;
rb.constraints = RigidbodyConstraints.FreezePositionY;
rb.constraints = RigidbodyConstraints.FreezePositionZ;

// Freeze rotation
rb.constraints = RigidbodyConstraints.FreezeRotation;
rb.constraints = RigidbodyConstraints.FreezeRotationX;
rb.constraints = RigidbodyConstraints.FreezeRotationY;
rb.constraints = RigidbodyConstraints.FreezeRotationZ;

// Freeze all
rb.constraints = RigidbodyConstraints.FreezeAll;

// Check constraints
bool frozenX = (rb.constraints & RigidbodyConstraints.FreezePositionX) != 0;
```

### Interpolation
```csharp
rb.interpolation = RigidbodyInterpolation.None; // No smoothing
rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth based on previous frame
rb.interpolation = RigidbodyInterpolation.Extrapolate; // Predict next frame
```

### Collision Detection
```csharp
rb.collisionDetectionMode = CollisionDetectionMode.Discrete; // Default
rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better for fast moving
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // For dynamic objects
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // Newest, most accurate
```

## Collision Detection

### Collision Events (Both objects need colliders, at least one needs Rigidbody)
```csharp
void OnCollisionEnter(Collision collision) {
    // Collision started
    Debug.Log($"Hit {collision.gameObject.name}");
    Debug.Log($"Contact point: {collision.contacts[0].point}");
    Debug.Log($"Impact velocity: {collision.relativeVelocity.magnitude}");
}

void OnCollisionStay(Collision collision) {
    // Still colliding
}

void OnCollisionExit(Collision collision) {
    // Collision ended
}
```

### Trigger Events (Use isTrigger on collider)
```csharp
void OnTriggerEnter(Collider other) {
    // Entered trigger
    if (other.CompareTag("Coin")) {
        CollectCoin(other.gameObject);
    }
}

void OnTriggerStay(Collider other) {
    // Inside trigger
}

void OnTriggerExit(Collider other) {
    // Exited trigger
}
```

## Physics Queries

### Raycasting
```csharp
// Simple raycast
if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 100f)) {
    Debug.Log($"Hit: {hit.collider.name} at {hit.point}");
}

// Raycast with layer mask
int layerMask = LayerMask.GetMask("Enemies");
if (Physics.Raycast(transform.position, transform.forward, out hit, 100f, layerMask)) {
    // Hit enemy
}

// Raycast all
RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, 100f);

// Sphere cast
if (Physics.SphereCast(transform.position, 1f, transform.forward, out hit, 100f)) {
    // Hit something with sphere
}

// Box cast
if (Physics.BoxCast(transform.position, Vector3.one, transform.forward, out hit, Quaternion.identity, 100f)) {
    // Hit something with box
}
```

### Overlap Checks
```csharp
// Overlap sphere
Collider[] colliders = Physics.OverlapSphere(transform.position, 5f);

// Overlap box
Collider[] colliders = Physics.OverlapBox(transform.position, Vector3.one, Quaternion.identity);

// Overlap capsule
Collider[] colliders = Physics.OverlapCapsule(point1, point2, radius);

// With layer mask
Collider[] enemies = Physics.OverlapSphere(transform.position, 5f, LayerMask.GetMask("Enemies"));
```

### Line of Sight
```csharp
bool HasLineOfSight(Vector3 target) {
    Vector3 direction = target - transform.position;
    if (Physics.Raycast(transform.position, direction, out RaycastHit hit, direction.magnitude)) {
        return hit.collider.gameObject == targetGameObject;
    }
    return true;
}
```

## Physics Settings

### Global Settings
```csharp
// Time settings
Time.fixedDeltaTime = 0.02f; // 50 physics updates per second

// Gravity
Physics.gravity = new Vector3(0, -9.81f, 0);
Physics.gravity = new Vector3(0, -20f, 0); // Stronger gravity

// Default material
Physics.defaultMaterial = myPhysicsMaterial;

// Layer collisions
Physics.IgnoreLayerCollision(0, 8, true); // Ignore layer 0 and 8 collisions
```

## Common Patterns

### Jump
```csharp
void Jump() {
    if (isGrounded) {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
}
```

### Ground Check
```csharp
bool IsGrounded() {
    return Physics.Raycast(transform.position, Vector3.down, 1.1f);
}

// Or with sphere
bool IsGrounded() {
    return Physics.CheckSphere(transform.position - Vector3.up * 0.5f, 0.5f, groundLayer);
}
```

### Knockback
```csharp
void ApplyKnockback(Vector3 direction, float force) {
    rb.AddForce(direction * force, ForceMode.Impulse);
}
```

### Stop Movement
```csharp
void Stop() {
    rb.velocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
}
```
