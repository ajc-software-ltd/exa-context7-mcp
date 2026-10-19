# Exa & Context7 MCP Tools

MCP for Unity custom tools that give your AI assistant:

- **Neural web search** via [Exa](https://exa.ai) — search, find-similar, and fetch full page contents
- **Up-to-date library documentation** via [Context7](https://context7.com) — resolve libraries and query their docs without hallucinated API examples

These tools register with the [MCP for Unity](https://github.com/CoplayDev/unity-mcp) bridge and appear to your AI client as normal MCP tools.

## Requirements

- Unity 2021.3+
- [`com.coplaydev.unity-mcp`](https://github.com/CoplayDev/unity-mcp) installed and connected to your AI client
- An [Exa API key](https://dashboard.exa.ai) (for web search)
- A [Context7 API key](https://context7.com/dashboard) (for documentation lookup)

## Install

Add this package to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ajc-software-ltd.exa-context7-mcp": "https://github.com/ajc-software-ltd/exa-context7-mcp.git"
  }
}
```

Or, for local development, copy/embed this folder under your project's `Packages/` directory.

## Configure API keys

Keys are stored in your OS secure store (Keychain / Credential Manager / libsecret) and are never read back into the editor or sent over the MCP bridge.

**Option A — Editor window**

1. In Unity: **Window → MCP for EXA & Context7**
2. Paste each key into its masked field and press **Save** (or click away)

**Option B — Environment variables**

Set the standard environment variables before launching Unity:

```bash
export EXA_API_KEY="your-exa-key"
export CONTEXT7_API_KEY="your-context7-key"
```

Environment variables take precedence over the secure store.

After configuring keys, reconnect your AI client to the MCP server (or restart Unity) so the tools are re-registered.

## Tools

### Exa

| Tool | Description |
|---|---|
| `exa_search` | Neural web search with optional full text, highlights, domain filters, date ranges, and categories |
| `exa_find_similar` | Find pages semantically similar to a given URL |
| `exa_get_contents` | Fetch the full contents (text) of one or more URLs |

### Context7

| Tool | Description |
|---|---|
| `context7_resolve_library` | Resolve a library name to a Context7 library ID |
| `context7_query_docs` | Fetch up-to-date code snippets and docs for a library ID |
| `context7_search` | One-shot search (resolves and queries in a single call) |

### Typical flow

```
1. context7_resolve_library(library_name="Unity", query="camera follow script")
   → returns library ID like "/unitycom/docs"
2. context7_query_docs(library_id="/unitycom/docs", query="Cinemachine camera follow", language="C#")
   → returns current code snippets

— or, for web research —

1. exa_search(query="Unity 6 URP render pipeline migration guide", include_highlights=true)
   → returns results with snippets
2. exa_get_contents(urls=["https://..."])
   → fetches full article text
```

## Managing tool visibility

Like all MCP for Unity tools, these can be enabled/disabled per session via the `manage_tools` meta-tool or the MCP for Unity editor window.

## License

MIT — see [LICENSE](LICENSE).
