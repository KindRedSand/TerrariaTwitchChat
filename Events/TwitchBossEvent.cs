using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Utilities;
using TwitchChat.IRCClient;

namespace TwitchChat.Events
{
    public class TwitchBossEvent : VoteEvent
    {
        internal readonly List<string> part = new List<string>();
        private readonly WeightedRandom<string> rand = new WeightedRandom<string>();

        private DateTimeOffset assignTime = DateTimeOffset.Now;
        public override int Cooldown { get; set; } = 1000;

        public override float Chance { get; set; } = 0f;

        public override IDictionary<int, float> Invaders => null;

        public override string StartString => "It's time to select new chat boss!";

        public override string EndString => "New boss now is... ";

        public override int Length => 60 * 30;

        /// <summary>
        /// Make mod event registry to ignore this event
        /// </summary>
        public override Func<bool> ConditionAction => () => false;

        public override Func<bool> ChanceAction => () =>
        {
            if (string.IsNullOrEmpty(TwitchBoss.Boss))
                return true;

            return Main.rand.NextFloat() < Chance * ((DateTimeOffset.Now - assignTime).TotalMinutes * 0.15);
        };

        public override string Description => throw new NotImplementedException();

        public override Dictionary<string, Action<ChannelMessageEventArgs>> VoteSuggestion { get; }=new Dictionary<string, Action<ChannelMessageEventArgs>>
        {
            ["boss"] = (m) =>
            {

            }
        };

        public override VoteMode VoteMode => VoteMode.InstantAction;

        public void Start() => OnStart();
        public void End() => OnEnd();

        public bool Started { get; private set; }

        protected override void OnStart()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            rand.Clear();

            part.Clear();

            Started = true;

            TwitchChat.Instance.Irc.ChannelMessage += Handle;

            TwitchChat.Send(StartString + " Quick write \"boss\" in chat!");
        }

        protected override void OnEnd()
        {
            Started = false;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (rand.elements.Count == 0)
            {
                //TwitchChat.Send("No one was selected to become chat boss");
                //TwitchBoss.Boss = string.Empty;
                //TwitchChat.Post("No one...", Color.White);
                return;
            }


            var t = rand.Get();

            TwitchBoss.Boss = t;

            assignTime = DateTimeOffset.Now;
            TwitchChat.Post($"New boss is @[c/{TwitchChat.TwitchColor}:{TwitchBoss.Boss}]", Color.White);

            if (Main.netMode != NetmodeID.MultiplayerClient)
                TwitchChat.Send(EndString + $" @{TwitchBoss.Boss} you can use " +
                                $"{string.Join(" ", (from s in TwitchBoss.Commands select s.Key))}");

            TwitchChat.Instance.Irc.ChannelMessage -= Handle;
        }


        private void Handle(object sender, ChannelMessageEventArgs msg)
        {
            if (part.Contains(msg.From))
                return;

            part.Add(msg.From);
            rand.Add(msg.From, msg.Badge.sub ? 2 : 1);
        }
    }
}