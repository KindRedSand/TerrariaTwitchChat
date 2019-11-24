using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TwitchChat.Events
{
    public class TheTestEvent : IWorldEvent
    {
        public override int MusicId => MusicID.Jungle;

        public override int Cooldown { get; set; } = 5000;

        public override float Chance { get; set; } = 0.00f;

        public override bool DisableOthers => true;

        public override TriggerCondition Condition => TriggerCondition.BothBegin;

        public override float SpawnRateMul => 2;

        public override Func<bool> ConditionAction => () =>
        {
            if (NPC.downedQueenBee)
                return true;
            return false;
        };


        public override int Length => 2000;

        //
        private readonly IDictionary<int, float> dp = new Dictionary<int, float>
        {
            [ItemID.Acorn] = 0.5f,
        };
        public override IDictionary<int, float> InvadersDrop => dp;

        //Couse terraria code is shit, better have one instance, instead return each time new
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
        //
    }
}
