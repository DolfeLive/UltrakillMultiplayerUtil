
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MultiplayerUtil;

public class SteamManager : MonoBehaviour
{
    public static SteamManager instance;


    public const float importantUpdatesASec = 33.3f;
    public const float unimportantUpdatesAMin = 12;
    public static string p2pEstablishMessage = "IWouldLikeToEstablishP2P!";

    // Runtime
    public Lobby? current_lobby;

    public List<SteamId> BannedSteamIds = new();
    public List<SteamId> BlockedSteamIds = new();
    public static List<SteamId?> BannedLobbies = new();


    public SteamId selfID;
    private string playerName;
    public bool isLobbyOwner = false;
    string LobbyName;
    int maxPlayers;
    bool publicLobby;
    bool cracked;
    public Coroutine? dataLoop;

    public Server.Serveier server;
    private Client.Client client;
    private bool CheckForP2P = false;

    // End
#if DEBUG
    public static bool SelfP2PSafeguards = false;
#else
    public static bool SelfP2PSafeguards = true;
#endif

    public const int channelToUse = 0; // This is for testing the max channels

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        //this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        instance = this;
#if DEBUG
        Command.Register();
#endif
        Callbacks.StartupComplete?.Invoke();

        Callbacks.p2pMessageReceived.AddListener(_ =>
        {
            var (data, sender) = _; // (byte[], SteamId?)

            if (data == null || !sender.Value.IsValid)
            {
                Debug.LogError($"Received invalid P2P message: data or sender is null. data:{data == null}, Sender:{sender.Value}");
                return;
            }
            if (SelfP2PSafeguards)
                if (!sender.HasValue || sender.Value == LobbyManager.selfID) // Check if sender isnt null or if the sender is yourself
                {
                    Debug.Log($"Failed at reciving, why: {(sender.HasValue ? "Has value" : "Does not have value")} {(data.Length > 0 ? string.Join("", data) : "")}, Sender: {(sender.HasValue ? sender.Value.ToString() : "null")}");
                    return;
                }

            ObserveManager.OnMessageRecived(data, sender);
        });

        Debug.unityLogger.filterLogType = LogType.Log | LogType.Warning | LogType.Error | LogType.Exception | LogType.Assert;
        this.selfID = SteamClient.SteamId;
        
