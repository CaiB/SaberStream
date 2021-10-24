using SaberStream.Sources;
using System;
using System.Threading;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace SaberStream.Sources
{
    public static class Twitch
    {
        private static TwitchClient? Client;
        private static string? Channel;

        /// <summary>Connects to Twitch chat, and prepares to receive various events and send messages.</summary>
        /// <param name="username">The username to connect as</param>
        /// <param name="authToken">The bot auth token to connect to Twitch with</param>
        /// <param name="channel">The name of the channel to connect to the chat of</param>
        public static void Connect(string username, string authToken, string channel)
        {
            if (Client != null) { throw new InvalidOperationException("Already connected to Twitch"); }
            Channel = channel;
            Console.WriteLine("Connecting to Twitch...");
            Client = new();
            ConnectionCredentials Creds = new(username, authToken);
            Client.Initialize(Creds, Channel);
            Client.OnError += ErrorHandler;
            Client.OnConnected += InternalConnectedHandler;
            Client.OnMessageReceived += InternalMessageReceivedHandler;

            Client.Connect();
        }

        /// <summary>Disconnects from Twitch chat.</summary>
        public static void Disconnect()
        {
            Client?.Disconnect();
            while (Client?.IsConnected ?? false) { Thread.Sleep(50); } // Wait for the client to disconnect
            Client = null;
        }

        /// <summary>Sends a message in chat as the bot user.</summary>
        /// <param name="message">The message to send</param>
        public static void SendMessage(string message) => Client?.SendMessage(Channel, message);

        private static void ErrorHandler(object? sender, OnErrorEventArgs evt)
        {
            Console.WriteLine("Twitch API Error:");
            Console.WriteLine(evt.Exception.ToString());
        }

        private static void InternalConnectedHandler(object? sender, OnConnectedArgs evt)
        {
            Console.WriteLine("Connected to Twitch.");
            Client?.SendMessage(Channel, "ErzaBot ready!");
            Connected?.Invoke(null, new());
        }

        private static void InternalMessageReceivedHandler(object? sender, OnMessageReceivedArgs evt) => MessageReceived?.Invoke(null, new MessageReceivedEventArgs(evt.ChatMessage));

        // Events

        public delegate void ConnectedHandler(object? sender, EventArgs evt);
        public static event ConnectedHandler? Connected;

        public class MessageReceivedEventArgs
        {
            public ChatMessage Message { get; private set; }
            public MessageReceivedEventArgs(ChatMessage msg) { this.Message = msg; }
        }
        public delegate void MessageReceivedHandler(object? sender, MessageReceivedEventArgs evt);
        public static event MessageReceivedHandler? MessageReceived;
    }
}
