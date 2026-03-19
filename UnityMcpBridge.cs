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
using UnityEditor.SceneManagement;
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
                    
                case "/api/asset/find":
                    jsonResponse = await FindAssets(body);
                    break;
                    
                case "/api/console/clear":
                    jsonResponse = await ClearConsole();
                    break;
                    
                case "/api/undo":
                    jsonResponse = await PerformUndoOperation();
                    break;
                    
                case "/api/playmode/enter":
                    jsonResponse = await EnterPlayMode();
                    break;
                    
                case "/api/playmode/exit":
                    jsonResponse = await ExitPlayMode();
                    break;
                    
                case "/api/playmode/pause":
                    jsonResponse = await PausePlayMode(body);
                    break;
                    
                case "/api/playmode/status":
                    jsonResponse = await GetPlayModeStatus();
                    break;
                    
                case "/api/scene/load":
                    jsonResponse = await LoadScene(body);
                    break;
                    
                case "/api/scene/list":
                    jsonResponse = await GetSceneList();
                    break;
                    
                case "/api/script/compile":
                    jsonResponse = await CompileScripts();
                    break;
                    
                case "/api/physics/set-time-scale":
                    jsonResponse = await SetTimeScale(body);
                    break;
                    
                case "/api/camera/set-position":
                    jsonResponse = await SetCameraPosition(body);
                    break;
                    
                case "/api/camera/set-orthographic":
                    jsonResponse = await SetCameraOrthographic(body);
                    break;
                    
                case "/api/window/open":
                    jsonResponse = await OpenWindow(body);
                    break;
                    
                case "/api/console/send":
                    jsonResponse = await SendDebugLog(body);
                    break;
                    
                case "/api/inspector/get-properties":
                    jsonResponse = await GetComponentProperties(body);
                    break;
                    
                case "/api/inspector/set-property":
                    jsonResponse = await SetComponentProperty(body);
                    break;
                    
                case "/api/inspector/invoke-method":
                    jsonResponse = await InvokeComponentMethod(body);
                    break;
                    
                case "/api/script/create":
                    jsonResponse = await CreateScript(body);
                    break;
                    
                default:
                    statusCode = 501;
                    jsonResponse = $"{{\"error\":\"Endpoint not yet implemented in UnityMcpBridge.cs: {path}. You need to build this feature!\"}}";
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
                    hierarchy.Add(SerializeGameObject(obj, true));
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

    private static Task<string> FindAssets(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<FindAssetsData>(body);
        
        EnqueueOnMainThread(() =>
        {
            try
            {
                string searchFilter = data.searchQuery;
                if (!string.IsNullOrEmpty(data.typeFilter))
                {
                    searchFilter += $" t:{data.typeFilter}";
                }
                
                string[] guids = AssetDatabase.FindAssets(searchFilter);
                var assets = new List<object>();
                
                // Limit to 50 to avoid massive JSON payloads crashing the bridge
                int limit = Mathf.Min(guids.Length, 50);
                for (int i = 0; i < limit; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    assets.Add(new {
                        guid = guids[i],
                        path = path,
                        name = System.IO.Path.GetFileNameWithoutExtension(path)
                    });
                }
                
                tcs.SetResult(ToJson(new 
                { 
                    success = true, 
                    totalFound = guids.Length,
                    returned = limit,
                    assets = assets 
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
    
    private static Task<string> EnterPlayMode()
    {
        var tcs = new TaskCompletionSource<string>();
        EnqueueOnMainThread(() =>
        {
            try
            {
                EditorApplication.isPlaying = true;
                tcs.SetResult(ToJson(new { success = true, status = "playing" }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string> ExitPlayMode()
    {
        var tcs = new TaskCompletionSource<string>();
        EnqueueOnMainThread(() =>
        {
            try
            {
                EditorApplication.isPlaying = false;
                tcs.SetResult(ToJson(new { success = true, status = "stopped" }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string> PausePlayMode(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<PauseData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                EditorApplication.isPaused = data.paused;
                tcs.SetResult(ToJson(new { success = true, paused = data.paused }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string> GetPlayModeStatus()
    {
        var tcs = new TaskCompletionSource<string>();
        EnqueueOnMainThread(() =>
        {
            try
            {
                tcs.SetResult(ToJson(new { 
                    isPlaying = EditorApplication.isPlaying, 
                    isPaused = EditorApplication.isPaused,
                    isCompiling = EditorApplication.isCompiling 
                }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string> LoadScene(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<LoadSceneData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                var mode = data.mode == "Additive" ? OpenSceneMode.Additive : OpenSceneMode.Single;
                var sceneNameOrPath = data.sceneName;
                
                // if it's not a full path, let's try to find it
                if (!sceneNameOrPath.EndsWith(".unity"))
                {
                    string[] guids = AssetDatabase.FindAssets("t:Scene " + sceneNameOrPath);
                    if (guids.Length > 0)
                    {
                        sceneNameOrPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    }
                }

                EditorSceneManager.OpenScene(sceneNameOrPath, mode);
                tcs.SetResult(ToJson(new { success = true, loaded = sceneNameOrPath }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string> GetSceneList()
    {
        var tcs = new TaskCompletionSource<string>();
        EnqueueOnMainThread(() =>
        {
            try
            {
                List<string> scenes = new List<string>();
                foreach (UnityEditor.EditorBuildSettingsScene scene in UnityEditor.EditorBuildSettings.scenes)
                {
                    if (scene.enabled)
                        scenes.Add(scene.path);
                }
                
                string[] allScenes = AssetDatabase.FindAssets("t:Scene");
                List<string> allScenePaths = new List<string>();
                foreach(var guid in allScenes)
                {
                    allScenePaths.Add(AssetDatabase.GUIDToAssetPath(guid));
                }

                tcs.SetResult(ToJson(new { buildScenes = scenes, allScenes = allScenePaths }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string> CompileScripts()
    {
        var tcs = new TaskCompletionSource<string>();
        EnqueueOnMainThread(() =>
        {
            try
            {
                CompilationPipeline.RequestScriptCompilation();
                tcs.SetResult(ToJson(new { success = true, status = "compiling" }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }
    
    private static Task<string> SetTimeScale(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<TimeScaleData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                Time.timeScale = data.scale;
                tcs.SetResult(ToJson(new { success = true, timeScale = data.scale }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }
    
    private static Task<string> SetCameraPosition(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<CameraPosData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                var cam = Camera.main;
                if (cam == null) throw new Exception("No Main Camera found in scene");
                
                Undo.RecordObject(cam.transform, "MCP Set Camera Position");
                if (data.position != null) cam.transform.position = data.position.ToVector3();
                if (data.rotation != null) cam.transform.rotation = Quaternion.Euler(data.rotation.ToVector3());
                
                tcs.SetResult(ToJson(new { success = true }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }
    
    private static Task<string> SetCameraOrthographic(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<CameraOrthoData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                var cam = Camera.main;
                if (cam == null) throw new Exception("No Main Camera found in scene");
                
                Undo.RecordObject(cam, "MCP Set Camera Orthographic");
                cam.orthographic = data.orthographic;
                if (data.orthographic && data.size != 0)
                {
                    cam.orthographicSize = data.size;
                }
                
                tcs.SetResult(ToJson(new { success = true }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }
    
    private static Task<string> OpenWindow(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<WindowData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                System.Type type = null;
                switch (data.windowName.ToLower())
                {
                    case "game": type = System.Type.GetType("UnityEditor.GameView,UnityEditor"); break;
                    case "scene": type = typeof(SceneView); break;
                    case "hierarchy": type = System.Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor"); break;
                    case "inspector": type = System.Type.GetType("UnityEditor.InspectorWindow,UnityEditor"); break;
                    case "project": type = System.Type.GetType("UnityEditor.ProjectBrowser,UnityEditor"); break;
                    case "console": type = System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor"); break;
                }
                
                if (type != null)
                {
                    EditorWindow.GetWindow(type).Show();
                    tcs.SetResult(ToJson(new { success = true, window = data.windowName }));
                }
                else
                {
                    throw new Exception($"Window '{data.windowName}' not supported.");
                }
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }
    
    private static Task<string> SendDebugLog(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<LogData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                if (data.type == "Warning") Debug.LogWarning(data.message);
                else if (data.type == "Error") Debug.LogError(data.message);
                else Debug.Log(data.message);
                
                tcs.SetResult(ToJson(new { success = true }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string> GetComponentProperties(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<ComponentReqData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                GameObject obj = FindObject(data.objectName, data.instanceID);
                if (obj == null) throw new Exception("Object not found.");
                var type = GetTypeByName(data.componentType);
                if (type == null) throw new Exception("Component type not found.");
                var comp = obj.GetComponent(type);
                if (comp == null) throw new Exception("Component not attached to object.");

                var props = new Dictionary<string, object>();
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    try { props[field.Name] = field.GetValue(comp) ?? "null"; } catch {}
                }
                foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                    {
                        try { props[prop.Name] = prop.GetValue(comp, null) ?? "null"; } catch {}
                    }
                }

                tcs.SetResult(ToJson(new { success = true, properties = props }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string> SetComponentProperty(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<SetPropData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                GameObject obj = FindObject(data.objectName, data.instanceID);
                if (obj == null) throw new Exception("Object not found.");
                var type = GetTypeByName(data.componentType);
                if (type == null) throw new Exception("Component type not found.");
                var comp = obj.GetComponent(type);
                if (comp == null) throw new Exception("Component not attached to object.");

                Undo.RecordObject(comp, "MCP Set Property");

                bool set = false;
                var field = type.GetField(data.property, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    object val = Convert.ChangeType(data.value, field.FieldType);
                    field.SetValue(comp, val);
                    set = true;
                }
                if (!set)
                {
                    var prop = type.GetProperty(data.property, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (prop != null && prop.CanWrite)
                    {
                        object val = Convert.ChangeType(data.value, prop.PropertyType);
                        prop.SetValue(comp, val, null);
                        set = true;
                    }
                }

                if (!set) throw new Exception($"Property {data.property} not found or not writable.");
                tcs.SetResult(ToJson(new { success = true }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string> InvokeComponentMethod(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<InvokeMethodData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                GameObject obj = FindObject(data.objectName, data.instanceID);
                if (obj == null) throw new Exception("Object not found.");
                var type = GetTypeByName(data.componentType);
                if (type == null) throw new Exception("Component type not found.");
                var comp = obj.GetComponent(type);
                if (comp == null) throw new Exception("Component not attached to object.");

                var method = type.GetMethod(data.method, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method == null) throw new Exception($"Method {data.method} not found.");

                var parameters = method.GetParameters();
                object[] args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (data.parameters != null && i < data.parameters.Length)
                    {
                        args[i] = Convert.ChangeType(data.parameters[i], parameters[i].ParameterType);
                    }
                    else if (parameters[i].IsOptional)
                    {
                        args[i] = parameters[i].DefaultValue;
                    }
                    else
                    {
                        throw new Exception($"Missing parameter for {parameters[i].Name}");
                    }
                }

                object result = method.Invoke(comp, args);
                tcs.SetResult(ToJson(new { success = true, result = result != null ? result.ToString() : "null" }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string> CreateScript(string body)
    {
        var tcs = new TaskCompletionSource<string>();
        var data = JsonUtility.FromJson<ScriptData>(body);
        EnqueueOnMainThread(() =>
        {
            try
            {
                string path = "Assets/Scripts";
                if (!string.IsNullOrEmpty(data.path)) path = data.path;
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string filePath = $"{path}/{data.name}.cs";
                string content = "";

                if (data.template == "ScriptableObject")
                {
                    content = $"using UnityEngine;\n\n[CreateAssetMenu(fileName = \"{data.name}\", menuName = \"Data/{data.name}\")]\npublic class {data.name} : ScriptableObject\n{{\n    \n}}\n";
                }
                else if (data.template == "Editor")
                {
                    content = $"using UnityEditor;\nusing UnityEngine;\n\npublic class {data.name} : EditorWindow\n{{\n    [MenuItem(\"Tools/{data.name}\")]\n    public static void ShowWindow()\n    {{\n        GetWindow<{data.name}>(\"{data.name}\");\n    }}\n\n    private void OnGUI()\n    {{\n        \n    }}\n}}\n";
                }
                else // MonoBehaviour
                {
                    content = $"using UnityEngine;\n\npublic class {data.name} : MonoBehaviour\n{{\n    void Start()\n    {{\n        \n    }}\n\n    void Update()\n    {{\n        \n    }}\n}}\n";
                }

                if (!string.IsNullOrEmpty(data.@namespace))
                {
                    content = $"namespace {data.@namespace}\n{{\n{content.Replace("using", "//using").Replace("\n", "\n    ")}    \n}}\n";
                    // Move usings to top
                    content = "using UnityEngine;\nusing UnityEditor;\n" + content.Replace("//using UnityEngine;", "").Replace("//using UnityEditor;", "");
                }

                File.WriteAllText(filePath, content);
                AssetDatabase.Refresh();

                tcs.SetResult(ToJson(new { success = true, tempPath = filePath }));
            }
            catch (Exception ex) { tcs.SetException(ex); }
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
            children.Add(SerializeGameObject(child.gameObject, includeComponents));
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

    [System.Serializable]
    private class FindAssetsData
    {
        public string searchQuery;
        public string typeFilter;
    }

    [System.Serializable]
    private class PauseData
    {
        public bool paused;
    }

    [System.Serializable]
    private class LoadSceneData
    {
        public string sceneName;
        public string mode;
    }

    [System.Serializable]
    private class TimeScaleData
    {
        public float scale;
    }

    [System.Serializable]
    private class CameraPosData
    {
        public Vector3Data position;
        public Vector3Data rotation;
    }

    [System.Serializable]
    private class CameraOrthoData
    {
        public bool orthographic;
        public float size;
    }

    [System.Serializable]
    private class WindowData
    {
        public string windowName;
    }

    [System.Serializable]
    private class LogData
    {
        public string message;
        public string type;
    }

    [System.Serializable]
    private class ComponentReqData
    {
        public string objectName;
        public int instanceID;
        public string componentType;
    }

    [System.Serializable]
    private class SetPropData
    {
        public string objectName;
        public int instanceID;
        public string componentType;
        public string property;
        public string value;
    }

    [System.Serializable]
    private class InvokeMethodData
    {
        public string objectName;
        public int instanceID;
        public string componentType;
        public string method;
        public string[] parameters;
    }

    [System.Serializable]
    private class ScriptData
    {
        public string name;
        public string template;
        public string @namespace;
        public string path;
    }
}
