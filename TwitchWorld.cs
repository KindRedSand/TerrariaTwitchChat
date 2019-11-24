using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TwitchChat
{
    public class TwitchWorld : ModWorld
    {

        public bool firstNight = false;

        public List<string> UsedNicks = new List<string>();

        public override void Load(TagCompound tag)
        {
            firstNight = tag.ContainsKey("firstNigh") ? (bool)tag["firstNight"] : false;
            firstNight = true;
            UsedNicks = tag.ContainsKey("usedNicks") ? (List<string>) tag["usedNicks"] : new List<string>();
        }

        public override TagCompound Save()
        {
            return new TagCompound()
            {
                ["firstNight"] = firstNight,
                ["usedNicks"]  = UsedNicks,
            };
            
        }


        bool statePrinted = false;
        public override void Initialize()
        {
            base.Initialize();
            statePrinted = false;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                TwitchChat.shadowNpc[i] = Main.npc[i].type;
            }
        }


        public override void PostUpdate()
        {
            base.PostUpdate();
            if (!statePrinted)
            {
                TwitchChat.Text(((TwitchChat)mod).LastStatus);
                statePrinted = true;
            }
        }


    }
}
