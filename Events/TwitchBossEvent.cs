using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Utilities;
using TwitchChat.IRCClient;

namespace TwitchChat.Events
{
    public class TwitchBossEvent : WorldEvent
    {
        private readonly List<string> part = new List<string>();
        private readonly WeightedRandom<string> rand = new WeightedRandom<string>();

        private DateTimeOffset assignTime = DateTimeOffset.Now;
        public override int Cooldown { get; set; } = 1000;

        public override float Chance { get; set; } = 0.1f;

        public override IDictionary<int, float> Invaders => null;

        public override string StartString => "It's time to select new chat boss!";

        public override string EndString => "New boss now is... ";

        public override int Length => 60 * 30;

        public override Func<bool> ChanceAction => () =>
        {
            if (string.IsNullOrEmpty(TwitchChat.Instance.ChatBoss))
                return true;

            return Main.rand.NextFloat() < Chance * ((DateTimeOffset.Now - assignTime).TotalMinutes * 0.15);
        };

        protected override void OnStart()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            rand.Clear();

            part.Clear();
            if (TwitchChat.Instance.CurrentPool == null)
                TwitchChat.Instance.CurrentPool = new Dictionary<string, Action<ChannelMessageEventArgs>>();
            TwitchChat.Instance.CurrentPool.Clear();
            TwitchChat.Instance.CurrentPool.Add("boss", Handle);

            TwitchChat.Send(StartString + " Quick write boss in chat!");
        }

        protected override void OnEnd()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (rand.elements.Count == 0)
            {
                TwitchChat.Send("No one was selected to become chat boss");
                TwitchChat.Instance.ChatBoss = TwitchChat.Instance.Username;
                TwitchChat.Post("No one...", Color.White);
                return;
            }


            var t = rand.Get();

            TwitchChat.Instance.ChatBoss = t;

            assignTime = DateTimeOffset.Now;
            TwitchChat.Post($" @{TwitchChat.Instance.ChatBoss}", Color.Purple);

            if (Main.netMode != NetmodeID.MultiplayerClient)
                TwitchChat.Send(EndString + $" @{TwitchChat.Instance.ChatBoss} you can use heal, buff, death or quit");
        }


        private void Handle(ChannelMessageEventArgs msg)
        {
            if (!part.Contains(msg.From))
            {
                part.Add(msg.From);
                rand.Add(msg.From, msg.Badge.sub ? 2 : 1);
            }
        }
    }
}