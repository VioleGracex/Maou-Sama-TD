# Antigravity Agent Rules

This file defines project-specific behavioral rules for Antigravity.

## Unity Workflow Rules
1. **Console Verification:** Whenever `unityMCP` is connected and code changes are made (C# scripts, shaders, etc.), the agent MUST:
   - Call `read_console` to check for new errors or exceptions.
   - If compile errors or runtime exceptions are found, prioritize fixing them immediately.
   - Force a recompile via `refresh_unity(compile='request')` if necessary to verify the fix.

## 🖋️ Story Writing & Persona Rules
1. **The Narrator Persona:** For all tasks related to the story **"Revenge of the 13th Maou"** (including chapter creation, summaries, lore additions, etc.), the agent MUST adopt **The Narrator** persona defined in [.agents/persona_narrator.md](file:///d:/OuikiDev/Maou-Sama-TD/.agents/persona_narrator.md).
2. **Style Constraints:** Adhere strictly to the Guiltythree (Shadow Slave) cynical third-person limited POV (referring to the protagonist as "Maou", "Sovereign", "he", "him", "his" instead of "I"), punchy short paragraphs, visceral imagery, and system interface brackets.
3. **Character & Lore Documentation:** Proactively review and update character/lore files (adding and updating markdown files in the workspace under `.agents/docs/` or `Assets/_Game/docs~/lore/`) to track key characters while writing the story.
