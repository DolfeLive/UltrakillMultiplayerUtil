 
namespace MultiplayerUtil.Client;

public class Client
{
    public List<SteamId> connectedPeers = new List<SteamId>();

    public void Connect(SteamId hostId)
    {
        bool success = SteamManager.instance.EstablishP2P(hostId);
        if (success)
        {
            connectedPeers.Add(hostId);
            SteamManager.instance.dataLoop = SteamManager.instance.StartCoroutine(SteamManager.instance.DataLoopInit());
            Clogger.Log($"P2P Connection established with {hostId}", true);
        }
        else
        {
            Clogger.LogError($"Failed to establish P2P connection with {hostId}", true);
        }
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

        foreach (var peerId in connectedPeers)
        {
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
                Clogger.LogError($"Failed to send P2P packet to {peerId}", true);
            }
        }
    }
}
