/**
 * RAG (Retrieval-Augmented Generation) System for Unity Documentation
 * 
 * Uses Local Ollama for embeddings + Vectra for vector storage
 */

import { LocalIndex } from "vectra";
import * as fs from "fs/promises";
import * as path from "path";

// Local Ollama Configuration
const OLLAMA_URL = process.env.OLLAMA_URL || "http://localhost:11434";
const EMBED_MODEL = process.env.EMBED_MODEL || "nomic-embed-text";

export interface DocChunk {
  content: string;
  source: string;
  category: string;
  methodName?: string;
  namespace?: string;
}

export interface SearchResult {
  score: number;
  content: string;
  source: string;
  category: string;
}

export class UnityDocRAG {
  private index: LocalIndex | null = null;
  private dbPath: string;
  private isInitialized = false;

  constructor(dbPath: string = "./unity-docs-db") {
    this.dbPath = dbPath;
  }

  /**
   * Initialize the vector database
   */
  async initialize(): Promise<void> {
    if (this.isInitialized) return;

    this.index = new LocalIndex(this.dbPath);
    
    // Check if index exists, create if not
    try {
      await this.index.getIndexStats();
    } catch {
      // Index doesn't exist, create it
      await this.index.createIndex({ version: 1 });
    }
    
    this.isInitialized = true;
    
    console.error(`[RAG] Unity Docs DB initialized at ${this.dbPath}`);
  }

  /**
   * Get embeddings from Local Ollama API
   */
  private async getEmbedding(text: string): Promise<number[]> {
    const response = await fetch(`${OLLAMA_URL}/api/embeddings`, {
      method: "POST",
      headers: { 
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        model: EMBED_MODEL,
        prompt: text
      })
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Ollama error: ${response.status} - ${errorText}`);
    }

    const data = await response.json() as { embedding: number[] };
    return data.embedding;
  }

  /**
   * Add a document chunk to the vector store
   */
  async addDocument(chunk: DocChunk, index: number): Promise<void> {
    if (!this.index) throw new Error("RAG not initialized");

    const embedding = await this.getEmbedding(chunk.content);
    
    await this.index.insertItem({
      id: this.generateId(chunk, index),
      vector: embedding,
      metadata: {
        text: chunk.content,
        source: chunk.source,
        category: chunk.category,
        methodName: chunk.methodName || "",
        namespace: chunk.namespace || ""
      }
    });
  }

  /**
   * Search for relevant Unity API documentation
   */
  async search(query: string, topK: number = 3): Promise<SearchResult[]> {
    if (!this.index) throw new Error("RAG not initialized");

    const queryEmbedding = await this.getEmbedding(query);
    
    const results = await this.index.queryItems(queryEmbedding, query, topK, undefined, false);

    return results.map((r: any) => ({
      score: r.score,
      content: r.item.metadata?.text as string || "",
      source: r.item.metadata?.source as string || "unknown",
      category: r.item.metadata?.category as string || "unknown"
    }));
  }

  /**
   * Close the database
   */
  async close(): Promise<void> {
    this.isInitialized = false;
    this.index = null;
  }

  /**
   * Generate unique ID for document
   */
  private generateId(chunk: DocChunk, index: number): string {
    const sourcePrefix = chunk.source.replace(/[^a-zA-Z0-9]/g, "").slice(0, 10);
    const contentHash = Buffer.from(chunk.content).toString("base64").slice(0, 15);
    return `${sourcePrefix}_${chunk.category}_${index}_${contentHash}`;
  }

  /**
   * Check if Local Ollama API is accessible
   */
  async checkOllama(): Promise<boolean> {
    try {
      const response = await fetch(`${OLLAMA_URL}/api/tags`);
      return response.ok;
    } catch {
      return false;
    }
  }

  /**
   * Get database stats
   */
  async getStats(): Promise<{ documentCount: number; dbPath: string }> {
    if (!this.index) throw new Error("RAG not initialized");
    const stats = await this.index.getIndexStats();
    return {
      documentCount: stats.items,
      dbPath: this.dbPath
    };
  }
}

/**
 * Chunk text into smaller pieces with overlap
 */
export function chunkText(text: string, chunkSize: number = 500, overlap: number = 50): string[] {
  const chunks: string[] = [];
  const words = text.split(/\s+/);
  
  for (let i = 0; i < words.length; i += chunkSize - overlap) {
    const chunk = words.slice(i, i + chunkSize).join(" ");
    if (chunk.length > 0) {
      chunks.push(chunk);
    }
  }
  
  return chunks;
}

/**
 * Parse Unity documentation markdown file
 */
export function parseUnityDoc(content: string, source: string): DocChunk[] {
  const chunks: DocChunk[] = [];
  
  // Extract code blocks (they contain API signatures)
  const codeBlockRegex = /```csharp\n([\s\S]*?)```/g;
  let match;
  
  while ((match = codeBlockRegex.exec(content)) !== null) {
    const codeBlock = match[1].trim();
    
    // Try to extract method name
    const methodMatch = codeBlock.match(/(?:public|private|protected|internal)?\s*(?:static|virtual|override|abstract)?\s*\w+\s+(\w+)\s*\(/);
    const methodName = methodMatch ? methodMatch[1] : undefined;
    
    // Try to extract namespace
    const namespaceMatch = codeBlock.match(/namespace\s+([\w.]+)/);
    const namespace = namespaceMatch ? namespaceMatch[1] : undefined;
    
    chunks.push({
      content: codeBlock,
      source: source,
      category: "code_example",
      methodName: methodName,
      namespace: namespace
    });
  }
  
  // Chunk the rest of the content
  const textChunks = chunkText(content.replace(/```[\s\S]*?```/g, ""));
  
  for (const textChunk of textChunks) {
    if (textChunk.length > 100) {  // Only meaningful chunks
      chunks.push({
        content: textChunk,
        source: source,
        category: "documentation"
      });
    }
  }
  
  return chunks;
}

/**
 * Load and index all Unity docs from a folder
 */
export async function ingestUnityDocs(
  rag: UnityDocRAG,
  docsFolder: string
): Promise<{ indexed: number; errors: number }> {
  let indexed = 0;
  let errors = 0;
  let chunkIndex = 0;
  
  async function processDirectory(dir: string): Promise<void> {
    const entries = await fs.readdir(dir, { withFileTypes: true });
    
    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      
      if (entry.isDirectory()) {
        await processDirectory(fullPath);
      } else if (entry.name.endsWith(".md") || entry.name.endsWith(".txt")) {
        try {
          const content = await fs.readFile(fullPath, "utf-8");
          const relativePath = path.relative(docsFolder, fullPath);
          const chunks = parseUnityDoc(content, relativePath);
          
          for (const chunk of chunks) {
            await rag.addDocument(chunk, chunkIndex++);
            indexed++;
          }
          
          console.error(`[RAG] Indexed: ${relativePath} (${chunks.length} chunks)`);
        } catch (err) {
          console.error(`[RAG] Error processing ${fullPath}: ${err}`);
          errors++;
        }
      }
    }
  }
  
  await processDirectory(docsFolder);
  
  return { indexed, errors };
}
