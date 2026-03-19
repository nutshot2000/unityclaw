#!/usr/bin/env node

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ListPromptsRequestSchema,
  GetPromptRequestSchema,
  Tool,
} from "@modelcontextprotocol/sdk/types.js";
import { UnityDocRAG } from "./rag.js";

import * as path from "path";
import * as fs from "fs";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Unity Bridge configuration
const UNITY_BRIDGE_URL = process.env.UNITY_BRIDGE_URL || "http://localhost:7778";

// RAG instance (lazy loaded)
let ragInstance: UnityDocRAG | null = null;

async function getRAG(): Promise<UnityDocRAG> {
  if (!ragInstance) {
    const dbPath = path.join(__dirname, "..", "unity-docs-db");
    ragInstance = new UnityDocRAG(dbPath);
    await ragInstance.initialize();
  }
  return ragInstance;
}

// ============================================
// ALL TOOL DEFINITIONS (50+ tools)
// ============================================

const TOOLS: Tool[] = [
  // === SCENE MANAGEMENT ===
  {
    name: "unity_get_hierarchy",
    description: "Get the complete hierarchy of GameObjects in the active Unity scene",
    inputSchema: { type: "object", properties: {}, required: [] }
  },
  {
    name: "unity_get_scene_info",
    description: "Get information about the active Unity scene (name, path, root count, dirty state)",
    inputSchema: { type: "object", properties: {}, required: [] }
  },
  {
    name: "unity_load_scene",
    description: "Load a Unity scene by name or path",
    inputSchema: {
      type: "object",
      properties: {
        sceneName: { type: "string", description: "Name or path of scene to load" },
        mode: { type: "string", enum: ["Single", "Additive"], description: "Load mode (default: Single)" }
      },
      required: ["sceneName"]
    }
  },
  {
    name: "unity_create_scene",
    description: "Create a new empty Unity scene",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Optional name for the new scene" }
      },
      required: []
    }
  },
  {
    name: "unity_save_scene",
    description: "Save the current Unity scene",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "Optional path to save to (default: current path)" }
      },
      required: []
    }
  },
  {
    name: "unity_get_scene_list",
    description: "Get list of all loaded scenes",
    inputSchema: { type: "object", properties: {}, required: [] }
  },

  // === PLAY MODE ===
  {
    name: "unity_enter_play_mode",
    description: "Enter Unity Play Mode to test the game",
    inputSchema: { type: "object", properties: {}, required: [] }
  },
  {
    name: "unity_exit_play_mode",
    description: "Exit Unity Play Mode",
    inputSchema: { type: "object", properties: {}, required: [] }
  },
  {
    name: "unity_pause_play_mode",
    description: "Pause or unpause Unity Play Mode",
    inputSchema: {
      type: "object",
      properties: {
        paused: { type: "boolean", description: "True to pause, false to unpause" }
      },
      required: ["paused"]
    }
  },
  {
    name: "unity_get_play_mode_status",
    description: "Get current play mode status (playing, paused, compiling)",
    inputSchema: { type: "object", properties: {}, required: [] }
  },

  // === OBJECT MANAGEMENT ===
  {
    name: "unity_spawn_object",
    description: "Create a new GameObject in Unity. Can create primitives (Cube, Sphere, etc.) or empty GameObjects.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Name of the GameObject" },
        primitive: { type: "string", enum: ["Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad"], description: "Primitive type (optional)" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        rotation: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        scale: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        parent: { type: "string", description: "Name of parent GameObject (optional)" }
      },
      required: ["name"]
    }
  },
  {
    name: "unity_modify_object",
    description: "Modify an existing GameObject's properties (position, rotation, scale, name, active state, tag, layer)",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Name of GameObject to modify" },
        instanceID: { type: "number", description: "Instance ID (alternative to name)" },
        newName: { type: "string", description: "New name for the object" },
        active: { type: "boolean", description: "Set active/inactive" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        rotation: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        scale: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        tag: { type: "string", description: "New tag" },
        layer: { type: "number", description: "Layer index (0-31)" }
      },
      required: []
    }
  },
  {
    name: "unity_delete_object",
    description: "Delete a GameObject from the scene",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Name of GameObject to delete" },
        instanceID: { type: "number", description: "Instance ID (alternative to name)" }
      },
      required: []
    }
  },
  {
    name: "unity_get_selection",
    description: "Get the currently selected GameObjects in Unity Editor",
    inputSchema: { type: "object", properties: {}, required: [] }
  },
  {
    name: "unity_set_selection",
    description: "Set the selection in Unity Editor",
    inputSchema: {
      type: "object",
      properties: {
        objectNames: { type: "array", items: { type: "string" }, description: "Names of objects to select" }
      },
      required: ["objectNames"]
    }
  },
  {
    name: "unity_duplicate_selection",
    description: "Duplicate the currently selected objects",
    inputSchema: { type: "object", properties: {}, required: [] }
  },

  // === COMPONENTS ===
  {
    name: "unity_add_component",
    description: "Add a component to a GameObject (e.g., Rigidbody, BoxCollider, Animator)",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { type: "string", description: "Name of GameObject" },
        instanceID: { type: "number", description: "Instance ID (alternative to name)" },
        componentType: { type: "string", description: "Component type name (e.g., 'Rigidbody', 'BoxCollider')" }
      },
      required: ["componentType"]
    }
  },
  {
    name: "unity_remove_component",
    description: "Remove a component from a GameObject",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { type: "string", description: "Name of GameObject" },
        instanceID: { type: "number", description: "Instance ID (alternative to name)" },
        componentType: { type: "string", description: "Component type name to remove" }
      },
      required: ["componentType"]
    }
  },
  {
    name: "unity_get_component_properties",
    description: "Get all properties and fields of a component on a GameObject",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { type: "string", description: "Name of GameObject" },
        instanceID: { type: "number", description: "Instance ID (alternative to name)" },
        componentType: { type: "string", description: "Component type name" }
      },
      required: ["componentType"]
    }
  },
  {
    name: "unity_set_property",
    description: "Set a property or field value on a component",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { type: "string", description: "Name of GameObject" },
        instanceID: { type: "number", description: "Instance ID (alternative to name)" },
        componentType: { type: "string", description: "Component type name" },
        property: { type: "string", description: "Property or field name" },
        value: { type: "string", description: "Value to set (will be converted to appropriate type)" }
      },
      required: ["componentType", "property", "value"]
    }
  },
  {
    name: "unity_invoke_method",
    description: "Invoke a method on a component",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { type: "string", description: "Name of GameObject" },
        instanceID: { type: "number", description: "Instance ID (alternative to name)" },
        componentType: { type: "string", description: "Component type name" },
        method: { type: "string", description: "Method name to invoke" },
        parameters: { type: "array", items: { type: "string" }, description: "Method parameters as strings" }
      },
      required: ["componentType", "method"]
    }
  },

  // === ASSETS ===
  {
    name: "unity_create_material",
    description: "Create a new material asset",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Material name" },
        shader: { type: "string", description: "Shader name (default: Standard)" },
        color: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } }, description: "RGBA color (0-1)" }
      },
      required: ["name"]
    }
  },
  {
    name: "unity_create_prefab",
    description: "Create a prefab from an existing GameObject",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { type: "string", description: "Name of GameObject to convert to prefab" },
        path: { type: "string", description: "Path to save prefab (e.g., 'Assets/Prefabs/MyPrefab.prefab')" }
      },
      required: ["objectName", "path"]
    }
  },
  {
    name: "unity_instantiate_prefab",
    description: "Instantiate a prefab into the scene",
    inputSchema: {
      type: "object",
      properties: {
        prefabPath: { type: "string", description: "Path to prefab asset" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        rotation: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } }
      },
      required: ["prefabPath"]
    }
  },
  {
    name: "unity_find_assets",
    description: "Search for assets in the Unity project (like the Project window search bar)",
    inputSchema: {
      type: "object",
      properties: {
        searchQuery: { type: "string", description: "Search query (e.g. 'Player', 't:Prefab', 't:Material')" },
        typeFilter: { type: "string", description: "Optional asset type (e.g. 'Prefab', 'Material', 'Texture2D', 'AudioClip', 'Script')" }
      },
      required: ["searchQuery"]
    }
  },

  // === SCRIPTS ===
  {
    name: "unity_create_script",
    description: "Create a new C# script from template",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Script class name" },
        template: { type: "string", enum: ["MonoBehaviour", "ScriptableObject", "Editor"], description: "Template type" },
        namespace: { type: "string", description: "Optional namespace" },
        path: { type: "string", description: "Folder path (default: Assets/Scripts)" }
      },
      required: ["name"]
    }
  },
  {
    name: "unity_compile_scripts",
    description: "Force recompile all scripts",
    inputSchema: { type: "object", properties: {}, required: [] }
  },

  // === ANIMATION ===
  {
    name: "unity_set_animation_trigger",
    description: "Set an animation trigger parameter",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { type: "string", description: "Name of GameObject with Animator" },
        trigger: { type: "string", description: "Trigger parameter name" }
      },
      required: ["objectName", "trigger"]
    }
  },
  {
    name: "unity_set_animation_bool",
    description: "Set an animation bool parameter",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { type: "string", description: "Name of GameObject with Animator" },
        parameter: { type: "string", description: "Bool parameter name" },
        value: { type: "boolean", description: "Bool value" }
      },
      required: ["objectName", "parameter", "value"]
    }
  },
  {
    name: "unity_set_animation_float",
    description: "Set an animation float parameter",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { type: "string", description: "Name of GameObject with Animator" },
        parameter: { type: "string", description: "Float parameter name" },
        value: { type: "number", description: "Float value" }
      },
      required: ["objectName", "parameter", "value"]
    }
  },
  {
    name: "unity_get_animation_state",
    description: "Get current animation state info",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { type: "string", description: "Name of GameObject with Animator" }
      },
      required: ["objectName"]
    }
  },

  // === CAMERA ===
  {
    name: "unity_set_camera_position",
    description: "Set the main camera position and rotation",
    inputSchema: {
      type: "object",
      properties: {
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        rotation: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } }
      },
      required: []
    }
  },
  {
    name: "unity_set_camera_orthographic",
    description: "Set camera to orthographic or perspective",
    inputSchema: {
      type: "object",
      properties: {
        orthographic: { type: "boolean", description: "True for orthographic, false for perspective" },
        size: { type: "number", description: "Orthographic size (if orthographic)" }
      },
      required: ["orthographic"]
    }
  },
  {
    name: "unity_camera_follow_target",
    description: "Make camera follow a target GameObject",
    inputSchema: {
      type: "object",
      properties: {
        targetName: { type: "string", description: "Name of target GameObject" },
        offset: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } }, description: "Camera offset from target" },
        smooth: { type: "number", description: "Smoothing factor (0-1, default: 0.125)" }
      },
      required: ["targetName"]
    }
  },
  {
    name: "unity_take_screenshot",
    description: "Capture a screenshot",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "Path to save screenshot (optional)" }
      },
      required: []
    }
  },

  // === PHYSICS ===
  {
    name: "unity_set_time_scale",
    description: "Set Unity time scale (for slow motion effects)",
    inputSchema: {
      type: "object",
      properties: {
        scale: { type: "number", description: "Time scale (1 = normal, 0.5 = half speed, 0 = paused)" }
      },
      required: ["scale"]
    }
  },
  {
    name: "unity_physics_raycast",
    description: "Perform a physics raycast",
    inputSchema: {
      type: "object",
      properties: {
        origin: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } }, description: "Ray origin" },
        direction: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } }, description: "Ray direction" },
        maxDistance: { type: "number", description: "Maximum distance (default: infinity)" }
      },
      required: ["origin", "direction"]
    }
  },
  {
    name: "unity_physics_overlap_sphere",
    description: "Find colliders within a spherical area",
    inputSchema: {
      type: "object",
      properties: {
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } }, description: "Sphere center" },
        radius: { type: "number", description: "Sphere radius" }
      },
      required: ["position", "radius"]
    }
  },

  // === BUILD ===
  {
    name: "unity_build_player",
    description: "Build the Unity player",
    inputSchema: {
      type: "object",
      properties: {
        target: { type: "string", enum: ["StandaloneWindows", "StandaloneWindows64", "StandaloneLinux64", "StandaloneOSX", "Android", "iOS", "WebGL"], description: "Build target platform" },
        outputPath: { type: "string", description: "Output path for build" },
        scenes: { type: "array", items: { type: "string" }, description: "Scene paths to include (optional, uses build settings if omitted)" }
      },
      required: ["target", "outputPath"]
    }
  },
  {
    name: "unity_get_build_settings",
    description: "Get current build settings",
    inputSchema: { type: "object", properties: {}, required: [] }
  },

  // === UNDO/REDO ===
  {
    name: "unity_undo",
    description: "Undo the last action",
    inputSchema: {
      type: "object",
      properties: {
        actionName: { type: "string", description: "Optional action name to undo" }
      },
      required: []
    }
  },
  {
    name: "unity_redo",
    description: "Redo the last undone action",
    inputSchema: { type: "object", properties: {}, required: [] }
  },

  // === EDITOR WINDOWS ===
  {
    name: "unity_open_window",
    description: "Open an Editor window",
    inputSchema: {
      type: "object",
      properties: {
        windowName: { type: "string", enum: ["Game", "Scene", "Hierarchy", "Inspector", "Project", "Console", "Animator", "Animation"], description: "Window to open" }
      },
      required: ["windowName"]
    }
  },

  // === CONSOLE ===
  {
    name: "unity_get_console_logs",
    description: "Get recent console logs",
    inputSchema: { type: "object", properties: {}, required: [] }
  },
  {
    name: "unity_clear_console",
    description: "Clear the Unity console",
    inputSchema: { type: "object", properties: {}, required: [] }
  },
  {
    name: "unity_send_debug_log",
    description: "Send a debug log message to Unity console",
    inputSchema: {
      type: "object",
      properties: {
        message: { type: "string", description: "Log message" },
        type: { type: "string", enum: ["Log", "Warning", "Error"], description: "Log type" }
      },
      required: ["message"]
    }
  },

  // === PROJECT SETTINGS ===
  {
    name: "unity_get_project_settings",
    description: "Get project settings info",
    inputSchema: { type: "object", properties: {}, required: [] }
  },

  // === DOCUMENTATION RAG ===
  {
    name: "unity_search_docs",
    description: "Search Unity documentation using RAG (Retrieval-Augmented Generation). Use this BEFORE writing Unity code to get accurate API information.",
    inputSchema: {
      type: "object",
      properties: {
        query: { type: "string", description: "Search query (e.g., 'Rigidbody.AddForce', 'NavMeshAgent pathfinding')" },
        topK: { type: "number", description: "Number of results to return (default: 3)" }
      },
      required: ["query"]
    }
  }
];

