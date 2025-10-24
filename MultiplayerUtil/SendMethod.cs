
namespace MultiplayerUtil;

/// <summary>
/// Flags for how you send the data
/// The types:
///  Unreliable = 0, Fast but no guarantee of delivery or order
///  UnreliableNoDelay = 1, very fast, no delay, no guarantee of it arriving or being in order
///  Reliable = 2, Guaranteed delivery, but not necessarily ordered
///  ReliableWithBuffering = 3, Reliable and ensures they are sent in the right order
/// </summary>
public enum SendMethod
{
    /// <summary>
    /// Fast but no guarantee of delivery or order
    /// </summary>
    Unreliable = 0,

    /// <summary>
    /// very fast , no delay, no guarantee of it arriving or being in order
    /// no delay
    /// </summary>
    UnreliableNoDelay = 1,


    /// <summary>
    /// Guaranteed delivery, but not necessarily ordered
    /// </summary>
    Reliable = 2,

    /// <summary>
    /// Reliable and ensures they are sent in the right order
    /// </summary>
    ReliableWithBuffering = 3,
}
