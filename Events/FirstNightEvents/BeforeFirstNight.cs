using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TwitchChat.IRCClient;

namespace TwitchChat.Events.FirstNightEvents
{
    public class BeforeFirstNight : VoteEvent
    {
        public override int Cooldown { get; set; } = 100;

        public override float Chance { get; set; } = 0.9f;

        public override TriggerCondition Condition => TriggerCondition.OnDay;

        public override int Length => (int) (Main.dayLength - Main.time);

        public override Func<bool> ConditionAction => () => !ModContent.GetInstance<TwitchWorld>().FirstNight;

        public override IDictionary<int, float> Invaders => null;

        public override string Description => "How should the first night go? eye -> Eye of Cthulhu at beginning, peace -> no mobs during first nigh, mine -> spelunker potion effect during this night";

        public override Dictionary<string, Action<ChannelMessageEventArgs>> VoteSuggestion { get; } =
            new Dictionary<string, Action<ChannelMessageEventArgs>>
            {
                ["eye"] = m =>
                {
                    EventWorld world = ModContent.GetInstance<EventWorld>();

                    ModContent.GetInstance<TwitchWorld>().FirstNight = true;

                    TwitchChat.Send("The eye incoming!");
                    world.WorldScheduler.AddDelayed(
                        () =>
                        {
                            NPC.NewNPC((int) Main.player[0].position.X, (int) Main.player[0].position.Y + 400,
                                NPCID.EyeofCthulhu);
                        }, 500);
                },
                ["peace"] = m =>
                {
                    EventWorld world = ModContent.GetInstance<EventWorld>();

                    ModContent.GetInstance<TwitchWorld>().FirstNight = true;

                    TwitchChat.Send("Night without enemy, have a chill time");
                    world.WorldScheduler.Add(() => { world.StartWorldEvent(new PeaceEvent()); });
                },
                ["mine"] = m =>
                {
                    EventWorld world = ModContent.GetInstance<EventWorld>();

                    ModContent.GetInstance<TwitchWorld>().FirstNight = true;

                    TwitchChat.Send("Mining time!");
                    world.WorldScheduler.Add(() => { world.StartWorldEvent(new SpelunkerEvent()); });
                }
            };

        public override VoteMode VoteMode => VoteMode.EndAction;

        protected override void OnTick()
        {
            if (!IsDayStateValid) TimeLeft = 1;
        }


        public class PeaceEvent : WorldEvent
        {
            public override int Cooldown { get; set; } = 0;

            public override float Chance { get; set; } = 0;

            public override string StartString => "Peace night. Only ducks can shatter your mind...";

            public override string EndString => "Peace night was ended";

            public override IDictionary<int, float> Invaders { get; } = new Dictionary<int, float>
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
                [NPCID.Duck] = 0.5f
            };

            public override TriggerCondition Condition => TriggerCondition.OnNightBegin;

            public override bool DisableOthers => true;
        }

        public class SpelunkerEvent : WorldEvent
        {
            public override int Cooldown { get; set; } = 0;

            public override float Chance { get; set; } = 0;

            public override string StartString => "You start sense all ores";

            public override string EndString => "Strange feeling left you...";

            public override IDictionary<int, float> Invaders => null;

            public override TriggerCondition Condition => TriggerCondition.OnNightBegin;

            protected override void OnTick()
            {
                foreach (Player it in Main.player)
                    if (it.active)
                        if (!it.HasBuff(BuffID.Spelunker))
                            it.AddBuff(BuffID.Spelunker, 5000);
            }
        }
    }
}