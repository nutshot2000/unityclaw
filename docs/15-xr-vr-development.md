# Unity XR/VR Development

## XR Interaction Toolkit

### Setup
```csharp
// Install packages:
// - XR Plugin Management
// - XR Interaction Toolkit
// - Device-specific plugin (Oculus, OpenXR, etc.)
```

### XR Origin
```csharp
// XR Origin is the player rig
// Contains Camera Offset and Camera

XROrigin xrOrigin = FindObjectOfType<XROrigin>();

// Recenter
xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;

// Move to position
xrOrigin.MoveCameraToWorldLocation(Vector3.zero);

// Match floor height
xrOrigin.MatchOriginUpOriginForward(Vector3.up, Vector3.forward);
```

## Input (XR)

### XR Controller Input
```csharp
// Using Input System
using UnityEngine.InputSystem;
using UnityEngine.XR;

// Get devices
var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

// Check features
bool isTriggerPressed;
leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out isTriggerPressed);

float triggerValue;
leftHand.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);

Vector2 thumbstick;
leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick);

// Haptics
leftHand.SendHapticImpulse(0, 0.5f, 0.1f); // Channel, amplitude, duration
```

### Action-based Input (New)
```csharp
// XR Controller with Input Actions
public InputActionProperty triggerAction;
public InputActionProperty gripAction;
public InputActionProperty thumbstickAction;

void Update() {
    float triggerValue = triggerAction.action.ReadValue<float>();
    float gripValue = gripAction.action.ReadValue<float>();
    Vector2 thumbstick = thumbstickAction.action.ReadValue<Vector2>();
}
```

## Interactables

### Grabbable Object
```csharp
// Add XR Grab Interactable component
XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();

// Events
grabInteractable.selectEntered.AddListener(OnGrab);
grabInteractable.selectExited.AddListener(OnRelease);

void OnGrab(SelectEnterEventArgs args) {
    Debug.Log($"Grabbed by {args.interactorObject.transform.name}");
}

void OnRelease(SelectExitEventArgs args) {
    Debug.Log("Released");
}

// Configuration
grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;

// Smooth position/rotation
grabInteractable.smoothPosition = true;
grabInteractable.smoothRotation = true;
grabInteractable.smoothPositionAmount = 5f;
grabInteractable.smoothRotationAmount = 5f;
```

### Socket Interactor
```csharp
// Create socket for snap placement
XRSocketInteractor socket = GetComponent<XRSocketInteractor>();

// Show hover mesh
socket.showInteractableHoverMeshes = true;
socket.interactableHoverMeshMaterial = hoverMaterial;

// Events
socket.selectEntered.AddListener(OnObjectPlaced);

void OnObjectPlaced(SelectEnterEventArgs args) {
    Debug.Log($"Placed {args.interactableObject.transform.name}");
}
```

## Teleportation

### Teleportation Area
```csharp
// Add Teleportation Area component to floor
TeleportationArea teleportArea = GetComponent<TeleportationArea>();

// Or Teleportation Anchor for specific points
TeleportationAnchor anchor = GetComponent<TeleportationAnchor>();
anchor.teleportAnchorTransform = destinationTransform;
```

### Custom Teleport
```csharp
public class CustomTeleport : MonoBehaviour {
    [SerializeField] private XRRayInteractor rayInteractor;
    [SerializeField] private InputActionProperty teleportAction;
    
    void Update() {
        if (teleportAction.action.WasPressedThisFrame()) {
            if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit)) {
                Teleport(hit.point);
            }
        }
    }
    
    void Teleport(Vector3 position) {
        XROrigin xrOrigin = FindObjectOfType<XROrigin>();
        xrOrigin.MoveCameraToWorldLocation(position);
    }
}
```

## UI in XR

### Canvas Setup
```csharp
// For XR UI:
// 1. Set Canvas Render Mode to World Space
// 2. Add Tracked Device Graphic Raycaster
// 3. Add XR UI Input Module to EventSystem

Canvas canvas = GetComponent<Canvas>();
canvas.renderMode = RenderMode.WorldSpace;

// Position in front of player
canvas.transform.position = player.position + player.forward * 2f;
canvas.transform.rotation = Quaternion.LookRotation(player.forward);

// Scale for comfortable viewing
canvas.transform.localScale = Vector3.one * 0.001f;
```

### XR Ray Interactor for UI
```csharp
// XRRayInteractor handles UI interaction
XRRayInteractor rayInteractor = GetComponent<XRRayInteractor>();

// Show line
rayInteractor.lineType = XRRayInteractor.LineType.ProjectileCurve;
rayInteractor.lineType = XRRayInteractor.LineType.StraightLine;

// Visuals
LineRenderer lineRenderer = GetComponent<LineRenderer>();
```

## Hand Tracking

