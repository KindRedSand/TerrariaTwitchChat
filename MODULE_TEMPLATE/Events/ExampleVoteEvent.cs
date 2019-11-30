using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using TwitchChat.Events;
using TwitchChat.IRCClient;

namespace TwitchModule.Events
{
    public class ExampleVoteEvent : VoteEvent
    {
        public override int Cooldown { get; set; } = 3000;

        /// <summary>
        /// This event can happen with 10% chance
        /// </summary>
        public override float Chance { get; set; } = 0.1f;

        public override IDictionary<int, float> Invaders => null;

        public override string Description => "Test vote event";

        public override Dictionary<string, Action<ChannelMessageEventArgs>> VoteSuggestion { get; } = new Dictionary<string, Action<ChannelMessageEventArgs>>()
        {
            ["test1"] = (m) =>
            {
                TwitchChat.TwitchChat.Send("First vote win. This message was send to twitch chat");
                TwitchChat.TwitchChat.Post("First vote win. This message was send to game chat", Color.White);

                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Item.NewItem(Main.LocalPlayer.getRect(), ItemID.DirtBlock, 990);
                }else if (Main.netMode == NetmodeID.Server)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active)
                        {
                            Item.NewItem(Main.player[i].getRect(), ItemID.DirtBlock, 990);
                        }
                    }
                }
            },
            ["2test"] = (m) =>
            {
                TwitchChat.TwitchChat.Send("Second vote win. This message was send to twitch chat");
                TwitchChat.TwitchChat.Post("Second vote win. This message was send to game chat", Color.White);

                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Item.NewItem(Main.LocalPlayer.getRect(), ItemID.DirtBlock, 990);
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active)
                        {
                            Item.NewItem(Main.player[i].getRect(), ItemID.DirtBlock, 990);
                        }
                    }
                }
            }


        };

        public override VoteMode VoteMode => VoteMode.EndAction;
    }
}
