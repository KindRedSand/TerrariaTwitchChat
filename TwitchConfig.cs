using TwitchChat.Razorwing.Framework.Configuration;
using TwitchChat.Razorwing.Framework.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchChat
{
    public class TwitchConfig : ConfigManager<TwitchCfg>
    {
        protected override string Filename => @"Twitch.cfg";

        protected override void InitialiseDefaults()
        {
            Set(TwitchCfg.AutoConnet, true);
            Set(TwitchCfg.Channel, "#kindredthefox");
            Set(TwitchCfg.Username, "missingno");
            Set(TwitchCfg.OAToken, "https://twitchapps.com/tmi/");
            Set(TwitchCfg.ShowAllIrc, false);
            Set(TwitchCfg.IgnoreCommands, false);
            Set(TwitchCfg.IgnoreCommandPrefix, "!");
        }

        public TwitchConfig(Storage storage)
            : base(storage)
        {
        }
    }

    public enum TwitchCfg
    {
        Channel,
        OAToken,
        Username,
        AutoConnet,
        ShowAllIrc,
        IgnoreCommands,
        IgnoreCommandPrefix,
    }
}
