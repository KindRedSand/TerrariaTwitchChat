using TwitchChat.Razorwing.Framework.Platform;
using TwitchChat.Razorwing.Framework.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TwitchChat.IRCClient;
using TwitchChat.Razorwing.Framework.Configuration;

namespace TwitchChat
{
    public class TwitchChat : Mod
    {
        internal TwitchConfig Config { get; set; }
        internal DesktopStorage Storage { get; set; }
        internal IrcClient Irc { get; set; }

        public Bindable<string> LastStatus = new Bindable<string>($"[c/{TwitchColor}: Client not connected]");

        public const string TwitchColor = "942adf";
        private bool InRestoringState = false;

        public bool ShowDebug = false;

        public TwitchChat()
        {
            LastStatus.ValueChanged += LastStatus_ValueChanged;
        }

        private void LastStatus_ValueChanged(string newValue)
        {
            Main.NewText(LastStatus.Value);
        }

        public override void Load()
        {
            base.Load();
            if (Main.netMode == NetmodeID.Server)
                return;

            LastStatus.Value = $"[c/{TwitchColor}: Client not connected]";

            Config = new TwitchConfig(Storage = new DesktopStorage("TwitchChat")
            {
                TPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\My Games\Terraria\ModLoader\",
            });

            // Just to create file
            Config.Save();

            Irc = new IrcClient(); // This client used in my twitch bot so class know all info about twitch irc server so ve don't need to provide what info here 

            ShowDebug = Config.Get<bool>(TwitchCfg.ShowAllIrc);

            if (ShowDebug)
            {
                EventHandler<string> p3 = (s, m) =>
                   {
                       try
                       {
                           Main.NewText(m);
                       }catch (Exception e)
                       {
                           
                       }
                   };
                Irc.ServerMessage += p3;
            }

            EventHandler p = (s, e) =>
               {
                   LastStatus.Value = $"[c/{TwitchColor}:Connected]";

                   Irc.SendRaw("CAP REQ :twitch.tv/tags");
                   Thread.Sleep(500);
                   Irc.SendRaw("CAP REQ :twitch.tv/commands");
                   Thread.Sleep(500);
                   Irc.JoinChannel(Config.Get<string>(TwitchCfg.Channel));

                   InRestoringState = false;
                //Thread.Sleep(500);
                //irc.JoinChannel(ChatBotChannel);
            };
            Irc.OnConnect += p;

            EventHandler p1 = (s, e) =>
               {
                   if (!InRestoringState)
                   {
                       LastStatus.Value = $"[c/{TwitchColor}:Connection lost!]";
                       InRestoringState = true;
                       Thread.Sleep(5000);
                       Irc.Connect();
                   }
                   else
                   {
                       LastStatus.Value = $"[c/{TwitchColor}:Connection terminated! Client now offline]";
                   }
               };
            Irc.ConnectionClosed += p1;

            EventHandler<ChannelMessageEventArgs> p2 = (s, e) =>
               {
               if (Main.ActiveWorldFileData != null)
                   {
                       string prefix = "";
                       if (e.Badge.sub)
                       {
                           prefix += $"[i:{ItemID.Star}] ";
                       }
                       if (e.Badge.mod)
                       {
                           prefix += $"[i:{ItemID.Arkhalis}]";
                       }

                    //String format 
                    Main.NewText($@"{prefix} [c/{TwitchColor}:{e.Badge.DisplayName}]: {e.Message}");
                   }
               };
            Irc.ChannelMessage += p2;

            if (Config.Get<bool>(TwitchCfg.AutoConnet) && Config.Get<string>(TwitchCfg.OAToken) != "https://twitchapps.com/tmi/" 
                && Config.Get<string>(TwitchCfg.Username) != "missingno")
            {
                Irc.Username = Config.Get<string>(TwitchCfg.Username);
                Irc.AuthToken = Config.Get<string>(TwitchCfg.OAToken);
                Irc.Connect();
            }
        }

        public override void Unload()
        {
            base.Unload();
            if (Main.netMode == NetmodeID.Server)
                return;

            if (Irc.Connected)
                Irc.Disconnect();
            Irc.Dispose();
            Irc = null;
            Config = null;
            Storage = null;
        }


    }
}
