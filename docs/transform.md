# Unity Transform Component

## Overview
The Transform component determines the Position, Rotation, and Scale of each object in the scene.

## Properties

### position
- Type: Vector3
- Description: The position of the transform in world space.
- Example: `transform.position = new Vector3(0, 5, 0);`

### rotation
- Type: Quaternion
- Description: The rotation of the transform in world space stored as a Quaternion.
- Example: `transform.rotation = Quaternion.Euler(0, 90, 0);`

### localRotation
- Type: Quaternion
- Description: The rotation of the transform relative to the parent transform.
- Example: `transform.localRotation = Quaternion.identity;`

### eulerAngles
- Type: Vector3
- Description: The rotation as Euler angles in degrees.
- Example: `transform.eulerAngles = new Vector3(0, 45, 0);`

### localEulerAngles
- Type: Vector3
- Description: The rotation as Euler angles in degrees relative to the parent transform.
- Example: `transform.localEulerAngles = new Vector3(0, 45, 0);`

## Methods

### Rotate
```csharp
void Rotate(Vector3 eulers, Space relativeTo = Space.Self)
```
Rotates the transform around the specified axis.

Example:
```csharp
transform.Rotate(0, 90, 0); // Rotate 90 degrees around Y axis
transform.Rotate(Vector3.up * Time.deltaTime * 100); // Rotate continuously
```

### RotateAround
```csharp
void RotateAround(Vector3 point, Vector3 axis, float angle)
```
Rotates the transform around a point in world space.

Example:
```csharp
transform.RotateAround(Vector3.zero, Vector3.up, 20); // Orbit around origin
```

### LookAt
```csharp
void LookAt(Transform target)
void LookAt(Vector3 worldPosition)
```
Rotates the transform to look at a target.

Example:
```csharp
transform.LookAt(playerTransform);
transform.LookAt(new Vector3(0, 0, 10));
```

## Common Patterns

### Smooth Rotation
```csharp
void Update() {
    // Smoothly rotate towards target rotation
    transform.rotation = Quaternion.Slerp(
        transform.rotation, 
        targetRotation, 
        Time.deltaTime * rotationSpeed
    );
}
```

### Rotate Towards Target
```csharp
void Update() {
    Vector3 direction = target.position - transform.position;
    Quaternion targetRotation = Quaternion.LookRotation(direction);
    transform.rotation = Quaternion.Slerp(
        transform.rotation, 
        targetRotation, 
        Time.deltaTime * 5f
    );
}
```

### Spin Object
```csharp
void Update() {
    transform.Rotate(0, spinSpeed * Time.deltaTime, 0);
}
```
