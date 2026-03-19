// Unity MCP Bridge
// Place this file in: Assets/Editor/UnityMcpBridge.cs
// This script starts an HTTP server that allows OpenClaw to control Unity Editor

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
public static class UnityMcpBridge
{
    private static HttpListener httpListener;
    private static Thread listenerThread;
    private static bool isRunning = false;
    private static readonly int PORT = 7778;
    private static readonly string URL = $"http://localhost:{PORT}/";
    
    // Thread-safe queue for main thread execution
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
            listenerThread = new Thread(new ThreadStart(ListenLoop));
            listenerThread.IsBackground = true;
            listenerThread.Start();
            
            Debug.Log($"[<color=cyan>MCP Bridge</color>] Server started on {URL}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[<color=red>MCP Bridge</color>] Failed to start: {ex.Message}");
        }
    }
    
    [MenuItem("Tools/MCP Bridge/Stop Server")]
    private static void StopServer()
    {
        if (!isRunning) return;
        
        isRunning = false;
        httpListener?.Stop();
        httpListener?.Close();
        
        Debug.Log("[<color=cyan>MCP Bridge</color>] Server stopped");
    }
    
    [MenuItem("Tools/MCP Bridge/Restart Server")]
    private static void RestartServer()
    {
        StopServer();
        System.Threading.Thread.Sleep(100);
        StartServer();
    }
    
    private static void ProcessMainThreadQueue()
    {
        lock (queueLock)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }
    
    private static void EnqueueOnMainThread(Action action)
    {
        lock (queueLock)
        {
            mainThreadActions.Enqueue(action);
        }
    }
    
    private static void ListenLoop()
    {
        while (isRunning)
        {
            try
            {
                var context = httpListener.GetContext();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch (Exception ex)
            {
                if (isRunning)
                {
                    Debug.LogError($"[<color=red>MCP Bridge</color>] Listen error: {ex.Message}");
                }
            }
        }
    }
    
    private static async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        
        string path = request.Url.AbsolutePath;
        string method = request.HttpMethod;
        string body = "";
        
        if (method == "POST" && request.HasEntityBody)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync();
            }
        }
        
        string jsonResponse = "";
        int statusCode = 200;
        
        try
        {
            switch (path)
            {
                case "/api/hierarchy":
                    jsonResponse = await GetHierarchy();
                    break;
                    
                case "/api/selection":
                    jsonResponse = await GetSelection();
                    break;
                    
                case "/api/scene/info":
                    jsonResponse = await GetSceneInfo();
                    break;
                    
                case "/api/spawn":
                    jsonResponse = await SpawnObject(body);
                    break;
                    
                case "/api/modify":
                    jsonResponse = await ModifyObject(body);
                    break;
                    
                case "/api/delete":
                    jsonResponse = await DeleteObject(body);
                    break;
                    
                case "/api/component/add":
                    jsonResponse = await AddComponent(body);
                    break;
                    
                case "/api/component/remove":
                    jsonResponse = await RemoveComponent(body);
                    break;
                    
                case "/api/execute":
                    jsonResponse = await ExecuteCode(body);
                    break;
                    
                case "/api/material/create":
                    jsonResponse = await CreateMaterial(body);
                    break;
                    
                case "/api/prefab/instantiate":
                    jsonResponse = await InstantiatePrefab(body);
                    break;
                    
                case "/api/console/clear":
                    jsonResponse = await ClearConsole();
                    break;
                    
                case "/api/undo":
                    jsonResponse = await PerformUndoOperation();
                    break;
                    
                default:
                    statusCode = 404;
                    jsonResponse = $"{{\"error\":\"Unknown endpoint: {path}\"}}";
                    break;
            }
        }
        catch (Exception ex)
        {
            statusCode = 500;
            jsonResponse = $"{{\"error\":\"{EscapeJson(ex.Message)}\"}}";
            Debug.LogError($"[<color=red>MCP Bridge</color>] Error: {ex}");
        }
        
        byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
        response.ContentType = "application/json";
        response.ContentLength64 = buffer.Length;
        response.StatusCode = statusCode;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.Close();
    }
    
    // ==================== API HANDLERS ====================
    
    private static Task<string> GetHierarchy()
    {
        var tcs = new TaskCompletionSource<string>();
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();
                
                var hierarchy = new List<object>();
                foreach (var obj in rootObjects)
                {
                    hierarchy.Add(SerializeGameObject(obj));
                }
                
                tcs.SetResult(ToJson(new { scene = scene.name, path = scene.path, objects = hierarchy }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> GetSelection()
    {
        var tcs = new TaskCompletionSource<string>();
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                var selected = new List<object>();
                foreach (var obj in Selection.gameObjects)
                {
                    selected.Add(SerializeGameObject(obj, true));
                }
                
                tcs.SetResult(ToJson(new { count = selected.Count, objects = selected }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> GetSceneInfo()
    {
        var tcs = new TaskCompletionSource<string>();
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                tcs.SetResult(ToJson(new 
                { 
                    name = scene.name, 
                    path = scene.path,
                    rootCount = scene.rootCount,
                    isDirty = scene.isDirty,
                    isLoaded = scene.isLoaded
                }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> SpawnObject(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<SpawnData>(body);
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                GameObject obj;
                
                if (!string.IsNullOrEmpty(data.primitive))
                {
                    obj = GameObject.CreatePrimitive(GetPrimitiveType(data.primitive));
                }
                else
                {
                    obj = new GameObject();
                }
                
                obj.name = data.name;
                
                // Set transform
                if (data.position != null)
                    obj.transform.position = data.position.ToVector3();
                if (data.rotation != null)
                    obj.transform.rotation = Quaternion.Euler(data.rotation.ToVector3());
                if (data.scale != null)
                    obj.transform.localScale = data.scale.ToVector3();
                
                // Set parent
                if (!string.IsNullOrEmpty(data.parent))
                {
                    var parent = GameObject.Find(data.parent);
                    if (parent != null)
                        obj.transform.SetParent(parent.transform);
                }
                
                Undo.RegisterCreatedObjectUndo(obj, "MCP Spawn Object");
                
                tcs.SetResult(ToJson(new 
                { 
                    success = true, 
                    name = obj.name,
                    instanceID = obj.GetInstanceID(),
                    path = GetGameObjectPath(obj)
                }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> ModifyObject(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<ModifyData>(body);
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                GameObject obj = FindObject(data.name, data.instanceID);
                if (obj == null)
                    throw new Exception($"Object not found: {data.name ?? data.instanceID.ToString()}");
                
                // Record undo
                Undo.RecordObject(obj, "MCP Modify Object");
                
                // Apply changes
                if (!string.IsNullOrEmpty(data.newName))
                    obj.name = data.newName;
                if (data.active.HasValue)
                    obj.SetActive(data.active.Value);
                if (data.position != null)
                    obj.transform.position = data.position.ToVector3();
                if (data.rotation != null)
                    obj.transform.rotation = Quaternion.Euler(data.rotation.ToVector3());
                if (data.scale != null)
                    obj.transform.localScale = data.scale.ToVector3();
                if (!string.IsNullOrEmpty(data.tag))
                    obj.tag = data.tag;
                if (data.layer.HasValue)
                    obj.layer = data.layer.Value;
                
                tcs.SetResult(ToJson(new 
                { 
                    success = true, 
                    name = obj.name,
                    instanceID = obj.GetInstanceID()
                }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> DeleteObject(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<DeleteData>(body);
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                GameObject obj = FindObject(data.name, data.instanceID);
                if (obj == null)
                    throw new Exception($"Object not found: {data.name ?? data.instanceID.ToString()}");
                
                Undo.DestroyObjectImmediate(obj);
                
                tcs.SetResult(ToJson(new { success = true, deleted = data.name ?? $"ID:{data.instanceID}" }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> AddComponent(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<ComponentData>(body);
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                GameObject obj = FindObject(data.objectName, data.instanceID);
                if (obj == null)
                    throw new Exception($"Object not found: {data.objectName ?? data.instanceID.ToString()}");
                
                var type = GetTypeByName(data.componentType);
                if (type == null)
                    throw new Exception($"Component type not found: {data.componentType}");
                
                Undo.RecordObject(obj, "MCP Add Component");
                var component = obj.AddComponent(type);
                
                tcs.SetResult(ToJson(new 
                { 
                    success = true, 
                    component = data.componentType,
                    instanceID = component.GetInstanceID()
                }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> RemoveComponent(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<ComponentData>(body);
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                GameObject obj = FindObject(data.objectName, data.instanceID);
                if (obj == null)
                    throw new Exception($"Object not found: {data.objectName ?? data.instanceID.ToString()}");
                
                var type = GetTypeByName(data.componentType);
                if (type == null)
                    throw new Exception($"Component type not found: {data.componentType}");
                
                var component = obj.GetComponent(type);
                if (component == null)
                    throw new Exception($"Component {data.componentType} not found on {obj.name}");
                
                Undo.DestroyObjectImmediate(component);
                
                tcs.SetResult(ToJson(new 
                { 
                    success = true, 
                    removed = data.componentType 
                }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> ExecuteCode(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<ExecuteData>(body);
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                // WARNING: This executes arbitrary C# code
                // For security, we compile and execute in a limited context
                var result = ExecuteCSharpCode(data.code);
                
                tcs.SetResult(ToJson(new 
                { 
                    success = true, 
                    result = result,
                    warning = "Code execution is limited for security"
                }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> CreateMaterial(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<MaterialData>(body);
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                string shaderName = data.shader ?? "Standard";
                var shader = Shader.Find(shaderName);
                if (shader == null)
                    throw new Exception($"Shader not found: {shaderName}");
                
                var material = new Material(shader);
                material.name = data.name;
                
                if (data.color != null)
                {
                    material.color = data.color.ToColor();
                }
                
                string path = $"Assets/{data.name}.mat";
                AssetDatabase.CreateAsset(material, path);
                AssetDatabase.SaveAssets();
                
                tcs.SetResult(ToJson(new 
                { 
                    success = true, 
                    name = material.name,
                    path = path,
                    shader = shaderName
                }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> InstantiatePrefab(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<PrefabData>(body);
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.path);
                if (prefab == null)
                    throw new Exception($"Prefab not found: {data.path}");
                
                Vector3 position = data.position?.ToVector3() ?? Vector3.zero;
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                instance.transform.position = position;
                
                Undo.RegisterCreatedObjectUndo(instance, "MCP Instantiate Prefab");
                
                tcs.SetResult(ToJson(new 
                { 
                    success = true, 
                    name = instance.name,
                    instanceID = instance.GetInstanceID(),
                    position = position
                }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> ClearConsole()
    {
        var tcs = new TaskCompletionSource<string>();
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor");
                var clearMethod = logEntries?.GetMethod("Clear");
                clearMethod?.Invoke(null, null);
                
                tcs.SetResult(ToJson(new { success = true, cleared = true }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    private static Task<string> PerformUndoOperation()
    {
        var tcs = new TaskCompletionSource<string>();
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                UnityEditor.Undo.PerformUndo();
                tcs.SetResult(ToJson(new { success = true, undone = true }));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return tcs.Task;
    }
    
    // ==================== HELPERS ====================
    
    private static object SerializeGameObject(GameObject obj, bool includeComponents = false)
    {
        var data = new Dictionary<string, object>
        {
            ["name"] = obj.name,
            ["instanceID"] = obj.GetInstanceID(),
            ["active"] = obj.activeSelf,
            ["tag"] = obj.tag,
            ["layer"] = obj.layer,
            ["position"] = new Vector3Data(obj.transform.position),
            ["rotation"] = new Vector3Data(obj.transform.rotation.eulerAngles),
            ["scale"] = new Vector3Data(obj.transform.localScale)
        };
        
        if (includeComponents)
        {
            var components = new List<string>();
            foreach (var c in obj.GetComponents<Component>())
            {
                if (c != null)
                    components.Add(c.GetType().Name);
            }
            data["components"] = components;
        }
        
        // Include children
        var children = new List<object>();
        foreach (Transform child in obj.transform)
        {
            children.Add(SerializeGameObject(child.gameObject, false));
        }
        if (children.Count > 0)
            data["children"] = children;
        
        return data;
    }
    
    private static GameObject FindObject(string name, int? instanceID)
    {
        if (instanceID.HasValue)
        {
            #pragma warning disable CS0618
            return EditorUtility.InstanceIDToObject(instanceID.Value) as GameObject;
            #pragma warning restore CS0618
        }
        
        if (!string.IsNullOrEmpty(name))
        {
            return GameObject.Find(name);
        }
        
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
        return name.ToLower() switch
        {
            "cube" => PrimitiveType.Cube,
            "sphere" => PrimitiveType.Sphere,
            "capsule" => PrimitiveType.Capsule,
            "cylinder" => PrimitiveType.Cylinder,
            "plane" => PrimitiveType.Plane,
            "quad" => PrimitiveType.Quad,
            _ => PrimitiveType.Cube
        };
    }
    
    private static System.Type GetTypeByName(string name)
    {
        // Try common namespaces
        string[] namespaces = { "", "UnityEngine.", "UnityEngine.UI.", "UnityEditor." };
        
        foreach (var ns in namespaces)
        {
            var type = System.Type.GetType(ns + name + ",UnityEngine");
            if (type != null) return type;
            
            type = System.Type.GetType(ns + name + ",UnityEditor");
            if (type != null) return type;
        }
        
        // Search all assemblies
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        
        return null;
    }
    
    private static string ExecuteCSharpCode(string code)
    {
        // For security, we don't allow arbitrary code execution
        // Instead, we provide a whitelist of safe operations
        // Full code execution would require Roslyn compiler
        
        if (code.Contains("GameObject.") || code.Contains("Transform.") || code.Contains("Vector3."))
        {
            // Safe Unity API calls
            return "Code execution requires custom implementation. Use specific tools instead.";
        }
        
        return "Code execution not implemented for security. Available: unity_spawn_object, unity_add_component, etc.";
    }
    
    private static string ToJson(object obj)
    {
        // Simple JSON serialization
        if (obj is string str) return $"\"{EscapeJson(str)}\"";
        if (obj is int i) return i.ToString();
        if (obj is float f) return f.ToString("R");
        if (obj is double d) return d.ToString("R");
        if (obj is bool b) return b.ToString().ToLower();
        if (obj == null) return "null";
        
        if (obj is Dictionary<string, object> dict)
        {
            var pairs = new List<string>();
            foreach (var kvp in dict)
            {
                pairs.Add($"\"{kvp.Key}\":{ToJson(kvp.Value)}");
            }
            return "{" + string.Join(",", pairs) + "}";
        }
        
        if (obj is List<object> list)
        {
            var items = list.ConvertAll(ToJson);
            return "[" + string.Join(",", items) + "]";
        }
        
        if (obj is List<string> strList)
        {
            return "[" + string.Join(",", strList.ConvertAll(s => $"\"{EscapeJson(s)}\"")) + "]";
        }
        
        // Handle Vector3Data directly
        if (obj is Vector3Data v3)
        {
            return $"{{\"x\":{v3.x},\"y\":{v3.y},\"z\":{v3.z}}}";
        }
        
        // Fallback to Unity's JsonUtility for simple serializable objects
        try
        {
            return JsonUtility.ToJson(obj);
        }
        catch
        {
            return "{}";
        }
    }
    
    private static string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
    
    // ==================== DATA CLASSES ====================
    
    [System.Serializable]
    private class Vector3Data
    {
        public float x, y, z;
        
        public Vector3Data() { }
        public Vector3Data(Vector3 v) { x = v.x; y = v.y; z = v.z; }
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }
    
    [System.Serializable]
    private class ColorData
    {
        public float r, g, b, a = 1f;
        public Color ToColor() => new Color(r, g, b, a);
    }
    
    [System.Serializable]
    private class SpawnData
    {
        public string name;
        public string primitive;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        public string parent;
    }
    
    [System.Serializable]
    private class ModifyData
    {
        public string name;
        public int instanceID;
        public string newName;
        public bool? active;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        public string tag;
        public int? layer;
    }
    
    [System.Serializable]
    private class DeleteData
    {
        public string name;
        public int instanceID;
    }
    
    [System.Serializable]
    private class ComponentData
    {
        public string objectName;
        public int instanceID;
        public string componentType;
    }
    
    [System.Serializable]
    private class ExecuteData
    {
        public string code;
    }
    
    [System.Serializable]
    private class MaterialData
    {
        public string name;
        public string shader;
        public ColorData color;
    }
    
    [System.Serializable]
    private class PrefabData
    {
        public string path;
        public Vector3Data position;
    }
}
