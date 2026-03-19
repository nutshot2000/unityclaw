# Unity AI Navigation (NavMesh)

## NavMesh Setup

### Baking NavMesh
```csharp
// In Editor: Window -> AI -> Navigation
// Select objects, mark as Navigation Static
// Bake the NavMesh
```

### NavMeshAgent Component
```csharp
NavMeshAgent agent = GetComponent<NavMeshAgent>();

// Basic movement
agent.SetDestination(targetPosition);
agent.SetDestination(targetTransform.position);

// Properties
agent.speed = 5f;
agent.angularSpeed = 120f;
agent.acceleration = 8f;
agent.stoppingDistance = 1f;

// Auto braking
agent.autoBraking = true;

// Height and radius
agent.height = 2f;
agent.radius = 0.5f;

// Obstacle avoidance
agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
agent.avoidancePriority = 50; // 0-99, lower = higher priority
```

### Checking Path
```csharp
// Check if path is complete
if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance) {
    // Reached destination
}

// Check if path is valid
NavMeshPath path = new NavMeshPath();
if (NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, path)) {
    if (path.status == NavMeshPathStatus.PathComplete) {
        agent.SetPath(path);
    }
}

// Path status
if (agent.pathStatus == NavMeshPathStatus.PathComplete) { }
if (agent.pathStatus == NavMeshPathStatus.PathPartial) { }
if (agent.pathStatus == NavMeshPathStatus.PathInvalid) { }
```

## NavMesh Queries

### Sample Position
```csharp
// Find nearest valid position on NavMesh
Vector3 targetPos = new Vector3(10, 0, 10);
NavMeshHit hit;
if (NavMesh.SamplePosition(targetPos, out hit, 10f, NavMesh.AllAreas)) {
    Vector3 validPosition = hit.position;
}
```

### Raycast on NavMesh
```csharp
// Raycast on NavMesh (not physics raycast)
NavMeshHit hit;
if (NavMesh.Raycast(transform.position, targetPosition, out hit, NavMesh.AllAreas)) {
    // Hit obstacle
    Vector3 hitPoint = hit.position;
}
```

### Find Closest Edge
```csharp
NavMeshHit hit;
if (NavMesh.FindClosestEdge(transform.position, out hit, NavMesh.AllAreas)) {
    Vector3 edgePoint = hit.position;
    Vector3 edgeNormal = hit.normal;
}
```

### Calculate Path
```csharp
NavMeshPath path = new NavMeshPath();
bool hasPath = NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, path);

// Get path corners
if (hasPath) {
    Vector3[] corners = path.corners;
    foreach (Vector3 corner in corners) {
        Debug.DrawLine(transform.position, corner, Color.red);
    }
}
```

## NavMesh Areas

### Setting Area Mask
```csharp
// Walkable (area 0)
agent.areaMask = NavMesh.AllAreas;

// Specific areas only
agent.areaMask = 1 << 0 | 1 << 3; // Walkable and Water

// Cost modifiers
NavMesh.SetAreaCost(3, 2f); // Water costs 2x
NavMesh.SetAreaCost(4, 10f); // Mud costs 10x
```

### Getting Area from Position
```csharp
NavMeshHit hit;
if (NavMesh.SamplePosition(transform.position, out hit, 0.1f, NavMesh.AllAreas)) {
    int area = hit.mask;
    bool isWalkable = (area & 1) != 0;
}
```

## NavMesh Obstacles

### NavMeshObstacle Component
```csharp
NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();

// Shape
obstacle.shape = NavMeshObstacleShape.Box;
obstacle.shape = NavMeshObstacleShape.Capsule;

// Size
obstacle.size = new Vector3(2, 2, 2);
obstacle.center = Vector3.zero;

// Carving
obstacle.carving = true; // Carve hole in NavMesh
obstacle.carveOnlyStationary = true;
obstacle.carvingMoveThreshold = 0.1f;

// Moving obstacle
obstacle.velocity = Vector3.forward * 2f;
```

## Off-Mesh Links

### Creating Links
```csharp
// In Editor: Navigation tab -> Object -> Navigation Static
// Check "Generate OffMeshLinks"
// Or create manually:

GameObject linkObj = new GameObject("OffMeshLink");
OffMeshLink link = linkObj.AddComponent<OffMeshLink>();
link.startTransform = startPoint;
link.endTransform = endPoint;
link.costOverride = -1; // Use default cost
link.biDirectional = true;
link.activated = true;
link.autoUpdatePositions = true;
```

