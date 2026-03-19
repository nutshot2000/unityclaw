# Unity Input System

## Legacy Input (Input class)

### Keyboard Input
```csharp
// Key pressed this frame
if (Input.GetKeyDown(KeyCode.Space)) { Jump(); }
if (Input.GetKeyDown(KeyCode.W)) { MoveForward(); }

// Key held down
if (Input.GetKey(KeyCode.LeftShift)) { Sprint(); }

// Key released
if (Input.GetKeyUp(KeyCode.Space)) { StopJump(); }

// Any key
if (Input.anyKeyDown) { }
```

### Mouse Input
```csharp
// Mouse buttons
if (Input.GetMouseButtonDown(0)) { } // Left click
if (Input.GetMouseButtonDown(1)) { } // Right click
if (Input.GetMouseButtonDown(2)) { } // Middle click
if (Input.GetMouseButton(0)) { } // Held
if (Input.GetMouseButtonUp(0)) { } // Released

// Mouse position
Vector3 mousePos = Input.mousePosition; // Screen coordinates (0,0) to (Screen.width, Screen.height)

// Mouse movement
float mouseX = Input.GetAxis("Mouse X");
float mouseY = Input.GetAxis("Mouse Y");

// Mouse scroll
float scroll = Input.GetAxis("Mouse ScrollWheel");
```

### Axis Input
```csharp
// Horizontal/Vertical (WASD or Arrow keys)
float horizontal = Input.GetAxis("Horizontal"); // -1 to 1, smoothed
float vertical = Input.GetAxis("Vertical");

// Raw (no smoothing)
float horizontalRaw = Input.GetAxisRaw("Horizontal"); // -1, 0, or 1

// Custom axes defined in Input Manager
float custom = Input.GetAxis("CustomAxis");
```

### Touch Input
```csharp
// Touch count
int touchCount = Input.touchCount;

// Get touch
Touch touch = Input.GetTouch(0);

// Touch phases
if (touch.phase == TouchPhase.Began) { }
if (touch.phase == TouchPhase.Moved) { }
if (touch.phase == TouchPhase.Ended) { }
if (touch.phase == TouchPhase.Stationary) { }

// Touch position
Vector2 touchPos = touch.position;
Vector2 touchDelta = touch.deltaPosition;

// Multi-touch
foreach (Touch t in Input.touches) {
    Debug.Log($"Touch {t.fingerId}: {t.position}");
}
```

## New Input System (Recommended)

### Setup
```csharp
// Install via Package Manager: com.unity.inputsystem
// Enable in Player Settings: Active Input Handling = Input System Package
```

### Basic Usage
```csharp
using UnityEngine.InputSystem;

// Check if key is pressed
if (Keyboard.current.spaceKey.isPressed) { }

// Check if key was pressed this frame
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }

// Mouse
if (Mouse.current.leftButton.isPressed) { }
Vector2 mousePos = Mouse.current.position.ReadValue();
Vector2 mouseDelta = Mouse.current.delta.ReadValue();
float scroll = Mouse.current.scroll.ReadValue().y;

// Gamepad
if (Gamepad.current.buttonSouth.isPressed) { }
Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
float rightTrigger = Gamepad.current.rightTrigger.ReadValue();
```

### Input Actions (Recommended approach)
```csharp
// Create Input Actions asset in Project window
// Generate C# class from asset

public class PlayerInput : MonoBehaviour {
    private PlayerInputActions inputActions;
    
    void Awake() {
        inputActions = new PlayerInputActions();
        
        // Subscribe to actions
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Jump.performed += OnJump;
        inputActions.Player.Attack.performed += OnAttack;
    }
    
    void OnEnable() {
        inputActions.Player.Enable();
    }
    
    void OnDisable() {
        inputActions.Player.Disable();
    }
    
    void OnMove(InputAction.CallbackContext context) {
        Vector2 moveInput = context.ReadValue<Vector2>();
        // Use moveInput
    }
    
    void OnJump(InputAction.CallbackContext context) {
        Jump();
    }
    
    void OnAttack(InputAction.CallbackContext context) {
        Attack();
    }
}
```

