using Steamworks;

namespace MultiplayerUtil;

public static class LobbyManager
{
    public static bool isLobbyOwner => SteamManager.instance.isLobbyOwner;
    public static Lobby? current_lobby => SteamManager.instance.current_lobby;
    public static SteamId selfID => SteamManager.instance.selfID;

    /// <summary>
    /// If MultiplayerUtil should log more messages, useful for debugging and making mods
    /// </summary>
#if RELEASE
    public static bool extraLogging = false;
#elif DEBUG
    public static bool extraLogging = true;
#endif

    /// <summary>
    /// How many important updates are sent a second, approx 30fps
    /// </summary>
    public static float importantUpdatesASec
    {
        get
        {
            return SteamManager.importantUpdatesASec;
        }
    }

    /// <summary>
    /// How many unimportant updates are sent a min, approx 1 every 10 seconds
    /// </summary>
    public static float unimportantUpdatesAMin
    {
        get
        {
            return SteamManager.unimportantUpdatesAMin;
        }
    }
    /// <summary>
    /// Restarts DataLoop if something goes wrong, can cause problems
    /// </summary>
    public static void restartLoop()
    { 
        if (SteamManager.instance.dataLoop != null)
        {
            SteamManager.instance.StopCoroutine(SteamManager.instance.dataLoop);
            SteamManager.instance.dataLoop = SteamManager.instance.StartCoroutine(SteamManager.instance.DataLoopInit());
        }
    }

    /// <summary>
    /// Creates a lobby with the set settings
    /// </summary>
    /// <param name="lobbyName">The name of the lobby.</param>
    /// <param name="maxPlayers">The maximum number of players allowed in the lobby. If null defaults to 8.</param>
    /// <param name="publicLobby">Indicates whether the lobby is public or private.</param>
    /// <param name="cracked">Indicates if the server will run be joinable by cracked clients or offical.</param>
    /// <param name="cheats">Indicates whether cheats are enabled in the lobby.</param>
    /// <param name="mods">Indicates whether mods are enabled in the lobby.</param>
    /// <param name="modIdentifier">The identifier your mod uses when making a lobby</param>
    public static void CreateLobby(string lobbyName, int? maxPlayers, bool publicLobby, bool cracked, bool mods, (string, string) modIdentifier)
    {
        Clogger.Log("Creating Lobby");
        SteamManager.instance.HostLobby(lobbyName, maxPlayers, publicLobby, cracked, mods, modIdentifier);
    }
    
    /// <summary>
    /// returns a list of all lobbies matching your mods lobby identifier
    /// </summary>
    /// <param name="modIdentifierKVP">The identifier your mod uses when making a lobby</param>
    public static async Task<List<Lobby>> FetchLobbies((string, string) modIdentifierKVP)
    {
        List<Lobby> foundLobbies = new List<Lobby>();
        try
        {
            var lobbyList = await SteamMatchmaking.LobbyList.RequestAsync();

            if (lobbyList != null)
            {
                foundLobbies = lobbyList
                    .Where(lobby =>
                        lobby.Data.Any(data =>
                            data.Key == modIdentifierKVP.Item1 &&
                            data.Value == modIdentifierKVP.Item2) &&
                        !SteamManager.BannedLobbies.Contains(lobby.Id))
                    .ToList();
            }
        }
        catch (Exception e)
        {
            Clogger.LogError($"Lobby finding exeption: {e}");
        }

        Clogger.Log($"Found Lobbies: {foundLobbies.Count}");
        return foundLobbies;
    }

    /// <summary>
    /// Joins a lobby with the specified ulong id
    /// </summary>
    /// <param name="id">The lobby id</param>
    public static void JoinLobbyWithID(ulong id)
    {
        Clogger.Log("Joining Lobby");
        
        SteamManager.instance.JoinLobbyWithID(id);
    }

