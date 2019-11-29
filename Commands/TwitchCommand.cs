using System.Collections.Generic;
using Terraria.ModLoader;

namespace TwitchChat.Commands
{
    public class TwitchCommand : ModCommand
    {
        public override string Command => "t";

        public override CommandType Type => CommandType.Chat;

        public override string Description => "Universal command to handle mod operations. Reload -> force mod reloading, used if mod get bad state or settings is changed, Settings -> open settings file";

        public override string Usage => "/t connect (c) / disconnect (dc) / reload (r) / open (o) / settings (s) / message (msg, m)";

        private TwitchChat Mod => (TwitchChat) mod;

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length > 0)
                switch (args[0].ToLower())
                {
                    case "c":
                    case "connect":
                        if (Mod.OldConfig.Get<string>(TwitchCfg.OAToken) != "https://twitchapps.com/tmi/"
                            && Mod.OldConfig.Get<string>(TwitchCfg.Username) != "missingno")
                        {
                            Mod.Irc.Username = Mod.OldConfig.Get<string>(TwitchCfg.Username);
                            Mod.Irc.AuthToken = Mod.OldConfig.Get<string>(TwitchCfg.OAToken);
                            Mod.Irc.Connect();
                        }
                        else
                        {
                            caller.Reply("You missed username or token in settings. Type /t s to open settings file and /t r to reload mod");
                        }

                        break;
                    case "dc":
                    case "disconnect":
                        Mod.Irc.Disconnect();
                        break;
                    case "r":
                    case "reload":
                        Mod.Unload();
                        Mod.Load();
                        caller.Reply("Reloading mod....");
                        break;
                    case "s":
                    case "settings":
                        Mod.Storage.OpenFileExternally(Mod.Storage.GetFullPath("Twitch.ini"));
                        break;
                    case "o":
                    case "open":
                        Mod.Storage.OpenInNativeExplorer();
                        break;
                    case "m":
                    case "message":
                        var a = new List<string>();
                        for (var i = 1; i < args.Length; i++)
                            a.Add(args[i]);
                        var text = string.Join(" ", a);

                        TwitchChat.Send(text);

                        caller.Reply($"[c/{TwitchChat.TwitchColor}:<-- To Twitch:] {text}");
                        break;
                    default:
                        caller.Reply(Usage);
                        return;
                }
            else
                caller.Reply(Usage);
        }
    }
}