// Unity MCP Bridge - Clean, Fixed Version v3.0
// Place in: Assets/Editor/UnityMcpBridge.cs
// Controls Unity Editor via HTTP API on localhost:7778

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class UnityMcpBridge
{
    private static HttpListener httpListener;
    private static Thread listenerThread;
    private static bool isRunning = false;
    private static readonly int PORT = 7778;
    private static readonly string URL = "http://localhost:" + PORT + "/";
    private static readonly Queue<Action> mainThreadActions = new Queue<Action>();
    private static readonly object queueLock = new object();
    
    static UnityMcpBridge()
    {
        EditorApplication.update += ProcessMainThreadQueue;
        StartServer();
    }
    
    [MenuItem("Tools/MCP Bridge/Start Server")]
    private static void StartServer()
    {
        if (isRunning) return;
        try
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(URL);
            httpListener.Start();
            isRunning = true;
            listenerThread = new Thread(ListenLoop);
            listenerThread.IsBackground = true;
            listenerThread.Start();
            Debug.Log("[MCP] Server started on " + URL);
        }
        catch (Exception ex)
        {
            Debug.LogError("[MCP] Failed to start: " + ex.Message);
        }
    }
    
    [MenuItem("Tools/MCP Bridge/Stop Server")]
    private static void StopServer()
    {
        if (!isRunning) return;
        isRunning = false;
        try { httpListener?.Stop(); } catch { }
        try { httpListener?.Close(); } catch { }
        Debug.Log("[MCP] Server stopped");
    }
    
    [MenuItem("Tools/MCP Bridge/Restart Server")]
    private static void RestartServer()
    {
        StopServer();
        Thread.Sleep(100);
        StartServer();
    }
    
    private static void ProcessMainThreadQueue()
    {
        lock (queueLock)
        {
            while (mainThreadActions.Count > 0)
            {
                try { mainThreadActions.Dequeue()?.Invoke(); }
                catch (Exception ex) { Debug.LogError("[MCP] Queue error: " + ex.Message); }
            }
        }
    }
    
    private static void EnqueueOnMainThread(Action action)
    {
        lock (queueLock) { mainThreadActions.Enqueue(action); }
    }
    
    private static void ListenLoop()
    {
        while (isRunning)
        {
            try
            {
                var context = httpListener.GetContext();
                ThreadPool.QueueUserWorkItem(HandleRequest, context);
            }
            catch (HttpListenerException) { break; }
            catch (Exception ex) { if (isRunning) Debug.LogError("[MCP] Listen: " + ex.Message); }
        }
    }
    
    private static void HandleRequest(object state)
    {
        var context = (HttpListenerContext)state;
        var response = context.Response;
        string result = "{\"error\":\"Internal error\"}";
        int statusCode = 500;
        
        try
        {
            string path = context.Request.Url.AbsolutePath;
            string body = "";
            if (context.Request.HasEntityBody)
            {
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    body = reader.ReadToEnd();
            }
            Debug.Log("[MCP] " + context.Request.HttpMethod + " " + path);
            
            var done = new ManualResetEvent(false);
            string asyncResult = null;
            
            EnqueueOnMainThread(() =>
            {
                try { asyncResult = ProcessRequest(path, body); }
                catch (Exception ex) { asyncResult = "{\"error\":\"" + EscapeJson(ex.Message) + "\"}"; }
                done.Set();
            });
            
            if (done.WaitOne(5000))
            {
                result = asyncResult ?? "{}";
                statusCode = 200;
            }
            else
            {
                result = "{\"error\":\"timeout\"}";
                statusCode = 500;
            }
        }
        catch (Exception ex)
        {
            result = "{\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            Debug.LogError("[MCP] Request error: " + ex.Message);
        }
        finally
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(result);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = statusCode;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }
            catch { }
        }
    }
    
    private static string ProcessRequest(string path, string body)
    {
        // Core endpoints
        if (path == "/api/hierarchy") return GetHierarchy();
        if (path == "/api/selection") return GetSelection();
        if (path == "/api/scene-info") return GetSceneInfo();
        
        // Object operations
        if (path == "/api/spawn") return SpawnObject(body);
        if (path == "/api/modify") return ModifyObject(body);
        if (path == "/api/delete") return DeleteObject(body);
        if (path == "/api/duplicate") return DuplicateObject(body);
        
        // Components
        if (path == "/api/add-component") return AddComponent(body);
        if (path == "/api/remove-component") return RemoveComponent(body);
        if (path == "/api/get-components") return GetComponents(body);
        if (path == "/api/set-material") return SetMaterial(body);
        
        // Materials
        if (path == "/api/create-material") return CreateMaterial(body);
        if (path == "/api/create-water") return CreateWater(body);
        
        // Camera
        if (path == "/api/camera-move") return MoveCamera(body);
        if (path == "/api/camera-look-at") return CameraLookAt(body);
        
        // Lights
        if (path == "/api/create-light") return CreateLight(body);
        
        // Play mode
        if (path == "/api/play") return SetPlayMode(true);
        if (path == "/api/stop") return SetPlayMode(false);
        if (path == "/api/pause") return TogglePause();
        
        // Console
        if (path == "/api/clear-console") { Debug.ClearDeveloperConsole(); return J("{success", true, "}"); }
        
        return "{\"error\":\"Unknown endpoint: " + path + "\"}";
    }
    
    // ==================== HIERARCHY & SCENE ====================
    
    private static string GetHierarchy()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();
        var objects = new List<object>();
        foreach (var obj in rootObjects) objects.Add(SerializeGameObject(obj));
        return J(
            "scene", scene.name,
            "path", scene.path,
            "objectCount", objects.Count,
            "objects", objects
        );
    }
    
    private static string GetSelection()
    {
        var selected = new List<object>();
        foreach (var obj in Selection.gameObjects) selected.Add(SerializeGameObject(obj, true));
        return J("count", selected.Count, "objects", selected);
    }
    
    private static string GetSceneInfo()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return J(
            "name", scene.name,
            "path", scene.path,
            "rootCount", scene.rootCount,
            "isDirty", scene.isDirty,
            "isPlaying", EditorApplication.isPlaying,
            "isPaused", EditorApplication.isPaused
        );
    }
    
    // ==================== OBJECT OPERATIONS ====================
    
    private static string SpawnObject(string body)
    {
        var data = JsonUtility.FromJson<SpawnData>(body);
        
        GameObject obj;
        if (string.IsNullOrEmpty(data.primitive) || data.primitive.ToLower() == "empty")
        {
            obj = new GameObject();
        }
        else
        {
            obj = GameObject.CreatePrimitive(GetPrimitiveType(data.primitive));
        }
        
        obj.name = string.IsNullOrEmpty(data.name) ? (string.IsNullOrEmpty(data.primitive) ? "GameObject" : data.primitive) : data.name;
        
        // Position - default (0,0,0)
        var pos = data.position ?? new Vector3Data();
        obj.transform.position = new Vector3(pos.x, pos.y, pos.z);
        
        // Rotation - default (0,0,0)
        var rot = data.rotation ?? new Vector3Data();
        obj.transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        
        // Scale - check if scale was actually provided in JSON
        // JsonUtility can't distinguish between "scale not provided" and "scale is (0,0,0)"
        // So we check if "scale" keyword exists in raw JSON
        bool hasScaleInJson = body.Contains("\"scale\"");
        
        if (hasScaleInJson && data.scale != null)
        {
            obj.transform.localScale = new Vector3(data.scale.x, data.scale.y, data.scale.z);
        }
        else if (!string.IsNullOrEmpty(data.primitive) && data.primitive.ToLower() != "empty")
        {
            // Primitives default to (1,1,1) - this is the fix!
            obj.transform.localScale = new Vector3(1, 1, 1);
        }
        // Empty GameObjects can have (0,0,0) scale - user must set explicitly
        
        // Parent
        if (!string.IsNullOrEmpty(data.parent))
        {
            var parent = GameObject.Find(data.parent);
            if (parent != null) obj.transform.SetParent(parent.transform);
        }
        
        Undo.RegisterCreatedObjectUndo(obj, "MCP Spawn");
        
        return J(
            "success", true,
            "name", obj.name,
            "instanceID", obj.GetInstanceID(),
            "position", new Vector3Data { x = obj.transform.position.x, y = obj.transform.position.y, z = obj.transform.position.z },
            "scale", new Vector3Data { x = obj.transform.localScale.x, y = obj.transform.localScale.y, z = obj.transform.localScale.z },
            "path", GetGameObjectPath(obj)
        );
    }
    
    private static string ModifyObject(string body)
    {
        var data = JsonUtility.FromJson<ModifyData>(body);
        var obj = FindObject(data.name, data.instanceID);
        if (obj == null) return J("error", "Object not found: " + data.name);
        
        Undo.RecordObject(obj.transform, "MCP Modify");
        
        if (data.newName != null) obj.name = data.newName;
        if (data.active.HasValue) obj.SetActive(data.active.Value);
        if (data.position != null) obj.transform.position = new Vector3(data.position.x, data.position.y, data.position.z);
        if (data.rotation != null) obj.transform.rotation = Quaternion.Euler(data.rotation.x, data.rotation.y, data.rotation.z);
        if (data.scale != null) obj.transform.localScale = new Vector3(data.scale.x, data.scale.y, data.scale.z);
        
        return J(
            "success", true,
            "name", obj.name,
            "position", new Vector3Data { x = obj.transform.position.x, y = obj.transform.position.y, z = obj.transform.position.z },
            "rotation", new Vector3Data { x = obj.transform.eulerAngles.x, y = obj.transform.eulerAngles.y, z = obj.transform.eulerAngles.z },
            "scale", new Vector3Data { x = obj.transform.localScale.x, y = obj.transform.localScale.y, z = obj.transform.localScale.z }
        );
    }
    
    private static string DeleteObject(string body)
    {
        var data = JsonUtility.FromJson<DeleteData>(body);
        var obj = FindObject(data.name, data.instanceID);
        if (obj == null) return J("error", "Object not found: " + data.name);
        Undo.DestroyObjectImmediate(obj);
        return J("success", true, "name", data.name);
    }
    
    private static string DuplicateObject(string body)
    {
        var data = JsonUtility.FromJson<DeleteData>(body);
        var original = FindObject(data.name, data.instanceID);
        if (original == null) return J("error", "Object not found: " + data.name);
        var duplicate = UnityEngine.Object.Instantiate(original);
        duplicate.name = original.name + " (1)";
        Undo.RegisterCreatedObjectUndo(duplicate, "MCP Duplicate");
        return J(
            "success", true,
            "name", duplicate.name,
            "instanceID", duplicate.GetInstanceID()
        );
    }
    
    // ==================== COMPONENTS ====================
    
    private static string AddComponent(string body)
    {
        var data = JsonUtility.FromJson<ComponentData>(body);
        var obj = FindObject(data.objectName, data.instanceID);
        if (obj == null) return J("error", "Object not found: " + data.objectName);
        
        var type = GetTypeByName(data.componentType);
        if (type == null) return J("error", "Component type not found: " + data.componentType);
        
        var component = obj.AddComponent(type);
        return J("success", true, "component", data.componentType, "instanceID", component.GetInstanceID());
    }
    
    private static string RemoveComponent(string body)
    {
        var data = JsonUtility.FromJson<ComponentData>(body);
        var obj = FindObject(data.objectName, data.instanceID);
        if (obj == null) return J("error", "Object not found: " + data.objectName);
        
        var type = GetTypeByName(data.componentType);
        if (type == null) return J("error", "Component type not found: " + data.componentType);
        
        var component = obj.GetComponent(type);
        if (component == null) return J("error", "Component not found on object");
        
        Undo.DestroyObjectImmediate(component);
        return J("success", true);
    }
    
    private static string GetComponents(string body)
    {
        var data = JsonUtility.FromJson<ComponentData>(body);
        var obj = FindObject(data.objectName, data.instanceID);
        if (obj == null) return J("error", "Object not found: " + data.objectName);
        
        var components = new List<object>();
        foreach (var comp in obj.GetComponents<Component>())
        {
            components.Add(new Dictionary<string, object> {
                ["type"] = comp.GetType().Name,
                ["instanceID"] = comp.GetInstanceID()
            });
        }
        return J("objectName", obj.name, "components", components);
    }
    
    private static string SetMaterial(string body)
    {
        var data = JsonUtility.FromJson<SetMaterialData>(body);
        var obj = FindObject(data.objectName, data.instanceID);
        if (obj == null) return J("error", "Object not found: " + data.objectName);
        
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return J("error", "Object has no renderer");
        
        var material = new Material(Shader.Find(data.shader ?? "Standard"));
        material.name = data.name ?? "Material";
        
        if (data.color != null)
            material.color = new Color(data.color.r, data.color.g, data.color.b, data.color.a);
        
        renderer.material = material;
        Undo.RegisterCreatedObjectUndo(material, "MCP Create Material");
        
        return J("success", true, "material", material.name);
    }
    
    // ==================== MATERIALS & WATER ====================
    
    private static string CreateMaterial(string body)
    {
        var data = JsonUtility.FromJson<MaterialData>(body);
        var material = new Material(Shader.Find(data.shader ?? "Standard"));
        material.name = data.name ?? "NewMaterial";
        
        if (data.color != null)
            material.color = new Color(data.color.r, data.color.g, data.color.b, data.color.a);
        
        var path = "Assets/" + material.name + ".mat";
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        
        return J("success", true, "path", path, "name", material.name);
    }
    
    private static string CreateWater(string body)
    {
        var data = JsonUtility.FromJson<WaterData>(body);
        
        // Create water plane
        var waterObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterObj.name = data.name ?? "Water";
        
        // Position
        var pos = data.position ?? new Vector3Data { y = 0 };
        waterObj.transform.position = new Vector3(pos.x, pos.y, pos.z);
        
        // Scale - water should be large
        var scale = data.scale ?? new Vector3Data { x = 100, y = 1, z = 100 };
        waterObj.transform.localScale = new Vector3(scale.x, scale.y, scale.z);
        
        // Create water material - try multiple shader options
        Material material;
        string shaderName = "Standard";
        
        // Try built-in water shaders first
        string[] waterShaders = {
            "Water4/Example/FastBlade",
            "Legacy Shaders/Transparent/Diffuse",
            "Sprites/Default"
        };
        
        foreach (var sh in waterShaders)
        {
            var testMat = new Material(Shader.Find(sh));
            if (testMat != null && testMat.shader != null)
            {
                shaderName = sh;
                material = testMat;
                break;
            }
        }
        
        material = new Material(Shader.Find(shaderName));
        material.name = (data.name ?? "Water") + "_Mat";
        
        // Set water-like properties
        material.color = new Color(0.1f, 0.3f, 0.6f, 0.7f); // Blue-ish transparent
        material.SetFloat("_Mode", 3); // Transparent mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        
        var renderer = waterObj.GetComponent<Renderer>();
        renderer.material = material;
        
        Undo.RegisterCreatedObjectUndo(waterObj, "MCP Create Water");
        
        return J(
            "success", true,
            "name", waterObj.name,
            "instanceID", waterObj.GetInstanceID(),
            "position", new Vector3Data { x = waterObj.transform.position.x, y = waterObj.transform.position.y, z = waterObj.transform.position.z },
            "scale", new Vector3Data { x = waterObj.transform.localScale.x, y = waterObj.transform.localScale.y, z = waterObj.transform.localScale.z },
            "material", material.name,
            "shader", shaderName
        );
    }
    
    // ==================== CAMERA ====================
    
    private static string MoveCamera(string body)
    {
        var data = JsonUtility.FromJson<CameraData>(body);
        var camera = Camera.main;
        if (camera == null) return J("error", "No main camera found");
        
        Undo.RecordObject(camera.transform, "MCP Move Camera");
        
        if (data.position != null)
            camera.transform.position = new Vector3(data.position.x, data.position.y, data.position.z);
        
        if (data.rotation != null)
            camera.transform.rotation = Quaternion.Euler(data.rotation.x, data.rotation.y, data.rotation.z);
        
        if (data.fieldOfView.HasValue)
            camera.fieldOfView = data.fieldOfView.Value;
        
        if (data.orthographic.HasValue)
            camera.orthographic = data.orthographic.Value;
        
        if (data.orthographicSize.HasValue)
            camera.orthographicSize = data.orthographicSize.Value;
        
        return J(
            "success", true,
            "position", new Vector3Data { x = camera.transform.position.x, y = camera.transform.position.y, z = camera.transform.position.z },
            "rotation", new Vector3Data { x = camera.transform.eulerAngles.x, y = camera.transform.eulerAngles.y, z = camera.transform.eulerAngles.z },
            "fieldOfView", camera.fieldOfView
        );
    }
    
    private static string CameraLookAt(string body)
    {
        var data = JsonUtility.FromJson<LookAtData>(body);
        var camera = Camera.main;
        if (camera == null) return J("error", "No main camera found");
        
        var target = FindObject(data.target, data.targetInstanceID);
        if (target == null) return J("error", "Target not found: " + data.target);
        
        Undo.RecordObject(camera.transform, "MCP Camera LookAt");
        camera.transform.LookAt(target.transform);
        
        return J(
            "success", true,
            "target", target.name,
            "position", new Vector3Data { x = camera.transform.position.x, y = camera.transform.position.y, z = camera.transform.position.z },
            "rotation", new Vector3Data { x = camera.transform.eulerAngles.x, y = camera.transform.eulerAngles.y, z = camera.transform.eulerAngles.z }
        );
    }
    
    // ==================== LIGHTS ====================
    
    private static string CreateLight(string body)
    {
        var data = JsonUtility.FromJson<LightData>(body);
        
        var lightObj = new GameObject();
        lightObj.name = data.name ?? "Light";
        
        var light = lightObj.AddComponent<Light>();
        
        // Light type
        light.type = data.type != null ? GetLightType(data.type) : LightType.Directional;
        
        // Position
        if (data.position != null)
            lightObj.transform.position = new Vector3(data.position.x, data.position.y, data.position.z);
        
        // Color
        if (data.color != null)
            light.color = new Color(data.color.r, data.color.g, data.color.b, data.color.a);
        
        // Intensity
        if (data.intensity.HasValue)
            light.intensity = data.intensity.Value;
        
        // Shadows
        if (data.shadows.HasValue)
            light.shadows = data.shadows.Value ? LightShadows.Soft : LightShadows.None;
        
        // Range (for point/spot lights)
        if (data.range.HasValue)
            light.range = data.range.Value;
        
        // Spot angle (for spot lights)
        if (data.spotAngle.HasValue)
            light.spotAngle = data.spotAngle.Value;
        
        Undo.RegisterCreatedObjectUndo(lightObj, "MCP Create Light");
        
        return J(
            "success", true,
            "name", lightObj.name,
            "type", light.type.ToString(),
            "instanceID", light.GetInstanceID()
        );
    }
    
    private static LightType GetLightType(string type)
    {
        switch (type.ToLower())
        {
            case "directional": return LightType.Directional;
            case "point": return LightType.Point;
            case "spot": return LightType.Spot;
            case "area": return LightType.Area;
            case "rectangle": return LightType.Rectangle;
            default: return LightType.Directional;
        }
    }
    
    // ==================== PLAY MODE ====================
    
    private static string SetPlayMode(bool play)
    {
        if (play && !EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;
        else if (!play && EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        
        return J("success", true, "isPlaying", EditorApplication.isPlaying);
    }
    
    private static string TogglePause()
    {
        EditorApplication.isPaused = !EditorApplication.isPaused;
        return J("success", true, "isPaused", EditorApplication.isPaused);
    }
    
    // ==================== HELPERS ====================
    
    private static object SerializeGameObject(GameObject obj, bool shallow = false)
    {
        var result = new Dictionary<string, object>
        {
            ["name"] = obj.name,
            ["instanceID"] = obj.GetInstanceID(),
            ["active"] = obj.activeSelf,
            ["tag"] = obj.tag,
            ["layer"] = obj.layer,
            ["position"] = new Vector3Data { x = obj.transform.position.x, y = obj.transform.position.y, z = obj.transform.position.z },
            ["rotation"] = new Vector3Data { x = obj.transform.eulerAngles.x, y = obj.transform.eulerAngles.y, z = obj.transform.eulerAngles.z },
            ["scale"] = new Vector3Data { x = obj.transform.localScale.x, y = obj.transform.localScale.y, z = obj.transform.localScale.z }
        };
        
        if (!shallow)
        {
            var children = new List<object>();
            foreach (Transform child in obj.transform)
                children.Add(SerializeGameObject(child.gameObject, false));
            result["childCount"] = children.Count;
            result["children"] = children;
        }
        
        return result;
    }
    
    private static GameObject FindObject(string name, int? instanceID)
    {
        if (instanceID.HasValue)
            return (GameObject)EditorUtility.InstanceIDToObject(instanceID.Value);
        if (!string.IsNullOrEmpty(name))
            return GameObject.Find(name);
        return null;
    }
    
    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
    
    private static PrimitiveType GetPrimitiveType(string name)
    {
        switch (name.ToLower())
        {
            case "cube": return PrimitiveType.Cube;
            case "sphere": return PrimitiveType.Sphere;
            case "capsule": return PrimitiveType.Capsule;
            case "cylinder": return PrimitiveType.Cylinder;
            case "plane": return PrimitiveType.Plane;
            case "quad": return PrimitiveType.Quad;
            case "rope": return PrimitiveType.Capsule; // Fallback
            default: return PrimitiveType.Cube;
        }
    }
    
    private static System.Type GetTypeByName(string name)
    {
        string[] namespaces = { "", "UnityEngine.", "UnityEngine.UI.", "UnityEditor." };
        foreach (var ns in namespaces)
        {
            var type = System.Type.GetType(ns + name + ",UnityEngine");
            if (type != null) return type;
            type = System.Type.GetType(ns + name + ",UnityEditor");
            if (type != null) return type;
        }
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        return null;
    }
    
    // JSON helper - simple key-value builder
    private static string J(params object[] pairs)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        for (int i = 0; i < pairs.Length; i += 2)
        {
            if (i > 0) sb.Append(",");
            var key = pairs[i].ToString();
            var val = pairs[i + 1];
            sb.Append("\"").Append(key).Append("\":");
            sb.Append(ToJson(val));
        }
        sb.Append("}");
        return sb.ToString();
    }
    
    private static string ToJson(object obj)
    {
        if (obj == null) return "null";
        if (obj is string s) return "\"" + EscapeJson(s) + "\"";
        if (obj is bool b) return b ? "true" : "false";
        if (obj is int i) return i.ToString();
        if (obj is float f) return f.ToString("R");
        if (obj is double d) return d.ToString("R");
        
        if (obj is Dictionary<string, object> dict)
        {
            var pairs = new List<string>();
            foreach (var kv in dict) pairs.Add("\"" + kv.Key + "\":" + ToJson(kv.Value));
            return "{" + string.Join(",", pairs) + "}";
        }
        
        if (obj is List<object> list)
        {
            var items = new List<string>();
            foreach (var item in list) items.Add(ToJson(item));
            return "[" + string.Join(",", items) + "]";
        }
        
        if (obj is Vector3Data v)
            return "{\"x\":" + v.x.ToString("R") + ",\"y\":" + v.y.ToString("R") + ",\"z\":" + v.z.ToString("R") + "}";
        
        try { return JsonUtility.ToJson(obj); }
        catch { return "\"" + EscapeJson(obj.ToString()) + "\""; }
    }
    
    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
    
    // ==================== DATA CLASSES ====================
    
    [System.Serializable] private class Vector3Data { public float x, y, z; }
    
    [System.Serializable] private class SpawnData
    {
        public string name;
        public string primitive;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        public string parent;
    }
    
    [System.Serializable] private class ModifyData
    {
        public string name;
        public int? instanceID;
        public string newName;
        public bool? active;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
    }
    
    [System.Serializable] private class DeleteData
    {
        public string name;
        public int? instanceID;
    }
    
    [System.Serializable] private class ComponentData
    {
        public string objectName;
        public int? instanceID;
        public string componentType;
    }
    
    [System.Serializable] private class MaterialData
    {
        public string name;
        public string shader;
        public ColorData color;
    }
    
    [System.Serializable] private class SetMaterialData
    {
        public string objectName;
        public int? instanceID;
        public string name;
        public string shader;
        public ColorData color;
    }
    
    [System.Serializable] private class WaterData
    {
        public string name;
        public Vector3Data position;
        public Vector3Data scale;
    }
    
    [System.Serializable] private class ColorData
    {
        public float r, g, b, a = 1f;
    }
    
    [System.Serializable] private class CameraData
    {
        public Vector3Data position;
        public Vector3Data rotation;
        public float? fieldOfView;
        public bool? orthographic;
        public float? orthographicSize;
    }
    
    [System.Serializable] private class LookAtData
    {
        public string target;
        public int? targetInstanceID;
    }
    
    [System.Serializable] private class LightData
    {
        public string name;
        public string type;
        public Vector3Data position;
        public ColorData color;
        public float? intensity;
        public bool? shadows;
        public float? range;
        public float? spotAngle;
    }
}
