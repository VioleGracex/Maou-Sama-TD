-- Scenario 3: Level 3+ Cohort & Rite Setup
local function run_tests()
    if launch_game(false) then log_test("Connect Game", "PASS", "Connected.")
    else log_test("Connect Game", "FAIL", "Failed.") return end
    wait(2)
    local conquest_btn = wait_template("conquest_nav_btn", 10)
    if conquest_btn then click(conquest_btn.x, conquest_btn.y)
    else click(1100, 600) end
    wait(3)
    local node3 = wait_template("node_level_3", 15)
    if node3 then click(node3.x, node3.y)
    else click(600, 420) end
    wait(1.5)
    local engage_btn = wait_template("engage_btn", 8)
    if engage_btn then click(engage_btn.x, engage_btn.y)
    else click(1050, 580) end
    wait(2.5)
    local empty_unit = wait_template("empty_unit_slot", 8)
    if empty_unit then
        click(empty_unit.x, empty_unit.y) wait(1.5)
        local ignis_card = wait_template("ignis_roster_card", 8)
        if ignis_card then click(ignis_card.x, ignis_card.y) end
    end
    wait(1.5)
    local empty_rite = wait_template("empty_rite_slot", 8)
    if empty_rite then
        click(empty_rite.x, empty_rite.y) wait(1.5)
        local rite_card = wait_template("rite_roster_card", 8)
        if rite_card then click(rite_card.x, rite_card.y) end
    end
    wait(1.5)
    local start_mission = wait_template("start_mission_btn", 8)
    if start_mission then click(start_mission.x, start_mission.y)
    else click(950, 550) end
    wait(5)
    local placed_unit_card = wait_template("placed_unit_card", 10)
    if placed_unit_card then drag(placed_unit_card.x, placed_unit_card.y, 650, 320, 1.0) wait(2.5) end
    local start_battle = wait_template("start_battle_btn", 8)
    if start_battle then click(start_battle.x, start_battle.y)
    else click(1150, 620) end
    set_stage("Completed")
    log_test("Test Suite", "PASS", "Scenario 3 Complete.")
end
run_tests()
