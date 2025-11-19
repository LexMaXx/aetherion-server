using System;
using UnityEngine;

/// <summary>
/// Shared data classes for network communication
/// Used by SocketIOManager, RoomManager, and other networking components
/// </summary>

/// <summary>
/// Join room request
/// </summary>
[Serializable]
public class JoinRoomRequest
{
    public string roomId;
    public string username;
    public string characterClass;
    public string userId;
    public string password;
    public int level;
}

/// <summary>
/// Create room request
/// </summary>
[Serializable]
public class CreateRoomRequest
{
    public string roomName;
    public int maxPlayers;
    public bool isPrivate;
    public string password;
    public string characterClass;
    public string username;
    public int level;
}

/// <summary>
/// Room info
/// </summary>
[Serializable]
public class RoomInfo
{
    public string roomId;
    public string roomName;
    public int currentPlayers;
    public int maxPlayers;
    public bool canJoin;
    public string status;
    public bool isHost;
}

/// <summary>
/// Create room response
/// </summary>
[Serializable]
public class CreateRoomResponse
{
    public bool success;
    public string message;
    public RoomInfo room;
}

/// <summary>
/// Join room response
/// </summary>
[Serializable]
public class JoinRoomResponse
{
    public bool success;
    public string message;
    public RoomInfo room;
}

/// <summary>
/// Room ID data for requests
/// </summary>
[Serializable]
public class RoomIdData
{
    public string roomId;
}

/// <summary>
/// Room list response
/// </summary>
[Serializable]
public class RoomListResponse
{
    public bool success;
    public RoomInfo[] rooms;
    public int total;
}
