---
description: How to generate and assign an AI image to a Unity Image component
---

1. **Find Selection**: Use `mcp_unityMCP_find_gameobjects` with `search_method='by_component', search_term='AiImageGenerator'` to find the target object. Or check currently selected object.
2. **Read Configuration**:
    - Use `mcp_unityMCP_read_resource` on the target object's `AiImageGenerator` component.
    - Extract `prompt`, `resolution`, `assetPath`, `fileName`, and the reference to `AiImageGeneratorConfig`.
    - If `config` exists, read its `projectContext`, `globalNegativePrompt`, `palettes`, and `styles`.
3. **Construct Final Prompt**:
    - Context: `config.projectContext`
    - Base Prompt: `generator.prompt`
    - Style: `generator.styleOverride` or from config.
    - Negative Prompt: `config.globalNegativePrompt` + `style.excludePrompts`.
4. **Generate Image**:
    - Call `generate_image(Prompt=fullPrompt, ImageName=generator.fileName)`.
5. **Move & Import**:
    - Copy the generated artifact from its temporary path to `Assets/[assetPath]/[fileName].png`.
    - Use `mcp_unityMCP_manage_texture(action='set_import_settings', path=fullPath, import_settings={'textureType': 'Sprite'})`.
6. **Assign to Component**:
    - Use `mcp_unityMCP_manage_components(action='set_property', target=id, component_type='Image', property='m_Sprite', value={'path': fullPath})`.
7. **Refresh Unity**:
    - Call `mcp_unityMCP_refresh_unity(scope='assets')`.
