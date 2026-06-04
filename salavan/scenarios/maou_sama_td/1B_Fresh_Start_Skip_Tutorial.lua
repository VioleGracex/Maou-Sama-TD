-- Scenario 1B: Fresh Start & Skip Tutorial
-- Full flow: boot → loading screen Clear Data → confirm deletion → Start Game →
-- ascension → skip tutorial → skip dialogue → Level 2 start.

local function run_tests()

    -- ============================================================
    -- STAGE 1: Boot Game
    -- ============================================================
    set_stage("1. Boot Game")
    log_test("Launch Game", "STARTING", "Starting Maou-Sama-TD.exe in windowed mode (960x540)...")
    if launch_game() then
        log_test("Launch Game", "PASS", "Game process started and window positioned at (0, 0).")
    else
        log_test("Launch Game", "FAIL", "Failed to launch game executable. Aborting.")
        return
    end

    -- Allow the engine to initialise and the loading screen to appear
    wait(5)

    -- ============================================================
    -- STAGE 2: Loading Screen — Clear Data → Confirm → Start Game
    -- ============================================================
    set_stage("2. Loading Screen — Clear Data")
    log_test("Clear Data", "STARTING", "Waiting for loading screen to appear...")

    -- Wait for the loading screen Clear Data button
    -- Unity GameObject name: ClearCacheButton  →  template key: "ClearCacheButton"
    local clear_data_btn = wait_template("ClearCacheButton", 20)
    if clear_data_btn then
        click(clear_data_btn.x, clear_data_btn.y)
        log_test("Clear Data", "INFO", "Clear Data button clicked — waiting for confirmation dialog...")
        wait(1.5)

        -- Confirm deletion: Unity GameObject name: YesButton (inside ConfirmPopup_Root)
        local confirm_btn = wait_template("YesButton", 10)
        if confirm_btn then
            click(confirm_btn.x, confirm_btn.y)
            log_test("Clear Data", "PASS", "Save data cleared via in-game confirmation dialog.")
        else
            -- Fallback: NoButton sibling is also inside ConfirmPopup_Root, try "yes" label match
            local ok_btn = wait_template("yes", 6)
            if ok_btn then
                click(ok_btn.x, ok_btn.y)
                log_test("Clear Data", "PASS", "Save data cleared via OK button (fallback template).")
            else
                log_test("Clear Data", "FAIL", "Confirmation dialog not found — save data may not have been cleared.")
                return
            end
        end
        wait(2)
    else
        log_test("Clear Data", "FAIL", "Clear Data button not found on loading screen. Aborting.")
        return
    end

    -- Now click Start Game: Unity GameObject name: StartButton
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

    -- Roll a character name with the dice button
    -- Unity GO: DiceButton (AscensionPanel.prefab)
    local dice_pos = wait_template("DiceButton", 25)
    if dice_pos then
        click(dice_pos.x, dice_pos.y)
        wait(1.5)
        log_test("Ascension Panel", "INFO", "Dice rolled — character name generated.")

        -- Confirm / Arise  — Unity GO: AriseButton (AscensionPanel.prefab)
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

    -- Try FullSkipButton first (dialogue skip), then SkipButton (mini skip)
    -- Unity GOs: FullSkipButton, SkipButton (BattleScene)
    local skip_btn = wait_template("FullSkipButton", 6)
    if not skip_btn then
        skip_btn = wait_template("SkipButton", 3)
    end
    if skip_btn then
        click(skip_btn.x, skip_btn.y)
        wait(2)
        log_test("Intro Dialogue", "PASS", "Dialogue skipped via Skip button.")
    else
        -- No skip available — advance manually (3 dialogue panels with 2s gaps)
        log_test("Intro Dialogue", "INFO", "No Skip button found — advancing dialogues manually.")
        for i = 1, 3 do
            click(850, 490)   -- bottom-right dialogue advance area (960x540 coords)
            wait(2.0)
        end
        log_test("Intro Dialogue", "PASS", "Intro dialogues advanced manually.")
    end

    -- ============================================================
    -- STAGE 6: Tutorial Choice — Skip Tutorial
    -- ============================================================
    set_stage("6. Tutorial Choice")
    log_test("Skip Tutorial", "STARTING", "Waiting for tutorial choice prompt...")

    -- Unity GO: SkipTutorial_Btn (inside tutorial choice popup) or NoButton fallback
    local skip_tut_btn = wait_template("SkipTutorial_Btn", 25)
    if not skip_tut_btn then
        skip_tut_btn = wait_template("NoButton", 5)
    end
    if skip_tut_btn then
        click(skip_tut_btn.x, skip_tut_btn.y)
        log_test("Skip Tutorial", "PASS", "'Skip Tutorial' clicked. Proceeding to main menu or Level 2.")
    else
        log_test("Skip Tutorial", "FAIL", "'Skip Tutorial' button did not appear.")
        return
    end

    wait(5)

    -- ============================================================
    -- DONE
    -- ============================================================
    set_stage("Completed")
    log_test("Test Suite", "PASS", "Scenario 1B — Fresh Start & Skip Tutorial completed successfully!")
end

run_tests()
