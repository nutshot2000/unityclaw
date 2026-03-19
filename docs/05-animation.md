# Unity Animation System

## Animator Component

### Setup
```csharp
Animator animator = GetComponent<Animator>();

// Get parameters
float speed = animator.GetFloat("Speed");
int health = animator.GetInteger("Health");
bool isGrounded = animator.GetBool("IsGrounded");

// Set parameters
animator.SetFloat("Speed", 5f);
animator.SetInteger("Health", 100);
animator.SetBool("IsGrounded", true);
animator.SetTrigger("Jump");

// Reset trigger
animator.ResetTrigger("Jump");
```

### Animation States
```csharp
// Get current state info
AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

// Check state name
if (stateInfo.IsName("Base Layer.Idle")) { }

// Check state tag
if (stateInfo.IsTag("Attack")) { }

// Get normalized time (0-1 for loop, >1 for non-loop)
float progress = stateInfo.normalizedTime;

// Check if animation is playing
bool isPlaying = stateInfo.length > 0;
```

### Animation Control
```csharp
// Play animation directly
animator.Play("Run");
animator.Play("Run", 0, 0.5f); // Start at 50%

// Cross fade (smooth transition)
animator.CrossFade("Run", 0.25f); // 0.25 second transition
animator.CrossFadeInFixedTime("Run", 0.25f);

// Speed control
animator.speed = 2f; // Double speed
animator.speed = 0.5f; // Half speed
animator.speed = 0f; // Pause

// Layer weight
animator.SetLayerWeight(1, 0.5f);
```

### Animation Events
```csharp
// In Animation window: Add Event at specific frame
// Call function on script attached to same GameObject

void OnAnimationEvent() {
    Debug.Log("Animation event triggered!");
}

void OnAnimationEventWithParam(string param) {
    Debug.Log($"Event param: {param}");
}

void OnAnimationEventWithInt(int value) {
    Debug.Log($"Event value: {value}");
}
```

## Animation Clips

### Creating Clips
```csharp
// Load from Resources
AnimationClip clip = Resources.Load<AnimationClip>("Animations/Run");

// Create programmatically
AnimationClip clip = new AnimationClip();
clip.name = "CustomAnimation";

// Add curves
AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
```

### Clip Properties
```csharp
AnimationClip clip = animator.GetCurrentAnimatorClipInfo(0)[0].clip;

float length = clip.length; // Duration in seconds
float frameRate = clip.frameRate;
bool isLooping = clip.isLooping;
```

## Legacy Animation System

```csharp
Animation anim = GetComponent<Animation>();

// Play animation
anim.Play("Run");
anim.Play("Run", PlayMode.StopAll); // Stop others
anim.Play("Run", PlayMode.Queue); // Queue after current

// Cross fade
anim.CrossFade("Run", 0.25f);

// Blend
anim.Blend("Run", 1f, 0.25f);

// Speed
anim["Run"].speed = 2f;

// Time
anim["Run"].time = 0.5f;

// Weight
anim["Run"].weight = 0.5f;

// Events
anim["Run"].AddEvent(new AnimationEvent() {
    time = 0.5f,
    functionName = "OnFootstep",
    intParameter = 0
});
```

## Blend Trees

### 1D Blend Tree
```csharp
// Blend based on single parameter (e.g., speed)
// Animator Controller: Create Blend Tree, set parameter to "Speed"
// Add clips: Idle (0), Walk (0.5), Run (1)

// In code:
animator.SetFloat("Speed", currentSpeed);
```

### 2D Blend Tree
```csharp
// Blend based on two parameters (e.g., velocity x and z)
// Animator Controller: Create 2D Blend Tree
// Set parameters to "VelocityX" and "VelocityZ"

// In code:
animator.SetFloat("VelocityX", velocity.x);
animator.SetFloat("VelocityZ", velocity.z);
```

## Root Motion

```csharp
// Apply root motion to transform
void OnAnimatorMove() {
    if (animator) {
        transform.position += animator.deltaPosition;
        transform.rotation *= animator.deltaRotation;
    }
}

// Or use built-in
animator.applyRootMotion = true;
```

## Animation Rigging (Package)

```csharp
// Install: Animation Rigging package
using UnityEngine.Animations.Rigging;

// Multi-Aim Constraint
var constraint = GetComponent<MultiAimConstraint>();
constraint.data.constrainedObject = headTransform;
constraint.data.sourceObjects.Add(new WeightedTransform(target, 1f));

// Two Bone IK
var ik = GetComponent<TwoBoneIKConstraint>();
ik.data.root = upperArm;
ik.data.mid = foreArm;
ik.data.tip = hand;
ik.data.target = targetTransform;
```

## Timeline & Playables

```csharp
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Play Timeline
PlayableDirector director = GetComponent<PlayableDirector>();
director.Play();
director.Pause();
director.Stop();
director.time = 5f; // Jump to 5 seconds

// Bindings
var binding = director.playableAsset.outputs.First();
director.SetGenericBinding(binding.sourceObject, targetGameObject);

// Control playable
var playable = director.playableGraph.GetRootPlayable(0);
playable.SetSpeed(2f); // Double speed
```

## Common Patterns

### Smooth Parameter Changes
```csharp
void Update() {
    // Smoothly blend to target speed
    float currentSpeed = animator.GetFloat("Speed");
    float targetSpeed = isRunning ? 1f : 0f;
    currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);
    animator.SetFloat("Speed", currentSpeed);
}
```

### Animation States
```csharp
public enum PlayerState { Idle, Walk, Run, Attack, Dead }

void SetState(PlayerState state) {
    switch (state) {
        case PlayerState.Idle:
            animator.SetBool("IsMoving", false);
            break;
        case PlayerState.Walk:
            animator.SetBool("IsMoving", true);
            animator.SetFloat("Speed", 0.5f);
            break;
        case PlayerState.Run:
            animator.SetBool("IsMoving", true);
            animator.SetFloat("Speed", 1f);
            break;
        case PlayerState.Attack:
            animator.SetTrigger("Attack");
            break;
        case PlayerState.Dead:
            animator.SetBool("IsDead", true);
            break;
    }
}
```

### Combo System
```csharp
int comboCount = 0;
float comboWindow = 1f;
float lastAttackTime = 0f;

void Attack() {
    if (Time.time - lastAttackTime < comboWindow) {
        comboCount++;
        if (comboCount > 3) comboCount = 1;
    } else {
        comboCount = 1;
    }
    
    animator.SetInteger("ComboCount", comboCount);
    animator.SetTrigger("Attack");
    
    lastAttackTime = Time.time;
}
```

### Hit Reaction
```csharp
void TakeHit(Vector3 hitDirection) {
    // Set hit direction for blend tree
    float hitAngle = Vector3.SignedAngle(transform.forward, hitDirection, Vector3.up);
    animator.SetFloat("HitAngle", hitAngle / 180f); // -1 to 1
    animator.SetTrigger("Hit");
}
```
