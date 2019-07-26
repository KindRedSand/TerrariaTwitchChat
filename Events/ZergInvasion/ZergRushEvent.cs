using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.World;

namespace TwitchChat.Events.ZergInvasion
{
    public class ZergRushEvent : IWorldEvent
    {
        public override int Cooldown => 60000;

        public override float Chance => 0;

        public override IDictionary<int, float> Invaders => null;

        public override bool UseWarning => true;

        public override int StartDelay => 300;

        public override string StartString => "The everlasting raid begins!";
        public override string EndString => "You survived!";
        public override string Warning => "What happen with everyone...";

        public override InvasionType Type => InvasionType.Invasion;

        public override bool MultiplyByPlayers => true;

        internal float mul = 2;
        public override float SpawnRateMul => mul;

        public override float MaxSpawnMul => 20;

        public override int InvasionSize => Main.invasionProgressWave == 5 ? 100 : 300;

        public override Color StartColor => Color.OrangeRed;

        public override Color EndColor => Color.Aqua;

        public override IDictionary<int, float> InvadersDrop
        {
            get
            {
                if(Main.invasionProgressWave == 5)
                return new Dictionary<int, float>()
                {
                    [ItemID.FragmentVortex] = 0.02f,
                    [ItemID.FragmentSolar] = 0.02f,
                    [ItemID.FragmentStardust] = 0.02f,
                    [ItemID.FragmentNebula] = 0.20f,
                };
                return null;
            }
        }



        public override IDictionary<int, List<Tuple<int, float, int>>> InvasionList => new Dictionary<int, List<Tuple<int, float, int>>>
        {
            [1] = new List<Tuple<int, float, int>>
                {
                    Tuple.Create<int,float,int>(NPCID.BloodFeeder, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.CorruptSlime, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.BloodCrawler, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.FaceMonster, 0.4f, 2),
                    Tuple.Create<int,float,int>(NPCID.CorruptBunny, 0.1f, 2),
                    Tuple.Create<int,float,int>(NPCID.Corruptor, 0.5f, 1),
                },
            [2] = new List<Tuple<int, float, int>>
                {
                    Tuple.Create<int,float,int>(NPCID.BloodFeeder, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.CorruptSlime, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.BloodCrawler, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.FaceMonster, 0.4f, 2),
                    Tuple.Create<int,float,int>(NPCID.CorruptBunny, 0.1f, 2),
                    Tuple.Create<int,float,int>(NPCID.Corruptor, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.IceElemental, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.IceTortoise, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.IchorSticker, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.PigronCrimson, 0.4f, 2),
                    Tuple.Create<int,float,int>(NPCID.Psycho, 0.1f, 2),
                },
            [3] = new List<Tuple<int, float, int>>
                {
                    Tuple.Create<int,float,int>(NPCID.BloodFeeder, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.CorruptSlime, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.BloodCrawler, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.FaceMonster, 0.4f, 2),
                    Tuple.Create<int,float,int>(NPCID.IceGolem, 0.1f, 2),
                    Tuple.Create<int,float,int>(NPCID.IceElemental, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.IceTortoise, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.IchorSticker, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.PigronCrimson, 0.4f, 2),
                    Tuple.Create<int,float,int>(NPCID.Psycho, 0.1f, 2),
                    Tuple.Create<int,float,int>(NPCID.QueenBee, 0.01f, 20),
                    Tuple.Create<int,float,int>(NPCID.RuneWizard, 0.01f, 20),
                    Tuple.Create<int,float,int>(NPCID.SantaNK1, 0.01f, 20),
                    Tuple.Create<int,float,int>(NPCID.CorruptBunny, 0.1f, 2),
                    Tuple.Create<int,float,int>(NPCID.Corruptor, 0.5f, 1),
            },
            [4] = new List<Tuple<int, float, int>>
                {
                    Tuple.Create<int,float,int>(NPCID.BloodFeeder, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.CorruptSlime, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.BloodCrawler, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.FaceMonster, 0.4f, 2),
                    Tuple.Create<int,float,int>(NPCID.IceGolem, 0.1f, 2),
                    Tuple.Create<int,float,int>(NPCID.IceElemental, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.IceTortoise, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.IchorSticker, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.PigronCrimson, 0.4f, 2),
                    Tuple.Create<int,float,int>(NPCID.Psycho, 0.1f, 2),
                    Tuple.Create<int,float,int>(NPCID.QueenBee, 0.01f, 20),
                    Tuple.Create<int,float,int>(NPCID.RuneWizard, 0.01f, 20),
                    Tuple.Create<int,float,int>(NPCID.SantaNK1, 0.01f, 20),
                    Tuple.Create<int,float,int>(NPCID.IceQueen, 0.01f, 20),
                    Tuple.Create<int,float,int>(NPCID.ManEater, 0.01f, 20),
                    Tuple.Create<int,float,int>(NPCID.Medusa, 0.01f, 20),
                    Tuple.Create<int,float,int>(NPCID.MartianSaucerCore, 0.01f, 20),
                    Tuple.Create<int,float,int>(NPCID.CorruptBunny, 0.1f, 2),
                    Tuple.Create<int,float,int>(NPCID.Corruptor, 0.5f, 1),
                },
            [5] = new List<Tuple<int, float, int>>
                {
                    Tuple.Create<int,float,int>(NPCID.NebulaBeast, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.NebulaBrain, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.NebulaHeadcrab, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.NebulaSoldier, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.StardustCellBig, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.StardustJellyfishBig, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.StardustSoldier, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.StardustSpiderBig, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.StardustWormHead, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.SolarCorite, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.SolarDrakomire, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.SolarFlare, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.SolarSpearman, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.SolarSroller, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.VortexHornet, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.VortexHornetQueen, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.VortexLarva, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.VortexRifleman, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.VortexSoldier, 0.5f, 1),
                    Tuple.Create<int,float,int>(NPCID.MoonLordCore, 0.005f, 400),
                },
        };


    }
}
