using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Razorwing.Framework.Extensions;

namespace TwitchChat.Commands
{
    public class TwitchCommand : ModCommand
    {
        public override string Command => "t";

        public override CommandType Type => CommandType.Chat;

        public override string Description => "Universal command to handle mod operations. Reload -> force mod reloading, used if mod get bad state or settings is changed, Settings -> open settings file";

        public override string Usage => "/t connect (c) / disconnect (dc) / reload (r) / open (o) / settings (s) / message (msg, m)";

        private new TwitchChat mod => (TwitchChat)((ModCommand)this).mod;

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "connect" || args[0].ToLower() == "c")
                {
                    if (mod.Config.Get<string>(TwitchCfg.OAToken) != "https://twitchapps.com/tmi/"
                        && mod.Config.Get<string>(TwitchCfg.Username) != "missingno")
                    {
                        mod.Irc.Username = mod.Config.Get<string>(TwitchCfg.Username);
                        mod.Irc.AuthToken = mod.Config.Get<string>(TwitchCfg.OAToken);
                        mod.Irc.Connect();
                    }  
                    else
                    {
                        caller.Reply("You missed username or token in settings. Type /t s to open settings file and /t r to reload mod");
                    }
                }else
                if (args[0].ToLower() == "disconnect" || args[0].ToLower() == "dc")
                {
                    mod.Irc.Disconnect();
                }else
                if (args[0].ToLower() == "reload" || args[0].ToLower() == "r")
                {
                    mod.Unload();
                    mod.Load();
                    caller.Reply("Reloading mod....");
                }else
                if (args[0].ToLower() == "settings" || args[0].ToLower() == "s")
                {
                    mod.Storage.OpenFileExternally(mod.Storage.GetFullPath("Twitch.ini"));
                }
                else
                if (args[0].ToLower() == "open" || args[0].ToLower() == "o")
                {
                    mod.Storage.OpenInNativeExplorer();
                }else
                if(args[0].ToLower() == "message" || args[0].ToLower() == "msg" || args[0].ToLower() == "m")
                {
                    var a = new List<string>();
                    for (int i = 1; i < args.Length; i++)
                        a.Add(args[i]);
                    string text = string.Join(" ", a);

                    TwitchChat.Send(text);

                    caller.Reply($"[c/{TwitchChat.TwitchColor}:<-- To Twitch:] {text}");

                }else
                {
                    caller.Reply(Usage);
                }
            }
            else
            {
                caller.Reply(Usage);
            }
        }
    }
}
