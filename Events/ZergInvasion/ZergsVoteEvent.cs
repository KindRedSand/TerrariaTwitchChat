using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.Utilities;
using TwitchChat.IRCClient;

namespace TwitchChat.Events.ZergInvasion
{
    public class ZergVoteEvent : IWorldEvent
    {
        public override int Cooldown => 1500;

        public override float Chance => NPC.downedMechBossAny ? 0.1f : 0;

        public override IDictionary<int, float> Invaders => null;

        public override string StartString => "Didn't you noticed what where not enought enemies?";

        public override int Lengt => 3600;

        public static Dictionary<string, int> StandartVotes = new Dictionary<string, int> { };

        protected override void OnStart()
        {
            StandartVotes.Clear();

            ended = false;

            TwitchChat.Instance.CurrentPool = new Dictionary<string, Action<ChannelMessageEventArgs>>
            {
                ["more"] = More,
                ["less"] = Less,
                ["nochange"] = NoChange,
            };

            TwitchChat.Send("Zerg invasion incoming! Hom many monsters should invade the world: more, less, nochange");
        }

        private void More(ChannelMessageEventArgs msg)
        {
            if (StandartVotes.ContainsKey(msg.From))
            {
                return;
            }
            else
            {
                StandartVotes.Add(msg.From, 0);
            }
        }

        private void Less(ChannelMessageEventArgs msg)
        {
            if (StandartVotes.ContainsKey(msg.From))
            {
                return;
            }
            else
            {
                StandartVotes.Add(msg.From, 1);
            }
        }

        private void NoChange(ChannelMessageEventArgs msg)
        {
            if (StandartVotes.ContainsKey(msg.From))
            {
                return;
            }
            else
            {
                StandartVotes.Add(msg.From, 2);
            }
        }

        private bool ended = false;

        protected override void OnEnd()
        {
            if (ended)
                return;
            ended = true;
            SortedDictionary<int, int> votesCount = new SortedDictionary<int, int> { };
            foreach (var it in StandartVotes)
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
                WeightedRandom<int> rand = new WeightedRandom<int>();
                rand.Add(index);
                rand.Add(draftIndex);

                index = rand.Get();
            }

            switch (index)
            {
                case 0:
                    TwitchChat.Send("More enemy!");
                    TwitchChat.Instance.GetModWorld<EventWorld>().WorldScheduler.Add(() => { TwitchChat.Instance.GetModWorld<EventWorld>().StartWorldEvent(new ZergRushEvent() { mul = 1000, }); });
                    //TwitchChat.Instance.GetModWorld<EventWorld>().WorldScheduler.AddDelayed(() => { NPC.NewNPC((int)Main.player[0].position.X, (int)Main.player[0].position.Y + 400, NPCID.EyeofCthulhu); }, 500);
                    break;
                case 1:
                    TwitchChat.Send("Less enemy");
                    TwitchChat.Instance.GetModWorld<EventWorld>().WorldScheduler.Add(() => { TwitchChat.Instance.GetModWorld<EventWorld>().StartWorldEvent(new ZergRushEvent() { mul = 1, }); });

                    //TwitchChat.Instance.GetModWorld<EventWorld>().WorldScheduler.Add(() => { TwitchChat.Instance.GetModWorld<EventWorld>().StartWorldEvent(new PeaceEvent()); });
                    break;
                case 2:
                    TwitchChat.Send("No spawn changing");
                    TwitchChat.Instance.GetModWorld<EventWorld>().WorldScheduler.Add(() => { TwitchChat.Instance.GetModWorld<EventWorld>().StartWorldEvent(new ZergRushEvent()); });

                    //TwitchChat.Instance.GetModWorld<EventWorld>().WorldScheduler.Add(() => { TwitchChat.Instance.GetModWorld<EventWorld>().StartWorldEvent(new SpelunkerEvent()); });
                    break;
                case -1:
                    TwitchChat.Send("No votes...");
                    break;
            }

        }
    }
}