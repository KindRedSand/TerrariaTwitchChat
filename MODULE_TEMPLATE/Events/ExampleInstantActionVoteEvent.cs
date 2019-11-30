using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TwitchChat.Events;
using TwitchChat.IRCClient;

namespace TwitchModule.Events
{
    public class ExampleInstantActionVoteEvent : VoteEvent
    {
        public override int Cooldown { get; set; } = 3000;

        /// <summary>
        /// This event can happen with 10% chance
        /// </summary>
        public override float Chance { get; set; } = 0.1f;

        public override IDictionary<int, float> Invaders => null;

        public override string Description => "Test event with instant actions";

        public override Dictionary<string, Action<ChannelMessageEventArgs>> VoteSuggestion { get; } = new Dictionary<string, Action<ChannelMessageEventArgs>>()
        {
            ["coin"] = (m) =>
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Item.NewItem(Main.LocalPlayer.getRect(), ItemID.PlatinumCoin, 2);
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active)
                        {
                            Item.NewItem(Main.player[i].getRect(), ItemID.PlatinumCoin, 2);
                        }
                    }
                }
            },
            ["shadowball"] = (m) =>
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Projectile.NewProjectile(Main.LocalPlayer.position + new Vector2(0, 20), new Vector2(0, -10), ProjectileID.Shadowflames, 1, 0);
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active)
                        {
                            Projectile.NewProjectile(Main.player[i].position + new Vector2(0, 20), new Vector2(0, -10), ProjectileID.Shadowflames, 1, 0);
                        }
                    }
                }
            }


        };

        /// <summary>
        /// Instant action make function call for every vote made in chat!
        /// </summary>
        public override VoteMode VoteMode => VoteMode.InstantAction;
    }
}
