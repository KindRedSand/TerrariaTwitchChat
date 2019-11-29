using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TwitchChat.Overrides;

namespace TwitchChat.Events
{
    public class LunarInvasion : WorldEvent
    {
        public override int MusicId => MusicID.TheTowers;

        public override int Cooldown { get; set; } = 5000;

        public override bool UseWarning => true;

        public override int StartDelay => 300;

        public override float Chance { get; set; } = 0.0f;

        public override string StartString => "Your doom begin!";
        public override string EndString => "You survived!";
        public override string Warning => "Impeting doom approach...";

        public override TriggerCondition Condition => TriggerCondition.OnNightBegin;

        //Make this event as post moon lord
        public override Func<bool> ConditionAction => () => NPC.downedMoonlord;


        public override InvasionType Type => InvasionType.Invasion;

        /// <summary>
        ///     We use <see cref="WorldEvent.InvasionList" /> instead this line couse <see cref="WorldEvent.Type" /> selected as
        ///     <see cref="InvasionType.Invasion" />
        /// </summary>
        public override IDictionary<int, float> Invaders => null;

        public override bool MultiplyByPlayers => true;

        public override float SpawnRateMul => 2;

        public override float MaxSpawnMul => 2;

        ///Just 100 scores for each tower
        public override int InvasionSize => 100;

        public override Color StartColor => Color.OrangeRed;

        public override Color EndColor => Color.Aqua;

        public override bool DisableOthers => Main.invasionProgressWave != 5;

        /// <summary>
        ///     Wave setup: Vortex, Solar, Stardust, Nebula, Moon lord
        /// </summary>

        public override IDictionary<int, float> InvadersDrop
        {
            get
            {
                switch (Main.invasionProgressWave)
                {
                    case 1:
                        return new Dictionary<int, float>
                        {
                            [ItemID.FragmentVortex] = 0.2f
                        };
                    case 2:
                        return new Dictionary<int, float>
                        {
                            [ItemID.FragmentSolar] = 0.2f
                        };
                    case 3:
                        return new Dictionary<int, float>
                        {
                            [ItemID.FragmentStardust] = 0.2f
                        };
                    case 4:
                        return new Dictionary<int, float>
                        {
                            [ItemID.FragmentNebula] = 0.2f
                        };
                    default:
                        return new Dictionary<int, float>
                        {
                            [ItemID.FragmentVortex] = 0.2f,
                            [ItemID.FragmentSolar] = 0.2f,
                            [ItemID.FragmentStardust] = 0.2f,
                            [ItemID.FragmentNebula] = 0.2f
                        };
                }
            }
        }

        public override IDictionary<int, List<Tuple<int, float, int>>> InvasionList => new Dictionary<int, List<Tuple<int, float, int>>>
        {
            [1] = new List<Tuple<int, float, int>>
            {
                Tuple.Create<int, float, int>(NPCID.VortexHornet, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.VortexHornetQueen, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.VortexLarva, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.VortexRifleman, 0.4f, 2),
                Tuple.Create<int, float, int>(NPCID.VortexSoldier, 0.4f, 2)
            },
            [2] = new List<Tuple<int, float, int>>
            {
                Tuple.Create<int, float, int>(NPCID.SolarCorite, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.SolarDrakomire, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.SolarFlare, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.SolarSpearman, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.SolarSroller, 0.5f, 1)
            },
            [3] = new List<Tuple<int, float, int>>
            {
                Tuple.Create<int, float, int>(NPCID.StardustCellBig, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.StardustJellyfishBig, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.StardustSoldier, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.StardustSpiderBig, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.StardustWormHead, 0.5f, 1)
            },
            [4] = new List<Tuple<int, float, int>>
            {
                Tuple.Create<int, float, int>(NPCID.NebulaBeast, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.NebulaBrain, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.NebulaHeadcrab, 0.5f, 1),
                Tuple.Create<int, float, int>(NPCID.NebulaSoldier, 0.5f, 1)
            },
            [5] = new List<Tuple<int, float, int>>
            {
                Tuple.Create<int, float, int>(NPCID.NebulaBeast, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.NebulaBrain, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.NebulaHeadcrab, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.NebulaSoldier, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.StardustCellBig, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.StardustJellyfishBig, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.StardustSoldier, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.StardustSpiderBig, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.StardustWormHead, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.SolarCorite, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.SolarDrakomire, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.SolarFlare, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.SolarSpearman, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.SolarSroller, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.VortexHornet, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.VortexHornetQueen, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.VortexLarva, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.VortexRifleman, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.VortexSoldier, 0.5f, 0),
                Tuple.Create<int, float, int>(NPCID.MoonLordCore, 0.0f, 100)
            }
        };


        protected override void OnStart() { }

        protected override void OnEnd() { EventPlayer.LunarSky = LunarSkies.None; }

        //protected override void OnTick()
        //{
        //    base.OnTick();
        //}

        public override void OnWaveChange()
        {
            switch (Main.invasionProgressWave)
            {
                case 1:
                    TwitchChat.Post("Vortex tower was awoken!", Color.ForestGreen);
                    EventPlayer.LunarSky = LunarSkies.Vortex;
                    if (Main.netMode != 1)
                        GlobalSpawnOverride.OverrideItemPool(InvadersDrop);
                    break;
                case 2:
                    TwitchChat.Post("Vortex tower was defeated!", Color.ForestGreen);
                    TwitchChat.Post("Solar tower was awoken!", Color.Orange);
                    EventPlayer.LunarSky = LunarSkies.Solar;
                    if (Main.netMode != 1)
                        GlobalSpawnOverride.OverrideItemPool(InvadersDrop);
                    break;
                case 3:
                    TwitchChat.Post("Solar tower was defeated!", Color.Orange);
                    TwitchChat.Post("Stardust tower was awoken!", Color.AliceBlue);
                    EventPlayer.LunarSky = LunarSkies.Stardust;
                    if (Main.netMode != 1)
                        GlobalSpawnOverride.OverrideItemPool(InvadersDrop);
                    break;
                case 4:
                    TwitchChat.Post("Stardust tower was defeated!", Color.AliceBlue);
                    TwitchChat.Post("Nebula tower was awoken!", Color.Violet);
                    EventPlayer.LunarSky = LunarSkies.Nebula;
                    if (Main.netMode != 1)
                        GlobalSpawnOverride.OverrideItemPool(InvadersDrop);
                    break;
                case 5:
                    TwitchChat.Post("Nebula tower was defeated!", Color.Violet);
                    TwitchChat.Post("Impeting doom approach...", Color.PaleGreen);
                    EventPlayer.LunarSky = LunarSkies.None;
                    if (Main.netMode != 1)
                    {
                        NPC.NewNPC(Main.spawnTileX, Main.spawnTileY, NPCID.MoonLordCore);
                        GlobalSpawnOverride.OverrideItemPool(InvadersDrop);
                    }

                    break;
            }
        }
    }
}