// ============================================
// TOOL HANDLERS
// ============================================

async function callUnityBridge(endpoint: string, method: string = "GET", body?: object): Promise<any> {
  const url = `${UNITY_BRIDGE_URL}${endpoint}`;
  
  const options: RequestInit = {
    method,
    headers: {
      "Content-Type": "application/json",
    },
  };
  
  if (body && method === "POST") {
    options.body = JSON.stringify(body);
  }
  
  try {
    const response = await fetch(url, options);
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Unity Bridge error: ${response.status} - ${errorText}`);
    }
    
    return await response.json();
  } catch (error) {
    console.error(`Error calling Unity Bridge at ${url}:`, error);
    throw error;
  }
}

// Tool handler mapping
const TOOL_HANDLERS: Record<string, (args: any) => Promise<any>> = {
  // Scene Management
  "unity_get_hierarchy": async () => callUnityBridge("/api/hierarchy", "GET"),
  "unity_get_scene_info": async () => callUnityBridge("/api/scene/info", "GET"),
  "unity_load_scene": async (args) => callUnityBridge("/api/scene/load", "POST", args),
  "unity_create_scene": async (args) => callUnityBridge("/api/scene/create", "POST", args),
  "unity_save_scene": async (args) => callUnityBridge("/api/scene/save", "POST", args),
  "unity_get_scene_list": async () => callUnityBridge("/api/scene/list", "GET"),

  // Play Mode
  "unity_enter_play_mode": async () => callUnityBridge("/api/playmode/enter", "POST"),
  "unity_exit_play_mode": async () => callUnityBridge("/api/playmode/exit", "POST"),
  "unity_pause_play_mode": async (args) => callUnityBridge("/api/playmode/pause", "POST", args),
  "unity_get_play_mode_status": async () => callUnityBridge("/api/playmode/status", "GET"),

  // Object Management
  "unity_spawn_object": async (args) => callUnityBridge("/api/spawn", "POST", args),
  "unity_modify_object": async (args) => callUnityBridge("/api/modify", "POST", args),
  "unity_delete_object": async (args) => callUnityBridge("/api/delete", "POST", args),
  "unity_get_selection": async () => callUnityBridge("/api/selection", "GET"),
  "unity_set_selection": async (args) => callUnityBridge("/api/selection/set", "POST", args),
  "unity_duplicate_selection": async () => callUnityBridge("/api/selection/duplicate", "POST"),

  // Components
  "unity_add_component": async (args) => callUnityBridge("/api/component/add", "POST", args),
  "unity_remove_component": async (args) => callUnityBridge("/api/component/remove", "POST", args),
  "unity_get_component_properties": async (args) => callUnityBridge("/api/inspector/get-properties", "POST", args),
  "unity_set_property": async (args) => callUnityBridge("/api/inspector/set-property", "POST", args),
  "unity_invoke_method": async (args) => callUnityBridge("/api/inspector/invoke-method", "POST", args),

  // Assets
  "unity_create_material": async (args) => callUnityBridge("/api/material/create", "POST", args),
  "unity_create_prefab": async (args) => callUnityBridge("/api/asset/create-prefab", "POST", args),
  "unity_instantiate_prefab": async (args) => callUnityBridge("/api/prefab/instantiate", "POST", args),
  "unity_find_assets": async (args) => callUnityBridge("/api/asset/find", "POST", args),

  // Scripts
  "unity_create_script": async (args) => callUnityBridge("/api/script/create", "POST", args),
  "unity_compile_scripts": async () => callUnityBridge("/api/script/compile", "POST"),

  // Animation
  "unity_set_animation_trigger": async (args) => callUnityBridge("/api/animation/set-trigger", "POST", args),
  "unity_set_animation_bool": async (args) => callUnityBridge("/api/animation/set-bool", "POST", args),
  "unity_set_animation_float": async (args) => callUnityBridge("/api/animation/set-float", "POST", args),
  "unity_get_animation_state": async (args) => callUnityBridge("/api/animation/get-state", "POST", args),

  // Camera
  "unity_set_camera_position": async (args) => callUnityBridge("/api/camera/set-position", "POST", args),
  "unity_set_camera_orthographic": async (args) => callUnityBridge("/api/camera/set-orthographic", "POST", args),
  "unity_camera_follow_target": async (args) => callUnityBridge("/api/camera/follow-target", "POST", args),
  "unity_take_screenshot": async (args) => callUnityBridge("/api/camera/screenshot", "POST", args),

  // Physics
  "unity_set_time_scale": async (args) => callUnityBridge("/api/physics/set-time-scale", "POST", args),
  "unity_physics_raycast": async (args) => callUnityBridge("/api/physics/raycast", "POST", args),
  "unity_physics_overlap_sphere": async (args) => callUnityBridge("/api/physics/overlap-sphere", "POST", args),

  // Build
  "unity_build_player": async (args) => callUnityBridge("/api/build/player", "POST", args),
  "unity_get_build_settings": async () => callUnityBridge("/api/build/settings", "GET"),

  // Undo/Redo
  "unity_undo": async (args) => callUnityBridge("/api/undo", "POST", args),
  "unity_redo": async () => callUnityBridge("/api/redo", "POST"),

  // Windows
  "unity_open_window": async (args) => callUnityBridge("/api/window/open", "POST", args),

  // Console
  "unity_get_console_logs": async () => callUnityBridge("/api/console/logs", "GET"),
  "unity_clear_console": async () => callUnityBridge("/api/console/clear", "POST"),
  "unity_send_debug_log": async (args) => callUnityBridge("/api/console/send", "POST", args),

  // Project Settings
  "unity_get_project_settings": async () => callUnityBridge("/api/settings/get", "GET"),

  // Documentation RAG
  "unity_search_docs": async (args) => {
    const rag = await getRAG();
    return await rag.search(args.query, args.topK || 3);
  }
};

// ============================================
// SERVER SETUP
// ============================================

async function main() {
  const server = new Server(
    {
      name: "unity-editor-mcp",
      version: "2.0.0",
    },
    {
      capabilities: {
        tools: {},
        prompts: {},
      },
    }
  );

  // List available prompts
  server.setRequestHandler(ListPromptsRequestSchema, async () => {
    return {
      prompts: [
        {
          name: "unity_mcp_system_prompt",
          description: "System instructions and rules for the Unity MCP Agent. Helps the model understand how to use tools properly.",
        },
        {
          name: "unity_mcp_quick_reference",
          description: "Quick reference and syntax overview for the Unity MCP.",
        }
      ],
    };
  });

  // Handle prompt requests
  server.setRequestHandler(GetPromptRequestSchema, async (request) => {
    const { name } = request.params;
    
    if (name === "unity_mcp_system_prompt" || name === "unity_mcp_quick_reference") {
      const fileName = name === "unity_mcp_system_prompt" ? "SYSTEM_PROMPT.md" : "QUICK_REFERENCE.md";
      const filePath = path.join(__dirname, "..", fileName);
      try {
        const content = fs.readFileSync(filePath, "utf8");
        return {
          description: "Unity Agent Instructions",
          messages: [
            {
              role: "user",
              content: { type: "text", text: content },
            }
          ]
        };
      } catch (err) {
        throw new Error(`Failed to read prompt file ${fileName}`);
      }
    }
    throw new Error("Prompt not found");
  });

  // List available tools
  server.setRequestHandler(ListToolsRequestSchema, async () => {
    return {
      tools: TOOLS,
    };
  });

  // Handle tool calls
  server.setRequestHandler(CallToolRequestSchema, async (request) => {
    const { name, arguments: args } = request.params;
    
    try {
      const handler = TOOL_HANDLERS[name];
      if (!handler) {
        throw new Error(`Unknown tool: ${name}`);
      }
      
      const result = await handler(args || {});
      
      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(result, null, 2),
          },
        ],
      };
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      return {
        content: [
          {
            type: "text",
            text: `Error: ${errorMessage}`,
          },
        ],
        isError: true,
      };
    }
  });

  // Start server
  const transport = new StdioServerTransport();
  await server.connect(transport);
  
  console.error("Unity MCP Server v2.0.0 running on stdio");
  console.error(`Connected to Unity Bridge at ${UNITY_BRIDGE_URL}`);
  console.error(`Documentation RAG: ${path.join(__dirname, "..", "unity-docs-db")}`);
}

main().catch((error) => {
  console.error("Fatal error:", error);
  process.exit(1);
});
