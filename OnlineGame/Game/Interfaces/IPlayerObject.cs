﻿// File: OnlineGame/Game/Interfaces/IPlayer.cs
using System.Net.Sockets;
using OnlineGame.Core.Interfaces;
using OnlineGame.Game.GameObjects.ComponentInterfaces;
using OnlineGame.Game.GameObjects.Things.Living.Player;
using OnlineGame.Game.Interfaces;
using OnlineGame.Network.Client;

namespace OnlineGame.Game.Interfaces
{
    public interface IPlayerObject : ILiving
    {
        Task<string> ReadMessageAsync();

        Task SendMessageAsync(string message);
        // Any additional player-specific methods or properties can go here
    }
}