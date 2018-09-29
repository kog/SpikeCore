﻿using System.Threading;
using System.Threading.Tasks;

using Foundatio.Messaging;

using Microsoft.AspNetCore.Identity;

using SpikeCore.Data.Models;
using SpikeCore.Irc.Configuration;
using SpikeCore.MessageBus;

namespace SpikeCore.Irc
{
    public class IrcConnection : IIrcConnection, IMessageHandler<IrcConnectMessage>, IMessageHandler<IrcSendMessage>
    {
        private readonly IIrcClient _ircClient;
        private readonly IMessageBus _messageBus;
        private readonly UserManager<SpikeCoreUser> _userManager;
        private readonly IrcConnectionConfig _config;

        public IrcConnection(IIrcClient ircClient, IMessageBus messageBus, UserManager<SpikeCoreUser> userManager, IrcConnectionConfig botConfig)
        {
            _ircClient = ircClient;
            _messageBus = messageBus;
            _userManager = userManager;
            _config = botConfig;
        }

        public Task HandleMessageAsync(IrcConnectMessage message, CancellationToken cancellationToken)
        {
            _ircClient.ChannelMessageReceived = async (channelMessage) =>
            {
                var user = await _userManager.FindByLoginAsync("IrcHost", channelMessage.UserHostName);

                var ircChannelMessageMessage = new IrcChannelMessageMessage()
                {
                    ChannelName = channelMessage.ChannelName,
                    UserName = channelMessage.UserName,
                    UserHostName = channelMessage.UserHostName,
                    Text = channelMessage.Text,
                    IdentityUser = user
                };

                await _messageBus.PublishAsync(ircChannelMessageMessage);
            };

            _ircClient.MessageReceived = (receivedMessage) => _messageBus.PublishAsync(new IrcReceiveMessage(receivedMessage));

            _ircClient.Connect(_config.Host, _config.Port, _config.Nickname, _config.Channels);

            return Task.CompletedTask;
        }

        public Task HandleMessageAsync(IrcSendMessage message, CancellationToken cancellationToken)
        {
            _ircClient.SendMessage(message.Message);

            return Task.CompletedTask;
        }
    }
}