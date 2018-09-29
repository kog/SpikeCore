﻿using System;

using IrcDotNet;

using SpikeCore.Irc.Configuration;

namespace SpikeCore.Irc.IrcDotNet
{
    public class IrcClient : IIrcClient
    {
        private readonly BotConfig _botConfig;
        private StandardIrcClient _ircClient;
        
        public Action<string> MessageReceived { get; set; }
        public Action<ChannelMessage> ChannelMessageReceived { get; set; }

        public IrcClient(BotConfig botConfig)
        {
            _botConfig = botConfig;
        }
        
        public void Connect()
        {            
            _ircClient = new StandardIrcClient();
            _ircClient.RawMessageReceived += IrcClient_RawMessageReceived;

            _ircClient.Connected += IrcClient_Connected;
            _ircClient.ConnectFailed += IrcClient_ConnectFailed;
            _ircClient.Registered += _ircClient_Registered;

            _ircClient.Connect(_botConfig.Host, _botConfig.Port, false, new IrcUserRegistrationInfo()
            {
                NickName = _botConfig.Nickname,
                RealName = _botConfig.Nickname,
                UserName = _botConfig.Nickname,
            });
        }

        private void _ircClient_Registered(object sender, EventArgs e)
        {
            _ircClient.LocalUser.JoinedChannel += LocalUser_JoinedChannel;
            _ircClient.LocalUser.LeftChannel += LocalUser_LeftChannel;
        }

        private void LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            e.Channel.MessageReceived += Channel_MessageReceived;
        }

        private void LocalUser_LeftChannel(object sender, IrcChannelEventArgs e)
        {
            e.Channel.MessageReceived -= Channel_MessageReceived;
        }

        private void Channel_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var ircChannel = (IrcChannel)sender;
            var ircUser = e.Source as IrcUser;

            if (ircChannel != null && ircUser != null)
            {
                ChannelMessageReceived?.Invoke(new ChannelMessage()
                {
                    ChannelName = ircChannel.Name,
                    UserName = ircUser.NickName,
                    UserHostName = ircUser.HostName,
                    Text = e.Text
                });
            }
        }

        private void IrcClient_Connected(object sender, EventArgs e)
        {
            _ircClient.LocalUser.MessageReceived += LocalUser_MessageReceived;
            _botConfig.Channels.ForEach(channel => _ircClient.Channels.Join(channel));
        }

        private void LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            MessageReceived?.Invoke($"{e.Source.Name}: {e.Targets}: {e.Text}");
        }

        private void IrcClient_ConnectFailed(object sender, IrcErrorEventArgs e) => MessageReceived?.Invoke($"IrcClient_ConnectFailed: {e.Error.Message}");
        private void IrcClient_RawMessageReceived(object sender, IrcRawMessageEventArgs e) => MessageReceived?.Invoke($"RAW: {e.RawContent}");
        
        // TODO: [Kog 9/17/2018] - Need to wire this back through the UI, which is kinda broken right now anyway...
        public void SendMessage(string message) => _ircClient.LocalUser.SendMessage("#spikelite", message);
    }
}
