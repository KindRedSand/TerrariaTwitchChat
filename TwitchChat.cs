using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Razorwing.Framework.Configuration;
using Razorwing.Framework.IO.Stores;
using Razorwing.Framework.Logging;
using Razorwing.Framework.Platform;
using Razorwing.Framework.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TwitchChat.Chat;
using TwitchChat.Events;
using TwitchChat.IRCClient;
using TwitchChat.Overrides;
using TwitchChat.Razorwing.Overrides;
using TwitchChat.Razorwing.Overrides.Timing;

namespace TwitchChat
{
    public class TwitchChat : Mod
    {
        internal TwitchConfig Config { get; set; }
        internal DesktopStorage Storage { get; set; }
        internal IrcClient Irc { get; set; }
        internal WebClient Web { get; set; } = new WebClient();
        internal ResourceStore<byte[]> Store { get; private set; }
        internal Texture2DStore Textures { get; private set; }
        public static readonly string Path;

        private static TwitchChat instance;

        public Bindable<string> LastStatus = new Bindable<string>($"[c/{TwitchColor}: Client not connected]");

        private static List<IWorldEvent> eventsPool = new List<IWorldEvent>();
        public static List<IWorldEvent> EventsPool => eventsPool;

        public Dictionary<string, Action<ChannelMessageEventArgs>> CurrentPool = null;

        internal static TwitchChat Instance => instance;

        private readonly SEmoteComparrer comparrer = new SEmoteComparrer();

        public const string TwitchColor = "942adf";
        private bool InRestoringState = false;

        public bool ShowDebug = false;
        public bool IgnoreCommands = false;
        public string CommandPrefix = "!";
        public string Username = "";
        public bool Fun = false;

        public string ChatBoss = "";
        public Dictionary<string, Action> BossCommands = new Dictionary<string, Action>();
        private DateTimeOffset BossColdown = DateTimeOffset.Now;
        private Terraria.Utilities.UnifiedRandom rand = new Terraria.Utilities.UnifiedRandom();

        public string Channel { get; private set; }

        public readonly string[] KnownBots = new string[]
        {
            "nightbot",
            "moobot",
            "starbottv",
            "starbotttv",
            "stay_hydrated_bot",
        };

        static TwitchChat()
        {
            Path = $@"{ModLoader.ModPath}\Cache\Twitch\"; 
        }

