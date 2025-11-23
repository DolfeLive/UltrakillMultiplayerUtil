namespace MultiplayerUtil;

// Wrapper so i can handle multiple classes
[System.Serializable]
public class NetworkWrapper
{
    public string ClassType { get; set; }
    public byte[] ClassData { get; set; }
}
