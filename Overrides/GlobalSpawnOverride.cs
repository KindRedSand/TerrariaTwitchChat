using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using TwitchChat.Events;

namespace TwitchChat.Overrides
{
    public class GlobalSpawnOverride : GlobalNPC
    {
        private static bool isOverriding = false;
        public static bool IsOverrdie => isOverriding;
        private static float spawnRateOverride = 0, maxSpawnsOverride = 0;

        public static void StartOverrideSpawnRate(float spawnrate, float maxSpawns)
        {
            isOverriding = true;
            spawnRateOverride = spawnrate;
            maxSpawnsOverride = maxSpawns;
        }

        public static void EndOverride() => isOverriding = false;


        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
         

            if (isOverriding)
            {
                spawnRate = (int)Math.Floor(spawnRate/spawnRateOverride);
                maxSpawns = (int)Math.Floor(maxSpawnsOverride * maxSpawns);
            }else
                base.EditSpawnRate(player, ref spawnRate, ref maxSpawns);
        }

        private static bool SpawnPoolOverride = false;
        private static bool useTuple = false;
        public static bool IsSpawnpoolOverrided => SpawnPoolOverride;
        private static IDictionary<int, float> spawnPool = null;
        public static IDictionary<int, float> SpawnPool => spawnPool;
        private static bool DisableOtherSpawn = false;
        private static bool IsDisableOtherSpawns => DisableOtherSpawn;
        private static bool enchanceLifetime = false;

        private static IDictionary<int, Tuple<float, int>> invasionTuple = null;
        public static IDictionary<int, Tuple<float, int>> InvasionList => invasionTuple;

        public static void OverridePool(IDictionary<int, Tuple<float, int>> pool, bool disableOthers, bool enchanceLifetime = false)
        {
            invasionTuple = pool;
            DisableOtherSpawn = disableOthers;
            SpawnPoolOverride = true;
            useTuple = true;
        }

        public static void OverridePool(IDictionary<int, float> pool, bool disableOthers, bool enchanceLifetime = false)
        {
            spawnPool = pool;
            DisableOtherSpawn = disableOthers;
            SpawnPoolOverride = true;
            useTuple = false;
        }

        private static bool ItempoolOverride = false;
        public static bool IsItemPoolOverrided => ItempoolOverride;
        private static IDictionary<int, float> itemPool = null;
        public static IDictionary<int, float> ItemPool => itemPool;
        public static void OverrideItemPool(IDictionary<int, float> pool)
        {
            itemPool = pool;
            ItempoolOverride = true;
        }

        public static void DisableItemPool() => ItempoolOverride = false;

        public static void DisablePoolOverride() { SpawnPoolOverride = false; enchanceLifetime = false; useTuple = false; DisableOtherSpawn = false; }

        public override void PostAI(NPC npc)
        {
            if (Main.netMode == 1)
                return;
            //Changes NPCs so they do not despawn when invasion up and invasion at spawn
            if (enchanceLifetime)
            {
                npc.timeLeft = 1000;
            }
        }

        public override void NPCLoot(NPC npc)
        {
            if (useTuple)
            {
                if (invasionTuple.ContainsKey(npc.type))
                {
                    mod.GetModWorld<EventWorld>().CurrentEvent.TimeLeft -= invasionTuple[npc.type].Item2;
                }
            }

            if (Main.netMode == 1)
                return;

            if (ItempoolOverride && ItemPool != null)
            {
                var rand = new WeightedRandom<int>();
                foreach(var it in ItemPool)
                {
                    rand.Add(it.Key, it.Value);
                }
                if (rand.random.NextFloat(100)>55)
                {
                    Item.NewItem(npc.position, npc.Size, rand.Get());
                }
            }
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (Main.netMode == 1)
                return;

            if (!SpawnPoolOverride)
                return;

            if (DisableOtherSpawn)
            {
                if (!useTuple && spawnPool != null || useTuple && invasionTuple != null)
                {
                    pool.Clear();
                }
            }
            {
                if(!useTuple)
                foreach(var it in spawnPool)
                {
                    if (pool.ContainsKey(it.Key))
                    {
                        pool[it.Key] = it.Value;
                    }
                    else
                    {
                        pool.Add(it.Key, it.Value);
                    }
                }
                else
                {
                    foreach(var it in invasionTuple)
                    {
                        if (pool.ContainsKey(it.Key))
                        {
                            pool[it.Key] = it.Value.Item1;
                        }
                        else
                        {
                            pool.Add(it.Key, it.Value.Item1);
                        }
                    }
                }
            }
        }
       

        static internal void HandleCleanup()
        {
            spawnPool = null;
            itemPool = null;
            invasionTuple = null;
            ItempoolOverride = false;
            isOverriding = false;
            SpawnPoolOverride = false;
        }
    }
}
