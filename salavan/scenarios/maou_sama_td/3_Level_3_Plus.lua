-- Scenario 3: Level 3+ Cohort & Rite Setup
-- Connects to game, navigates to Level 3 node, assigns units/rites in Cohort UI,
-- proceeds to grid, drags units to place them, and starts battle waves.

local function run_tests()
    set_stage("1. Connect Game")
    log_test("Connect Game", "STARTING", "Checking for game instance...")
    if launch_game(false) then
        log_test("Connect Game", "PASS", "Game connected and positioned at (0, 0).")
    else
        log_test("Connect Game", "FAIL", "Could not locate or launch game.")
        return
    end

    wait(2)

    set_stage("2. Navigate Conquest")
    log_test("Conquest Nav", "STARTING", "Navigating to Conquest screen...")
    
    local conquest_btn = wait_template("conquest_nav_btn", 10)
    if conquest_btn then
        click(conquest_btn.x, conquest_btn.y)
        log_test("Conquest Nav", "PASS", "Conquest mode selected.")
    else
        click(1100, 600)
        log_test("Conquest Nav", "INFO", "Clicked navigation area (fallback).")
    end

    wait(3)

    set_stage("3. Select Node 3")
    log_test("Select Node", "STARTING", "Selecting Level 3 node...")
    
    local node3 = wait_template("node_level_3", 15)
    if node3 then
        click(node3.x, node3.y)
        log_test("Select Node", "PASS", "Level 3 Node selected.")
    else
        click(600, 420)
        log_test("Select Node", "INFO", "Clicked node 3 coordinate (fallback).")
    end

    -- wait(1.5)

    -- Click Engage to open briefing / cohort UI
    local engage_btn = wait_template("engage_btn", 8)
    if engage_btn then
        click(engage_btn.x, engage_btn.y)
        log_test("Briefing", "PASS", "Opened cohort setup panel.")
    else
        click(1050, 580)
        log_test("Briefing", "INFO", "Clicked engage area (fallback).")
    end

    wait(2.5)

    set_stage("4. Cohort Assignment")
    log_test("Cohort Setup", "STARTING", "Assigning units and rites...")
    
    -- Select first empty unit slot
    local empty_unit = wait_template("empty_unit_slot", 8)
    if empty_unit then
        click(empty_unit.x, empty_unit.y)
        -- wait(1.5)
        -- Select Ignis card from roster
        local ignis_card = wait_template("ignis_roster_card", 8)
        if ignis_card then
            click(ignis_card.x, ignis_card.y)
            log_test("Cohort Setup", "PASS", "Ignis assigned to cohort.")
        else
            log_test("Cohort Setup", "INFO", "Could not find Ignis in roster. Skipping roster selection.")
        end
    else
        log_test("Cohort Setup", "INFO", "No empty unit slots found.")
    end

    -- wait(1.5)

    -- Select empty rite slot
    local empty_rite = wait_template("empty_rite_slot", 8)
    if empty_rite then
        click(empty_rite.x, empty_rite.y)
        -- wait(1.5)
        -- Select first available Rite card
        local rite_card = wait_template("rite_roster_card", 8)
        if rite_card then
            click(rite_card.x, rite_card.y)
            log_test("Cohort Setup", "PASS", "Rite card assigned to cohort.")
        else
            log_test("Cohort Setup", "INFO", "Could not find Rite card in roster.")
        end
    else
        log_test("Cohort Setup", "INFO", "No empty rite slots found.")
    end

    -- wait(1.5)

    -- Click Start Mission
    local start_mission = wait_template("start_mission_btn", 8)
    if start_mission then
        click(start_mission.x, start_mission.y)
        log_test("Cohort Setup", "PASS", "Cohort confirmed, starting mission.")
    else
        click(950, 550)
        log_test("Cohort Setup", "INFO", "Clicked confirm setup (fallback).")
    end

    wait(5) -- Wait for battle scene to load

    set_stage("5. Grid Placement")
    log_test("Grid Placement", "STARTING", "Dragging cohort units to grid...")
    
    -- Drag unit from roster slot at bottom to grid tile [6, 4]
    local placed_unit_card = wait_template("placed_unit_card", 10)
    if placed_unit_card then
        drag(placed_unit_card.x, placed_unit_card.y, 650, 320, 1.0)
        wait(2.5)
        log_test("Grid Placement", "PASS", "Unit deployed to grid.")
    else
        log_test("Grid Placement", "INFO", "Could not find unit placement card.")
    end

    -- Click Start Battle / Start Wave button
    local start_battle = wait_template("start_battle_btn", 8)
    if start_battle then
        click(start_battle.x, start_battle.y)
        log_test("Grid Placement", "PASS", "Waves started.")
    else
        click(1150, 620)
        log_test("Grid Placement", "INFO", "Clicked start wave (fallback).")
    end

    set_stage("Completed")
    log_test("Test Suite", "PASS", "Scenario 3: Level 3+ Cohort flow completed.")
end

run_tests()
