using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace TwitchChat.Overrides
{
    public class GlobalSpawnOverride : GlobalNPC
    {
        private static float spawnRateOverride, maxSpawnsOverride;

        private static bool useTuple;
        private static bool enchanceLifetime;

        public static bool IsOverrdie { get; private set; }

        public static bool IsSpawnpoolOverrided { get; private set; }

        public static IDictionary<int, float> SpawnPool { get; private set; }

        private static bool IsDisableOtherSpawns { get; set; }

        public static IDictionary<int, Tuple<float, int>> InvasionList { get; private set; }

        public static bool IsItemPoolOverrided { get; private set; }

        public static IDictionary<int, float> ItemPool { get; private set; }

        public static void StartOverrideSpawnRate(float spawnrate, float maxSpawns)
        {
            IsOverrdie = true;
            spawnRateOverride = spawnrate;
            maxSpawnsOverride = maxSpawns;
        }

        public static void EndOverride() { IsOverrdie = false; }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (IsOverrdie)
            {
                spawnRate = (int) Math.Floor(spawnRate / spawnRateOverride);
                maxSpawns = (int) Math.Floor(maxSpawnsOverride * maxSpawns);
            }
            else
            {
                base.EditSpawnRate(player, ref spawnRate, ref maxSpawns);
            }
        }

        public static void OverridePool(IDictionary<int, Tuple<float, int>> pool, bool disableOthers, bool enchanceLifetime = false)
        {
            InvasionList = pool;
            IsDisableOtherSpawns = disableOthers;
            IsSpawnpoolOverrided = true;
            useTuple = true;
        }

        public static void OverridePool(IDictionary<int, float> pool, bool disableOthers, bool enchanceLifetime = false)
        {
            SpawnPool = pool;
            IsDisableOtherSpawns = disableOthers;
            IsSpawnpoolOverrided = true;
            useTuple = false;
        }

        public static void OverrideItemPool(IDictionary<int, float> pool)
        {
            ItemPool = pool;
            IsItemPoolOverrided = true;
        }

        public static void DisableItemPool() { IsItemPoolOverrided = false; }

        public static void DisablePoolOverride()
        {
            IsSpawnpoolOverrided = false;
            enchanceLifetime = false;
            useTuple = false;
            IsDisableOtherSpawns = false;
        }

        public override void PostAI(NPC npc)
        {
            if (Main.netMode == 1)
                return;
            //Changes NPCs so they do not despawn when invasion up and invasion at spawn
            if (enchanceLifetime) npc.timeLeft = 1000;
        }

        public override void NPCLoot(NPC npc)
        {
            EventWorld world = ModContent.GetInstance<EventWorld>();

            if (useTuple)
                if (InvasionList.ContainsKey(npc.type))
                    world.CurrentEvent.TimeLeft -= InvasionList[npc.type].Item2;

            if (Main.netMode == 1)
                return;

            if (IsItemPoolOverrided && ItemPool != null)
            {
                var rand = new WeightedRandom<int>();
                foreach (KeyValuePair<int, float> it in ItemPool) rand.Add(it.Key, it.Value);
                if (rand.random.NextFloat(100) > 55) Item.NewItem(npc.position, npc.Size, rand.Get());
            }
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (Main.netMode == 1)
                return;

            if (!IsSpawnpoolOverrided)
                return;

            if (IsDisableOtherSpawns)
                if (!useTuple && SpawnPool != null || useTuple && InvasionList != null)
                    pool.Clear();
            {
                if (!useTuple)
                    foreach (KeyValuePair<int, float> it in SpawnPool)
                        if (pool.ContainsKey(it.Key))
                            pool[it.Key] = it.Value;
                        else
                            pool.Add(it.Key, it.Value);
                else
                    foreach (KeyValuePair<int, Tuple<float, int>> it in InvasionList)
                        if (pool.ContainsKey(it.Key))
                            pool[it.Key] = it.Value.Item1;
                        else
                            pool.Add(it.Key, it.Value.Item1);
            }
        }


        internal static void HandleCleanup()
        {
            SpawnPool = null;
            ItemPool = null;
            InvasionList = null;
            IsItemPoolOverrided = false;
            IsOverrdie = false;
            IsSpawnpoolOverrided = false;
        }
    }
}