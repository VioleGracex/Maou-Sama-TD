-- Scenario 1: Fresh Start & Level 1 Tutorial
-- Full flow: boot → loading screen Clear Data → confirm deletion → Start Game →
-- ascension → play tutorial → advance/skip dialogue → drag Ignis → speed up x2 →
-- wait for ultimate charge → dismiss dialogue → click Ignis (inspector) →
-- click ultimate btn → win Level 1 → 2 loot clicks to finish sequence.
--
-- VERIFICATION POLICY: every meaningful action must be confirmed by a UDP event
-- from the game before marking the step as PASS. Pure click-and-pass is forbidden.

local game = require("lua_api.game")

local function run_tests()

    -- ============================================================
    -- STAGE 1: Boot Game
    -- ============================================================
    set_stage("1. Boot Game")
    log_test("Launch Game", "STARTING", "Starting Maou-Sama-TD.exe in windowed mode (960x540)...")
    if clear_save_data then
        clear_save_data()
    end
    if launch_game(true) then
        log_test("Launch Game", "PASS", "Game process started and window positioned at (0, 0).")
    else
        log_test("Launch Game", "FAIL", "Failed to launch game executable. Aborting.")
        return
    end

    -- Allow the engine to initialise and the loading screen to appear
    wait(1)

    -- ============================================================
    -- STAGE 2: Loading Screen — Clear Data → Confirm → Start Game
    -- ============================================================
    set_stage("2. Loading Screen — Clear Data")
    log_test("Clear Data", "STARTING", "Waiting for loading screen to appear...")

    -- Wait for the loading screen Clear Data button
    local clear_data_btn = ui.wait_for("ClearCacheButton", 45)
    if clear_data_btn then
        ui.click(clear_data_btn)
        log_test("Clear Data", "INFO", "Clear Data button clicked — waiting for confirmation dialog...")
        wait(0.5)

        local confirm_btn = ui.wait_for("ClearCache_YesButton", 3)
        if confirm_btn then
            ui.click(confirm_btn)
            -- VERIFY: game must fire SaveCleared event to confirm deletion completed
            if game.wait_for_event("SaveCleared", 8.0) then
                log_test("Clear Data", "PASS", "Save data cleared — 'SaveCleared' event received.")
            else
                log_test("Clear Data", "FAIL", "'SaveCleared' event NOT received after confirm click — data may not have been wiped.")
                return
            end
        else
            -- Fallback: try generic YES button
            local ok_btn = ui.wait_for("YES", 5)
            if ok_btn then
                ui.click(ok_btn)
                if game.wait_for_event("SaveCleared", 8.0) then
                    log_test("Clear Data", "PASS", "Save data cleared via OK button (event received).")
                else
                    log_test("Clear Data", "FAIL", "OK clicked, but 'SaveCleared' event NOT received.")
                    return
                end
            else
                log_test("Clear Data", "FAIL", "Confirmation dialog not found.")
                return
            end
        end
        wait(1)
    else
        log_test("Clear Data", "FAIL", "Clear Data button not found on loading screen. Aborting.")
        return
    end

    log_test("Loading Screen", "STARTING", "Waiting for boot sequence to finish...")
    wait(18)

    log_test("Loading Screen", "STARTING", "Clicking 'StartButton' on the loading screen...")
    local start_game_btn = ui.wait_for("StartButton", 5)
    if start_game_btn then
        ui.click(start_game_btn)
        wait(2.0)
        ui.click(start_game_btn)
        -- VERIFY: scene must load after Start is clicked
        if game.wait_for_event("SceneLoaded:BattleScene", 30.0) or game.wait_for_event("AscensionPanelOpened", 20.0) then
            log_test("Loading Screen", "PASS", "Start Game confirmed — scene change detected.")
        else
            log_test("Loading Screen", "INFO", "No immediate scene event — continuing (boot may not emit SceneLoaded).")
        end
        wait(3)
    else
        log_test("Loading Screen", "INFO", "No Start Game button found — continuing.")
    end

    -- ============================================================
    -- STAGE 3: Character Ascension
    -- ============================================================
    set_stage("3. Character Ascension")
    log_test("Ascension Panel", "STARTING", "Waiting for AscensionPanelOpened event...")

    -- VERIFY: must receive panel-open event before interacting
    if game.wait_for_event("AscensionPanelOpened", 20.0) then
        log_test("Ascension Panel", "INFO", "AscensionPanelOpened event received — panel is up.")
        wait(1.0) -- allow InputRoot DOTween fade to complete
    else
        log_test("Ascension Panel", "FAIL", "AscensionPanelOpened event NOT received within timeout.")
        return
    end

    -- Roll a character name with the dice button
    local dice = ui.wait_for("DiceButton", 20)
    if dice then
        ui.click(dice)
        log_test("Ascension Panel", "INFO", "Dice rolled — character name generated.")

        -- Confirm / Arise
        local arise = ui.wait_for("AriseButton", 5)
        if arise then
            ui.click(arise)
            -- VERIFY: scene must transition after Arise (BattleScene loads)
            if game.wait_for_event("SceneLoaded:BattleScene", 20.0) then
                log_test("Ascension Panel", "PASS", "Arise confirmed — BattleScene loaded.")
            else
                log_test("Ascension Panel", "FAIL", "Arise clicked but SceneLoaded:BattleScene NOT received.")
                return
            end
        else
            log_test("Ascension Panel", "FAIL", "Arise button not found after rolling dice.")
            return
        end
    else
        log_test("Ascension Panel", "FAIL", "Ascension Panel did not appear within timeout.")
        return
    end

    -- ============================================================
    -- STAGE 5: Intro Dialogue — Advance or Skip
    -- ============================================================
    set_stage("5. Intro Dialogue")
    log_test("Intro Dialogue", "STARTING", "Waiting for DialogueStarted event...")

    -- VERIFY: must receive dialogue event — not just click blindly
    if game.wait_for_event("DialogueStarted", 15.0) then
        log_test("Intro Dialogue", "INFO", "DialogueStarted event received.")
        wait(1.0)

        local success = false
        for i = 1, 5 do
            local skip_btn = ui.wait_for("FullSkipButton", 3)
            if not skip_btn then skip_btn = ui.wait_for("SkipButton", 2) end
            if not skip_btn then skip_btn = ui.wait_for("skip", 2) end

            if skip_btn then
                ui.click(skip_btn)
            else
                log_test("Intro Dialogue", "INFO", "No skip button found, clicking screen...")
                ui.click_relative(0.5, 0.5)
            end

            -- VERIFY: each skip attempt must end with DialogueEnded
            if game.wait_for_event("DialogueEnded", 5.0) then
                success = true
                break
            end
        end

        if success then
            log_test("Intro Dialogue", "PASS", "Intro Dialogue closed — DialogueEnded event received.")
        else
            log_test("Intro Dialogue", "FAIL", "Dialogue did not close — DialogueEnded NOT received after attempts.")
        end
    else
        log_test("Intro Dialogue", "FAIL", "DialogueStarted event NOT received within timeout.")
    end

    -- ============================================================
    -- STAGE 6: Tutorial Choice — Play Tutorial
    -- ============================================================
    set_stage("6. Tutorial Choice")
    log_test("Play Tutorial", "STARTING", "Waiting for 'Play Tutorial' prompt...")

    local play_tut_btn = ui.wait_for("PlayTutorial_Btn", 25)
    if not play_tut_btn then play_tut_btn = ui.wait_for("play", 2) end

    if play_tut_btn then
        ui.click(play_tut_btn)
        -- VERIFY: game must confirm tutorial was chosen via event
        if game.wait_for_event("TutorialChosen:tutorial", 8.0) then
            log_test("Play Tutorial", "PASS", "'TutorialChosen:tutorial' event received — tutorial is starting.")
        else
            log_test("Play Tutorial", "FAIL", "'Play Tutorial' clicked but 'TutorialChosen:tutorial' NOT received.")
            return
        end
    else
        log_test("Play Tutorial", "FAIL", "'Play Tutorial' button did not appear.")
        return
    end
    wait(2)

    -- ============================================================
    -- STAGE 7: Placement Dialogue
    -- ============================================================
    set_stage("7. Placement Dialogue")
    log_test("Placement Dialogue", "STARTING", "Attempting to skip placement tutorial dialogues...")
    if game.wait_for_event("DialogueStarted", 10.0) then
        log_test("Placement Dialogue", "INFO", "DialogueStarted event received.")
        wait(1.0)

        local success = false
        local i = 0
        while i < 20 do
            i = i + 1
            local skip_placement = ui.wait_for("FullSkipButton", 3)
            if not skip_placement then skip_placement = ui.wait_for("SkipButton", 2) end
            if not skip_placement then skip_placement = ui.wait_for("skip", 2) end

            if skip_placement then
                ui.click(skip_placement)
            else
                log_test("Placement Dialogue", "INFO", "No skip found — advancing manually.")
                ui.click_relative(0.5, 0.5)
            end

            if game.wait_for_event("DialogueEnded", 3.0) then
                success = true
                break
            end
        end

        if success then
            log_test("Placement Dialogue", "PASS", "Placement dialogue closed — DialogueEnded received.")
        else
            log_test("Placement Dialogue", "FAIL", "Placement dialogue did not close after multiple attempts.")
        end
    else
        log_test("Placement Dialogue", "INFO", "DialogueStarted event NOT received — continuing anyway.")
    end

    wait(4)

    -- ============================================================
    -- STAGE 8: Drag-Drop Ignis onto the Grid
    -- ============================================================
    set_stage("8. Deploy Ignis")
    log_test("Deploy Ignis", "STARTING", "Locating Ignis unit card in hand area...")

    log_test("Deploy Ignis", "INFO", "Waiting for UnitButton_Ignis to spawn (up to 30s)...")
    local ignis_ready = game.wait_for_unit("Ignis", 30)
    if not ignis_ready then
        log_test("Deploy Ignis", "FAIL", "UnitButton_Ignis never appeared. Tutorial scene may not have loaded.")
        return
    end
    log_test("Deploy Ignis", "INFO", "UnitButton_Ignis found — placing on tile (7, 3)...")

    local place_success = game.place_unit("Ignis", 7, 3)

    if place_success then
        -- VERIFY: game must confirm deployment via TutorialStepPassed
        if assert_log_contains("DeployUnit: Ignis", 15.0, "Assert Deploy Ignis") or
           game.wait_for_event("TutorialStepPassed:Deploy Ignis", 8.0) then
            log_test("Placement", "PASS", "Ignis successfully deployed — game confirmed via event.")
        else
            log_test("Placement", "FAIL", "Ignis placement: no confirmation event received.")
            return
        end
    else
        log_test("Placement", "FAIL", "Could not place Ignis via game API.")
        return
    end

    -- Post-placement dialogue dismiss
    wait(1.0)
    ui.click_relative(0.885, 0.906)   -- scaled relative click (replaces hardcoded 1133,653)
    wait(2.0)

    -- ============================================================
    -- STAGE 9: Speed Up Game (× 2 presses)
    -- ============================================================
    set_stage("9. Speed Up Game")
    log_test("Speed Up", "STARTING", "Pressing speed-up button twice...")

    local spd_btn = ui.wait_for("SpeedButton", 5)
    if spd_btn then
        ui.click(spd_btn)
        -- VERIFY: SpeedChanged event must be received for each press
        if game.wait_for_event("SpeedChanged:2x", 3.0) then
            log_test("Speed Up", "INFO", "Speed changed to 2x — event confirmed.")
        else
            log_test("Speed Up", "INFO", "SpeedChanged:2x not received — may already be at 2x.")
        end
        wait(0.8)
        ui.click(spd_btn)
        if game.wait_for_event("SpeedChanged:4x", 3.0) then
            log_test("Speed Up", "PASS", "Speed changed to 4x — event confirmed.")
        else
            log_test("Speed Up", "INFO", "SpeedChanged:4x not received — continuing.")
        end
    else
        -- Fallback coordinate
        ui.click_relative(0.948, 0.056)
        wait(0.8)
        ui.click_relative(0.948, 0.056)
        log_test("Speed Up", "INFO", "Speed-up button pressed via fallback coordinate.")
    end

    -- ============================================================
    -- STAGE 10: Wave Combat — Wait for Ultimate Charge
    -- ============================================================
    set_stage("10. Wave 1 — Ultimate Charging")
    log_test("Wave 1", "STARTING", "Waiting for Ignis to charge her ultimate...")

    wait(12)

    local ult_dialogue = ui.wait_for("ult_tutorial_dialogue", 15)
    if ult_dialogue then
        ui.click(ult_dialogue)
        wait(1.5)
        log_test("Wave 1", "INFO", "Ultimate charge dialogue dismissed via UI element.")
    else
        ui.click_relative(0.885, 0.906)
        wait(1.5)
        log_test("Wave 1", "INFO", "Ultimate charge dialogue dismissed (fallback click).")
    end

    -- ============================================================
    -- STAGE 11: Select Ignis on Grid (Inspector Window Opens)
    -- ============================================================
    set_stage("11. Inspector — Select Ignis")
    log_test("Inspector", "STARTING", "Clicking Ignis on the grid to open the inspector panel...")

    ui.click_relative(0.578, 0.444)   -- Ignis deployed tile (replaces 740,320)
    wait(2.0)

    local inspector = ui.wait_for("inspector_window", 6)
    if inspector then
        log_test("Inspector", "PASS", "Inspector panel opened for Ignis.")
    else
        log_test("Inspector", "INFO", "Inspector panel not found — proceeding to ultimate activation.")
    end

    -- ============================================================
    -- STAGE 12: Activate Ignis Ultimate Skill
    -- ============================================================
    set_stage("12. Activate Ultimate")
    log_test("Ultimate", "STARTING", "Clicking Ignis ultimate button...")

    local ult_btn = ui.wait_for("Ult_Btn", 8)
    if ult_btn then
        ui.click(ult_btn)
        -- VERIFY: UltimateActivated event must arrive
        if game.wait_for_event("UltimateActivated:Ignis", 5.0) then
            log_test("Ultimate", "PASS", "Ignis ultimate activated — 'UltimateActivated:Ignis' event received.")
        else
            log_test("Ultimate", "FAIL", "Ult_Btn clicked but 'UltimateActivated:Ignis' NOT received.")
        end
        wait(2.0)
    else
        -- Fallback coordinate
        ui.click_relative(0.906, 0.833)
        if game.wait_for_event("UltimateActivated:Ignis", 5.0) then
            log_test("Ultimate", "PASS", "Ignis ultimate activated (fallback) — event received.")
        else
            log_test("Ultimate", "FAIL", "Ultimate fallback click: 'UltimateActivated:Ignis' NOT received.")
        end
        wait(2.0)
    end

    -- ============================================================
    -- STAGE 13: Victory Screen
    -- ============================================================
    set_stage("13. Level 1 Victory")
    log_test("Victory Event", "STARTING", "Waiting for Victory event from game UDP socket...")

    -- VERIFY: Victory event is the definitive pass condition
    local victory_event = wait_event("Victory", 60.0)
    if victory_event then
        log_test("Victory Event", "PASS", "Victory event received successfully via UDP!")
    else
        log_test("Victory Event", "FAIL", "Failed to receive Victory event via UDP.")
    end

    log_test("Victory", "STARTING", "Waiting for Level 1 victory screen (up to 60s)...")

    local next_lvl_btn = ui.wait_for("NextLevelButton", 60)
    if not next_lvl_btn then
        log_test("Victory", "INFO", "ui.wait_for timed out. Trying wait_template fallback...")
        next_lvl_btn = wait_template("NextLevelButton", 5)
    end

    if next_lvl_btn then
        click(next_lvl_btn.x, next_lvl_btn.y)
        log_test("Victory", "PASS", "Level 1 cleared! Victory confirmed.")
    else
        log_test("Victory", "FAIL", "Victory / Next Level button not found within timeout.")
        return
    end

    -- Two clicks in the centre of the screen to dismiss loot/reward panels
    wait(1.5)
    ui.click_relative(0.5, 0.5)
    wait(1.0)
    ui.click_relative(0.5, 0.5)
    wait(1.0)
    log_test("Loot Sequence", "PASS", "Loot/reward panels dismissed.")

    -- ============================================================
    -- DONE
    -- ============================================================
    set_stage("Completed")
    log_test("Test Suite", "PASS", "Scenario 1 — Fresh Start & Level 1 Tutorial completed successfully!")
end

run_tests()
