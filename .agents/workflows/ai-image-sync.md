---
description: Batch generate images for all Pending AiImageGenerator components
---

// turbo-all
1. **Find Pending Objects**: 
    - Search for all GameObjects with the `AiImageGenerator` component.
    - Filter for those where `state == Pending`.
2. **Read Config**:
    - Locate `AiImageGeneratorConfig` (usually in `Assets/Plugins/AiImageGenerator/Resources/`).
    - Note `defaultSavePath` (e.g. `_Game/Art/Generated/`) and `globalNegativePrompt`.
3. **Batch Process**:
    - For each pending object:
        a. Set `state = Generating`.
        b. Ensure target folder exists: Create `Assets/[defaultSavePath]` using `run_command` if missing.
        c. Construct final prompt (Project Context + Styles + Palettes + Prompt).
        d. Call `generate_image`.
        e. **Move Artifact**: Move from the temp path to `Assets/[defaultSavePath]/[fileName].png`.
        f. **Import Settings**: Set Texture Type to `Sprite` (UI) for the new asset.
        g. **Assign**: Assign to the `m_Sprite` property of the `Image` component.
        h. **Finalize**: Call `CompleteGeneration(path, true)` to log to persistent history.
4. **Final Refresh**:
    - Call `refresh_unity(scope='assets')`.
