using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using TwitchChat.Events.ZergInvasion;
using TwitchChat.Overrides;

namespace TwitchChat
{
    public class EventPlayer : ModPlayer
    {
        internal bool frameUpdated = false;

        public static LunarSkies LunarSky = LunarSkies.None;

        public override void UpdateBiomeVisuals()
        {
            
        }

        public override void PostUpdate()
        {
            if (Main.netMode == 1)
            {
                if(mod.GetModWorld<EventWorld>().CurrentEvent != null)
                {
                    mod.GetModWorld<EventWorld>().CurrentEvent.PerformTick(mod.GetModWorld<EventWorld>(), (TwitchChat)mod);
                }
            }
        }

        public override void UpdateDead()
        {
            var e = mod.GetModWorld<EventWorld>().CurrentEvent;
            if (e is ZergRushEvent)
                e.TimeLeft -= 20;
        }


        public override void UpdateBiomes()
        {
            switch (LunarSky)
            {
                case LunarSkies.Vortex:
                    player.ZoneTowerVortex = true;
                    break;
                case LunarSkies.Solar:
                    player.ZoneTowerSolar = true;
                    break;
                case LunarSkies.Stardust:
                    player.ZoneTowerStardust = true;
                    break;
                case LunarSkies.Nebula:
                    player.ZoneTowerNebula = true;
                    break;
            }
        }

    }

    public enum LunarSkies : byte
    {
        Vortex,
        Solar,
        Stardust,
        Nebula,
        None
    }
    
}
