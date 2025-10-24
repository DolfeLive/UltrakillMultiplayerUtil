#if DEBUG 
using MU = MultiplayerUtil;
using UnityEngine;
using MultiplayerUtil;
using UnityEngine.Events;

namespace ExampleMod;

class ExampleClass1 : MonoBehaviour
{
    public static ExampleClass1 instance; // Singleton instance of this class
    public bool DoPlayerStuff = false; // I keep this false because it gets annoying when testing
    void Start()
    {
        gameObject.hideFlags = HideFlags.HideAndDontSave; // An attempt to keep the mod safe when in aggressive scenes
        instance = this; // Set singleton instance
        counter = new CounterClass();
        player = new Player();

        #region callbacks
        // You can send data at any time but these are pre set loops for convenience
        MU.Callbacks.TimeToSendImportantData.AddListener(() => // use for things like player positions where they need to update often
        {
            try
            {
                if (DoPlayerStuff)
                {
                    Debug.Log($"Sending player pos: {player.position.ToVector3()}");
                    MU.LobbyManager.SendData(player, SendMethod.UnreliableNoDelay); // Send the player class
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send data: {e.Message}"); // If the sending fails don't nuke everything
            }
        });

        MU.Callbacks.TimeToSendUnimportantData.AddListener(() =>  // UnimportantData Runs less than important (x times a seconds), use for things like leaderboards, unimportant is only ran if you are the lobby owner
        {
            // if (!MU.LobbyManager.isLobbyOwner) return; // Only run if lobby owner // only do this if its owner only stuff

            try
            {
                Debug.Log($"Sending counter value: {counter.counter}");
                MU.LobbyManager.SendData(counter, SendMethod.Reliable);

                //MU.LobbyManager.SendData(scoreboard, SendMethod.ReliableWithBuffering);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send data: {e.Message}"); // If the sending fails don't nuke everything
            }
        });
        #endregion

        #region TypeSubscribing
        MU.ObserveManager.SubscribeToType(typeof(CounterClass), out Callbacks.SenderUnityEvent CounterDetected);
        CounterDetected.AddListener(_ => // When counter changes run this codeblock
        {
            var counter = Data.Deserialize<ExampleMod.ExampleClass1.CounterClass>(_.Item1);
            print($"Counter value: {counter.counter}, Sender id: {_.Item2.Value}"); // Log detected counter value
        });

        MU.ObserveManager.SubscribeToType(typeof(Player), out Callbacks.SenderUnityEvent PlayerDetected);
        PlayerDetected.AddListener(_ =>
        {
            var player = Data.Deserialize<Player>(_.Item1);
            print($"player Pos: {player.position.ToVector3()}, Sender id: {_.Item2.Value}");
        });
        #endregion

        #region Miscellaneous
        /*
        // Changing the app id between 480 and ultrakill's id
        MU.LobbyManager.ReInnitSteamClient(cracked: false);
        */
        #endregion

        // Start coroutines for counting and player position updates
        StartCoroutine(Couting());
        if (DoPlayerStuff)
            StartCoroutine(UpdatePlayerPos());
    }

    /*
     *  Please note:
     *  Try and keep your classes small
     *  Try and substitute values like health from an int to a byte (0-255)
     *  If you want to put the extra effort you can split up data based on their importance and send them at different times
     *  Try and avoid strings if you can or set limits to the length of strings
     *  
     *  Below is the int types C# supports
     *  
     *  The sbyte type represents signed 8-bit integers with values from -128 to 127, inclusive.
     *  The byte type represents unsigned 8-bit integers with values from 0 to 255, inclusive.
     *  The short type represents signed 16-bit integers with values from -32768 to 32767, inclusive.
     *  The ushort type represents unsigned 16-bit integers with values from 0 to 65535, inclusive.
     *  The int type represents signed 32-bit integers with values from -2147483648 to 2147483647, inclusive.
     *  The uint type represents unsigned 32-bit integers with values from 0 to 4294967295, inclusive.
     *  The long type represents signed 64-bit integers with values from -9223372036854775808 to 9223372036854775807, inclusive.
     *  The ulong type represents unsigned 64-bit integers with values from 0 to 18446744073709551615, inclusive.
     *  
     * - https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/types 8.3.6 Integral types
     *   https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
    */

    #region counterExample
    // Counter example
    public CounterClass counter;

    [Serializable] // if you want to use this class to store values it has to be [Serializable]
    public class CounterClass
    {
        public CounterClass()
        {
            this.counter = 0;
        }
        public int counter = 0;
    }

    IEnumerator Couting()
    {
        while (true)
        {
            counter.counter++; // Increment counter value every second
            yield return new WaitForSeconds(1f);
        }
    }
    #endregion
    #region playerExample
    // Player Pos example
    public Player player;

    [Serializable]
    public class Player
    {
        public Player() { this.position = new SerializableVector3(Vector3.zero); }

        public SerializableVector3 position = new SerializableVector3(Vector3.zero);
    }
    [System.Serializable]
    public class SerializableVector3 // You normally cant Serialize a vector3 so this is a lil workaround
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
    IEnumerator UpdatePlayerPos()
    {

        while (true)
        {
            /*
            if (NewMovement.Instance == null)
            {
                yield return new WaitForSeconds(2);
                continue;
            }
            player.position = new SerializableVector3(NewMovement.Instance?.gameObject?.transform.position ?? Vector3.zero); // if the value is null ?? will make it return Vector3.zero instead of an error
            */
            yield return new WaitForSeconds(.1f);
        }
    }
    #endregion

    #region Lobbies
    public async void Search()
    {
        Clogger.Log("Retriving all open lobbies");
        List<Lobby> getthingy = getthingy = await MU.LobbyManager.FetchLobbies(("ModIdentifier", "ModIdentifier2")); // Note mod identifier is what you put in LobbyManager.CreateLobby so you only get lobbies from you own mod


        foreach (Lobby lob in getthingy)
        {
            Clogger.Log("-------------------");
            // Logs lobby's name. member count, id and owner
            Clogger.Log($"Lobby name: {lob.Data
                .Where(kvp => kvp.Key == "name" && !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => kvp.Value)
                .FirstOrDefault()} ");

            Clogger.Log($"Members: {lob.Data
                .Where(kvp => kvp.Key == "members" && !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => kvp.Value)
                .FirstOrDefault()} ");

            Clogger.Log($"Id: {lob.Id}");
            Clogger.Log($"Owner:{lob.Data
                .Where(kvp => kvp.Key == "Owner" && !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => kvp.Value)
                .FirstOrDefault()}");
        }
    }

    public async void Create()
    {
        // lob name, max players (if invalid or null defaults to 8, if its hosted on steam 480 or ultrakill, if mods are allowed, and the identifier ref Search()
        MU.LobbyManager.CreateLobby(
            lobbyName: "New lobby",
            maxPlayers: 3,
            publicLobby: true,
            cracked: true,
            mods: false,
            modIdentifier: ("ModIdentifier", "ModIdentifier2")
        );
    }

    public void JoinLobby(ulong id)
    {
        MU.LobbyManager.JoinLobbyWithID(id);
    }

    public void SendMessage(string message)
    {
        MU.LobbyManager.SendMessage(message);
    }

    public void Disconnect()
    {
        MU.LobbyManager.Disconnect();

    }
    #endregion
}
#endif