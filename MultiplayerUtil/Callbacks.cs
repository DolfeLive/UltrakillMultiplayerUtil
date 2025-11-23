namespace MultiplayerUtil;

public static class Callbacks
{
    public class SenderUnityEvent : UnityEvent<(byte[], SteamId?)> { }

    /// <summary>
    /// Invoked when a P2P message is received. The message object is already deserialized.
    /// </summary>
    public static SenderUnityEvent p2pMessageReceived = new SenderUnityEvent();

    /// <summary>
    /// Invoked at the regular update interval for sending important data.
    /// </summary>
    public static UnityEvent TimeToSendImportantData = new UnityEvent();

    /// <summary>
    /// Invoked during unimportant update cycles for sending less critical data.
    /// </summary>
    public static UnityEvent TimeToSendUnimportantData = new UnityEvent();

    /// <summary>
    /// Invoked when the SteamManager is fully initialized and ready for use.
    /// Make sure to call Steam-dependent methods only after this event fires.
    /// </summary>
    public static UnityEvent StartupComplete = new UnityEvent();

    /// <summary>
    /// Invoked when a member joins the Steam lobby. P2P setup is handled automatically.
    /// </summary>
    public static UnityEvent<Lobby, Friend> OnLobbyMemberJoined = new UnityEvent<Lobby, Friend>();

    /// <summary>
    /// Invoked when a member leaves the Steam lobby.
    /// </summary>
    public static UnityEvent<SteamId> OnLobbyMemberLeave = new UnityEvent<SteamId>();

    /// <summary>
    /// Invoked when a chat message is received in the lobby.
    /// </summary>
    public static UnityEvent<Lobby, Friend, string> OnChatMessageReceived = new UnityEvent<Lobby, Friend, string>();

/*    /// <summary>
    /// Invoked when another user attempts to start a P2P session with the local user.
    /// </summary>
    public static UnityEvent<SteamId> OnP2PSessionRequest = new UnityEvent<SteamId>();*/

    /// <summary>
    /// Invoked when a P2P connection attempt fails with a specific user.
    /// </summary>
    public static UnityEvent<SteamId, P2PSessionError> OnP2PConnectionFailed = new UnityEvent<SteamId, P2PSessionError>();

    /// <summary>
    /// Invoked when a lobby member is banned.
    /// </summary>
    public static UnityEvent<SteamId> OnLobbyMemberBanned = new UnityEvent<SteamId>();

    /// <summary>
    /// Invoked when the local user successfully enters a lobby.
    /// </summary>
    public static UnityEvent<Lobby> OnLobbyEntered = new UnityEvent<Lobby>();

    /// <summary>
    /// Invoked when a lobby is successfully created by the local user.
    /// </summary>
    public static UnityEvent<Lobby> OnLobbyCreated = new UnityEvent<Lobby>();

    public static Action OnSteamShutdown = delegate { };
}
