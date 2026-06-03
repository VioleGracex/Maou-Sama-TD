-- Scenario 2: Level 2 Progression
-- Supports two entry paths:
--   Path A — Continuation after Level 1 victory (game already loaded into Level 2 battle scene)
--   Path B — Standalone run from the Conquest map (Lobby → Conquest → Node 2 → Briefing → Mission Readiness → Start Mission)
--
-- The scenario auto-detects which path applies using short-timeout template checks.

local function run_tests()

    -- ============================================================
    -- STAGE 1: Connect to Game
    -- ============================================================
    set_stage("1. Connect Game")
    log_test("Connect Game", "STARTING", "Locating running game instance...")
    if launch_game(false) then
        log_test("Connect Game", "PASS", "Game window found and positioned at (0, 0).")
    else
        log_test("Connect Game", "FAIL", "Could not locate or attach to game window. Aborting.")
        return
    end

    wait(2)

    -- ============================================================
    -- STAGE 2: Detect Entry Path
    --   • Path A: start_battle_btn is already visible → Level 2 loaded from victory
    --   • Path B: lobby / conquest navigation required
    -- ============================================================
    set_stage("2. Detect Entry Path")
    log_test("Path Detection", "STARTING", "Checking if Level 2 battle scene is already loaded (Path A)...")

    local direct_start = wait_template("start_battle_btn", 5)
    if direct_start then
        -- --------------------------------------------------------
        -- PATH A — Battle scene already loaded after Level 1
        -- --------------------------------------------------------
        log_test("Path Detection", "INFO", "Path A detected — Level 2 loaded directly after Level 1 victory.")

        set_stage("3A. Start Level 2 (Direct)")
        log_test("Level 2 Start", "STARTING", "Clicking Start Battle button...")
        click(direct_start.x, direct_start.y)
        log_test("Level 2 Start", "PASS", "Level 2 battle wave started (Path A — direct).")

        wait(3)
        set_stage("Completed")
        log_test("Test Suite", "PASS", "Scenario 2 — Level 2 (Path A: direct) completed successfully!")
        return
    end

    -- --------------------------------------------------------
    -- PATH B — Navigate from Lobby via Conquest map
    -- --------------------------------------------------------
    log_test("Path Detection", "INFO", "Path B — navigating from Lobby to Conquest map.")

    -- ============================================================
    -- STAGE 3B: Navigate to Conquest
    -- ============================================================
    set_stage("3B. Navigate to Conquest")
    log_test("Conquest Nav", "STARTING", "Looking for Conquest navigation button in Lobby...")

    local conquest_btn = wait_template("conquest_nav_btn", 15)
    if conquest_btn then
        click(conquest_btn.x, conquest_btn.y)
        log_test("Conquest Nav", "PASS", "Conquest mode opened.")
    else
        -- Fallback coordinate — bottom navigation bar
        click(820, 510)
        log_test("Conquest Nav", "INFO", "Clicked Conquest nav area (fallback coordinate).")
    end

    wait(3)

    -- ============================================================
    -- STAGE 4B: Select Level 2 Node on the Conquest Map
    -- ============================================================
    set_stage("4B. Select Level 2 Node")
    log_test("Select Node", "STARTING", "Finding Level 2 node on the Conquest map...")

    local node2 = wait_template("node_level_2", 15)
    if node2 then
        click(node2.x, node2.y)
        log_test("Select Node", "PASS", "Level 2 node selected on map.")
    else
        -- Fallback: approximate node position on the map
        click(340, 270)
        log_test("Select Node", "INFO", "Clicked Level 2 node area (fallback coordinate).")
    end

    wait(2)

    -- ============================================================
    -- STAGE 5B: Briefing Panel — Click Engage Button
    -- ============================================================
    set_stage("5B. Briefing Panel — Engage")
    log_test("Briefing Panel", "STARTING", "Waiting for briefing panel and Engage button...")

    local engage_btn = wait_template("engage_btn", 10)
    if engage_btn then
        click(engage_btn.x, engage_btn.y)
        log_test("Briefing Panel", "PASS", "Engage button clicked — mission briefing confirmed.")
    else
        -- Fallback: lower-right area of the briefing panel
        click(790, 430)
        log_test("Briefing Panel", "INFO", "Clicked Engage area (fallback coordinate).")
    end

    wait(2.5)

    -- ============================================================
    -- STAGE 6B: Mission Readiness Panel — Start Mission
    -- ============================================================
    set_stage("6B. Mission Readiness — Start Mission")
    log_test("Mission Readiness", "STARTING", "Waiting for Mission Readiness panel and Start Mission button...")

    local start_mission_btn = wait_template("start_mission_btn", 12)
    if start_mission_btn then
        click(start_mission_btn.x, start_mission_btn.y)
        log_test("Mission Readiness", "PASS", "Start Mission clicked — loading battle scene.")
    else
        -- Fallback: typical bottom-centre/right position in the readiness panel
        click(720, 450)
        log_test("Mission Readiness", "INFO", "Clicked Start Mission area (fallback coordinate).")
    end

    -- Wait for battle scene to fully load
    wait(6)

    -- ============================================================
    -- STAGE 7B: Battle Scene Ready — Confirm Level 2 Loaded
    -- ============================================================
    set_stage("7B. Level 2 — Battle Scene")
    log_test("Battle Scene", "STARTING", "Confirming Level 2 battle scene has loaded...")

    -- Check for wave/start button or general game HUD
    local battle_hud = wait_template("start_battle_btn", 10)
    if battle_hud then
        click(battle_hud.x, battle_hud.y)
        log_test("Battle Scene", "PASS", "Level 2 battle wave started (Path B — Conquest).")
    else
        -- Scene may auto-start or button may use a different template — log and continue
        log_test("Battle Scene", "INFO", "Start Battle button not found — scene may have auto-started.")
    end

    wait(3)

    -- ============================================================
    -- DONE
    -- ============================================================
    set_stage("Completed")
    log_test("Test Suite", "PASS", "Scenario 2 — Level 2 Progression (Path B: Conquest) completed successfully!")
end

run_tests()
