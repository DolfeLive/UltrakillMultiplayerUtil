namespace MultiplayerUtil;

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
