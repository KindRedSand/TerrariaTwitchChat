using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using TwitchChat.Events;

namespace TwitchModule.Events.ExampleContinued
{
    public class CExample2 : WorldEvent
    {
        public override int Cooldown { get; set; } = 3600;
        public override float Chance { get; set; } = 0;
        public override IDictionary<int, float> InvadersDrop { get; } = new Dictionary<int, float>
        {
            [ItemID.DirtBlock] = 0.5f
        };

        /// <summary>
        /// Disable spawn other mobs
        /// </summary>
        public override bool DisableOthers => true;

        public override IDictionary<int, float> Invaders { get; } = new Dictionary<int, float>
        {
            [NPCID.Zombie] = 0.5f,
            [NPCID.Vulture] = 0.5f,
            [NPCID.UmbrellaSlime] = 0.5f,
            [NPCID.FlyingFish] = 0.1f,
            [NPCID.CaveBat] = 0.1f,
            [NPCID.Bunny] = 0.5f,
            [NPCID.BunnyXmas] = 0.5f,
            [NPCID.Butterfly] = 0.5f,
            [NPCID.GoldButterfly] = 0.1f,
            [NPCID.Duck] = 0.5f
        };
    }
}
