using Terraria;
using Terraria.ModLoader;
using TwitchChat.Events;
using TwitchChat.Events.ZergInvasion;

namespace TwitchChat
{
    public class EventPlayer : ModPlayer
    {
        public static LunarSkies LunarSky = LunarSkies.None;

        public bool Teleportationpotion;

        public override void UpdateBiomeVisuals() { }

        public override void PostUpdate()
        {
            EventWorld world = ModContent.GetInstance<EventWorld>();

            if (Teleportationpotion)
            {
                Teleportationpotion = false;
                player.TeleportationPotion();
            }

            if (Main.netMode == 1)
                world.CurrentEvent?.PerformTick(world, (TwitchChat) mod);


                
        }

        public override void UpdateDead()
        {
            EventWorld world = ModContent.GetInstance<EventWorld>();

            WorldEvent e = world.CurrentEvent;
            if (e is ZergRushEvent && Main.rand.NextFloat() < 0.2f)
                e.TimeLeft -= 1;


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