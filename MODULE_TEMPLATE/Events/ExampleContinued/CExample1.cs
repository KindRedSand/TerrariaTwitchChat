using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using TwitchChat.Events;

namespace TwitchModule.Events.ExampleContinued
{
    public class CExample1 : WorldEvent
    {
        public override int Cooldown { get; set; } = 3600;
        /// <summary>
        /// This event supposed to be started by other event, so chance should be 0
        /// </summary>
        public override float Chance { get; set; } = 0;

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
