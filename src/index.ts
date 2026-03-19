#!/usr/bin/env node

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  Tool,
} from "@modelcontextprotocol/sdk/types.js";
import { UnityDocRAG } from "./rag.js";

import * as path from "path";
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

// Tool definitions - mapping to C# endpoints
const TOOLS: Tool[] = [
  {
    name: "unity_get_hierarchy",
    description: "Get the complete hierarchy of GameObjects in the active Unity scene. Returns all root objects with their transforms, components, and children.",
    inputSchema: {
      type: "object",
      properties: {},
      required: []
    }
  },
  {
    name: "unity_get_selection",
    description: "Get the currently selected GameObjects in Unity Editor",
    inputSchema: {
      type: "object",
      properties: {},
      required: []
    }
  },
  {
    name: "unity_get_scene_info",
    description: "Get information about the active Unity scene (name, path, root count, dirty state)",
    inputSchema: {
      type: "object",
      properties: {},
      required: []
    }
  },
  {
    name: "unity_spawn_object",
    description: "Create a new GameObject in Unity. Can create primitives (Cube, Sphere, etc.) or empty GameObjects.",
    inputSchema: {
      type: "object",
      properties: {
        name: { 
          type: "string", 
          description: "Name of the GameObject to create"
        },
        primitive: { 
          type: "string", 
          enum: ["Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad"],
          description: "Primitive type to create. If omitted, creates an empty GameObject."
        },
        position: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" }
          },
          description: "Position in world space (default: 0,0,0)"
        },
        rotation: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" }
          },
          description: "Rotation in Euler angles (default: 0,0,0)"
        },
        scale: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" }
          },
          description: "Local scale (default: 1,1,1)"
        },
        parent: { 
          type: "string", 
          description: "Name of parent GameObject to attach to (optional)"
        }
      },
      required: ["name"]
    }
  },
  {
    name: "unity_modify_object",
    description: "Modify an existing GameObject's properties like position, rotation, scale, name, active state, tag, or layer",
    inputSchema: {
      type: "object",
      properties: {
        name: { 
          type: "string", 
          description: "Name of the GameObject to modify (use this or instanceID)"
        },
        instanceID: { 
          type: "number", 
          description: "Instance ID of the GameObject (preferred for uniqueness)"
        },
        newName: { 
          type: "string", 
          description: "New name for the GameObject"
        },
        active: { 
          type: "boolean", 
          description: "Set active (true) or inactive (false)"
        },
        position: {
          type: "object",
          properties: { 
            x: { type: "number" }, 
            y: { type: "number" }, 
            z: { type: "number" } 
          },
          description: "New world position"
        },
        rotation: {
          type: "object",
          properties: { 
            x: { type: "number" }, 
            y: { type: "number" }, 
            z: { type: "number" } 
          },
          description: "New rotation in Euler angles"
        },
        scale: {
          type: "object",
          properties: { 
            x: { type: "number" }, 
            y: { type: "number" }, 
            z: { type: "number" } 
          },
          description: "New local scale"
        },
        tag: { 
          type: "string", 
          description: "New tag for the GameObject"
        },
        layer: { 
          type: "number", 
          description: "New layer index (0-31)"
        }
      },
      required: []
    }
  },
  {
    name: "unity_delete_object",
    description: "Delete a GameObject from the scene by name or instanceID",
    inputSchema: {
      type: "object",
      properties: {
        name: { 
          type: "string", 
          description: "Name of the GameObject to delete"
        },
        instanceID: { 
          type: "number", 
          description: "Instance ID of the GameObject"
        }
      },
      required: []
    }
  },
  {
    name: "unity_add_component",
    description: "Add a Unity component to a GameObject (e.g., Rigidbody, BoxCollider, MeshRenderer, custom scripts)",
    inputSchema: {
      type: "object",
      properties: {
        objectName: { 
          type: "string", 
          description: "Name of the GameObject (use this or instanceID)"
        },
        instanceID: { 
          type: "number", 
          description: "Instance ID of the GameObject"
        },
        componentType: { 
          type: "string", 
          description: "Component type name. Can be short (e.g., 'Rigidbody', 'BoxCollider') or full namespace ('UnityEngine.Rigidbody')"
        }
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
        objectName: { 
          type: "string", 
          description: "Name of the GameObject"
        },
        instanceID: { 
          type: "number", 
          description: "Instance ID of the GameObject"
        },
        componentType: { 
          type: "string", 
          description: "Type of component to remove"
        }
      },
      required: ["componentType"]
    }
  },
  {
    name: "unity_execute_code",
    description: "Execute arbitrary C# code in Unity Editor. WARNING: This can modify your project! Use with caution. The code runs in the Editor context with access to Unity APIs.",
    inputSchema: {
      type: "object",
      properties: {
        code: { 
          type: "string", 
          description: "C# code to execute. Must be valid Unity Editor code. Has access to UnityEngine and UnityEditor namespaces."
        }
      },
      required: ["code"]
    }
  },
  {
    name: "unity_create_material",
    description: "Create a new material asset in the project",
    inputSchema: {
      type: "object",
      properties: {
        name: { 
          type: "string", 
          description: "Name for the material asset"
        },
        shader: { 
          type: "string", 
          description: "Shader name (default: 'Standard'). Examples: 'Standard', 'Unlit/Color', 'Particles/Standard Unlit'"
        },
        color: {
          type: "object",
          properties: {
            r: { type: "number", description: "Red (0-1)" },
            g: { type: "number", description: "Green (0-1)" },
            b: { type: "number", description: "Blue (0-1)" },
            a: { type: "number", description: "Alpha (0-1)" }
          },
          description: "RGBA color values (0-1 range)"
        }
      },
      required: ["name"]
    }
  },
  {
    name: "unity_instantiate_prefab",
    description: "Instantiate a prefab from the Assets folder into the scene",
    inputSchema: {
      type: "object",
      properties: {
        path: { 
          type: "string", 
          description: "Path to prefab in Assets folder (e.g., 'Assets/Prefabs/Enemy.prefab', 'Assets/Characters/Player.prefab')"
        },
        position: {
          type: "object",
          properties: { 
            x: { type: "number" }, 
            y: { type: "number" }, 
            z: { type: "number" } 
          },
          description: "Position to spawn at (default: 0,0,0)"
        }
      },
      required: ["path"]
    }
  },
  {
    name: "unity_clear_console",
    description: "Clear the Unity Console window of all logs and errors",
    inputSchema: {
      type: "object",
      properties: {},
      required: []
    }
  },
  {
    name: "unity_undo",
    description: "Undo the last action in Unity Editor (Ctrl+Z equivalent)",
    inputSchema: {
      type: "object",
      properties: {},
      required: []
    }
  },
  {
    name: "unity_search_docs",
    description: "Search Unity documentation using RAG (Retrieval-Augmented Generation). Returns relevant API documentation with code examples. Requires: 1) Ollama running with nomic-embed-text, 2) Unity docs indexed via ingest-docs.ts",
    inputSchema: {
      type: "object",
      properties: {
        query: { 
          type: "string", 
          description: "Search query for Unity API (e.g., 'Rigidbody.AddForce', 'Instantiate', 'Quaternion.Lerp', 'how to move an object')"
        },
        topK: {
          type: "number",
          description: "Number of results to return (default: 3)",
          default: 3
        }
      },
      required: ["query"]
    }
  }
];

