// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using koromo_copy_backend.Setting;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace koromo_copy_backend.ChatBot
{
    public class DiscordBotIdentifier : BotUserIdentifier
    {
        public ulong id { get; private set; }

        public DiscordBotIdentifier(ulong id)
        {
            this.id = id;
        }

        public override bool Equals(BotUserIdentifier other)
        {
            if (!(other is DiscordBotIdentifier))
                return false;
            return id == (other as DiscordBotIdentifier).id;
        }

        public override string ToString()
        {
            return id.ToString();
        }
    }

    public class DiscordBot : BotModel
    {
        DiscordSocketClient client;
        ulong guild_id;

        public override Task SendMessage(BotUserIdentifier user, string message)
        {
            //var guild = client.GetGuild(guild_id);
            var embed = new EmbedBuilder();
            embed.Title = "DCInside Archive Bot";
            embed.WithColor(Color.Blue);
            embed.Description = message;
            //embed.Footer = new EmbedFooterBuilder().
            //embed.Url = "https://koromo_copy_backendbot.Core.kro.kr";
            //var footer = new EmbedFooterBuilder();
            //footer.Text = "봇 초대 https://koromo_copy_backendbot.Core.kro.kr";
            //footer.IconUrl = "https://v.Core.kro.kr";

            //embed.Footer = footer;
            //embed.Fields.Add(new EmbedFieldBuilder { Name = "봇 초대", Value = "https://koromo_copy_backendbot.Core.kro.kr" });
            return (client.GetChannel((user as DiscordBotIdentifier).id) as IMessageChannel).SendMessageAsync("", false, embed.Build());
        }

        public override Task SendMessage(UserDBModel udb, string message)
        {
            return SendMessage(new DiscordBotIdentifier(Convert.ToUInt64(udb.ChatBotId)), message);
        }

        public override void Start()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true,  });

            client.MessageReceived += Client_MessageReceived;
            //client.GuildAvailable += Client_GuildAvailable;
            client.Ready += Client_Ready;

            client.LoginAsync(TokenType.Bot, Settings.Instance.Model.BotSettings.DiscordClientId).Wait();
            client.StartAsync().Wait();
        }

        private async Task Client_Ready()
        {
            Log.Logs.Instance.Push("[Discord Bot] Guild data is ready!");


            ulong x = 691573311129518111;
            var us = client.GetUser("rollrat","1302");
            var channel = client.GetChannel(x);

            await us.SendMessageAsync("fgh");
        }

        //private async Task Client_GuildAvailable(SocketGuild arg)
        //{
        //    //guild_id = arg.Id;
        //    //ulong x = 691573311129518111;
        //    ////
        //    //var guild = client.GetGuild(guild_id);
        //    //await guild.DownloadUsersAsync();
        //    ////(guild.GetChannel(x) as IMessageChannel).SendMessageAsync("asdf").Wait();
        //    //
        //    //var channel = guild.GetChannel(x);
        //    //var cc = client.GetChannel(x);
        //}

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            if (arg is SocketSystemMessage)
            {
                var message = arg as SocketSystemMessage;
                if (message.Type == MessageType.GuildMemberJoin)
                {
                    await BotAPI.ProcessMessage(this, new DiscordBotIdentifier(arg.Channel.Id), "/start");
                }
            }
            else
            {
                if (arg.Content == null || arg.Content == "") return;
                await BotAPI.ProcessMessage(this, new DiscordBotIdentifier(arg.Channel.Id), arg.Content);
            }
        }
    }
}