        SetupCallbacks();
    }

    public void ReInit(bool cracked)
    {
        if (cracked != this.cracked)
        {
            this.cracked = cracked;
            SteamClient.Shutdown();
            SteamClient.Init(_MultiplayerUtil.appId);
            this.selfID = SteamClient.SteamId;
        }
    }
    
    void SetupCallbacks()
    {
/*#if DEBUG
        Steamworks.Dispatch.OnDebugCallback = (type, str, server) =>
        {
            Clogger.Log($"[Callback {type} {(server ? "server" : "client")}] {str}");
        };
#endif*/
        Dispatch.OnException = (e) =>
        {
            Clogger.LogError($"Exception: {e.Message}, {e.StackTrace}");
        };

        SteamUtils.OnSteamShutdown += () => { Callbacks.OnSteamShutdown.Invoke(); };

        SteamMatchmaking.OnLobbyCreated += (result, lobby) =>
        {
            Clogger.ExtraLog($"Lobby Created, result: {result}, lobby: {lobby.Id}");
            Callbacks.OnLobbyCreated.Invoke(lobby);
        };
        
        SteamNetworking.OnP2PSessionRequest += (id) =>
        {
            Clogger.ExtraLog($"P2P requested from: {id}");
            // Callbacks.OnP2PSessionRequest.Invoke(id);
            
            if (SelfP2PSafeguards)
                if (id == selfID)
                {
                    //Clogger.Log("P2p comes from self, skipping");
                    return;
                }

            if (BannedSteamIds.Contains(id))
            {
                Clogger.ExtraLog($"P2P request from banned user: {id}");
                return;
            }
            if (client.connectedPeers.Contains(id))
            {
                Clogger.ExtraLog($"P2P request from already connected user: {id}");
                return;
            }
            bool accepted = SteamNetworking.AcceptP2PSessionWithUser(id);
            if (accepted)
            {
                Clogger.Log($"P2P session accepted with: {id}");
                if (isLobbyOwner)
                {
                    server.besties.Add(id);
                }
                else
                {
                    client.connectedPeers.Add(id);
                }
            }
            else
            {
                Clogger.ExtraLog($"P2P session request failed with: {id}");
            }
            
            
        };

        SteamNetworking.OnP2PConnectionFailed += (id, sessionError) =>
        {
            Clogger.LogWarning($"P2P Connection failed, id: {id}, error: {sessionError}");
            Callbacks.OnP2PConnectionFailed.Invoke(id, sessionError);
        };

        SteamMatchmaking.OnLobbyMemberJoined += (l, f) =>
        {
            Clogger.ExtraLog($"Lobby member joined: {f.Name}");
            if (f.Id != selfID && !BannedSteamIds.Contains(f.Id))
            {
                bool p2pEstablished = EstablishP2P(f);
                
                if (isLobbyOwner)
                    server.besties.Add(f.Id);
                else
                    client.connectedPeers.Add(f.Id);

                if (p2pEstablished == false)
                {
                    Clogger.LogWarning($"Falied to establish p2p with: {f.Name}");
                }

                Callbacks.OnLobbyMemberJoined.Invoke(l, f);
            }

            if (isLobbyOwner && BannedSteamIds.Contains(f.Id))
            {
                
            }
        };

        SteamMatchmaking.OnLobbyEntered += (l) => {
            if (!String.IsNullOrEmpty(l.Owner.Name) && l.Owner.Id != selfID)
            {
                Clogger.ExtraLog($"Joined Lobby: {l.Owner.Name}");
                client.Connect(l.Owner.Id);
                
                foreach (var member in l.Members)
                {
                    client.connectedPeers.Add(member.Id);
                    SteamManager.instance.EstablishP2P(member.Id);
                }

                Callbacks.OnLobbyEntered.Invoke(l);
            }
        };

        SteamMatchmaking.OnChatMessage += (lo, fr, st) =>
        {
            if (BlockedSteamIds.Contains(fr.Id)) return;
            if (st == p2pEstablishMessage) return;

            Clogger.Log($"Chat message received from {fr.Name}: {st}");
            Callbacks.OnChatMessageReceived.Invoke(lo, fr, st);

        };

        SteamMatchmaking.OnLobbyMemberLeave += (Lob, Fri) =>
        {
            if (isLobbyOwner)
            {
                server.besties.Remove(Fri.Id);
                Closep2P(Fri);
            }
            else
            {
                client.connectedPeers.Remove(Fri.Id);
                Closep2P(Fri);
            }
            Callbacks.OnLobbyMemberLeave.Invoke(Fri.Id);
            Clogger.ExtraLog($"Lobby member left: {Fri.Name}");
        };

        SteamMatchmaking.OnLobbyMemberDisconnected += (Lob, Fri) =>
        {
            if (isLobbyOwner)
            {
                server.besties.Remove(Fri.Id);
                Closep2P(Fri);
            }
            else
            {
                client.connectedPeers.Remove(Fri.Id);
                Closep2P(Fri);
            }
            Callbacks.OnLobbyMemberLeave.Invoke(Fri.Id);
            Clogger.ExtraLog($"Lobby member disconnected: {Fri.Name}");
        };

        /*SteamMatchmaking.OnLobbyMemberKicked += (Lob, Fri, Kicker) =>
        {
            if (isLobbyOwner)
            {
                server.besties.Remove(Fri.Id);
                Closep2P(Fri);
            }
            else
            {
                client.connectedPeers.Remove(Fri.Id);
                Closep2P(Fri);
            }
            Callbacks.OnLobbyMemberLeave.Invoke(Fri.Id);
            Clogger.Log($"Lobby Member kicked: {Fri.Name}, Kicker: {Kicker.Name}");
        };

        SteamMatchmaking.OnLobbyMemberBanned += (Lob, Banne, Kicker) =>
        {
            if (isLobbyOwner)
            {
                server.besties.Remove(Banne.Id);
                Closep2P(Banne);
            }
            else
            {
                client.connectedPeers.Remove(Banne.Id);
                Closep2P(Banne);
            }
            Callbacks.OnLobbyMemberLeave.Invoke(Banne.Id);
            Callbacks.OnLobbyMemberBanned.Invoke(Banne.Id);
            Clogger.Log($"Lobby Member Banned: {Banne.Name}, Banner: {Kicker.Name}");
        };*/

        ObserveManager.SubscribeToType(typeof(AuthoritativePacket), out Callbacks.SenderUnityEvent AuthorityImposed);
        AuthorityImposed.AddListener(_ =>
        {
            SteamId? sender = _.Item2;
            if (sender != current_lobby?.Owner.Id) return;

            AuthoritativePacket packet = Data.Deserialize<AuthoritativePacket>(_.Item1);

            if (packet.id == selfID)
                switch (packet.type)
                {
                    case AuthoritativeTypes.Kicked:
                        foreach (Friend member in current_lobby?.Members)
                        {
                            Closep2P(member);
                        }
                        if (isLobbyOwner)
                        {
                            server.besties.Clear();
                        }
                        else
                        {
                            client.connectedPeers.Clear();
                        }

                        current_lobby?.Leave();
                        current_lobby = null!;
                        break;

                    case AuthoritativeTypes.Banned:
                        foreach (Friend member in current_lobby?.Members)
                        {
                            Closep2P(member);
                        }
                        if (isLobbyOwner)
                        {
                            server.besties.Clear();
                        }
                        else
                        {
                            client.connectedPeers.Clear();
                        }

                        BannedLobbies.Add(current_lobby?.Id);
                        current_lobby?.Leave();
                        current_lobby = null!;
                        break;
                }
            else
            {
                Closep2P(packet.id);
                Clogger.ExtraLog($"player {(packet.type == AuthoritativeTypes.Kicked ? "kicked" : "banned")}: {packet.id}");
                if (isLobbyOwner)
                {
                    server.besties.Remove(packet.id);
                }
                else
                {
                    client.connectedPeers.Remove(packet.id);
                }
                if (packet.type == AuthoritativeTypes.Banned)
                {
                    Callbacks.OnLobbyMemberBanned.Invoke(packet.id);
                }
                else if (packet.type == AuthoritativeTypes.Kicked)
                {
                    Callbacks.OnLobbyMemberLeave.Invoke(packet.id);
                }
            }
        });
    }

    public bool EstablishP2P(dynamic bestie)
    {
        bool Result = false;
        byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(p2pEstablishMessage);

        switch (bestie)
        {
            case Friend friend:
                if (SelfP2PSafeguards)
                    if (friend.Id.Value == selfID.Value)
                    {
                        Clogger.ExtraLog("Skippng establishing p2p with self");
                        return true;
                    }
                Clogger.Log($"Establishing p2p with: {friend.Name}, {friend.Id}");
                Result = SteamNetworking.SendP2PPacket(friend.Id, messageBytes);
                return Result;
                break;
            case SteamId steamId:
                if (SelfP2PSafeguards)
                    if (steamId.Value == selfID.Value)
                    {
                        Clogger.ExtraLog("Skippng establishing p2p with self");
                        return true;
                    }
                Clogger.Log($"Establishing p2p with: {steamId}");
                Result = SteamNetworking.SendP2PPacket(steamId, messageBytes);
                return Result;
                break;
            default:
                Clogger.LogError("Error with establishing p2p");
                return false;
                break;
        }
    }

    public bool Closep2P(dynamic unbestie)
    {
        switch (unbestie)
        {
            case Friend friend:
                Clogger.StackTraceLog($"DeEstablishing p2p with: {friend.Name}, {friend.Id}");
                return SteamNetworking.CloseP2PSessionWithUser(friend.Id);

            case SteamId steamId:
                Clogger.StackTraceLog($"DeEstablishing p2p with: {steamId}");
                return SteamNetworking.CloseP2PSessionWithUser(steamId);

            default:
                Clogger.LogError("Error with closing p2p");
                return false;
        }
    }

    public IEnumerator DataLoopInit()
    {
        if (dataLoop != null)
        {
            Clogger.StackTraceLog("Dataloop alr running", 0); 
            yield break;
        }

        Clogger.ExtraLog("Data Loop Init Activated");
        float interval = 1f / importantUpdatesASec;
        float unimportantInterval = 60f / unimportantUpdatesAMin;

        float unimportantTimeElapsed = 0f;
        
        CheckForP2P = true;

        yield return new WaitForSecondsRealtime(0.1f);

        try
        {
            while (true)
            {
                if (current_lobby == null)
                {

                    yield return new WaitForSeconds(5f);
                    if (current_lobby == null)
                    {
                        Clogger.LogWarning("Breaking out of DataLoopInit");
                        yield break;
                    }
                    print("everything was fine");

                }

                Callbacks.TimeToSendImportantData?.Invoke();

                if (isLobbyOwner)
                {
                    unimportantTimeElapsed += interval;

                    if (unimportantTimeElapsed >= unimportantInterval)
                    {
                        Clogger.UselessLog("TimeToSendUnimportantData invoked");
                        Callbacks.TimeToSendUnimportantData?.Invoke();
                        current_lobby?.SetData("members", $"{current_lobby?.Members.Count()}/{maxPlayers}");
                        unimportantTimeElapsed = 0f;
                    }
                }

                if (current_lobby == null)
                {
                    Clogger.LogWarning("Breaking out of DataLoopInit");
                    yield break;
                }

                yield return new WaitForSeconds(interval);

            }
        }
        finally
        {
            CheckForP2P = false;
            Clogger.ExtraLog("DataLoopInit ending");
        }
    }

    public void DataSend(object data, SendMethod sendMethod)
    {
        if (current_lobby == null) return;

        try
        {
            NetworkWrapper wrapper = new()
            {
                ClassType = data.GetType().AssemblyQualifiedName,
                ClassData = Data.Serialize(data)
            };

            byte[] serializedData = Data.Serialize(wrapper);

            if (isLobbyOwner)
                server.Send(serializedData, sendMethod);
            else
                client.Send(serializedData, sendMethod);
        }
        catch (Exception e)
        {
            Clogger.ExtraLog($"Data Send Exception: {e}");
        }
    }
    
    public void LobbyOwnerSend(object data, SendMethod sendMethod)
    {
        if (current_lobby == null) return;

        try
        {
            NetworkWrapper wrapper = new()
            {
                ClassType = data.GetType().AssemblyQualifiedName,
                ClassData = Data.Serialize(data)
            };

            Friend lobbyOwner = current_lobby.Value.Owner;

            byte[] serializedData = Data.Serialize(wrapper);

            if (SteamManager.SelfP2PSafeguards)
                if (lobbyOwner.Id == LobbyManager.selfID)
                {
                    Clogger.UselessLog("Skipping sending p2p to self");
                    return;
                }
            P2PSend sendType = Data.ConvertSendMethodToP2PSend(sendMethod);
            bool success = SteamNetworking.SendP2PPacket(
                lobbyOwner.Id,
                serializedData,
                serializedData.Length,
                SteamManager.channelToUse,
                sendType
            );
        }
        catch (Exception e)
        {
            Clogger.ExtraLog($"Data Send Exception: {e}");
        }
    }

    void Update()
    {
        //SteamClient.RunCallbacks();

        if (CheckForP2P)
        {
            (byte[], SteamId?) data = CheckForP2PMessages();
            if (data != (null, null))
            {
                try
                {
                    string msgString = System.Text.Encoding.Default.GetString(data.Item1);

                    if (msgString == SteamManager.p2pEstablishMessage/* && !BlockedSteamIds.Contains(data.Item2?)*/)
                    {
                        Clogger.ExtraLog("Received P2P intro string message!");
                        return;
                    }

                }
                catch
                { }

                Callbacks.p2pMessageReceived.Invoke(data);
            }
        }
    }
    public async void HostLobby(string LobbyName, int? maxPlayers, bool publicLobby, bool cracked, bool mods, (string, string) ModLobbyIDentifiers)
    {
        if (!SteamClient.IsValid)
        {
            Clogger.LogWarning("Steam client is not initialized");

            try
            {
                ReInit(_MultiplayerUtil.appId == 480u ? true : false);
                Clogger.ExtraLog("Reinited steam");
            }
            catch (Exception e) { Clogger.LogError($"STEAM ERROR: {e}"); Clogger.LogWarning("Try launching steam if it isnt launched!"); }

            return;
        }

        if (current_lobby != null)
        {
            if (isLobbyOwner)
            {
                if (server.besties.Count > 0)
                {
                    if (current_lobby.Value is Lobby lobby)
                    {
                        Friend thatMember = lobby.Members.FirstOrDefault(_ => _.Id == server.besties[0].Value);
                        current_lobby?.SetData("Owner", thatMember.Name);
                        
                        lobby.Owner = thatMember;
                    }
                }
            }
            
            current_lobby?.SendChatString($":::Leaving.{selfID.Value}");
            current_lobby?.Leave();
            current_lobby = null;
            isLobbyOwner = false;
        }
        
        Lobby? createdLobby = await SteamMatchmaking.CreateLobbyAsync(maxPlayers ?? 8);
        if (createdLobby == null)
        {
            Clogger.LogError("Lobby creation failed - Result is null");
            return;
        }
                
        server = new Server.Serveier();

        this.LobbyName = LobbyName;


        if (maxPlayers <= 0) maxPlayers = 8;

        this.maxPlayers = maxPlayers ?? 8;
        this.publicLobby = publicLobby;

        isLobbyOwner = true;
        current_lobby = createdLobby;

        current_lobby?.SetJoinable(true);

        if (publicLobby)
            current_lobby?.SetPublic();
        else
            current_lobby?.SetPrivate();

        current_lobby?.SetData(ModLobbyIDentifiers.Item1, ModLobbyIDentifiers.Item2);
        current_lobby?.SetData("name", LobbyName);
        current_lobby?.SetData("mods", mods.ToString());
        current_lobby?.SetData("members", $"1/{maxPlayers}");
        current_lobby?.SetData("Owner", SteamClient.Name);

        Clogger.ExtraLog($"Lobby Created, id: {current_lobby?.Id}");
    }

    // Help collected from jaket github https://github.com/xzxADIxzx/Join-and-kill-em-together/blob/main/src/Jaket/Net/LobbyController.cs
    public async void JoinLobbyWithID(ulong id)
    {
        try
        {
            server = null;
            Clogger.ExtraLog("Joining Lobby with ID");
            Lobby lob = new Lobby(id);

            RoomEnter result = await lob.Join();

            if (result == RoomEnter.Success)
            {
                Clogger.ExtraLog($"Lobby join Success: {result}");
                isLobbyOwner = false;
                current_lobby = lob;

                client = new Client.Client();
            }
            else
            {
                current_lobby = null;
                isLobbyOwner= false;
                client = null;

                Clogger.LogWarning($"Couldn't join the lobby. Result is {result}");
            }
        }
        catch (Exception ex)
        {
            Clogger.LogError($"An error occurred while trying to join the lobby: {ex.Message}, The error might be because steam isnt launched");
        }
    }
    
    public (byte[], SteamId?) CheckForP2PMessages()
    {
        try
        {
            while (SteamNetworking.IsP2PPacketAvailable(out uint availableSize, channelToUse))
            {
                SteamId Sender = new SteamId();

                byte[] buffer = new byte[availableSize];
                bool worked = SteamNetworking.ReadP2PPacket(buffer, ref availableSize, ref Sender, channelToUse);

                if (worked)
                {
                    if (!Sender.IsValid)
                    {
                        Clogger.ExtraLog("Sender is null skipping");
                        continue;
                    }

                    if (SelfP2PSafeguards)
                        if (Sender == selfID)
                        {
                            //Clogger.Log("P2p comes from self, skipping");
                            continue;
                        }

                    return (buffer, Sender);
                }
                else
                {
                    Clogger.ExtraLog($"p2p failed: {Sender}");

                    return (null, null);
                }
            }
        }
        catch (ArgumentException ae)
        {
            Clogger.LogError($"CheckForP2p Arg Exeption: {ae}");
        }
        return (null, null);
    }

    public void InviteFriend() => SteamFriends.OpenGameInviteOverlay(SteamManager.instance.current_lobby.Value.Id);

    void OnApplicationQuit() => Disconnect();
    public void Disconnect()
    {
        if (isLobbyOwner)
        {
            if (server?.besties?.Count > 0)
            {
                foreach (var item in server.besties)
                {
                    if (item != null)
                        SteamNetworking.CloseP2PSessionWithUser(item);
                }
                
                if (current_lobby != null && current_lobby.Value is Lobby lobby)
                {
                    Friend thatMember = lobby.Members.FirstOrDefault(_ => _.Id == server.besties[0].Value);
                    lobby.SendChatString($"||| Setting Lobby Owner To: {thatMember.Name}");
                    lobby.SetData("members", server.besties.Count.ToString());
                    lobby.SetData("Owner", thatMember.Name);
                    lobby.IsOwnedBy(thatMember.Id);
                    Clogger.ExtraLog($"Setting Lobby Owner to: {thatMember.Name}");
                }   
            }
        }
        else
        {
            if (client?.connectedPeers != null)
            {
                foreach (var item in client.connectedPeers)
                {
                    SteamNetworking.CloseP2PSessionWithUser(item);
                }
            }
        }
        current_lobby?.Leave();
        current_lobby = null;
        isLobbyOwner = false;
        server = null;
        client = null;
    }

   
    public void SendChatMessage(string msg)
    {
        current_lobby?.SendChatString(msg);
    }

    ~SteamManager()
    {
        Disconnect();
    }

}


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

