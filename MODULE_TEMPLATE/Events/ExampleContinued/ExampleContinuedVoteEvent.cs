using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TwitchChat;
using TwitchChat.Events;
using TwitchChat.IRCClient;

namespace TwitchModule.Events.ExampleContinued
{
    public class ExampleContinuedVoteEvent : VoteEvent
    {
        public override int Cooldown { get; set; } = 3000;

        /// <summary>
        /// This event can happen with 10% chance
        /// </summary>
        public override float Chance { get; set; } = 0.1f;

        public override IDictionary<int, float> Invaders => null;

        public override string Description => "Test event with side effects";

        public override Dictionary<string, Action<ChannelMessageEventArgs>> VoteSuggestion { get; } = new Dictionary<string, Action<ChannelMessageEventArgs>>()
        {
            ["test1"] = (m) =>
            {
                TwitchChat.TwitchChat.Send("First vote win. This message was send to twitch chat");
                TwitchChat.TwitchChat.Post("First vote win. This message was send to game chat", Color.White);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    ModContent.GetInstance<EventWorld>().AddDelayedTask(() =>
                    {
                        ModContent.GetInstance<EventWorld>().StartWorldEvent(new CExample1());
                    }, 500);
            },
            ["2test"] = (m) =>
            {
                TwitchChat.TwitchChat.Send("Second vote win. This message was send to twitch chat");
                TwitchChat.TwitchChat.Post("Second vote win. This message was send to game chat", Color.White);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    ModContent.GetInstance<EventWorld>().AddDelayedTask(() =>
                    {
                        ModContent.GetInstance<EventWorld>().StartWorldEvent(new CExample2());
                    }, 500);
            }


        };

        public override VoteMode VoteMode => VoteMode.EndAction;
    }
}
