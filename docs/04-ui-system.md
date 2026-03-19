# Unity UI System (uGUI)

## Canvas

### Creating a Canvas
```csharp
// Canvas is automatically created when you add a UI element
// Or create manually: GameObject -> UI -> Canvas
```

### Canvas Render Modes
```csharp
Canvas canvas = GetComponent<Canvas>();

// Screen Space - Overlay (default)
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
// UI appears on top of everything, scales with screen

// Screen Space - Camera
canvas.renderMode = RenderMode.ScreenSpaceCamera;
canvas.worldCamera = Camera.main;
// UI placed in front of camera, can be affected by camera effects

// World Space
canvas.renderMode = RenderMode.WorldSpace;
// UI exists in 3D world, can be placed on objects
```

### Canvas Scaler
```csharp
CanvasScaler scaler = GetComponent<CanvasScaler>();

// Scale with screen size (recommended)
scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1920, 1080);
scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
scaler.matchWidthOrHeight = 0.5f; // 0 = width, 1 = height

// Constant pixel size
scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
scaler.scaleFactor = 1f;

// Constant physical size
scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
```

## RectTransform

### Positioning
```csharp
RectTransform rt = GetComponent<RectTransform>();

// Anchors (0-1 range, relative to parent)
rt.anchorMin = new Vector2(0, 0); // Bottom-left
rt.anchorMax = new Vector2(1, 1); // Top-right
rt.anchorMin = new Vector2(0.5f, 0.5f); // Center

// Anchored position (offset from anchor)
rt.anchoredPosition = new Vector2(100, 50);

// Size delta (width/height from anchors)
rt.sizeDelta = new Vector2(200, 100);

// Pivot (0-1, where rotation/scale happens)
rt.pivot = new Vector2(0.5f, 0.5f); // Center
rt.pivot = new Vector2(0, 0); // Bottom-left

// Rotation and scale
rt.rotation = Quaternion.Euler(0, 0, 45);
rt.localScale = new Vector3(1.5f, 1.5f, 1);
```

### Common Presets
```csharp
// Stretch to fill parent
rt.anchorMin = Vector2.zero;
rt.anchorMax = Vector2.one;
rt.offsetMin = Vector2.zero; // Left, Bottom padding
rt.offsetMax = Vector2.zero; // Right, Top padding (negative)

// Center with fixed size
rt.anchorMin = new Vector2(0.5f, 0.5f);
rt.anchorMax = new Vector2(0.5f, 0.5f);
rt.anchoredPosition = Vector2.zero;
rt.sizeDelta = new Vector2(200, 100);

// Top-left corner
rt.anchorMin = new Vector2(0, 1);
rt.anchorMax = new Vector2(0, 1);
rt.anchoredPosition = new Vector2(100, -50);
rt.pivot = new Vector2(0, 1);
```

## UI Elements

### Text (Legacy)
```csharp
Text text = GetComponent<Text>();
text.text = "Hello World";
text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
text.fontSize = 24;
text.color = Color.white;
text.alignment = TextAnchor.MiddleCenter;
text.horizontalOverflow = HorizontalWrapMode.Wrap;
text.verticalOverflow = VerticalWrapMode.Truncate;
```

### TextMeshPro (Recommended)
```csharp
using TMPro;

TextMeshProUGUI tmp = GetComponent<TextMeshProUGUI>();
tmp.text = "Hello World";
tmp.fontSize = 24;
tmp.color = Color.white;
tmp.alignment = TextAlignmentOptions.Center;
tmp.fontStyle = FontStyles.Bold;

// Rich text
tmp.text = "<color=red>Red</color> and <b>bold</b>";

// Auto size
tmp.enableAutoSizing = true;
tmp.fontSizeMin = 10;
tmp.fontSizeMax = 72;
```

### Image
```csharp
Image image = GetComponent<Image>();
image.sprite = mySprite;
image.color = new Color(1, 1, 1, 0.5f); // Half transparent
image.type = Image.Type.Simple; // Normal
image.type = Image.Type.Sliced; // 9-slice
image.type = Image.Type.Tiled; // Repeating
image.type = Image.Type.Filled; // Progress bar

// Filled image (progress bar)
image.type = Image.Type.Filled;
image.fillMethod = Image.FillMethod.Horizontal;
image.fillOrigin = (int)Image.OriginHorizontal.Left;
image.fillAmount = 0.75f; // 75% filled
```

### Button
```csharp
Button button = GetComponent<Button>();

// Add click listener
button.onClick.AddListener(OnButtonClick);
button.onClick.AddListener(() => Debug.Log("Clicked!"));

// Remove listener
button.onClick.RemoveListener(OnButtonClick);
button.onClick.RemoveAllListeners();

// Button properties
button.interactable = true;
button.transition = Selectable.Transition.ColorTint;

// Colors
ColorBlock colors = button.colors;
colors.normalColor = Color.white;
colors.highlightedColor = Color.yellow;
colors.pressedColor = Color.gray;
colors.disabledColor = new Color(1, 1, 1, 0.5f);
button.colors = colors;
```

### Slider
```csharp
Slider slider = GetComponent<Slider>();
slider.minValue = 0;
slider.maxValue = 100;
slider.value = 50;

// Event
slider.onValueChanged.AddListener(OnSliderChanged);

void OnSliderChanged(float value) {
    Debug.Log($"Slider value: {value}");
}
```

### Toggle
```csharp
Toggle toggle = GetComponent<Toggle>();
toggle.isOn = true;
toggle.onValueChanged.AddListener(OnToggleChanged);

void OnToggleChanged(bool isOn) {
    Debug.Log($"Toggle: {isOn}");
}
```

