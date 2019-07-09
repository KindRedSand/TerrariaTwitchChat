using TwitchChat.Razorwing.Framework.Platform;
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
using Terraria.UI.Chat;
using TwitchChat.Razorwing.Framework.Logging;
using TwitchChat.Chat;

namespace TwitchChat
{
    public class TwitchChat : Mod
    {
        internal TwitchConfig Config { get; set; }
        internal DesktopStorage Storage { get; set; }
        internal IrcClient Irc { get; set; }

        public Bindable<string> LastStatus = new Bindable<string>($"[c/{TwitchColor}: Client not connected]");

        private SEmoteComparrer comparrer = new SEmoteComparrer();

        public const string TwitchColor = "942adf";
        private bool InRestoringState = false;

        public bool ShowDebug = false;
        public bool IgnoreCommands = false;
        public string CommandPrefix = "!";
        public string Username = "";

        public readonly string[] KnownBots = new string[]
        {
            "nightbot",
            "moobot",
            "starbottv",
            "starbotttv",
            "stayhydratedbot",
            "stay_hydrated_bot",
        };

        public TwitchChat()
        {
            LastStatus.ValueChanged += LastStatus_ValueChanged;
            Terraria.UI.Chat.ChatManager.Register<EmoticonHandler>(new string[] { "emote", "e" });
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

            //Logger.Storage = Storage;

            // Just to create file
            Config.Save();

            Irc = new IrcClient(); // This client used in my twitch bot so class know all info about twitch irc server so we don't need to provide what info here 

            ShowDebug = Config.Get<bool>(TwitchCfg.ShowAllIrc);
            IgnoreCommands = Config.Get<bool>(TwitchCfg.IgnoreCommands);
            CommandPrefix = Config.Get<string>(TwitchCfg.IgnoreCommandPrefix);
            Username = Config.Get<string>(TwitchCfg.Username);

            if (ShowDebug)
            {
                //In uknown reason for me, tModLoader refuse to use () => {}; delegate in +=
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
                   if(!Main.gameMenu)
                   {
                       //If we ignore commands, we also want ignore bots messages
                       if (IgnoreCommands)
                       {
                           if (e.Message.StartsWith(CommandPrefix))
                               return;
                           //In case you selfbotting, we ignore your own messages 
                           if (e.From == Username)
                               return;
                           //if message was sended by known bot, we ignore it
                           if (KnownBots.Contains(e.From))
                               return;
                       }
                       var result = "";

                       List<SEmote> parsed = new List<SEmote>();

                       foreach(var it in e.Badge.emotes)
                       {
                           if (it == string.Empty)
                               break;
                           var pair = it.Split(':');
                           var ind = pair[1].Split(',');
                           foreach(var index in ind)
                           {
                               var ipair = index.Split('-');
                               parsed.Add(new SEmote(ipair[0], ipair[1], pair[0]));
                           }
                       }

                       //Note, what concat += create NEW string each time. 
                       //Need find more fast way to do this for minimal IRC client idle 
                       if (parsed.Count != 0)
                       {
                           parsed.Sort(comparrer);
                           var str = e.Message;
                           int indx = 0, i = 0;
                           for(; i < str.Length && indx < parsed.Count;)
                           {
                               if(parsed[indx].Start == i)
                               {
                                   result += $"[emote:{parsed[indx].Emote}]";
                                   i = parsed[indx].End + 1;
                                   indx++;
                                   continue;
                               }
                               result += str[i];
                               i++;
                           }
                           if(i != str.Length)
                            result += str.Substring(i);
                       }
                       else
                           result = e.Message;
                       

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
                       Main.NewText($@"{prefix} [c/{TwitchColor}:{e.Badge.DisplayName}]: {result}");
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

        private class SEmoteComparrer : IComparer<SEmote>
        {
            public int Compare(SEmote x, SEmote y)
            {
                return x.Start.CompareTo(y.Start);
            }
        }

        private class SEmote 
        {
            public int Start;
            public int End;
            public int Emote;
            public SEmote(string s, string e, string em)
            {
                Start = int.Parse(s);
                End = int.Parse(e);
                Emote = int.Parse(em);
            }

            public SEmote(int s, int e, int em)
            {
                Start = s;
                End = e;
                Emote = em;
            }

            public int CompareTo(object obj)
            {
                return Start.CompareTo(obj);
            }
        }
    }
}
