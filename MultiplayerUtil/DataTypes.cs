using Newtonsoft.Json;
using System.Text;

namespace MultiplayerUtil;


public static class Data
{
    public static T Deserialize<T>(byte[] serializedData)
    {
        if (serializedData == null || serializedData.Length == 0)
        {
            Clogger.LogError("Failed to deserialize data: Empty or null data received");
            throw new ArgumentException("Serialized data cannot be null or empty.", nameof(serializedData));
        }

        try
        {
            var json = Encoding.UTF8.GetString(serializedData);
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            Clogger.LogError($"Failed to deserialize data: {ex.Message}, Data: {serializedData}");
            throw;
        }
    }

    public static bool TryDeserialize<T>(byte[] serializedData, out T result)
    {
        if (serializedData == null || serializedData.Length == 0)
        {
            Clogger.LogError("Failed to deserialize data: Empty or null data received");
            throw new ArgumentException("Serialized data cannot be null or empty.", nameof(serializedData));
        }

        try
        {
            var json = Encoding.UTF8.GetString(serializedData);
            result = JsonConvert.DeserializeObject<T>(json);
            return true;
        }
        catch (Exception ex)
        {
        }
        result = default(T);
        return false;
    }

    public static byte[] Serialize(object data)
    {
        if (data == null)
        {
            Clogger.LogError("Failed to serialize data: Null object provided");
            throw new ArgumentNullException(nameof(data));
        }

        try
        {
            var json = JsonConvert.SerializeObject(data);
            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            Clogger.LogError($"Failed to serialize data: {ex.Message}");
            throw;
        }
    }

    public static P2PSend ConvertSendMethodToP2PSend(MultiplayerUtil.SendMethod method)
    {
        P2PSend p2pSend = 0;

        if ((method & MultiplayerUtil.SendMethod.Reliable) == MultiplayerUtil.SendMethod.Reliable)
            p2pSend |= P2PSend.Reliable;
        if ((method & MultiplayerUtil.SendMethod.Unreliable) == MultiplayerUtil.SendMethod.Unreliable)
            p2pSend |= P2PSend.Unreliable;
        if ((method & MultiplayerUtil.SendMethod.UnreliableNoDelay) == MultiplayerUtil.SendMethod.UnreliableNoDelay)
            p2pSend |= P2PSend.UnreliableNoDelay;
        if ((method & MultiplayerUtil.SendMethod.ReliableWithBuffering) == MultiplayerUtil.SendMethod.ReliableWithBuffering)
            p2pSend |= P2PSend.ReliableWithBuffering;

        return p2pSend;
    }

    public static MultiplayerUtil.SendMethod ConvertP2PSendToSendMethod(P2PSend p2pSend)
    {
        MultiplayerUtil.SendMethod method = 0;

        if ((p2pSend & P2PSend.Reliable) == P2PSend.Reliable)
            method |= MultiplayerUtil.SendMethod.Reliable;
        if ((p2pSend & P2PSend.Unreliable) == P2PSend.Unreliable)
            method |= MultiplayerUtil.SendMethod.Unreliable;
        if ((p2pSend & P2PSend.UnreliableNoDelay) == P2PSend.UnreliableNoDelay)
            method |= MultiplayerUtil.SendMethod.UnreliableNoDelay;
        if ((p2pSend & P2PSend.ReliableWithBuffering) == P2PSend.ReliableWithBuffering)
            method |= MultiplayerUtil.SendMethod.ReliableWithBuffering;

        return method;
    }

    public static byte boolsToBinary(bool[] bools)
    {
        byte binary = 0b00000000;
        int length = Math.Min(bools.Length, 8);
    
        for (int i = 0; i < length; i++)
        {
            if (bools[i])
            {
                binary |= (byte)(1 << i);
            }
        }
        return binary;
    }
    
    public static bool[] byteToBools(byte data)
    {
        bool[] result = new bool[8];
        for (int i = 0; i < 8; i++)
        {
            result[i] = (data & (1 << i)) != 0;
        }
        return result;
    }
}

/*
// An example of a data packet that i made for pvp multiplayer

[Serializable]
public class DataPacket
{
    // Player Core stuff
    public byte PlayerHealth;

    // Pos and Movement
    public float PositionX;
    public float PositionY;
    public float PositionZ;
    public float VelocityX;
    public float VelocityY;
    public float VelocityZ;
    public short RotationX;
    public short RotationY;

    // Combat State
    public byte CurrentWeapon;
    public byte CurrentVariation;
    public bool IsSliding;
    public bool IsPunching;

    // Movement State
    public bool IsWallJumping;
    public bool IsSlamStorage;

    public DataPacket(
        int Health,
        Vector3 Position,
        Vector3 Velocity,
        Vector3 Rotation,
        int CurrentWeapon,
        int CurrentVariation,
        bool IsSliding,
        bool IsPunching,
        bool IsWallJumping,
        bool IsSlamStorage)
    {
        this.PlayerHealth = (byte)Health;
        this.PositionX = Position.x;
        this.PositionY = Position.y;
        this.PositionZ = Position.z;
        this.VelocityX = Velocity.x;
        this.VelocityY = Velocity.y;
        this.VelocityZ = Velocity.z;
        this.RotationX = (short)Rotation.x;
        this.RotationY = (short)Rotation.y;
        this.CurrentWeapon = (byte)CurrentWeapon;
        this.CurrentVariation = (byte)CurrentVariation;
        this.IsSliding = IsSliding;
        this.IsPunching = IsPunching;
        this.IsWallJumping = IsWallJumping;
        this.IsSlamStorage = IsSlamStorage;
    }

    public void Display()
    {
        Console.WriteLine($"Health: {PlayerHealth}");
        Console.WriteLine($"Position: ({PositionX:F2}, {PositionY:F2}, {PositionZ:F2})");
        Console.WriteLine($"Velocity: ({VelocityX:F2}, {VelocityY:F2}, {VelocityZ:F2})");
        Console.WriteLine($"Rotation: ({RotationX:F2}, {RotationY:F2}");
        Console.WriteLine($"Weapon: {CurrentWeapon} | Variation: {CurrentVariation}");
        Console.WriteLine($"States: Sliding={IsSliding}, WallJump={IsWallJumping}, IsSlamStorage;={IsSlamStorage}");
    }
}

*/
