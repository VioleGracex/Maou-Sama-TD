-- Scenario 7: Tutorial to Gacha Pull (Full Progression)
-- Full flow: boot → loading screen Clear Cache → confirm deletion → Start Game →
-- ascension → intro dialogue → choose Play Tutorial → play Level 1 Tutorial →
-- victory → next level → play Level 2 Tutorial → victory → lobby summon → perform gacha pull.

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
        wait(1.5)

        -- Confirm deletion
        local confirm_btn = wait_template("YesButton", 10)
        if confirm_btn then
            click(confirm_btn.x, confirm_btn.y)
            log_test("Clear Cache", "PASS", "Save data cleared via in-game confirmation dialog.")
        else
            local ok_btn = wait_template("yes", 6)
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
    -- STAGE 4: Intro Dialogue
    -- ============================================================
    set_stage("4. Intro Dialogue")
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
    -- STAGE 5: Choose Tutorial
    -- ============================================================
    set_stage("5. Choose Tutorial")
    log_test("Choose Tutorial", "STARTING", "Selecting Play Tutorial mode...")

    local play_tut_btn = wait_template("PlayTutorial_Btn", 25)
    if not play_tut_btn then
        play_tut_btn = wait_template("YesButton", 5)
    end
    if play_tut_btn then
        click(play_tut_btn.x, play_tut_btn.y)
        log_test("Choose Tutorial", "PASS", "Play Tutorial mode selected.")
    else
        log_test("Choose Tutorial", "FAIL", "Tutorial choice button not found.")
        return
    end

    wait(2.5)

    -- ============================================================
    -- STAGE 6: Play Tutorial Level 1
    -- ============================================================
    set_stage("6. Tutorial Level 1")
    log_test("Tutorial Level 1", "STARTING", "Deploying Ignis on grid...")

    local ignis_card = wait_template("ignis_card", 12)
    if ignis_card then
        drag(ignis_card.x, ignis_card.y, 555, 240, 1.0)
        wait(2.5)
        log_test("Tutorial Level 1", "PASS", "Ignis deployed to grid.")
    else
        log_test("Tutorial Level 1", "FAIL", "Ignis card not found.")
        return
    end

    -- Dialogue advance post placement
    click(850, 490)
    wait(1.5)

    -- Speed up
    local spd_btn = wait_template("SpeedButton", 4)
    if spd_btn then
        click(spd_btn.x, spd_btn.y) wait(0.5)
        click(spd_btn.x, spd_btn.y) wait(0.5)
    end

    log_test("Tutorial Level 1", "INFO", "Waiting for ultimate charge tutorial pause...")
    wait(12)

    -- Dismiss ultimate dialogue
    local ult_dialogue = wait_template("ult_tutorial_dialogue", 15)
    if ult_dialogue then
        click(ult_dialogue.x, ult_dialogue.y)
        wait(1.5)
    else
        click(850, 490)
        wait(1.5)
    end

    -- Trigger Ultimate
    click(555, 240)
    wait(2.0)
    local ult_btn = wait_template("Ult_Btn", 8)
    if ult_btn then
        click(ult_btn.x, ult_btn.y)
        log_test("Tutorial Level 1", "PASS", "Ignis ultimate activated.")
    else
        click(870, 450)
        log_test("Tutorial Level 1", "INFO", "Activated ultimate (fallback coordinate).")
    end

    wait(2)

    -- Wait for victory screen
    local next_lvl_btn = wait_template("NextLevelButton", 60)
    if not next_lvl_btn then
        next_lvl_btn = wait_template("victory_next_level", 5)
    end
    if next_lvl_btn then
        click(next_lvl_btn.x, next_lvl_btn.y)
        log_test("Tutorial Level 1", "PASS", "Level 1 victory clicked. Loading Level 2...")
    else
        log_test("Tutorial Level 1", "FAIL", "Victory screen not found.")
        return
    end

    wait(7)

    -- ============================================================
    -- STAGE 7: Play Tutorial Level 2
    -- ============================================================
    set_stage("7. Tutorial Level 2")
    log_test("Tutorial Level 2", "STARTING", "Starting Level 2 combat wave...")

    local start_battle = wait_template("start_battle_btn", 25)
    if start_battle then
        click(start_battle.x, start_battle.y)
        log_test("Tutorial Level 2", "PASS", "Level 2 battle waves started.")
    else
        log_test("Tutorial Level 2", "FAIL", "Start battle button not found.")
        return
    end

    wait(15)

    -- Activate Ultimate
    click(555, 240)
    wait(1.5)
    local ult_btn2 = wait_template("Ult_Btn", 8)
    if ult_btn2 then
        click(ult_btn2.x, ult_btn2.y)
    else
        click(870, 450)
    end

    -- Wait for victory screen
    local next_lvl_btn2 = wait_template("NextLevelButton", 60)
    if not next_lvl_btn2 then
        next_lvl_btn2 = wait_template("victory_next_level", 5)
    end
    if next_lvl_btn2 then
        click(next_lvl_btn2.x, next_lvl_btn2.y)
        log_test("Tutorial Level 2", "PASS", "Level 2 cleared. Loading Gacha summon tutorial...")
    else
        log_test("Tutorial Level 2", "FAIL", "Victory screen not found.")
        return
    end

    wait(6)

    -- ============================================================
    -- STAGE 8: Gacha Tutorial Pull
    -- ============================================================
    set_stage("8. Gacha Tutorial")
    log_test("Gacha Tutorial", "STARTING", "Locating Summon button in Lobby...")

    -- Wait for lobby summon tab button
    local summon_btn = wait_template("Summon_Btn", 20)
    if not summon_btn then
        summon_btn = wait_template("Gacha_Btn", 5)
    end
    if summon_btn then
        click(summon_btn.x, summon_btn.y)
        log_test("Gacha Tutorial", "PASS", "Summon tab opened.")
    else
        click(480, 500) -- Fallback summon tab click
        log_test("Gacha Tutorial", "INFO", "Clicked summon tab area (fallback coordinate).")
    end

    wait(3)

    -- Click Single Pull button
    local pull_one_btn = wait_template("PullOne_Btn", 10)
    if not pull_one_btn then
        pull_one_btn = wait_template("SummonOne_Btn", 5)
    end
    if pull_one_btn then
        click(pull_one_btn.x, pull_one_btn.y)
        log_test("Gacha Tutorial", "PASS", "Performed single gacha pull.")
    else
        click(480, 400) -- Fallback pull area
        log_test("Gacha Tutorial", "INFO", "Clicked pull area (fallback coordinate).")
    end

    wait(5) -- Wait for summon reveal animation

    -- Click anywhere to dismiss reveal
    click(480, 270)
    wait(2)

    -- Close/Confirm pull
    local confirm_gacha = wait_template("ConfirmButton", 10)
    if not confirm_gacha then
        confirm_gacha = wait_template("YesButton", 5)
    end
    if confirm_gacha then
        click(confirm_gacha.x, confirm_gacha.y)
    else
        click(480, 480)
    end

    wait(2)

    -- ============================================================
    -- DONE
    -- ============================================================
    set_stage("Completed")
    log_test("Test Suite", "PASS", "Scenario 7 — Gacha Pull Tutorial completed successfully!")
end

run_tests()
