---
description: How to use the Writer AI to generate narrative content using the docs~ files.
---

# 🖋️ Writer AI Workflow

Use this workflow when you want the AI to generate new lore, dialogue, or character content.

## 1. Initializing the Writer Persona
// turbo
1. Read the core rules and persona: `view_file d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\documentation\writer_ai_rules.md`
2. Gather context from the lore bible: `view_file d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\lore\WorldPlotOverview.md`

## 2. Generating Content
*   **For Dialogue**: Ask the AI to "Write a dialogue script for [Character] in [Context]".
*   **For Character Bios**: Ask the AI to "Design a biography for a [Rarity] [Class] unit named [Name]".
*   **For Level Plot**: Ask the AI to "Flesh out the plot for [Chapter-Level]".

## 3. Formatting & Delivery
1. The AI should present the draft for review.
2. Once approved, the AI will save the content into the appropriate folder in `Assets/_Game/docs~/` (e.g., `lore/`, `characters/`, or `dialogues/`).
3. If requested, the AI can also generate the JSON or C# bridge code for Addressables integration.

---

> [!TIP]
> Use the command `/writer-ai [request]` to quickly trigger this workflow.
