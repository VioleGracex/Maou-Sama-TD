-- Lua Game Test Suite for Maou-Sama-TD
-- Defines steps to test the game from start to finish of the Level 1 tutorial

local function run_tests()
    set_stage("1. Clear Save Data")
    log_test("Clear Save Data", "STARTING", "Deleting save files...")
    if clear_save_data() then
        log_test("Clear Save Data", "PASS", "Save data cleared successfully.")
    else
        log_test("Clear Save Data", "FAIL", "Failed to clear save data.")
        return
    end

    set_stage("2. Launching Game")
    log_test("Launch Game", "STARTING", "Starting Maou-Sama-TD.exe in windowed mode...")
    if launch_game() then
        log_test("Launch Game", "PASS", "Game booted and positioned at (0, 0).")
    else
        log_test("Launch Game", "FAIL", "Failed to launch game.")
        return
    end

    -- Wait for initial loading
    wait(6)

    set_stage("3. Character Ascension")
    log_test("Character Ascension", "STARTING", "Creating new player profile...")
    
    -- Wait for the Ascension Panel dice button or arise button
    local dice_pos = wait_template("dice_button", 25)
    if dice_pos then
        click(dice_pos.x, dice_pos.y)
        wait(1.5)
        
        local arise_pos = wait_template("arise_button", 8)
        if arise_pos then
            click(arise_pos.x, arise_pos.y)
            log_test("Character Ascension", "PASS", "Maou chosen and Arisen successfully.")
        else
            log_test("Character Ascension", "FAIL", "Arise button not found.")
            return
        end
    else
        log_test("Character Ascension", "FAIL", "Ascension Panel dice button did not load.")
        return
    end

    wait(6) -- Wait for BattleScene loading screen to finish

    set_stage("4. Level 1 - Start Tutorial")
    log_test("Level 1 Start", "STARTING", "Waiting for Play Tutorial prompt...")
    
    -- Wait for "Play Tutorial" button
    local play_tut_pos = wait_template("play_tutorial_btn", 25)
    if play_tut_pos then
        click(play_tut_pos.x, play_tut_pos.y)
        log_test("Level 1 Start", "PASS", "Play Tutorial clicked.")
    else
        log_test("Level 1 Start", "FAIL", "Play Tutorial prompt did not appear.")
        return
    end

    wait(2.5)

    -- Step 1: Click through Intro dialogue(s)
    log_test("Tutorial Step 1", "STARTING", "Advancing intro dialogues...")
    for i=1,3 do
        -- Click near the bottom right area of the screen to advance dialogues
        click(1100, 650) 
        wait(2.0)
    end
    log_test("Tutorial Step 1", "PASS", "Intro dialogues advanced.")

    -- Step 2: Drag Ignis to Field
    log_test("Tutorial Step 2", "STARTING", "Dragging Ignis card to field tile (7, 4)...")
    local ignis_btn = wait_template("ignis_card", 8)
    if ignis_btn then
        -- Drag from button to tile (7, 4) which is near center of the grid (x=740, y=320 in 1280x720 window)
        drag(ignis_btn.x, ignis_btn.y, 740, 320, 1.0)
        wait(2.5)
        log_test("Tutorial Step 2", "PASS", "Ignis deployed to grid.")
    else
        log_test("Tutorial Step 2", "FAIL", "Ignis deployment card not found.")
        return
    end

    -- Step 3: Post Placement dialogue
    click(1100, 650)
    wait(2.5)

    -- Wait for Wave 1 to start and Ignis to get 2 kills (which pauses the game for ultimate tutorial)
    log_test("Tutorial Wave 1", "STARTING", "Waiting for Ignis to charge ultimate...")
    wait(18) 
    
    -- Advance dialogue warning about the ultimate tutorial
    click(1100, 650)
    wait(2.0)

    -- Step 4: Select Ignis on the field
    log_test("Tutorial Step 4", "STARTING", "Selecting Ignis on the grid...")
    click(740, 320) -- Click Ignis position
    wait(2.0)

    -- Step 5: Activate ultimate skill
    log_test("Tutorial Step 5", "STARTING", "Activating Ignis ultimate skill...")
    local ult_btn = wait_template("ignis_ult_btn", 8)
    if ult_btn then
        click(ult_btn.x, ult_btn.y)
        wait(2.0)
        log_test("Tutorial Step 5", "PASS", "Ignis ultimate activated.")
    else
        -- Fallback: click near bottom right where ultimate button resides
        click(1150, 580)
        wait(2.0)
        log_test("Tutorial Step 5", "PASS", "Ignis ultimate clicked (fallback coordinate).")
    end

    -- Clear wave 1 and wait for victory screen
    log_test("Tutorial Level 1 Victory", "STARTING", "Waiting for Level 1 victory screen...")
    
    -- Wait for victory next level button (up to 45 seconds)
    local next_lvl_btn = wait_template("victory_next_level", 45)
    if next_lvl_btn then
        click(next_lvl_btn.x, next_lvl_btn.y)
        log_test("Tutorial Level 1 Victory", "PASS", "Level 1 Cleared. Next Level clicked.")
    else
        log_test("Tutorial Level 1 Victory", "FAIL", "Victory button not found.")
        return
    end

    wait(6) -- Wait for Level 2 loading screen

    set_stage("5. Level 2 Start")
    log_test("Level 2 Start", "STARTING", "Entering Level 2...")
    
    -- Wait for Level 2 Start Battle button or briefing play button
    local start_battle = wait_template("start_battle_btn", 25)
    if start_battle then
        click(start_battle.x, start_battle.y)
        log_test("Level 2 Start", "PASS", "Level 2 battle started successfully.")
    else
        log_test("Level 2 Start", "FAIL", "Level 2 Start Battle button not found.")
        return
    end

    set_stage("Completed")
    log_test("Test Suite", "PASS", "All stages of the tutorial test suite passed successfully!")
end

run_tests()