### Input Field
```csharp
InputField input = GetComponent<InputField>();
input.text = "Default text";
input.placeholder.GetComponent<Text>().text = "Enter text...";
input.characterLimit = 20;
input.contentType = InputField.ContentType.Standard;
input.contentType = InputField.ContentType.Password;
input.contentType = InputField.ContentType.EmailAddress;
input.contentType = InputField.ContentType.DecimalNumber;

// Events
input.onValueChanged.AddListener(OnTextChanged);
input.onEndEdit.AddListener(OnTextSubmitted);

void OnTextSubmitted(string text) {
    Debug.Log($"Submitted: {text}");
}
```

### Dropdown
```csharp
Dropdown dropdown = GetComponent<Dropdown>();

// Set options
dropdown.options = new List<Dropdown.OptionData> {
    new Dropdown.OptionData("Option 1"),
    new Dropdown.OptionData("Option 2"),
    new Dropdown.OptionData("Option 3")
};

// Event
dropdown.onValueChanged.AddListener(OnDropdownChanged);

void OnDropdownChanged(int index) {
    Debug.Log($"Selected: {dropdown.options[index].text}");
}
```

### ScrollView
```csharp
// Structure: ScrollView -> Viewport -> Content
ScrollRect scrollRect = GetComponent<ScrollRect>();
scrollRect.content = contentRectTransform;
scrollRect.horizontal = true;
scrollRect.vertical = true;
scrollRect.movementType = ScrollRect.MovementType.Elastic;
scrollRect.elasticity = 0.1f;

// Scroll to position
scrollRect.normalizedPosition = new Vector2(0, 1); // Top
scrollRect.normalizedPosition = new Vector2(0, 0); // Bottom
```

## Layout Groups

### Horizontal Layout Group
```csharp
HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
layout.padding = new RectOffset(10, 10, 10, 10);
layout.spacing = 10;
layout.childAlignment = TextAnchor.MiddleCenter;
layout.childControlWidth = true;
layout.childControlHeight = true;
layout.childForceExpandWidth = false;
layout.childForceExpandHeight = false;
```

### Vertical Layout Group
```csharp
VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
layout.padding = new RectOffset(10, 10, 10, 10);
layout.spacing = 10;
layout.childAlignment = TextAnchor.UpperLeft;
```

### Grid Layout Group
```csharp
GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
grid.cellSize = new Vector2(100, 100);
grid.spacing = new Vector2(10, 10);
grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
grid.startAxis = GridLayoutGroup.Axis.Horizontal;
grid.childAlignment = TextAnchor.MiddleCenter;
grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
grid.constraintCount = 3;
```

### Content Size Fitter
```csharp
// Makes element size match content
ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
```

### Aspect Ratio Fitter
```csharp
AspectRatioFitter aspect = GetComponent<AspectRatioFitter>();
aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
aspect.aspectRatio = 16f / 9f;
```

## Creating UI Programmatically

```csharp
void CreateButton() {
    // Create canvas if needed
    Canvas canvas = FindObjectOfType<Canvas>();
    if (canvas == null) {
        GameObject canvasGO = new GameObject("Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
    }
    
    // Create button
    GameObject buttonGO = new GameObject("MyButton");
    buttonGO.transform.SetParent(canvas.transform, false);
    
    RectTransform rt = buttonGO.AddComponent<RectTransform>();
    rt.sizeDelta = new Vector2(200, 50);
    rt.anchoredPosition = Vector2.zero;
    
    Image image = buttonGO.AddComponent<Image>();
    image.color = Color.blue;
    
    Button button = buttonGO.AddComponent<Button>();
    button.onClick.AddListener(() => Debug.Log("Button clicked!"));
    
    // Add text
    GameObject textGO = new GameObject("Text");
    textGO.transform.SetParent(buttonGO.transform, false);
    
    RectTransform textRT = textGO.AddComponent<RectTransform>();
    textRT.anchorMin = Vector2.zero;
    textRT.anchorMax = Vector2.one;
    textRT.sizeDelta = Vector2.zero;
    
    Text text = textGO.AddComponent<Text>();
    text.text = "Click Me";
    text.alignment = TextAnchor.MiddleCenter;
    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
```

## World Space UI

```csharp
// Create world space canvas
GameObject canvasGO = new GameObject("WorldCanvas");
Canvas canvas = canvasGO.AddComponent<Canvas>();
canvas.renderMode = RenderMode.WorldSpace;

// Set size and position
canvasGO.transform.position = new Vector3(0, 2, 0);
canvasGO.transform.rotation = Quaternion.Euler(0, 180, 0);
RectTransform rt = canvas.GetComponent<RectTransform>();
rt.sizeDelta = new Vector2(2, 1); // 2 meters wide, 1 meter tall

// Add health bar, name tag, etc.
```

## UI Raycasting

```csharp
// Check if mouse is over UI
using UnityEngine.EventSystems;

bool IsPointerOverUI() {
    return EventSystem.current.IsPointerOverGameObject();
}

// Check if touch is over UI
bool IsTouchOverUI(int touchId) {
    return EventSystem.current.IsPointerOverGameObject(touchId);
}

// Raycast from screen position
PointerEventData pointerData = new PointerEventData(EventSystem.current);
pointerData.position = Input.mousePosition;

List<RaycastResult> results = new List<RaycastResult>();
EventSystem.current.RaycastAll(pointerData, results);

foreach (RaycastResult result in results) {
    Debug.Log($"Hit UI: {result.gameObject.name}");
}
```
