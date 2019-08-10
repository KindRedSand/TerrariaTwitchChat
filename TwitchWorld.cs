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

        public override void Load(TagCompound tag)
        {
            firstNight = tag.ContainsKey("firstNigh") ? (bool)tag["firstNight"] : false;
            firstNight = true;
        }

        public override TagCompound Save()
        {
            return new TagCompound()
            {
                ["firstNight"] = firstNight,
            };
            
        }


        bool statePrinted = false;
        public override void Initialize()
        {
            base.Initialize();
            statePrinted = false;
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
