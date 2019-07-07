using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace TwitchChat
{
    public class TwitchWorld : ModWorld
    {

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
                Main.NewText(((TwitchChat)mod).LastStatus);
                statePrinted = true;
            }
        }

    }
}
