# MyBoplMods
 A collection of my Bopl Battle mods!

# Ideas:
## Scale Control (WIP)
Fight like antman! Enlarge and shrink at will to shoot massive arrows and shrink for last minute dodges! 
### Current hurdles
Currently works in local, more extensive testing needed.
For it to work on multiplayer I need to change which inputs the game sends and recieves online.
## Enhanced Grapple (WIP)
DESCRIPTION
### Current hurdles
Still haven't figured out how this ability works at all
## Time Stop timer
Display how long until time stop starts and how long until it ends
### Current Hurdles
Learn how to add ui elements
## Rock enhancements:
* Dry Rock:Rock jumps off water for extreme rock plays!
* Cancle Rock: Cancel rock at will 
* Player Rock: Use abilities while in rock form
### Current Hurdles
No idea if direct control of motion is even possible
## Ability Cycle
Change which ability (left/right/middle) get's swichted when picking up a new ability. Either controlled or random.
### Current Hurdles
None expected, use AbilityStorm for refrence
## Ability Reroll
Reroll abilities once per round. Good for all random games or when you get super countered and need a hail-mary.
### Current Hurdles
## Ride The Lightning
Tesla coil is a solid surface instead of a damaging line. You can still kill directly by squishing, or combo with other abilities, such as Sonic for increased range.
* Bonus, thowable coil?
### Current Hurdles
No idea how to make/work with Physics objects
## Loadouts
Save/load premade ability loadouts.
* Simpler version: button that sets everyone to all random in ability selection screen.
### Current Hurdles
## Rainbow Player
Character cycles colors constantly. More just a POC to see what is possible.
### Current Hurdles
## Change Default Size
Change the the size all blobs start from in each round
* Can turn this into "cat & mice" mode - one player starts two sizes larger at random 
### Current Hurdles
Need to mess with the Session Handler
## Quicker Gust
I just hate the buildup time on gust, make it shorter
### Current Hurdles
## NAME
DESCRIPTION
### Current Hurdles
## NAME
DESCRIPTION
### Current Hurdles


# General knowledge I've gained
* Keyboard inputs are easy to accomplish locally but complicated (if even possible) to integrate online: the game only sends/recieves the inputs used by the game.
* The replay files also keep just the input history, which is hilarious if you replay without the original mods.

# General TODOs
* Document refrence code
* Streamline project creation (create project with all the usual edits/additions to structure)
* Streamline project build (DONE)
* -> Debug should goes to Scripts/ & Production to Plugins/ 
* Transfer to Splotch
* Change all PatchAll() to PatchAll(typeof(...))
