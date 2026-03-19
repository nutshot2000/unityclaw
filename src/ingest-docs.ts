#!/usr/bin/env node

/**
 * Unity Documentation Ingestion Script
 * 
 * Usage: npx tsx src/ingest-docs.ts <path-to-unity-docs-folder>
 * 
 * This script reads Unity documentation files, chunks them, generates embeddings
 * using Local Ollama API, and stores them in a local vector database for RAG queries.
 */

import { UnityDocRAG, ingestUnityDocs } from "./rag.js";
import * as path from "path";

async function main() {
  const docsFolder = process.argv[2];
  
  if (!docsFolder) {
    console.error("Usage: npx tsx src/ingest-docs.ts <path-to-unity-docs>");
    console.error("");
    console.error("Example:");
    console.error("  npx tsx src/ingest-docs.ts C:/UnityDocs");
    console.error("  npx tsx src/ingest-docs.ts ./docs/unity");
    process.exit(1);
  }
  
  const resolvedPath = path.resolve(docsFolder);
  
  console.error("🦞 Unity Documentation Ingestion (Local Ollama)");
  console.error("================================================");
  console.error("");
  console.error(`📁 Docs folder: ${resolvedPath}`);
  console.error(`🖥️  Local Ollama: http://localhost:11434`);
  console.error(`🧠 Embedding model: ${process.env.EMBED_MODEL || "nomic-embed-text"}`);
  console.error(`💾 Vector DB: ./unity-docs-db`);
  console.error("");
  
  // Initialize RAG
  const rag = new UnityDocRAG("./unity-docs-db");
  await rag.initialize();
  
  // Check Ollama Cloud
  console.error("🔍 Checking Ollama Cloud API...");
  const ollamaReady = await rag.checkOllama();
  
  if (!ollamaReady) {
    console.error("❌ Ollama Cloud API is not accessible!");
    console.error("");
    console.error("Check:");
    console.error("  1. Is your OLLAMA_API_KEY correct?");
    console.error("  2. Do you have internet connection?");
    console.error("  3. Is your API key valid? (Get a new one at https://ollama.com/settings)");
    await rag.close();
    process.exit(1);
  }
  
  console.error("✅ Ollama Cloud API is ready");
  console.error("");
  
  // Ingest documents
  console.error("📚 Indexing documents...");
  console.error("");
  
  const startTime = Date.now();
  const result = await ingestUnityDocs(rag, resolvedPath);
  const duration = ((Date.now() - startTime) / 1000).toFixed(2);
  
  console.error("");
  console.error("✅ Ingestion Complete!");
  console.error("======================");
  console.error(`📄 Documents indexed: ${result.indexed}`);
  console.error(`❌ Errors: ${result.errors}`);
  console.error(`⏱️  Duration: ${duration}s`);
  
  // Show stats
  const stats = await rag.getStats();
  console.error("");
  console.error(`💾 Total in database: ${stats.documentCount}`);
  console.error(`📂 Database location: ${stats.dbPath}`);
  console.error("");
  console.error("✨ Ready to use! Try querying with:");
  console.error('  unity_search_docs(query: "Rigidbody.AddForce")');
  
  await rag.close();
}

main().catch((error) => {
  console.error("❌ Fatal error:", error);
  process.exit(1);
});