// Wrapper so i can handle multiple classes
[System.Serializable]
public class NetworkWrapper
{
    public string ClassType { get; set; }
    public byte[] ClassData { get; set; }
}

// this system will allow users to subscribe with their class to notifications of when the specific class they are looking for is detected
public static class ObserveManager
{
    public static bool MessageReceivedLogging = false;
    public static Dictionary<Type, Callbacks.SenderUnityEvent> subscribedEvents = new();

    public static void SubscribeToType(Type classType, out Callbacks.
        SenderUnityEvent whenDetected)
    {
        Callbacks.SenderUnityEvent whenDetectedAction = new();

        subscribedEvents.Add(classType, whenDetectedAction);

        whenDetected = whenDetectedAction;
    }

    public static void OnMessageRecived(byte[] message, SteamId? sender)
    {

        NetworkWrapper recivedData = null;
        try
        {
            recivedData = Data.Deserialize<NetworkWrapper>(message);
        }
        catch (InvalidCastException e)
        {
            Logger.LogWarning($"Failed to cast p2p message, sender: {sender}, message len: {message.Length}");
            return;
        }

        if (MessageReceivedLogging)
            Clogger.Log($"Recived p2p message, sender: {sender}, type: {recivedData.ClassType}, data: {recivedData.ClassData}");

        Type type = Type.GetType(recivedData.ClassType);
        if (type != null && ObserveManager.subscribedEvents.TryGetValue(type, out Callbacks.SenderUnityEvent notifier))
        {
            notifier.Invoke((recivedData.ClassData, sender));
        }
    }

}


public enum AuthoritativeTypes
{
    Kicked,
    Banned,
}

public class AuthoritativePacket {
    public AuthoritativeTypes type;
    public SteamId id;
}