### Input Action Types
```csharp
// Value - continuous (movement, looking)
inputActions.Player.Move.ReadValue<Vector2>();

// Button - pressed/released (jump, fire)
inputActions.Player.Jump.triggered; // Was pressed this frame
inputActions.Player.Jump.IsPressed(); // Currently held

// Pass through - both
```

### Input Action Callbacks
```csharp
// Started - when action begins
action.started += ctx => Debug.Log("Started");

// Performed - when action triggers (button pressed, threshold crossed)
action.performed += ctx => Debug.Log("Performed");

// Canceled - when action ends (button released, below threshold)
action.canceled += ctx => Debug.Log("Canceled");
```

## Common Input Patterns

### Movement with WASD
```csharp
void Update() {
    float horizontal = Input.GetAxisRaw("Horizontal");
    float vertical = Input.GetAxisRaw("Vertical");
    
    Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;
    transform.position += direction * speed * Time.deltaTime;
}
```

### Mouse Look
```csharp
void Update() {
    float mouseX = Input.GetAxis("Mouse X") * sensitivity;
    float mouseY = Input.GetAxis("Mouse Y") * sensitivity;
    
    // Rotate player horizontally
    transform.Rotate(Vector3.up * mouseX);
    
    // Rotate camera vertically
    cameraTransform.Rotate(Vector3.left * mouseY);
}
```

### Double Click Detection
```csharp
float lastClickTime = 0f;
float doubleClickThreshold = 0.3f;

void Update() {
    if (Input.GetMouseButtonDown(0)) {
        if (Time.time - lastClickTime < doubleClickThreshold) {
            // Double click detected
            DoubleClick();
        }
        lastClickTime = Time.time;
    }
}
```

### Hold Detection
```csharp
float holdTime = 0f;
bool isHolding = false;

void Update() {
    if (Input.GetMouseButtonDown(0)) {
        isHolding = true;
        holdTime = 0f;
    }
    
    if (Input.GetMouseButton(0) && isHolding) {
        holdTime += Time.deltaTime;
        
        if (holdTime >= 1f) {
            // Held for 1 second
            ChargeAttack();
        }
    }
    
    if (Input.GetMouseButtonUp(0)) {
        if (holdTime < 1f) {
            QuickAttack();
        }
        isHolding = false;
    }
}
```

### Swipe Detection (Touch)
```csharp
Vector2 touchStartPos;
bool isTouching = false;

void Update() {
    if (Input.touchCount > 0) {
        Touch touch = Input.GetTouch(0);
        
        if (touch.phase == TouchPhase.Began) {
            touchStartPos = touch.position;
            isTouching = true;
        }
        
        if (touch.phase == TouchPhase.Ended && isTouching) {
            Vector2 swipeDelta = touch.position - touchStartPos;
            
            if (swipeDelta.magnitude > 100f) {
                // Detect swipe direction
                if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y)) {
                    // Horizontal swipe
                    if (swipeDelta.x > 0) SwipeRight();
                    else SwipeLeft();
                } else {
                    // Vertical swipe
                    if (swipeDelta.y > 0) SwipeUp();
                    else SwipeDown();
                }
            }
            isTouching = false;
        }
    }
}
```

## Input Manager (Legacy)

### Accessing Input Manager
```csharp
// Edit -> Project Settings -> Input Manager
// Define axes: Horizontal, Vertical, Jump, Fire1, etc.
```

### Common Default Axes
- **Horizontal**: A/D keys, Left/Right arrows
- **Vertical**: W/S keys, Up/Down arrows
- **Fire1**: Left Ctrl, Left Mouse Button
- **Fire2**: Left Alt, Right Mouse Button
- **Fire3**: Left Shift, Middle Mouse Button
- **Jump**: Space
- **Mouse X**: Mouse horizontal movement
- **Mouse Y**: Mouse vertical movement
- **Mouse ScrollWheel**: Mouse wheel
