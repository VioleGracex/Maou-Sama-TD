# Antigravity Agent Rules

This file defines project-specific behavioral rules for Antigravity.

## Unity Workflow Rules
1. **Console Verification:** Whenever `unityMCP` is connected and code changes are made (C# scripts, shaders, etc.), the agent MUST:
   - Call `read_console` to check for new errors or exceptions.
   - If compile errors or runtime exceptions are found, prioritize fixing them immediately.
   - Force a recompile via `refresh_unity(compile='request')` if necessary to verify the fix.
