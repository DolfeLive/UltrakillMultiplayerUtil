# ULTRAKILL Multiplayer Utility

**Author:** Dolfe

`ULTRAKILL Multiplayer Utility` is a lightweight, Steam-based peer-to-peer (P2P) utility library for creating multiplayer features in **ULTRAKILL** mods using BepInEx. It provides a high-level interface for lobby creation, network messaging, data synchronization, and designed to reduce boilerplate and the pain of setting up steamworks.

---

## Table of Contents

* [Features](#features)
* [Getting Started](#getting-started)
* [To Do](#todo)
* [Lobby Management](#lobby-management)
* [Data Transmission](#data-transmission)
* [Callbacks & Events](#callbacks--events)
* [Best Practices](#best-practices)
* [Ideas or Additions](#ideas-or-additions)
* [License](#license)

---

## Features

* Lightweight P2P communication built on Steam networking.
* Easy-to-use data sending (reliable, unreliable, etc.).
* Lobby creation, search, join, leave, and chat utils.
* Automatic serialization and deserialization of data.
* Custom type synchronization and event based observation.
* Optional support for using steam 480 (for testing), or mods.
* All LobbyManager methods are documented
---

## TODO
```
Set lobby settings method
Add documentation for current_lobby
```
---

## Getting Started

### Prerequisites

* Unity Engine
* A /Plugins dir in the unity project
* [Facepunch.Steamworks.Win64.dll](https://github.com/Facepunch/Facepunch.Steamworks/releases) in the /Plugins dir
* a /x86_64 dir in the /Plugins dir and [steam_api64.dll](https://partner.steamgames.com/downloads/list) (**Version 1.61**) to be in that
* Newtonsoft Json package installed (Window -> Package Manager, Click the + -> Add package by name. Then put in 'com.unity.nuget.newtonsoft-json')

### Installation

1. Place `MultiplayerUtil.dll` in the `Plugins/` directory.
2. Add SteamManager to a object in the scene
3. Hook into `MultiplayerUtil.Callbacks.StartupComplete` before invoking any Steam functionality.

---

## Lobby Management

You can easily manage Steam lobbies via static methods under `MultiplayerUtil.LobbyManager`:

```csharp
// Create a new public lobby for 4 players
LobbyManager.CreateLobby(
    lobbyName: "New lobby",
    maxPlayers: 4,
    publicLobby: true,
    cracked: false,
    mods: false,
    modIdentifier: ("YourMod", "Tag")
);
```

```csharp
// Search for existing lobbies using your identifier
var lobbies = await LobbyManager.FetchLobbies(("YourMod", "Tag"));
```

```csharp
// Join a lobby by its ulong ID
LobbyManager.JoinLobbyWithID(lobbyId);
```

```csharp
// Disconnect from current lobby
LobbyManager.Disconnect();
```

---

## Data Transmission

### Sending Data

```csharp
MyClass exampleData = new MyClass();
exampleData.playerPos = ...;
LobbyManager.SendData(exampleData, SendMethod.UnreliableNoDelay);
// Make sure your class is System.Serializable
```

Available send methods:

* `Unreliable` Fast but no guarantee of delivery or order, Ok at what it does
* `UnreliableNoDelay` Very fast, no delay, no guarantee of it arriving or being in order, Good for fast changing data such as player positions
* `Reliable` Guaranteed delivery, but not necessarily ordered, Good for one time updates
* `ReliableWithBuffering` Reliable and ensures they are sent in the right order, Good for bulk data sending

You can also:

```csharp
LobbyManager.SendToLobbyOwner(data, SendMethod.Reliable);
```

### Receiving Data

To observe incoming data:

```csharp
ObserveManager.SubscribeToType(typeof(MyClass), out Callbacks.SenderUnityEvent myEvent);
myEvent.AddListener(payload =>
{
    var obj = Data.Deserialize<MyClass>(payload.Item1);
    var sender = payload.Item2;
});
```

---

## Callbacks & Events

Hook into a range of events:

* `Callbacks.StartupComplete`
* `Callbacks.TimeToSendImportantData`
* `Callbacks.TimeToSendUnimportantData`
* `Callbacks.OnLobbyMemberJoined`
* `Callbacks.OnLobbyMemberLeave`
* `Callbacks.OnChatMessageReceived`
* `Callbacks.OnLobbyCreated`
* `Callbacks.OnLobbyEntered`
* `Callbacks.OnP2PConnectionFailed`

Example:

```csharp
Callbacks.OnChatMessageReceived.AddListener(msg => Debug.Log($"Chat: {msg}"));
```

---

## Best Practices

* **Use `UnreliableNoDelay`** for real-time data (e.g., positions).
* **Use `ReliableWithBuffering`** for sequential state or synced actions.
* Make sure the classes that contain the data you are sending are <ins>serialized</ins>
* Keep serialized classes small and avoid large strings.
* Use primitives (e.g., `byte`, `short`) when possible to reduce size.
* Catch exceptions around network operations to avoid crashes.

---

## Ideas Or additions
Just message me on discord (@dolfelive) or create a issue/pull request

---

## License

Dolfe AGPLish License
Refer to [LICENSE.md](https://github.com/DolfeLive/MultiplayerUtil/blob/master/LICENSE.md)