### Oculus Hand Tracking
```csharp
// Requires Oculus Integration SDK

// Check if hands are tracked
bool isLeftHandTracked = OVRInput.IsControllerConnected(OVRInput.Controller.Hand);

// Get hand pose
OVRHand hand = GetComponent<OVRHand>();
bool isTracked = hand.IsTracked;

// Finger tracking
float indexPinch = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
bool isPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);

// Bone poses
OVRBone[] bones = hand.GetComponent<OVRSkeleton>().Bones;
```

### XR Hands (OpenXR)
```csharp
// Using Unity XR Hands package

// Subscribe to hand updates
XRHandSubsystem handSubsystem = ...;
handSubsystem.updatedHands += OnHandUpdated;

void OnHandUpdated(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType updateType) {
    XRHand leftHand = subsystem.leftHand;
    XRHand rightHand = subsystem.rightHand;
    
    if (leftHand.isTracked) {
        // Get joint positions
        if (leftHand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose pose)) {
            Vector3 indexTipPosition = pose.position;
        }
    }
}
```

## Passthrough (AR)

### Oculus Passthrough
```csharp
// Enable passthrough
OVRManager.instance.isInsightPassthroughEnabled = true;

// Create passthrough layer
GameObject passthroughObj = new GameObject("Passthrough");
OVRPassthroughLayer passthrough = passthroughObj.AddComponent<OVRPassthroughLayer>();

// Configure
passthrough.overlayType = OVROverlay.OverlayType.Underlay;
passthrough.projectionSurfaceType = OVRPassthroughLayer.ProjectionSurfaceType.Reconstructed;

// Add holes (for UI elements)
passthrough.AddSurfaceGeometry(quadMeshFilter, true);
```

## Spatial Anchors

### Creating Anchors
```csharp
// Oculus Spatial Anchor
OVRSpatialAnchor anchor = gameObject.AddComponent<OVRSpatialAnchor>();

// Save anchor
await anchor.SaveAsync();

// Load anchors
var anchors = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(new OVRSpatialAnchor.LoadOptions {
    StorageLocation = OVRSpace.StorageLocation.Local,
    MaxResults = 100
});
```

## Performance Optimization

### Single Pass Instanced
```csharp
// Player Settings -> XR Settings
// Stereo Rendering Mode: Single Pass Instanced
// Better performance than Multi Pass
```

### Foveated Rendering
```csharp
// Oculus-specific
OVRManager.instance.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.High;
```

### LOD for XR
```csharp
// Use lower LOD bias for XR
QualitySettings.lodBias = 0.5f;

// Reduce shadow distance
QualitySettings.shadowDistance = 50f;
```

## Common Patterns

### Snap Turn
```csharp
public class SnapTurn : MonoBehaviour {
    [SerializeField] private InputActionProperty turnAction;
    [SerializeField] private float turnAngle = 45f;
    
    void Update() {
        Vector2 turnInput = turnAction.action.ReadValue<Vector2>();
        
        if (turnInput.x > 0.5f) {
            transform.Rotate(Vector3.up, turnAngle);
        } else if (turnInput.x < -0.5f) {
            transform.Rotate(Vector3.up, -turnAngle);
        }
    }
}
```

### Continuous Move
```csharp
public class ContinuousMove : MonoBehaviour {
    [SerializeField] private InputActionProperty moveAction;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private CharacterController characterController;
    
    void Update() {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        move = transform.TransformDirection(move);
        characterController.Move(move * moveSpeed * Time.deltaTime);
    }
}
```

### Physics Grabbing
```csharp
public class PhysicsGrabbable : XRGrabInteractable {
    private Rigidbody rb;
    private Transform grabPoint;
    
    protected override void Awake() {
        base.Awake();
        rb = GetComponent<Rigidbody>();
    }
    
    protected override void OnSelectEntering(SelectEnterEventArgs args) {
        base.OnSelectEntering(args);
        grabPoint = args.interactorObject.transform;
    }
    
    void FixedUpdate() {
        if (isSelected && grabPoint != null) {
            // Move towards hand
            Vector3 direction = grabPoint.position - transform.position;
            rb.velocity = direction / Time.fixedDeltaTime;
            
            // Rotate to match hand
            rb.MoveRotation(grabPoint.rotation);
        }
    }
}
```

## Platform Detection

```csharp
// Check if XR is enabled
bool isXREnabled = XRSettings.enabled;

// Get loaded device
string deviceName = XRSettings.loadedDeviceName;

// Check specific platform
bool isOculus = deviceName.Contains("Oculus");
bool isOpenXR = deviceName.Contains("OpenXR");
bool isSteamVR = deviceName.Contains("OpenVR");

// Get display dimensions
Vector2 resolution = new Vector2(XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight);
```
