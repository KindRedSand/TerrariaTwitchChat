﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using TwitchChat.IRCClient;
using TwitchChat.Overrides;

namespace TwitchChat.Events
{
    public class SpawnrateChangingVote : IWorldEvent
    {
        public override int Cooldown { get; set; } = 1500;

        public override float Chance { get; set; } = 0.1f;

        public override IDictionary<int, float> Invaders => null;

        public override string StartString => "Didn't you noticed what where not enought enemies?";

        public override int Length => 3600;

        public static Dictionary<string, int> StandardVotes = new Dictionary<string, int> { };

        protected override void OnStart()
        {
            StandardVotes.Clear();

            ended = false;

            TwitchChat.Instance.CurrentPool = new Dictionary<string, Action<ChannelMessageEventArgs>>
            {
                ["more"] = More,
                ["less"] = Less,
                ["nochange"] = NoChange,
            };

            if (Main.netMode != NetmodeID.MultiplayerClient)
                TwitchChat.Send("Time to change how many enemy will spawn: more, less, nochange");
        }

        private void More(ChannelMessageEventArgs msg)
        {
            if (StandardVotes.ContainsKey(msg.From))
            {
                return;
            }
            else
            {
                StandardVotes.Add(msg.From, 0);
            }
        }

        private void Less(ChannelMessageEventArgs msg)
        {
            if (StandardVotes.ContainsKey(msg.From))
            {
                return;
            }
            else
            {
                StandardVotes.Add(msg.From, 1);
            }
        }

        private void NoChange(ChannelMessageEventArgs msg)
        {
            if (StandardVotes.ContainsKey(msg.From))
            {
                return;
            }
            else
            {
                StandardVotes.Add(msg.From, 2);
            }
        }

        private bool ended = false;

        protected override void OnEnd()
        {
            if (ended)
                return;
            ended = true;
            var votesCount = new SortedDictionary<int, int> { };
            foreach (var it in StandardVotes)
            {
                if (votesCount.ContainsKey(it.Value))
                {
                    votesCount[it.Value]++;
                }
                else
                {
                    votesCount.Add(it.Value, 1);
                }
            }

            int bigger = 0, index = -1, draftIndex = -1;
            foreach (var it in votesCount)
            {
                if (it.Value > bigger)
                {
                    bigger = it.Value;
                    index = it.Key;
                    draftIndex = -1;
                }
                else if (it.Value == bigger)
                {
                    draftIndex = it.Key;
                }
            }


            if (index == draftIndex && index != -1)
            {
                var rand = new WeightedRandom<int>();
                rand.Add(index);
                rand.Add(draftIndex);

                index = rand.Get();
            }

            var world = ModContent.GetInstance<EventWorld>();

            if (Main.netMode != NetmodeID.MultiplayerClient)
                switch (index)
                {
                    case 0:
                        TwitchChat.Send("More enemy!");
                        world.WorldScheduler.Add(() => { GlobalSpawnOverride.StartOverrideSpawnRate(2.5f, 2f); }); 
                        break;
                    case 1:
                        TwitchChat.Send("Less enemy");
                        world.WorldScheduler.Add(() => { GlobalSpawnOverride.StartOverrideSpawnRate(0.01f, 0.3f); });
                        break;
                    case 2:
                        TwitchChat.Send("No spawn changing");
                        world.WorldScheduler.Add(() => { GlobalSpawnOverride.EndOverride(); });
                        break;
                    case -1:
                        TwitchChat.Send("No votes...");
                        break;
                }

        }
    }
}
