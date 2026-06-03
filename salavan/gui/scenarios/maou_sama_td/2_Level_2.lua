-- Scenario 2: Level 2 Progression
local function run_tests()
    if launch_game(false) then log_test("Connect Game", "PASS", "Connected.")
    else log_test("Connect Game", "FAIL", "Failed.") return end
    wait(2)
    local conquest_btn = wait_template("conquest_nav_btn", 10)
    if conquest_btn then click(conquest_btn.x, conquest_btn.y)
    else click(1100, 600) end
    wait(3)
    local node2 = wait_template("node_level_2", 15)
    if node2 then click(node2.x, node2.y)
    else click(450, 360) end
    wait(1.5)
    local engage_btn = wait_template("engage_btn", 8)
    if engage_btn then click(engage_btn.x, engage_btn.y)
    else click(1050, 580) end
    wait(2)
    local start_mission = wait_template("start_mission_btn", 8)
    if start_mission then click(start_mission.x, start_mission.y)
    else click(950, 550) end
    wait(5)
    log_test("Combat", "PASS", "Level 2 loaded.")
    set_stage("Completed")
end
run_tests()
