using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using TwitchChat.IRCClient;

namespace TwitchChat.Events.FirstNightEvents
{
    public class BeforeFirstNight : IWorldEvent
    {
        public override int Cooldown { get; set; } = 100;

        public override float Chance { get; set; } = 0.9f;

        public override TriggerCondition Condition => TriggerCondition.OnDay;

        public override int Length => (int)(Main.dayLength  - Main.time);

        public override Func<bool> ConditionAction => () =>
        {
            if(!ModContent.GetInstance<TwitchWorld>().firstNight)
                return true;
            return false;
        };

        public override IDictionary<int, float> Invaders => null;

        public static Dictionary<string, int> StandartVotes = new Dictionary<string, int> { };

        protected override void OnStart()
        {
            ended = false;

            StandartVotes.Clear();

            TwitchChat.Instance.CurrentPool = new Dictionary<string, Action<ChannelMessageEventArgs>>
            {
                ["eye"] = Eye,
                ["peace"] = Peace,
                ["mine"] = TimeToMine,
            };

            if (Main.netMode != NetmodeID.MultiplayerClient)
                TwitchChat.Send("How should the first night go? eye -> Eye of Cthulhu at beginning, peace -> no mobs during first nigh, mine -> spelunker potion effect during this night");

        }

        protected override void OnTick()
        {
            if (!IsDaystateValid)
            {
                TimeLeft = 1;
                
            }
                
        }

        private bool ended = false;

        protected override void OnEnd()
        {
            var world = ModContent.GetInstance<EventWorld>();

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
            foreach(var it in votesCount)
            {
                if (it.Value > bigger)
                {
                    bigger = it.Value;
                    index = it.Key;
                    draftIndex = -1;
                }else if(it.Value == bigger)
                {
                    draftIndex = it.Key;
                }
            }

            ModContent.GetInstance<TwitchWorld>().firstNight = true;


            if (index == draftIndex && index != -1)
            {
                WeightedRandom<int> rand = new WeightedRandom<int>();
                rand.Add(index);
                rand.Add(draftIndex);

                index = rand.Get();
            }


            if (Main.netMode != NetmodeID.MultiplayerClient)
                switch (index)
                {
                    case 0:
                        TwitchChat.Send("The eye incomming!");
                        world.WorldScheduler.AddDelayed(() => { NPC.NewNPC((int)Main.player[0].position.X, (int)Main.player[0].position.Y + 400, NPCID.EyeofCthulhu); }, 500);
                        break;
                    case 1:
                        TwitchChat.Send("Night without enemy, have a chill time");
                        world.WorldScheduler.Add(() => { world.StartWorldEvent(new PeaceEvent()); });
                        break;
                    case 2:
                        TwitchChat.Send("Mining time!");
                        world.WorldScheduler.Add(() => { world.StartWorldEvent(new SpelunkerEvent()); });
                        break;
                    case -1:
                        TwitchChat.Send("No votes...");
                        break;
                }

        }

        private void Eye(ChannelMessageEventArgs msg)
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

        private void Peace(ChannelMessageEventArgs msg)
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

        private void TimeToMine(ChannelMessageEventArgs msg)
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

        public class PeaceEvent : IWorldEvent
        {
            public override int Cooldown { get; set; } = 0;

            public override float Chance { get; set; } = 0;

            public override string StartString => "Peace night. Only ducks can shatter your mind...";

            public override string EndString => "Peace night was ended";

            private readonly IDictionary<int, float> op = new Dictionary<int, float>
            {
                [NPCID.Duck] = 0.5f,
                [NPCID.Duck2] = 0.5f,
                [NPCID.Frog] = 0.5f,
                [NPCID.GoldBird] = 0.1f,
                [NPCID.GoldBunny] = 0.1f,
                [NPCID.Bunny] = 0.5f,
                [NPCID.BunnyXmas] = 0.5f,
                [NPCID.Butterfly] = 0.5f,
                [NPCID.GoldButterfly] = 0.1f,
                [NPCID.Duck] = 0.5f,
            };

            public override IDictionary<int, float> Invaders => op;

            public override TriggerCondition Condition => TriggerCondition.OnNightBegin;

            public override bool DisableOthers => true;

        }

        public class SpelunkerEvent : IWorldEvent
        {
            public override int Cooldown { get; set; } = 0;

            public override float Chance { get; set; } = 0;

            public override string StartString => "You start sense all ores";

            public override string EndString => "Strange feeleng lefted you...";

            public override IDictionary<int, float> Invaders => null;

            public override TriggerCondition Condition => TriggerCondition.OnNightBegin;

            protected override void OnTick()
            {
                foreach(var it in Main.player)
                {
                    if(it.active)
                    {
                        if(!it.HasBuff(BuffID.Spelunker))
                        {
                            it.AddBuff(BuffID.Spelunker, 5000);
                        }
                    }
                }
            }
        }
    }
}
