 
namespace MultiplayerUtil;

[BepInPlugin("DolfeLive.Modding.MultiplayerUtil", "DolfeMultiplayersUtil", "1.0.0")]
public class _MultiplayerUtil : BaseUnityPlugin
{
    public static string modName = "MultiplayerUtil";

    public static _MultiplayerUtil instance;
    public static bool cracked = false;
    public static uint appId => cracked ? 480u : 1229490u;
    private GameObject smObj = null!;
    void Awake()
    {
        instance = this;
        
        Harmony har = new Harmony("DolfeLive.Modding.MultiplayerUtil");
        har.PatchAll();

        SceneManager.sceneLoaded += (Scene scene, LoadSceneMode lsm) =>
        {
            if (SceneHelper.CurrentScene == "Main Menu")
            {
                if (smObj != null) return;

                smObj = new GameObject("SteamManagerUtil");
                smObj.AddComponent<SteamManager>();
                DontDestroyOnLoad(smObj);
            }
        };


    }
    
    void Update()
    {
        SteamClient.RunCallbacks();
    }
}