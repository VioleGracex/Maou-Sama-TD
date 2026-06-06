-- Scenario 9: Tutorial Level 2 Alone
-- Flow: connect/launch game (no clear data) → navigate Conquest map → select Level 2 node → Engage → Start Mission → Choose Play Tutorial → Start Battle → Deploy Ignis → Ultimate → Level 2 victory.

local function run_tests()

    -- ============================================================
    -- STAGE 1: Connect to Game
    -- ============================================================
    set_stage("1. Connect Game")
    log_test("Connect Game", "STARTING", "Checking for game instance (no cache clear)...")
    if launch_game(false) then
        log_test("Connect Game", "PASS", "Game connected and positioned at (0, 0).")
    else
        log_test("Connect Game", "FAIL", "Could not locate or launch game.")
        return
    end

    -- wait(3)

    -- If the game is at the loading screen, click Start Game
    local start_btn = wait_template("StartButton", 5)
    if start_btn then
        click(start_btn.x, start_btn.y)
        log_test("Connect Game", "INFO", "Clicked StartButton on loading screen.")
        wait(4)
    end

    -- ============================================================
    -- STAGE 2: Navigate Conquest Map
    -- ============================================================
    set_stage("2. Navigate Conquest")
    log_test("Conquest Nav", "STARTING", "Navigating to Conquest map...")

    local conquest_btn = wait_template("conquest_nav_btn", 15)
    if conquest_btn then
        click(conquest_btn.x, conquest_btn.y)
        log_test("Conquest Nav", "PASS", "Conquest mode selected.")
    else
        click(820, 510) -- lobby conquest nav fallback
        log_test("Conquest Nav", "INFO", "Clicked conquest nav area (fallback).")
    end

    wait(3)

    -- ============================================================
    -- STAGE 3: Select Level 2 Node
    -- ============================================================
    set_stage("3. Select Node 2")
    log_test("Select Node", "STARTING", "Selecting Level 2 node on map...")

    local node2 = wait_template("node_level_2", 15)
    if node2 then
        click(node2.x, node2.y)
        log_test("Select Node", "PASS", "Level 2 Node selected.")
    else
        click(340, 270) -- approximate node 2 coordinates
        log_test("Select Node", "INFO", "Clicked node 2 coordinates (fallback).")
    end

    -- wait(2)

    -- Click Engage to open cohort setup
    local engage_btn = wait_template("engage_btn", 8)
    if engage_btn then
        click(engage_btn.x, engage_btn.y)
        log_test("Engage", "PASS", "Clicked Engage button.")
    else
        click(790, 430)
        log_test("Engage", "INFO", "Clicked engage area (fallback).")
    end

    -- wait(2.5)

    -- Click Start Mission
    local start_mission = wait_template("start_mission_btn", 8)
    if start_mission then
        click(start_mission.x, start_mission.y)
        log_test("Start Mission", "PASS", "Mission started.")
    else
        click(720, 450)
        log_test("Start Mission", "INFO", "Clicked start mission (fallback).")
    end

    -- Choose Play Tutorial if prompted
    local play_tut_btn = wait_template("PlayTutorial_Btn", 4)
    if not play_tut_btn then
        play_tut_btn = wait_template("YesButton", 1)
    end
    if play_tut_btn then
        click(play_tut_btn.x, play_tut_btn.y)
        log_test("Choose Tutorial", "PASS", "Selected Play Tutorial mode.")
        wait(2)
    end

    wait(5) -- Wait for battle scene to load

    -- ============================================================
    -- STAGE 4: Battle Combat
    -- ============================================================
    set_stage("4. Battle Combat")
    log_test("Battle Combat", "STARTING", "Starting combat waves...")

    local start_battle = wait_template("start_battle_btn", 15)
    if start_battle then
        click(start_battle.x, start_battle.y)
    end

    -- Deploy Ignis if not already on the board
    local ignis_card = wait_template("ignis_card", 10)
    if ignis_card then
        drag(ignis_card.x, ignis_card.y, 555, 240, 1.0)
        wait(2)
    end

    -- Speed up
    local spd_btn = wait_template("SpeedButton", 4)
    if spd_btn then
        click(spd_btn.x, spd_btn.y) wait(0.5)
        click(spd_btn.x, spd_btn.y) wait(0.5)
    end

    wait(15)

    -- Trigger Ultimate
    click(555, 240)
    -- wait(1.5)
    local ult_btn = wait_template("Ult_Btn", 8)
    if ult_btn then
        click(ult_btn.x, ult_btn.y)
    else
        click(870, 450)
    end

    -- ============================================================
    -- STAGE 5: Victory Screen
    -- ============================================================
    set_stage("5. Victory")
    log_test("Victory", "STARTING", "Waiting for Level 2 victory screen...")

    local next_lvl_btn = wait_template("NextLevelButton", 45)
    if not next_lvl_btn then
        next_lvl_btn = wait_template("victory_next_level", 5)
    end
    if next_lvl_btn then
        click(next_lvl_btn.x, next_lvl_btn.y)
        log_test("Victory", "PASS", "Level 2 cleared and returned.")
    else
        log_test("Victory", "FAIL", "Victory screen not found.")
    end

    wait(4)
    set_stage("Completed")
end

run_tests()
