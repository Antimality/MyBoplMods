# MyBoplMods
 A collection of my Bopl Battle mods!<br>
 If you have any questions, you're welcome to ping me on the [official Bopl Battle modding discord server](https://discord.gg/official-bopl-battle-modding-comunity-1175164882388275310) @Antimality.<br>
 For any bugs in my mods, please [create an issue](https://github.com/Antimality/MyBoplMods/issues).

# Mods:
## [Time Stop Timer](https://github.com/Antimality/MyBoplMods/releases/tag/TimeStopTimer-v1.0.0 "v1.0.0")
Display how long until time stop starts and how long until it ends
## [Scale Control](https://github.com/Antimality/MyBoplMods/releases/tag/ScaleControl-v1.0.0 "v1.0.0")
Fight like antman! Enlarge and shrink at will to shoot massive arrows and shrink for last minute dodges! 
#### Current Hurdles
Currently works only in local.
For it to work on multiplayer I need to change which inputs the game sends and recieves online. Possible workaround: using steam chat.
## [Default Size](https://github.com/Antimality/MyBoplMods/releases/tag/DefaultSize-v1.0.0 "v1.0.0")
Change the size all blobs start from in each round.
## [Infinite Revive Uses](https://github.com/Antimality/MyBoplMods/releases/tag/InfiniteReviveUses-v1.0.0 "v1.0.0")
No longer do you need multiple revive abilities to duplicate! Have infinitely many revives from just one ability!
## [Instant Gust](https://github.com/Antimality/MyBoplMods/releases/tag/InstantGust-v1.0.0 "v1.0.0")
Cast gust with lightning speed for the ultimate clutch!

# Ideas:
## EnhancedTimeStop (WIP)
Enhancement suite for time stop that will include:
* TimeStopTimer (and configuration to turn it on/off)
* Configuration for casting time, cooldown time, and duration (Cole's mod)
## Cat & Mouse gamemode
Based on [this](https://youtu.be/aT0UKAuCaTU?si=xn4OOS_zPOlJX7u6) video by Phunix.
One player starts large and the others small.
Add configurations:
* Starting size for cat & mouse
* Random cat/spesific cat (host)
* Disable scale weapons(?)
Add a warning against using with DefaultSize, or just make it override DefualtSize somehow?
## Enhanced Grapple
* More than one hook at the same time
* Detatch button
* Longer range
* If connected to player (or anything that isn't a platform) pull it to you instead of going to it
* Shoot a second time to attach to something else like Just Cause?
#### Current Hurdles
Still haven't figured out how this ability works at all
## Rock enhancements:
* Dry Rock:Rock jumps off water for extreme rock plays!
* Cancle Rock: Cancel rock at will 
* Player Rock: Use abilities while in rock form
#### Current Hurdles
No idea if direct control of motion is even possible
Haven't figured out how to control abilities
## Ability Reroll
Reroll abilities once per round. Good for all random games or when you get super countered and need a hail-mary.
## Ride The Lightning
Tesla coil lighting is a solid surface instead of a damaging line. You can still kill directly by squishing, or combo with other abilities, such as Sonic for increased range.
* Bonus, thowable coil?
#### Current Hurdles
No idea how to make/work with Physics objects
## Loadouts
Save/load premade ability loadouts.
* Simpler version: button that sets everyone to all random in ability selection screen.
## Rainbow Player
Character cycles colors constantly. More just a POC to see what is possible.
## Ability Cycle
Change which ability (left/right/middle) get's swichted when picking up a new ability. Either controlled or random. (get premission from AbilityStorm maker)
## InfiniteBlackHoles
Make as many black holes as you wish from a single ability!

# Other projects
* Steamline the Config Sync to an api-ish thing
* Make a general way of detecting if both players have the mod, possibly using Steam Chat kicking.
* Input library: Either using reflection or Steam Chat

# General knowledge I've gained
* Keyboard inputs are easy to accomplish locally but complicated (if even possible) to integrate online: the game only sends/recieves the inputs used by the game.
* The replay files also keep just the input history, which is hilarious if you replay without the original mods.
* How to use config and sync settings across players

Antimality's Bopl Battle Mods © 2024 by Antimality is licensed under CC BY 4.0