    /// <summary>
    /// Send a chat message
    /// </summary>
    /// <param name="msg">Sends a chat message to the current lobby</param>
    public static void SendMessage(string msg) => SteamManager.instance.SendChatMessage(msg);


    /// <summary>
    /// Send data to connected p2p players
    /// </summary>
    /// <param name="data">The class object to be sent, no need to Serialize, that is done automatically</param>
    public static void SendData(object data, SendMethod sendMethod = SendMethod.Reliable)
    {
        SteamManager.instance.DataSend(data, sendMethod);
    }

    /// <summary>
    /// Sends data to only the lobby owner
    /// </summary>
    public static void SendToLobbyOwner(object data, SendMethod sendMethod = SendMethod.Reliable)
    {
        SteamManager.instance.LobbyOwnerSend(data, sendMethod);
    }

    /// <summary>
    /// Opens steam invite friend menu
    /// </summary>
    public static void InviteFriend() => SteamManager.instance.InviteFriend();

    /// <summary>
    /// Ditto
    /// </summary>
    public static void Disconnect() => SteamManager.instance.Disconnect();

    /// <summary>
    /// Reinnits steam for use when switing between cracked or not
    /// </summary>
    /// <param name="cracked">if it should innit with appid 480 or ultrakill</param>
    public static void ReInnitSteamClient(bool cracked)
    {
        SteamManager.instance.ReInit(cracked);
    }

    /// <summary>
    /// Adds a Steam user to the banned list.
    /// </summary>
    /// <param name="steamId">The Steam ID of the user to ban.</param>
    public static void AddToBannedUsers(SteamId steamId)
    {
        SteamManager.instance.BannedSteamIds.Add(steamId);
    }

    /// <summary>
    /// Adds a Steam user to the blocked list.
    /// </summary>
    /// <param name="steamId">The Steam ID of the user to block.</param>
    public static void AddToBlockedUsers(SteamId steamId)
    {
        SteamManager.instance.BlockedSteamIds.Add(steamId);
    }

    /// <summary>
    /// Removes a Steam user from the banned list.
    /// </summary>
    /// <param name="steamId">The Steam ID of the user to unban.</param>
    public static void RemoveFromBannedUsers(SteamId steamId)
    {
        SteamManager.instance.BannedSteamIds.Remove(steamId);
    }

    /// <summary>
    /// Removes a Steam user from the blocked list.
    /// </summary>
    /// <param name="steamId">The Steam ID of the user to unblock.</param>
    public static void RemoveFromBlockedUsers(SteamId steamId)
    {
        SteamManager.instance.BlockedSteamIds.Remove(steamId);
    }

    /// <summary>
    /// Clears all users from the banned list.
    /// </summary>
    public static void ClearBannedUsers()
    {
        SteamManager.instance.BannedSteamIds.Clear();
    }

    /// <summary>
    /// Clears all users from the blocked list.
    /// </summary>
    public static void ClearBlockedUsers()
    {
        SteamManager.instance.BlockedSteamIds.Clear();
    }

    /// <summary>
    /// Bans a player from ever joining the lobby again
    /// </summary>
    /// <param name="steamId">The user's steamid taht you want to ban</param>
    public static void BanUserFromLobby(SteamId steamId)
    {
        if (!isLobbyOwner) return;
        AuthoritativePacket packet = new AuthoritativePacket();
        packet.type = AuthoritativeTypes.Banned;
        packet.id = steamId;
        SendData(packet, SendMethod.Reliable);
    }

    /// <summary>
    /// Kicks a player from the lobby
    /// </summary>
    /// <param name="steamId">The user's steamid to kick</param>
    public static void KickUserFromLobby(SteamId steamId)
    {
        if (!isLobbyOwner) return;
        AuthoritativePacket packet = new AuthoritativePacket();
        packet.type = AuthoritativeTypes.Kicked;
        packet.id = steamId;
        SendData(packet, SendMethod.Reliable);
    }

}