### Using OffMeshLinks
```csharp
// Agent automatically uses off-mesh links
// Can detect when using link:

void Update() {
    if (agent.isOnOffMeshLink) {
        // Currently traversing off-mesh link
        OffMeshLinkData linkData = agent.currentOffMeshLinkData;
        
        // Custom animation/movement across link
        StartCoroutine(TraverseLink(linkData));
    }
}

IEnumerator TraverseLink(OffMeshLinkData linkData) {
    Vector3 startPos = transform.position;
    Vector3 endPos = linkData.endPos;
    float duration = 1f;
    float elapsed = 0f;
    
    while (elapsed < duration) {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        transform.position = Vector3.Lerp(startPos, endPos, t);
        yield return null;
    }
    
    agent.CompleteOffMeshLink();
}
```

## NavMesh Surface (Runtime)

### Building at Runtime
```csharp
NavMeshSurface surface = GetComponent<NavMeshSurface>();

// Build NavMesh
surface.BuildNavMesh();

// Remove data
surface.RemoveData();

// Update existing
surface.UpdateNavMesh(surface.navMeshData);
```

### Multiple Surfaces
```csharp
// Different surfaces for different areas
NavMeshSurface indoorSurface;
NavMeshSurface outdoorSurface;

void Start() {
    indoorSurface.BuildNavMesh();
    outdoorSurface.BuildNavMesh();
}
```

## Common AI Patterns

### Simple Follow
```csharp
public class FollowAI : MonoBehaviour {
    [SerializeField] private Transform target;
    private NavMeshAgent agent;
    
    void Start() {
        agent = GetComponent<NavMeshAgent>();
    }
    
    void Update() {
        if (target != null) {
            agent.SetDestination(target.position);
        }
    }
}
```

### Patrol Points
```csharp
public class PatrolAI : MonoBehaviour {
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waitTime = 2f;
    
    private NavMeshAgent agent;
    private int currentPointIndex;
    private float waitTimer;
    
    void Start() {
        agent = GetComponent<NavMeshAgent>();
        GoToNextPoint();
    }
    
    void Update() {
        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            return;
        
        waitTimer += Time.deltaTime;
        if (waitTimer >= waitTime) {
            GoToNextPoint();
            waitTimer = 0;
        }
    }
    
    void GoToNextPoint() {
        if (patrolPoints.Length == 0) return;
        
        agent.SetDestination(patrolPoints[currentPointIndex].position);
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
    }
}
```

### Chase with Detection
```csharp
public class ChaseAI : MonoBehaviour {
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float chaseRange = 15f;
    [SerializeField] private Transform player;
    
    private NavMeshAgent agent;
    private Vector3 startPosition;
    private bool isChasing;
    
    void Start() {
        agent = GetComponent<NavMeshAgent>();
        startPosition = transform.position;
    }
    
    void Update() {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (isChasing) {
            if (distanceToPlayer < chaseRange) {
                agent.SetDestination(player.position);
            } else {
                isChasing = false;
                agent.SetDestination(startPosition);
            }
        } else {
            if (distanceToPlayer < detectionRange) {
                isChasing = true;
            }
        }
    }
}
```

### Formation Movement
```csharp
public class FormationAI : MonoBehaviour {
    [SerializeField] private Transform leader;
    [SerializeField] private Vector3 formationOffset;
    
    private NavMeshAgent agent;
    
    void Start() {
        agent = GetComponent<NavMeshAgent>();
    }
    
    void Update() {
        Vector3 targetPos = leader.position + leader.TransformDirection(formationOffset);
        agent.SetDestination(targetPos);
    }
}
```

## Local Avoidance

### RVO (Reciprocal Velocity Obstacles)
```csharp
// NavMeshAgent has built-in RVO
agent.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQualityObstacleAvoidance;

// Priority (lower = more likely to give way)
agent.avoidancePriority = Random.Range(0, 100);
```

## NavMesh Limitations

### Checking Reachability
```csharp
bool IsReachable(Vector3 target) {
    NavMeshPath path = new NavMeshPath();
    if (NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, path)) {
        return path.status == NavMeshPathStatus.PathComplete;
    }
    return false;
}
```

### Partial Paths
```csharp
// Handle partial paths
if (agent.pathStatus == NavMeshPathStatus.PathPartial) {
    // Can only get close, not all the way
    Debug.Log("Can only get close to target");
}
```
