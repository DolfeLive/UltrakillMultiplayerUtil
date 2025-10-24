 
namespace MultiplayerUtil.Server;

public class Serveier // Read it like its french, also yes i named it this on purpose
{
    public List<SteamId> besties = new List<SteamId>(); // People in lobby

    public Serveier()
    {
        SteamManager.instance.dataLoop = SteamManager.instance.StartCoroutine(SteamManager.instance.DataLoopInit());
    }

    public void Send(object data, SendMethod sendMethod)
    {
        byte[] serializedData;

        if (data is byte[])
        {
            serializedData = (byte[])data;
        }
        else
        {
            serializedData = Data.Serialize(data);
        }

        foreach (var bestie in besties)
        {
            var peerId = bestie.Value;
             
            if (SteamManager.SelfP2PSafeguards)
                if (peerId == LobbyManager.selfID)
                {
                    //Clogger.UselessLog("Skipping sending p2p to self");
                    return;
                }
            P2PSend sendType = Data.ConvertSendMethodToP2PSend(sendMethod);
            bool success = SteamNetworking.SendP2PPacket(
                peerId,
                serializedData,
                serializedData.Length,
                SteamManager.channelToUse,
                sendType
            );

            if (!success)
            {
                Clogger.LogError($"Failed to send P2P packet to {peerId}", false);
            }
        }
    }
}
