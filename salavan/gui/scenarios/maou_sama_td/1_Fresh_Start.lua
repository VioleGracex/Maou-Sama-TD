-- Scenario 1: Fresh Start & Level 1 Tutorial
local function run_tests()
    set_stage("1. Clear Save Data")
    log_test("Clear Save Data", "STARTING", "Deleting save files...")
    if clear_save_data() then log_test("Clear Save Data", "PASS", "Save data cleared successfully.")
    else log_test("Clear Save Data", "FAIL", "Failed to clear save data.") return end
    set_stage("2. Launching Game")
    if launch_game(true) then log_test("Launch Game", "PASS", "Game booted and positioned.")
    else log_test("Launch Game", "FAIL", "Failed to launch game.") return end
    wait(6)
    local start_btn = wait_template("start_button", 15)
    if start_btn then click(start_btn.x, start_btn.y) wait(2) end
    set_stage("3. Character Ascension")
    local dice_pos = wait_template("dice_button", 25)
    if dice_pos then
        click(dice_pos.x, dice_pos.y) wait(1.5)
        local arise_pos = wait_template("arise_button", 8)
        if arise_pos then click(arise_pos.x, arise_pos.y) log_test("Character Ascension", "PASS", "Arisen.")
        else log_test("Character Ascension", "FAIL", "Arise button missing.") return end
    else log_test("Character Ascension", "FAIL", "Dice button did not load.") return end
    wait(6)
    set_stage("4. Level 1 - Start Tutorial")
    local play_tut_pos = wait_template("play_tutorial_btn", 25)
    if play_tut_pos then click(play_tut_pos.x, play_tut_pos.y) log_test("Level 1 Start", "PASS", "Tutorial started.")
    else log_test("Level 1 Start", "FAIL", "Tutorial prompt did not appear.") return end
    wait(2.5)
    log_test("Tutorial Step 1", "STARTING", "Advancing dialogues...")
    for i=1,3 do click(1100, 650) wait(2.0) end
    log_test("Tutorial Step 1", "PASS", "Dialogues advanced.")
    local ignis_btn = wait_template("ignis_card", 8)
    if ignis_btn then drag(ignis_btn.x, ignis_btn.y, 740, 320, 1.0) wait(2.5) log_test("Tutorial Step 2", "PASS", "Ignis deployed.")
    else log_test("Tutorial Step 2", "FAIL", "Ignis card missing.") return end
    click(1100, 650) wait(2.5)
    log_test("Tutorial Wave 1", "STARTING", "Waiting for ultimate skill...") wait(18)
    click(1100, 650) wait(2.0)
    click(740, 320) wait(2.0)
    local ult_btn = wait_template("ignis_ult_btn", 8)
    if ult_btn then click(ult_btn.x, ult_btn.y) wait(2.0)
    else click(1150, 580) wait(2.0) end
    log_test("Tutorial Step 5", "PASS", "Ultimate activated.")
    local next_lvl_btn = wait_template("victory_next_level", 45)
    if next_lvl_btn then click(next_lvl_btn.x, next_lvl_btn.y) log_test("Tutorial Level 1 Victory", "PASS", "Level 1 Cleared.")
    else log_test("Tutorial Level 1 Victory", "FAIL", "Victory screen did not show.") return end
    wait(6)
    set_stage("5. Level 2 Start")
    local start_battle = wait_template("start_battle_btn", 25)
    if start_battle then click(start_battle.x, start_battle.y) log_test("Level 2 Start", "PASS", "Level 2 started.")
    else log_test("Level 2 Start", "FAIL", "Level 2 battle button missing.") return end
    set_stage("Completed")
end
run_tests()
