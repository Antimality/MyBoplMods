# How to use config (in general)
Consider the following basic (stripped) Plugin:
```c#
public class Plugin : BaseUnityPlugin
{
    // Config value
    internal static ConfigEntry<Type> yourSetting;

    private void Awake()
    {
        // Bind the config
        yourSetting = Config.Bind("CATEGORY", "NAME", defaultValue, "DESCRIPTION");
    }
}
```
`Config.Bind` creates a config file and loads it with the details describes if it doesn't exist, otherwise it gives the current state of the config field. To access the value assigned to it, use `yourSetting.Value`.<br>
Note: the type of in `ConfigEntry` should match the type of `defaultValue`.

If you wish to update the config file (using `Config.Save()`) anywhere outside of your `Plugin` class, you should copy it to an `internal` field within `Plugin`:
```c#
internal static ConfigFile config;
[...]
private void Awake()
    {
        [...]
        config = Config;
        [...]
    }
```
(`internal` means the field can be accessed anywhere in your assembly)
# How to sync across players (host dominates)
To add the config value to the lobby when the host creates a lobby:
```c#
[HarmonyPatch(typeof(SteamManager), "OnLobbyEnteredCallback")]
[HarmonyPostfix]
public static void OnEnterLobby(Lobby lobby)
{
    if (SteamManager.LocalPlayerIsLobbyOwner)
    {
        // Harmony lint thinks this won't work (because you're editing a parameter's value), but it does
        #pragma warning disable Harmony003
        lobby.SetData("YOURVALUE", Plugin.yourSetting.Value.ToString());
    }
}
```
To receive the correct value:
```c#
[HarmonyPatch(typeof(GameSession), nameof(GameSession.Init))]
[HarmonyPostfix]
public static void OnGameStart()
{
    if (GameLobby.isOnlineGame)
    {
        // Use host's setting
        storedValue = SteamManager.instance.currentLobby.GetData("YOURVALUE");
    }
    else
    {
        // Use value from local config
        storedValue = Plugin.yourSetting.Value;
    }
}
```
Where `storedValue` is the local field you use in your code.<br>
You can, of course, use this if block anywhere, not just `GameSession.Init`, but I found that to be the neatest.

Notes:
* I used harmony annotation, you can, obviously, accomplish the same using reflection.
* Lobby.SetData only recieves Strings, which means you must store your values as such and parse them back after Lobby.GetData.
* This *will* break if you reload a plugin mid-game, but since that can't happen accidentily, I opted to ignore this. There is a way to handle it, as you can see [here](https://github.com/Antimality/MyBoplMods/blob/830e66bc98c4971be5d098f450446bfee6533b4b/DefaultSize/Plugin.cs#L63). Or by just doing the entire if block every time the value is used.
* I recommend looking at DefaultSize or InfiniteReviveUses for refrence
