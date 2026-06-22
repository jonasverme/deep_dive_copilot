# Car Inventory — Demo Guide

## Pre-requisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 8.0+ | `dotnet --version` |
| Ollama | latest | `ollama --version` |
| GitHub CLI | latest | `gh --version` |
| GitHub Copilot CLI extension | latest | `gh copilot --version` |

```bash
# Pull the model once before the demo (takes a few minutes)
ollama pull qwen2.5
```

---

## Run the API

```bash
cd CarInventory.Api
dotnet run
```

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Chat UI: http://localhost:5000

The SQLite database (`carinventory.db`) is created and seeded automatically on first run
with 10 cars, 4 owners, and 10 service records.

---

## Run the tests

```bash
dotnet test CarInventory.Tests
```

---

## Register the MCP server with GitHub Copilot CLI

```bash
# Build first
dotnet build CarInventory.Mcp

# Register (update path to match your machine)
gh copilot mcp add car-inventory \
  --command "dotnet" \
  --args "run --project /full/path/to/CarInventory.Mcp -- /full/path/to/carinventory.db"
```

Or add to `.github/copilot-mcp.json` in your repo:

```json
{
  "servers": {
    "car-inventory": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "CarInventory.Mcp", "--", "carinventory.db"]
    }
  }
}
```

---

## Demo Script

### Act 1 — UNDERSTAND (10 min)

Show the starting state in Swagger, then trigger Copilot CLI with ADO MCP:

```bash
gh copilot suggest --mode plan \
  "Analyse the CarInventory solution. Fetch open ADO sprint items via MCP. \
   Identify gaps between the tickets and the current code. \
   Generate a Mermaid architecture diagram."
```

Point out: Copilot reads `*.csproj`, EF models, endpoint files — then fetches ADO items.

---

### Act 2 — BUILD (12 min)

Show and approve the plan, then:

```bash
gh copilot suggest --mode delegate \
  "Implement the approved plan: add OllamaService.cs, ChatEndpoints.cs, \
   ChatRequestValidator, and the wwwroot/index.html chat UI."
```

After generation:

```bash
dotnet build   # should compile clean
dotnet test    # ChatEndpointTests pass with mocked Ollama
```

Open the Chat UI and demo live queries (see table below).

---

### Act 3 — REVIEW (10 min)

```bash
# CLI review of all changes since last commit
gh copilot review --diff HEAD~1

# Rubber duck
gh copilot explain \
  "Why does OllamaService use IAsyncEnumerable<string> instead of Task<string>?"
```

Copilot QL:

```bash
gh copilot query \
  "Find all async methods in CarInventory.Api missing a CancellationToken parameter"
```

---

## Good demo questions for the Chat UI

| Question | What it demonstrates |
|----------|---------------------|
| "Which cars are available?" | Inventory context injection |
| "Who owns the Tesla Model 3?" | Owner relationship traversal |
| "Show the service history for the BMW 320i" | Service record reasoning |
| "What's cheaper — the Golf 8 or the Peugeot 308?" | Price comparison |
| "List all cars over 50 000 km" | Mileage filtering |
| "Which car had the most expensive service?" | Aggregate reasoning |

---

## Project structure

```
CarInventory/
├── CarInventory.Api/
│   ├── Data/
│   │   ├── CarInventoryDbContext.cs   ← STARTING POINT
│   │   └── SeedData.cs               ← STARTING POINT
│   ├── Models/
│   │   ├── Car.cs                    ← STARTING POINT
│   │   ├── Owner.cs                  ← STARTING POINT
│   │   └── ServiceRecord.cs          ← STARTING POINT
│   ├── Endpoints/
│   │   ├── CarEndpoints.cs           ← STARTING POINT
│   │   ├── OwnerEndpoints.cs         ← STARTING POINT
│   │   └── ChatEndpoints.cs          ← ADDED BY COPILOT DEMO
│   ├── Services/
│   │   └── OllamaService.cs          ← ADDED BY COPILOT DEMO
│   ├── wwwroot/
│   │   └── index.html                ← ADDED BY COPILOT DEMO
│   └── appsettings.json
├── CarInventory.Mcp/
│   └── Program.cs                    ← ADDED BY COPILOT DEMO (4 MCP tools)
└── CarInventory.Tests/
    ├── CarEndpointTests.cs           ← STARTING POINT (5 tests)
    └── ChatEndpointTests.cs          ← ADDED BY COPILOT DEMO (4 tests)
```
