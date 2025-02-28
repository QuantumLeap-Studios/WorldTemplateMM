-- the basic script itself
function main()  
    log("Hello, world!")  
end

-- runs every second
function tick()
    log("A tick has ran!")
end

-- when the touch event is ran
-- can only be used if you add "onTouched" to your SEF!
function onTouched()
    log("I got touched!")
end

-- this is the same as main and will be ran at almost the exact same time, use this when the map has finished loading
-- can only be used if you add "onAwake" to your SEF!
function onAwake() 
    log("Hello world, again!")
end

-- an example function for respawning, made for the simple obby map template
function Respawn()
    SetPlayerPosition(Vec3_new(56.73595, -5.069687, -61.9714))
end