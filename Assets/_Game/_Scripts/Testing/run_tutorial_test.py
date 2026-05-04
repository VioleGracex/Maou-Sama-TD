import time
import subprocess
import json

# This script is a wrapper to run the tutorial test via Unity MCP
# It assumes the unityMCP server is running and accessible via the tool environment.

def run_unity_code(code):
    # Mocking the call to the unityMCP tool for the user's reference
    # In practice, the AI agent (Antigravity) calls these tools directly.
    print(f"Executing C# in Unity:\n{code}")
    # result = mcp_unityMCP_execute_code(action="execute", code=code)
    # return result

def main():
    print("--- Starting Automated Tutorial Test ---")
    
    # 1. Setup the AutoPlayer in the scene if it doesn't exist
    setup_code = """
    var go = GameObject.Find("TutorialAutoPlayer");
    if (go == null) {
        go = new GameObject("TutorialAutoPlayer");
        go.AddComponent<MaouSamaTD.Testing.TutorialAutoPlayer>();
    }
    var player = go.GetComponent<MaouSamaTD.Testing.TutorialAutoPlayer>();
    player.IsAutoPlaying = true;
    player.AutoPlayOnStart = true;
    return "AutoPlayer setup complete.";
    """
    run_unity_code(setup_code)
    
    # 2. Load the scene and start playing
    # run_unity_tool("mcp_unityMCP_manage_scene", action="load", path="Assets/_Game/Scenes/BattleScene.unity")
    # run_unity_tool("mcp_unityMCP_manage_editor", action="play")
    
    print("3. Waiting for game to start...")
    time.sleep(5)
    
    # 4. Trigger Level 2
    trigger_level_code = """
    var level2 = UnityEditor.AssetDatabase.LoadAssetAtPath<MaouSamaTD.Levels.LevelData>("Assets/_Game/Data/Levels/LevelData_Level2.asset");
    var gameManager = GameObject.FindFirstObjectByType<MaouSamaTD.Managers.GameManager>();
    gameManager.LoadLevelData(level2);
    return "Level 2 Triggered.";
    """
    run_unity_code(trigger_level_code)
    
    print("4. Monitoring tutorial progress...")
    # In a real script, we would poll the status here.
    
    print("--- Test Script Finished ---")

if __name__ == "__main__":
    main()
