using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace TwitchChat.Events
{
    public class TheTestEvent : WorldEvent
    {
        public override int MusicId => MusicID.Jungle;

        public override int Cooldown { get; set; } = 5000;

        public override float Chance { get; set; } = 0.00f;

        public override bool DisableOthers => true;

        public override TriggerCondition Condition => TriggerCondition.BothBegin;

        public override float SpawnRateMul => 2;

        public override Func<bool> ConditionAction => () => NPC.downedQueenBee;


        public override int Length => 2000;

        public override IDictionary<int, float> InvadersDrop { get; } = new Dictionary<int, float>
        {
            [ItemID.Acorn] = 0.5f
        };

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
    }
}