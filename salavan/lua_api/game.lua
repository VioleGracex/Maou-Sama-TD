-- salavan/lua_api/game.lua
-- High-level automation API for Maou-Sama TD

game = game or {}

-- Place a unit on the grid
-- unit_name: The name of the unit (e.g. "Ignis")
-- grid_x: The X coordinate of the tile
-- grid_y: The Y coordinate of the tile
function game.place_unit(unit_name, grid_x, grid_y, timeout)
    timeout = timeout or 15.0
    local card_id = "UnitCard_" .. unit_name
    local tile_id = "Tile_" .. grid_x .. "_" .. grid_y
    
    print("[API] Attempting to place " .. unit_name .. " at (" .. grid_x .. ", " .. grid_y .. ")")
    
    local start_time = os.time()
    while os.difftime(os.time(), start_time) < timeout do
        local card = ui.wait_for(card_id, 1.0)
        if not card then
            card = ui.wait_for("UnitButton_" .. unit_name, 1.0)
        end
        local tile = ui.wait_for(tile_id, 1.0)
        
        if card and tile then
            -- Drag the card to the tile
            ui.drag(card, tile, 1.0)
            
            -- Wait a moment to see if it took
            wait(1.5)
            
            -- Check if occupied
            local state = get_state()
            local placed = false
            if state and state.occupied_tiles then
                print("[API] DEBUG occupied_tiles count: " .. #state.occupied_tiles)
                for i, t in ipairs(state.occupied_tiles) do
                    print("[API] DEBUG tile occupied: " .. t.id)
                    if t.id == tile_id then
                        placed = true
                        break
                    end
                end
            end
            
            if placed then
                print("[API] Successfully placed " .. unit_name)
                return true
            else
                print("[API] Placement failed, retrying...")
            end
        else
            wait(0.5)
        end
    end

    print("[API] Failed to place " .. unit_name .. " after timeout.")
    return false
end

-- Wait for a specific tile to become occupied
function game.wait_for_occupant(grid_x, grid_y, timeout)
    timeout = timeout or 5.0
    local tile_id = "Tile_" .. grid_x .. "_" .. grid_y
    local start_time = os.time()
    
    while os.difftime(os.time(), start_time) < timeout do
        local state = get_state()
        if state and state.occupied_tiles then
            for i, tile in ipairs(state.occupied_tiles) do
                if tile.id == tile_id then
                    return true
                end
            end
        end
        wait(0.5)
    end
    
    return false
end

-- Returns the current scene name (or nil if state unavailable)
function game.get_scene()
    local state = get_state()
    if state then return state.current_scene end
    return nil
end

-- Wait until the current_scene matches scene_name
function game.wait_for_scene(scene_name, timeout)
    timeout = timeout or 20.0
    local start_time = os.time()
    print("[API] Waiting for scene: " .. scene_name)
    while os.difftime(os.time(), start_time) < timeout do
        local scene = game.get_scene()
        if scene and scene == scene_name then
            print("[API] Scene reached: " .. scene_name)
            return true
        end
        wait(0.5)
    end
    print("[API] Timed out waiting for scene: " .. scene_name)
    return false
end

game.last_event_id = 0

function game.clear_events()
    local state = get_state()
    if state and state.debug_events then
        for _, ev in ipairs(state.debug_events) do
            local id_str = string.match(ev, "^%[(%d+)%]")
            if id_str then
                local id = tonumber(id_str)
                if id > game.last_event_id then
                    game.last_event_id = id
                end
            end
        end
    end
end

-- Wait until debug_events contains a string matching event_name
function game.wait_for_event(event_name, timeout)
    timeout = timeout or 20.0
    local start_time = os.time()
    print("[API] Waiting for event: " .. event_name)
    while os.difftime(os.time(), start_time) < timeout do
        local state = get_state()
        if state and state.debug_events then
            for _, ev in ipairs(state.debug_events) do
                local id_str = string.match(ev, "^%[(%d+)%]")
                local id = id_str and tonumber(id_str) or (game.last_event_id + 1)
                
                if id > game.last_event_id and string.find(ev, event_name, 1, true) then
                    game.last_event_id = id
                    print("[API] Event received: " .. ev)
                    return true
                end
            end
        end
        wait(0.3)
    end
    print("[API] Timed out waiting for event: " .. event_name)
    return false
end

-- Wait until unit_button_names contains unit_name
function game.wait_for_unit(unit_name, timeout)
    timeout = timeout or 25.0
    local start_time = os.time()
    local button_name = "UnitButton_" .. unit_name
    print("[API] Waiting for unit button: " .. button_name)
    while os.difftime(os.time(), start_time) < timeout do
        local state = get_state()
        if state and state.unit_button_names then
            for _, name in ipairs(state.unit_button_names) do
                if name == button_name then
                    print("[API] Unit button found: " .. button_name)
                    return true
                end
            end
        end
        -- Also check via ui.wait_for as fallback
        local elem = ui.wait_for(button_name, 0.1)
        if elem then
            print("[API] Unit button found via element: " .. button_name)
            return true
        end
        wait(0.5)
    end
    print("[API] Timed out waiting for unit: " .. unit_name)
    return false
end

-- Wait for a dynamic placement tile from UDP logs
function game.wait_for_tile(event_name, timeout)
    timeout = timeout or 15.0
    print("[API] Waiting for dynamic tile event: " .. event_name)
    local data = wait_event(event_name, timeout)
    if not data then
        print("[API] Timed out waiting for event: " .. event_name)
        return nil
    end
    -- parse "x,y"
    local x, y = string.match(data, "^%s*(%d+)%s*,%s*(%d+)%s*$")
    if x and y then
        print("[API] Received dynamic tile coordinates: " .. x .. ", " .. y)
        return tonumber(x), tonumber(y)
    end
    print("[API] Invalid tile coordinate format: " .. tostring(data))
    return nil
end

return game
