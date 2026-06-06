-- Scenario 4: Play Myself (Free Play: Level 1 & Level 2)
-- Full flow: boot → loading screen Clear Cache → confirm deletion → Start Game →
-- ascension → intro dialogue → choose Play Myself → deploy Ignis → speed up x2 →
-- wait for Level 1 victory → load Level 2 → Start Level 2 → speed up x2 → wait for Level 2 victory.

local function run_tests()

    -- ============================================================
    -- STAGE 1: Boot Game
    -- ============================================================
    set_stage("1. Boot Game")
    log_test("Launch Game", "STARTING", "Starting Maou-Sama-TD.exe in windowed mode (960x540)...")
    if launch_game(true) then
        log_test("Launch Game", "PASS", "Game process started and window positioned at (0, 0).")
    else
        log_test("Launch Game", "FAIL", "Failed to launch game executable. Aborting.")
        return
    end

    -- Allow the engine to initialise and the loading screen to appear
    wait(5)

    -- ============================================================
    -- STAGE 2: Loading Screen — Clear Cache → Confirm → Start Game
    -- ============================================================
    set_stage("2. Loading Screen — Clear Cache")
    log_test("Clear Cache", "STARTING", "Waiting for loading screen to appear...")

    -- Wait for the loading screen Clear Data/Cache button
    local clear_data_btn = wait_template("ClearCacheButton", 20)
    if clear_data_btn then
        click(clear_data_btn.x, clear_data_btn.y)
        log_test("Clear Cache", "INFO", "Clear Cache button clicked — waiting for confirmation dialog...")
        -- wait(1.5)

        -- Confirm deletion
        local confirm_btn = wait_template("ClearCache_YesButton", 10)
        if confirm_btn then
            click(confirm_btn.x, confirm_btn.y)
            log_test("Clear Cache", "PASS", "Save data cleared via in-game confirmation dialog.")
        else
            local ok_btn = wait_template("ClearCache_YesButton", 6)
            if ok_btn then
                click(ok_btn.x, ok_btn.y)
                log_test("Clear Cache", "PASS", "Save data cleared via OK button (fallback template).")
            else
                log_test("Clear Cache", "FAIL", "Confirmation dialog not found — save data may not have been cleared.")
                return
            end
        end
        wait(2)
    else
        log_test("Clear Cache", "FAIL", "Clear Cache button not found on loading screen. Aborting.")
        return
    end

    -- Click Start Game
    log_test("Loading Screen", "STARTING", "Clicking 'StartButton' on the loading screen...")
    local start_game_btn = wait_template("StartButton", 15)
    if start_game_btn then
        click(start_game_btn.x, start_game_btn.y)
        log_test("Loading Screen", "PASS", "Start Game clicked on loading screen.")
        wait(3)
    else
        log_test("Loading Screen", "INFO", "No Start Game button found — continuing.")
    end

    -- ============================================================
    -- STAGE 3: Character Ascension
    -- ============================================================
    set_stage("3. Character Ascension")
    log_test("Ascension Panel", "STARTING", "Waiting for Ascension Panel to appear...")

    local dice_pos = wait_template("DiceButton", 25)
    if dice_pos then
        click(dice_pos.x, dice_pos.y)
        wait(1.5)
        log_test("Ascension Panel", "INFO", "Dice rolled — character name generated.")

        local arise_pos = wait_template("AriseButton", 10)
        if arise_pos then
            click(arise_pos.x, arise_pos.y)
            log_test("Ascension Panel", "PASS", "Arise confirmed — entering game world.")
        else
            log_test("Ascension Panel", "FAIL", "Arise button not found after rolling dice.")
            return
        end
    else
        log_test("Ascension Panel", "FAIL", "Ascension Panel did not appear within timeout.")
        return
    end

    -- Wait for Battle Scene / Tutorial loading screen to finish
    wait(7)

    -- ============================================================
    -- STAGE 5: Intro Dialogue — Advance or Skip
    -- ============================================================
    set_stage("5. Intro Dialogue")
    log_test("Intro Dialogue", "STARTING", "Attempting to skip or advance tutorial intro dialogues...")

    local skip_btn = wait_template("FullSkipButton", 6)
    if not skip_btn then
        skip_btn = wait_template("SkipButton", 3)
    end
    if skip_btn then
        click(skip_btn.x, skip_btn.y)
        wait(2)
        log_test("Intro Dialogue", "PASS", "Dialogue skipped via Skip button.")
    else
        log_test("Intro Dialogue", "INFO", "No Skip button found — advancing dialogues manually.")
        for i = 1, 3 do
            click(850, 490)
            wait(2.0)
        end
        log_test("Intro Dialogue", "PASS", "Intro dialogues advanced manually.")
    end

    -- ============================================================
    -- STAGE 6: Tutorial Choice — Play Myself
    -- ============================================================
    set_stage("6. Tutorial Choice")
    log_test("Play Myself", "STARTING", "Waiting for 'Play Myself' prompt...")

    local play_myself_btn = wait_template("PlayMyself_Btn", 25)
    if not play_myself_btn then
        play_myself_btn = wait_template("NoButton", 5)
    end
    if play_myself_btn then
        click(play_myself_btn.x, play_myself_btn.y)
        log_test("Play Myself", "PASS", "'Play Myself' clicked. Custom game starting.")
    else
        log_test("Play Myself", "FAIL", "'Play Myself' button did not appear.")
        return
    end

    wait(2.5)

    -- ============================================================
    -- STAGE 8: Drag-Drop Ignis onto the Grid
    -- ============================================================
    set_stage("8. Deploy Ignis")
    log_test("Deploy Ignis", "STARTING", "Locating Ignis unit card in hand area...")

    local ignis_card = wait_template("ignis_card", 12)
    if ignis_card then
        drag(ignis_card.x, ignis_card.y, 555, 240, 1.0)
        wait(2.5)
        log_test("Deploy Ignis", "PASS", "Ignis dragged and deployed to grid.")
    else
        log_test("Deploy Ignis", "FAIL", "Ignis card not found in hand. Aborting.")
        return
    end

    -- Post-placement dialogue skip / click
    wait(1.0)
    click(850, 490)
    wait(1.5)

    -- ============================================================
    -- STAGE 9: Speed Up Game (× 2 presses)
    -- ============================================================
    set_stage("9. Speed Up Game")
    log_test("Speed Up", "STARTING", "Pressing speed-up button twice to increase game speed...")

    local spd_btn = wait_template("SpeedButton", 5)
    if spd_btn then
        click(spd_btn.x, spd_btn.y)
        wait(0.8)
        click(spd_btn.x, spd_btn.y)
        wait(0.5)
        log_test("Speed Up", "PASS", "Speed-up button pressed twice (template match).")
    else
        click(910, 30)
        wait(0.8)
        click(910, 30)
        wait(0.5)
        log_test("Speed Up", "PASS", "Speed-up button pressed twice (fallback coordinate).")
    end

    -- ============================================================
    -- STAGE 10: Wave 1 — Combat (wait for victory)
    -- ============================================================
    set_stage("10. Wave 1 — Combat")
    log_test("Wave 1", "STARTING", "Running wave combat at 2x speed — waiting for Level 1 victory...")

    -- ============================================================
    -- STAGE 13: Victory Screen
    -- ============================================================
    set_stage("13. Level 1 Victory")
    log_test("Victory", "STARTING", "Waiting for Level 1 victory screen...")

    local next_lvl_btn = wait_template("NextLevelButton", 60)
    if not next_lvl_btn then
        next_lvl_btn = wait_template("victory_next_level", 10)
    end
    if next_lvl_btn then
        click(next_lvl_btn.x, next_lvl_btn.y)
        log_test("Victory", "PASS", "Level 1 cleared! 'Next Level' clicked.")
    else
        log_test("Victory", "FAIL", "Victory / Next Level button not found within timeout.")
        return
    end

    wait(7)

    -- ============================================================
    -- STAGE 14: Level 2 — Start Battle
    -- ============================================================
    set_stage("14. Level 2 Start")
    log_test("Level 2 Start", "STARTING", "Waiting for Level 2 battle start screen...")

    local start_battle = wait_template("start_battle_btn", 25)
    if start_battle then
        click(start_battle.x, start_battle.y)
        log_test("Level 2 Start", "PASS", "Level 2 battle started successfully.")
    else
        log_test("Level 2 Start", "FAIL", "Level 2 Start Battle button not found.")
        return
    end

    -- ============================================================
    -- STAGE 15: Level 2 Combat (wait for victory)
    -- ============================================================
    set_stage("15. Level 2 Combat")
    log_test("Level 2 Combat", "STARTING", "Running Level 2 combat at 2x speed — waiting for victory...")

    -- ============================================================
    -- STAGE 16: Level 2 Victory Screen
    -- ============================================================
    set_stage("16. Level 2 Victory")
    log_test("Level 2 Victory", "STARTING", "Waiting for Level 2 victory screen...")

    local next_lvl_btn2 = wait_template("NextLevelButton", 60)
    if not next_lvl_btn2 then
        next_lvl_btn2 = wait_template("victory_next_level", 10)
    end
    if next_lvl_btn2 then
        click(next_lvl_btn2.x, next_lvl_btn2.y)
        log_test("Level 2 Victory", "PASS", "Level 2 cleared! Next Level/Confirm clicked.")
    else
        log_test("Level 2 Victory", "FAIL", "Level 2 victory screen not found within timeout.")
        return
    end

    wait(5)

    -- ============================================================
    -- DONE
    -- ============================================================
    set_stage("Completed")
    log_test("Test Suite", "PASS", "Scenario 4 — Play Myself (Level 1 & 2) completed successfully!")
end

run_tests()