// Unity Bridge HTTP client
async function unityBridgeRequest(endpoint: string, method: string = "GET", body?: object): Promise<any> {
  const url = `${UNITY_BRIDGE_URL}${endpoint}`;
  
  const options: RequestInit = {
    method,
    headers: {
      "Content-Type": "application/json",
      "Accept": "application/json"
    }
  };
  
  if (body) {
    options.body = JSON.stringify(body);
  }
  
  try {
    const response = await fetch(url, options);
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }
    const data = await response.json();
    return data;
  } catch (error: any) {
    throw new Error(`Unity Bridge error: ${error.message}. Is Unity running with the MCP Bridge on port ${PORT}?`);
  }
}

const PORT = 7778;

// Tool handlers - map to C# endpoints
const toolHandlers: Record<string, (args: any) => Promise<any>> = {
  unity_get_hierarchy: async () => {
    return await unityBridgeRequest("/api/hierarchy");
  },
  
  unity_get_selection: async () => {
    return await unityBridgeRequest("/api/selection");
  },
  
  unity_get_scene_info: async () => {
    return await unityBridgeRequest("/api/scene/info");
  },
  
  unity_spawn_object: async (args) => {
    return await unityBridgeRequest("/api/spawn", "POST", args);
  },
  
  unity_modify_object: async (args) => {
    return await unityBridgeRequest("/api/modify", "POST", args);
  },
  
  unity_delete_object: async (args) => {
    return await unityBridgeRequest("/api/delete", "POST", args);
  },
  
  unity_add_component: async (args) => {
    return await unityBridgeRequest("/api/component/add", "POST", args);
  },
  
  unity_remove_component: async (args) => {
    return await unityBridgeRequest("/api/component/remove", "POST", args);
  },
  
  unity_execute_code: async (args) => {
    return await unityBridgeRequest("/api/execute", "POST", args);
  },
  
  unity_create_material: async (args) => {
    return await unityBridgeRequest("/api/material/create", "POST", args);
  },
  
  unity_instantiate_prefab: async (args) => {
    return await unityBridgeRequest("/api/prefab/instantiate", "POST", args);
  },
  
  unity_clear_console: async () => {
    return await unityBridgeRequest("/api/console/clear", "POST");
  },
  
  unity_undo: async () => {
    return await unityBridgeRequest("/api/undo", "POST");
  },
  
  unity_search_docs: async (args) => {
    const rag = await getRAG();
    const results = await rag.search(args.query, args.topK || 3);
    return {
      query: args.query,
      results: results,
      note: "Results from local Unity docs RAG"
    };
  }
};

// Create MCP server
const server = new Server(
  {
    name: "unity-mcp-server",
    version: "0.2.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// Handle tool list request
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: TOOLS,
  };
});

// Handle tool call request
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  
  const handler = toolHandlers[name];
  if (!handler) {
    throw new Error(`Unknown tool: ${name}`);
  }
  
  try {
    const result = await handler(args || {});
    return {
      content: [
        {
          type: "text",
          text: JSON.stringify(result, null, 2)
        }
      ]
    };
  } catch (error: any) {
    return {
      content: [
        {
          type: "text",
          text: `Error: ${error.message}`
        }
      ],
      isError: true
    };
  }
});

// Start server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  
  // Log to stderr so it doesn't interfere with MCP protocol
  console.error("🦞 Unity MCP Server running (with RAG support)");
  console.error(`🔗 Connecting to Unity Bridge at ${UNITY_BRIDGE_URL}`);
  console.error("💾 Vector DB: ./unity-docs-db (will be created on first query)");
}

main().catch((error) => {
  console.error("Fatal error:", error);
  process.exit(1);
});
