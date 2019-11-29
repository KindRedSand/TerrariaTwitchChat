using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TwitchChat
{
    public class TwitchWorld : ModWorld
    {
        public bool FirstNight;


        private bool statePrinted;

        public List<string> UsedNicks = new List<string>();

        public override void Load(TagCompound tag)
        {
            FirstNight = tag.GetBool("firstNigh");
            //FirstNight = true;
            UsedNicks = tag.ContainsKey("usedNicks") ? (List<string>) tag["usedNicks"] : new List<string>();
            var inter = new List<string>();

            for (var i = 0; i < Main.maxNPCs; i++)
                if (Main.npc[i].active && Main.npc[i].townNPC && UsedNicks.Contains(Main.npc[i].GivenName))
                    inter.Add(Main.npc[i].GivenName);

            UsedNicks = inter;
        }

        public override TagCompound Save()
        {
            return new TagCompound
            {
                ["firstNight"] = FirstNight,
                ["usedNicks"] = UsedNicks
            };
        }

        public override void Initialize()
        {
            base.Initialize();
            statePrinted = false;
            for (var i = 0; i < Main.npc.Length; i++) TwitchChat.ShadowNpc[i] = Main.npc[i].type;
        }


        public override void PostUpdate()
        {
            base.PostUpdate();
            if (!statePrinted)
            {
                TwitchChat.Text(((TwitchChat) mod).LastStatus);
                statePrinted = true;
            }
        }
    }
}