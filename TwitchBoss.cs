using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Utilities;
using TwitchChat.Events;
using TwitchChat.IRCClient;

namespace TwitchChat
{
    public static class TwitchBoss
    {
        public static string Boss { get; set; }
        public static Dictionary<string, Action<ChannelMessageEventArgs>> Commands { get; } = new Dictionary<string, Action<ChannelMessageEventArgs>>();
        public static DateTimeOffset Cooldown = DateTimeOffset.Now;
        private static TwitchBossEvent BossEvent { get; } = new TwitchBossEvent();
        public static int CooldownLength { get; set; } = 60;

        /// <summary>
        /// Adds new command to boss
        /// </summary>
        /// <param name="commandName">Command alias</param>
        /// <param name="action">Action what performed once command entered</param>
        /// <returns>Return true if command was added or false if this command all ready exist</returns>
        public static bool AddCommand(string commandName, Action<ChannelMessageEventArgs> action)
        {
            if (CommandExist(commandName.ToLower()) || action == null)
                return false;
            Commands.Add(commandName.ToLower(), action);
            return true;
        }

        /// <summary>
        /// Remove a command from pool
        /// </summary>
        /// <param name="commandName">Command alias</param>
        public static void RemoveCommand(string commandName)
        {
            if (Commands.ContainsKey(commandName)) 
                Commands.Remove(commandName);
        }

        /// <summary>
        /// Clear commands pool
        /// </summary>
        public static void ClearPool()
        {
            Commands.Clear();
            BossEvent.End();
        }

        public static bool CommandExist(string commandName) => Commands.ContainsKey(commandName);

        internal static bool ProcessCommand(ChannelMessageEventArgs m)
        {
            var cmd = (from x in Commands where m.Message.ToLower().StartsWith(x.Key) select x.Value).ToArray();//Make it ToArray() to calm down ReSharper
            if (!cmd.Any())
                return false;
            cmd.First().Invoke(m);
            Cooldown = DateTimeOffset.Now.AddSeconds(CooldownLength);
            return true;
        }

        public static void SelectNew()
        {
            Boss = string.Empty;
#if DEBUG
            Cooldown = DateTimeOffset.Now.AddSeconds(5);
#else
            Cooldown = DateTimeOffset.Now.AddSeconds(CooldownLength);
#endif
            BossEvent.Start();
        }

        public static void ShatterBoss()
        {
            if (!BossEvent.Started && BossEvent.ChanceAction.Invoke())
                SelectNew();
            else if(BossEvent.Started && BossEvent.Part.Any() && Cooldown < DateTimeOffset.Now)
                BossEvent.End();
        }

        private static readonly UnifiedRandom Rand = new UnifiedRandom();

        internal static void InitialiseDefault()
        {
            AddCommand("test", (m) =>
            {
                if (Main.netMode == NetmodeID.Server)
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Main.player[i].GetModPlayer<EventPlayer>().Teleportationpotion = true;
                    }
                else if (Main.netMode == NetmodeID.SinglePlayer)
                    Main.LocalPlayer.GetModPlayer<EventPlayer>().Teleportationpotion = true;
            });

            AddCommand("heal", (m) =>
            {
                if (Main.netMode != NetmodeID.SinglePlayer)
                    foreach (Player it in Main.player)
                    {
                        if (it.active)
                            for (var i = Rand.Next(20); i > 0; i--)
                            {
                                Item.NewItem(it.position, ItemID.Heart, noGrabDelay: true);
                                Item.NewItem(it.position, ItemID.Star, noGrabDelay: true);
                            }
                    }
                else
                    for (var i = Rand.Next(20); i > 0; i--)
                    {
                        Item.NewItem(Main.LocalPlayer.position, ItemID.Heart, noGrabDelay: true);
                        Item.NewItem(Main.LocalPlayer.position, ItemID.Star, noGrabDelay: true);
                    }
            });

            AddCommand("buff", (m) =>
            {
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    foreach (Player it in Main.player)
                        if (it.active)
                        {
                            for (var i = Rand.Next(3); i > 0; i--)
                                Item.NewItem(it.position, ItemID.NebulaPickup1, noGrabDelay: true);
                            for (var i = Rand.Next(3); i > 0; i--)
                                Item.NewItem(it.position, ItemID.NebulaPickup2, noGrabDelay: true);
                            for (var i = Rand.Next(3); i > 0; i--)
                                Item.NewItem(it.position, ItemID.NebulaPickup3, noGrabDelay: true);
                        }
                }
                else
                {
                    for (var i = Rand.Next(3); i > 0; i--)
                        Item.NewItem(Main.LocalPlayer.position, ItemID.NebulaPickup1, noGrabDelay: true);
                    for (var i = Rand.Next(3); i > 0; i--)
                        Item.NewItem(Main.LocalPlayer.position, ItemID.NebulaPickup2, noGrabDelay: true);
                    for (var i = Rand.Next(3); i > 0; i--)
                        Item.NewItem(Main.LocalPlayer.position, ItemID.NebulaPickup3, noGrabDelay: true);
                }
            });

            AddCommand("death", (m) =>
            {
                if (Main.netMode != NetmodeID.SinglePlayer)
                    foreach (Player it in Main.player)
                    {
                        if (!it.active)
                            continue;
                        for (var i = Rand.Next(3); i > 0; i--)
                            Projectile.NewProjectile(it.position, new Vector2(0, 3), ProjectileID.EyeFire,
                                20, 0);
                    }
                else
                    for (var i = Rand.Next(3); i > 0; i--)
                        Projectile.NewProjectile(Main.LocalPlayer.position, new Vector2(0, 3),
                            ProjectileID.EyeFire, 20, 0);
            });

            AddCommand("quit", (m) =>
            {
                TwitchChat.Send($"@{m.Badge.DisplayName} become a pussy and no more chat boss!");
                Boss = "";
            });
        }
    }
}
