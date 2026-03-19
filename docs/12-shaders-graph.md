# Unity Shaders & Shader Graph

## Shader Graph (Visual Scripting)

### Creating Shader Graph
```csharp
// In Editor: Create -> Shader -> URP Shader Graph
// Or: Create -> Shader -> HDRP Shader Graph
```

### Using Shader Graph Material
```csharp
Material mat = new Material(shaderGraphAsset);
GetComponent<Renderer>().material = mat;

// Exposed properties (set in Shader Graph Blackboard)
mat.SetColor("_BaseColor", Color.red);
mat.SetFloat("_Metallic", 0.5f);
mat.SetTexture("_MainTex", myTexture);
```

## ShaderLab (Code)

### Basic Shader Structure
```csharp
Shader "Custom/MyShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        struct Input {
            float2 uv_MainTex;
        };
        
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        
        void surf (Input IN, inout SurfaceOutputStandard o) {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    
    FallBack "Diffuse"
}
```

### Unlit Shader
```csharp
Shader "Custom/Unlit" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}
```

## Shader Properties

### Property Types
```csharp
Properties {
    // Numbers
    _FloatValue ("Float", Float) = 0.5
    _RangeValue ("Range", Range(0, 1)) = 0.5
    _IntValue ("Int", Int) = 1
    
    // Colors
    _Color ("Color", Color) = (1,1,1,1)
    _Vector ("Vector", Vector) = (0,0,0,0)
    
    // Textures
    _MainTex ("Texture", 2D) = "white" {}
    _BumpMap ("Normal Map", 2D) = "bump" {}
    _CubeMap ("Cube Map", Cube) = "" {}
    _3DTexture ("3D Texture", 3D) = "" {}
}
```

## Shader Keywords

```csharp
// Multi-compile for different variants
#pragma multi_compile _ _EMISSION
#pragma multi_compile _ _NORMALMAP
#pragma multi_compile_fwdbase
#pragma multi_compile_fog

// Enable/disable keywords
Material mat = GetComponent<Renderer>().material;
mat.EnableKeyword("_EMISSION");
mat.DisableKeyword("_EMISSION");

// Check if keyword is enabled
bool isEmissionOn = mat.IsKeywordEnabled("_EMISSION");
```

## Shader Variants

```csharp
// Create shader variant collection
ShaderVariantCollection svc = new ShaderVariantCollection();

// Add variants
ShaderVariantCollection.ShaderVariant variant = new ShaderVariantCollection.ShaderVariant();
variant.shader = myShader;
variant.keywords = new string[] { "_EMISSION", "_NORMALMAP" };
svc.Add(variant);

// Warmup (precompile)
svc.WarmUp();
```

## Compute Shaders

### Basic Compute Shader
```csharp
// ComputeShader.compute file
#pragma kernel CSMain

RWTexture2D<float4> Result;

[numthreads(8,8,1)]
void CSMain (uint3 id : SV_DispatchThreadID) {
    Result[id.xy] = float4(id.x / 255.0, id.y / 255.0, 0, 1);
}
```

### Using Compute Shader
```csharp
ComputeShader computeShader;
RenderTexture resultTexture;

void Start() {
    resultTexture = new RenderTexture(256, 256, 0);
    resultTexture.enableRandomWrite = true;
    resultTexture.Create();
    
    computeShader.SetTexture(0, "Result", resultTexture);
    computeShader.Dispatch(0, 256/8, 256/8, 1);
}
```

## Shader Graph Nodes (Scripting)

### Custom Function Node
```csharp
// Create HLSL file for custom node
// Used in Shader Graph Custom Function node

void MyCustomFunction_float(float3 In, out float3 Out) {
    Out = In * 2;
}

void MyCustomFunction_half(half3 In, out half3 Out) {
    Out = In * 2;
}
```

## Material Property Blocks with Shaders

```csharp
MaterialPropertyBlock props = new MaterialPropertyBlock();
Renderer renderer = GetComponent<Renderer>();

// Set properties without creating material instance
props.SetColor("_Color", Color.red);
props.SetFloat("_Metallic", 0.5f);
props.SetTexture("_MainTex", texture);

renderer.SetPropertyBlock(props);
```

## Shader Replacement

```csharp
// Render objects with different shader
Camera.main.SetReplacementShader(myReplacementShader, "RenderType");

// Clear replacement
Camera.main.ResetReplacementShader();
```

## Common Shader Patterns

### Scrolling Texture
```csharp
// In shader
float2 uv = IN.uv_MainTex;
uv.x += _Time.y * _ScrollSpeed;
fixed4 c = tex2D(_MainTex, uv);
```

### Dissolve Effect
```csharp
// In shader properties
_DissolveTex ("Dissolve Texture", 2D) = "white" {}
_DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0

// In surf function
float dissolve = tex2D(_DissolveTex, IN.uv_MainTex).r;
clip(dissolve - _DissolveAmount);
```

### Rim Lighting
```csharp
// In surf function
float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
float rim = 1 - saturate(dot(viewDir, o.Normal));
o.Emission = _RimColor.rgb * pow(rim, _RimPower);
```

## Shader Performance

### LOD System
```csharp
SubShader {
    Tags { "LOD" = "100" }
    // High quality
}

SubShader {
    Tags { "LOD" = "50" }
    // Lower quality
}
```

### Shader Target
```csharp
#pragma target 2.0 // Mobile
#pragma target 3.0 // Default
#pragma target 4.0 // Tessellation
#pragma target 5.0 // Compute
```