        public TwitchChat()
        {
            LastStatus.ValueChanged += LastStatus_ValueChanged;
            if (Main.netMode != NetmodeID.Server)
                Terraria.UI.Chat.ChatManager.Register<EmoticonHandler>(new string[] { "emote", "e" });

            try
            {
                if (System.IO.File.Exists($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\My Games\Terraria\ModLoader\TwitchChat\Twitch.cfg") &&
                   !System.IO.File.Exists($@"{Path}Twitch.ini"))
                {
                    System.IO.File.Move($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\My Games\Terraria\ModLoader\TwitchChat\Twitch.cfg",
                        $@"{Path}Twitch.ini");
                }
            }catch(Exception e)
            {

            }

        }

        private void LastStatus_ValueChanged(string m)
        {
            Text(m);
        }

        public static void Text(string m)
        {
            switch (Main.netMode)
            {
                case NetmodeID.MultiplayerClient:
                case NetmodeID.SinglePlayer:
                    Main.NewText(m);
                    break;
                case NetmodeID.Server:
                    Console.WriteLine(m);
                    break;
            }
        }

        public static void Post(string m, Color color)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.BroadcastChatMessage(NetworkText.FromKey(m), color);
            }
            else if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(m, color);
            }
        }

        public static void Send(string text)
        {
            if(text != string.Empty)
            {
                instance.Irc?.SendMessage(instance.Channel,text);
            }
        }

        public override void Load()
        {
            base.Load();

            instance = this;

            BossCommands = new Dictionary<string, Action>();

            eventsPool = new List<IWorldEvent>();

            LastStatus.Value = $"[c/{TwitchColor}: Client not connected]";

            Config = new TwitchConfig(Storage = new ModStorage(@"Twitch"));

            Store = new ResourceStore<byte[]>(new StorageBackedResourceStore(Storage));
            Store.AddStore(new OnlineStore());

            Textures = new Texture2DStore(Store);

            EmoticonHandler.store = new EmoticonsStore(Store);

            if(Storage.Exists("EmoteIDs.json"))
                using (var p = Storage.GetStream("EmoteIDs.json", FileAccess.Read))
                using (var s = new StreamReader(p))
                {
                    try
                    {
                        EmoticonHandler.convertingEmotes = JsonConvert.DeserializeObject<Dictionary<string, int>>(s.ReadToEnd());
                    }catch (Exception e)
                    {

                    }
                    
                }

            // Just to create file
            Config.Save();            

            Irc = new IrcClient(); // This client used in my twitch bot so class know all info about twitch irc server so we don't need to provide what info here 

            ShowDebug = Config.Get<bool>(TwitchCfg.ShowAllIrc);
            IgnoreCommands = Config.Get<bool>(TwitchCfg.IgnoreCommands);
            CommandPrefix = Config.Get<string>(TwitchCfg.IgnoreCommandPrefix);
            Username = Config.Get<string>(TwitchCfg.Username);
            Fun = Config.Get<bool>(TwitchCfg.EnableFun);
            Channel = Config.Get<string>(TwitchCfg.Channel);

            if (ShowDebug)
                Logger.Storage = Storage;

            if ((Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.SinglePlayer) && Fun)
            {
                BossCommands.Add("heal", () =>
                {
                    foreach (var it in Main.player)
                        if(it.active)
                        {
                            it.statLife += rand.Next(0, it.statLifeMax - it.statLife);
                            it.statMana += rand.Next(0, it.statManaMax - it.statMana);
                        }
                });

                BossCommands.Add("buff", () =>
                {
                    foreach (var it in Main.player)
                        if (it.active)
                        {
                            it.AddBuff(rand.Next(255), rand.Next(200, 2000));
                        }
                });

                BossCommands.Add("death", () =>
                {
                    foreach (var it in Main.player)
                        if (it.active)
                        {
                            if (rand.NextFloat() < 0.20f)
                                it.statLife = 0;
                        }
                });

                BossCommands.Add("quit", () =>
                {
                    Send($"@{ChatBoss} become a pussy and no more chat boss!");
                    ChatBoss = "";
                });


                //Register inner world event invasions
                foreach (var smod in ModLoader.LoadedMods)
                    foreach (var it in smod.GetType().Assembly.DefinedTypes)
                    {
                        if (!it.IsAbstract && (
                            it.BaseType != typeof(object) && it.BaseType == typeof(IWorldEvent) ||
                            it.BaseType.BaseType != typeof(object) && it.BaseType.BaseType == typeof(IWorldEvent)))//In case if IWorldEvent is second parent
                        {
                            try
                            {
                                eventsPool.Add((IWorldEvent)Activator.CreateInstance(it));
                            }
                            catch (Exception e)
                            {
                                ErrorLogger.Log($"Exception caught in Events register loop. Report mod author with related stacktrace: \n" +
                                    $"{e.Message}\n" +
                                    $"{e.StackTrace}\n");
                            }
                        }
                    }
            }


            if (ShowDebug)
            {
                //In uknown reason for me, tModLoader refuse to use () => {}; delegate in +=
                Irc.ServerMessage += (s, m) =>
                   {
                       try
                       {
                           Text(m);
                       }
                       catch (Exception e)
                       {

                       }
                   };
            }

            Irc.OnConnect += (s, e) =>
               {
                   LastStatus.Value = $"[c/{TwitchColor}:Connected]";

                   Irc.SendRaw("CAP REQ :twitch.tv/tags");
                   Thread.Sleep(500);
                   Irc.SendRaw("CAP REQ :twitch.tv/commands");
                   Thread.Sleep(500);
                   Irc.JoinChannel(Channel);

                   InRestoringState = false;
               };

            Irc.ConnectionClosed += (s, e) =>
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

            Irc.ChannelMessage += (s, e) =>
               {
                   if (Main.netMode != NetmodeID.Server && !Main.gameMenu)
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
                       var result = e.Message;

                       List<SEmote> parsed = new List<SEmote>();

                       foreach (var it in e.Badge.emotes)
                       {
                           if (it == string.Empty)
                               break;
                           var pair = it.Split(':');
                           var ind = pair[1].Split(',');
                           foreach (var index in ind)
                           {
                               var ipair = index.Split('-');
                               parsed.Add(new SEmote(ipair[0], ipair[1], pair[0]));
                           }

                       } 

                       if (parsed.Count != 0)
                       {
                           var list = new Dictionary<int, string>();
                           foreach(var it in parsed)
                           {
                               if (list.ContainsKey(it.Emote))
                                   continue;
                               var st = e.Message.Substring(it.Start, it.End - it.Start + 1);

                               //Not perfect couse if Kappa mentioned in msg KappaPride get breaked,
                               //but it way faster what per gliph concat 
                               result = result.Replace(st, $"[e:{it.Emote}]");

                               list.Add(it.Emote, st);
                           }

                           foreach(var em in list)
                           {
                               if (!EmoticonHandler.convertingEmotes.ContainsKey(em.Value))
                                   EmoticonHandler.convertingEmotes.Add(em.Value, em.Key);
                           }
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

                   if ((Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.SinglePlayer) && Fun)
                   {
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

                       var word = e.Message.ToLower().Split(' ').First(); 

                       if (CurrentPool?.ContainsKey(word) ?? false)
                       {
                           CurrentPool[word]?.Invoke(e);
                       }
                       else
                       {
                           if(e.From == ChatBoss && BossColdown < DateTimeOffset.Now && BossCommands.ContainsKey(word))
                           {
                               BossColdown = DateTimeOffset.Now.AddSeconds(20);
                               BossCommands[word]?.Invoke();
                           }
                       }
                   }
               };

            if (Config.Get<bool>(TwitchCfg.AutoConnet) && Config.Get<string>(TwitchCfg.OAToken) != "https://twitchapps.com/tmi/" 
                && Config.Get<string>(TwitchCfg.Username) != "missingno")
            {
                Irc.Username = Config.Get<string>(TwitchCfg.Username);
                Irc.AuthToken = Config.Get<string>(TwitchCfg.OAToken);
                Irc.Connect();
            }
        }

        public override void UpdateMusic(ref int music, ref MusicPriority priority)
        {
            if (GetModWorld<EventWorld>() != null && GetModWorld<EventWorld>().CurrentEvent != null)
            {
                if (GetModWorld<EventWorld>().CurrentEvent.MusicId != -1)
                {
                    music = GetModWorld<EventWorld>().CurrentEvent.MusicId;
                    priority = MusicPriority.Environment;
                }
            }
            else base.UpdateMusic(ref music, ref priority);
        }

        public override void Unload()
        {
            base.Unload();

            using (var p = Storage?.GetStream("EmoteIDs.json", FileAccess.Write))
            using (var s = new StreamWriter(p))
            {
                s.Write(JsonConvert.SerializeObject(EmoticonHandler.convertingEmotes, Formatting.Indented));
            }

            if (Irc?.Connected ?? false)
                Irc?.Disconnect();
            Irc?.Dispose();
            Irc = null;
            Config.Load();
            Config.Dispose();
            Config = null;
            Storage = null;
            Store.Dispose();
            Store = null;
            Textures.Dispose();
            Textures = null;

            if (GetModWorld<EventWorld>() != null)
            {
                GetModWorld<EventWorld>().WorldScheduler = new Scheduler(Thread.CurrentThread, new GameTickClock(GetModWorld<EventWorld>()));
                GetModWorld<EventWorld>().RealtimeScheduler = new Scheduler();
                GetModWorld<EventWorld>().CurrentEvent = null;
            }

            Web = null;
            instance = null;
            eventsPool?.Clear();
            eventsPool = null;
            GlobalSpawnOverride.HandleCleanup();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            //Currentrly only server can said us what we should do
            if (whoAmI != 256)
                return;

            var type = (NetPacketType)reader.ReadByte();

            if (type == NetPacketType.EventWasStarted)
            {
                string name = reader.ReadString();
                foreach (var it in EventsPool)
                {
                    if (it.GetType().Name == name)
                    {
                        GetModWorld<EventWorld>().StartWorldEvent(it);
                        break;
                    }
                }
                if (GetModWorld<EventWorld>().CurrentEvent == null)
                {
                    Main.NewText($"WARNING! You ether or disable FunMode or you are using outdated version of mod, what havent an {name} event! Switching to NoSync mode...");
                }

                GetModWorld<EventWorld>().CurrentEvent.TimeLeft = reader.ReadInt32();
                var invType = (InvasionType)reader.ReadByte();
                if (invType == InvasionType.Invasion)
                {
                    Main.invasionProgressWave = reader.ReadInt32();
                    Main.invasionSizeStart = reader.ReadInt32();
                    Main.invasionSize = reader.ReadInt32();
                    Main.invasionType = reader.ReadInt32();
                    Main.invasionX = reader.ReadDouble();
                    Main.invasionProgress = reader.ReadInt32();
                }
            }
            else if (type == NetPacketType.EventWaveUpdated)
            {
                string name = reader.ReadString();
                if (GetModWorld<EventWorld>().CurrentEvent == null || GetModWorld<EventWorld>().CurrentEvent.GetType().Name != name)
                {
                    //if (GetModWorld<EventWorld>().CurrentEvent == null)
                    //    Main.NewText($"WARNING! Currently you wont have any executing event, but server send wave update for {name} event!");
                    if (GetModWorld<EventWorld>().CurrentEvent.GetType().Name != name)
                    {
                        Main.NewText($"ERROR! Currently executing event not the same what server sends! Executed event is: {GetModWorld<EventWorld>().CurrentEvent.GetType().Name}. Server has {name} event!");
                        GetModWorld<EventWorld>().CurrentEvent.EventEnd(GetModWorld<EventWorld>(), this);
                        GetModWorld<EventWorld>().CurrentEvent = null;
                    }


                    foreach (var it in EventsPool)
                    {
                        if (it.GetType().Name == name)
                        {
                            GetModWorld<EventWorld>().StartWorldEvent(it);
                            break;
                        }
                    }
                    if (GetModWorld<EventWorld>().CurrentEvent == null)
                    {
                        //Main.NewText($"WARNING! You ether or disable FunMode or you are using outdated version of mod, what havent an {name} event!");
                    }
                }

                GetModWorld<EventWorld>().CurrentEvent.TimeLeft = reader.ReadInt32();
                var invType = (InvasionType)reader.ReadByte();
                if (invType == InvasionType.Invasion)
                {
                    Main.invasionProgressWave = reader.ReadInt32();
                    Main.invasionSizeStart = reader.ReadInt32();
                    Main.invasionSize = reader.ReadInt32();
                    Main.invasionType = reader.ReadInt32();
                    Main.invasionX = reader.ReadDouble();
                    Main.invasionProgress = reader.ReadInt32();
                    GetModWorld<EventWorld>().CurrentEvent.OnWaveChange();
                }
            }
            else if (type == NetPacketType.EventEnded)
            {
                string name = reader.ReadString();
                if (GetModWorld<EventWorld>().CurrentEvent == null || GetModWorld<EventWorld>().CurrentEvent.GetType().Name != name)
                {
                    if (GetModWorld<EventWorld>().CurrentEvent == null)
                        Main.NewText($"WARNING! Currently you wont have any executing event, but server send event end for {name} event!");
                    if (GetModWorld<EventWorld>().CurrentEvent.GetType().Name != name)
                    {
                        Main.NewText($"ERROR! Currently executing event not the same what server sends! Executed event is: {GetModWorld<EventWorld>().CurrentEvent.GetType().Name}. Server has {name} event!");
                        GetModWorld<EventWorld>().CurrentEvent.EventEnd(GetModWorld<EventWorld>(), this);
                        GetModWorld<EventWorld>().CurrentEvent = null;
                    }
                }
                else
                {
                    GetModWorld<EventWorld>().CurrentEvent.EventEnd(GetModWorld<EventWorld>(), this);
                    GetModWorld<EventWorld>().CurrentEvent = null;
                }
            }
            else if (type == NetPacketType.Custom)
            {
                #region Constants
                const string LunarSky = "LunarSkies";
                #endregion

                string eve = reader.ReadString();
                if (eve == LunarSky)
                {
                    var t = (LunarSkies)reader.ReadByte();
                    EventPlayer.LunarSky = t;
                }
            }



        }

        public enum NetPacketType : byte
        {
            EventWasStarted,
            EventWaveUpdated,
            EventEnded,
            Custom,
        }


        #region SEmote
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
        #endregion
    }
}
