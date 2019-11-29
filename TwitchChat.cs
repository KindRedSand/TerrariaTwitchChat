#region usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using On.Terraria;
using Razorwing.Framework.Configuration;
using Razorwing.Framework.IO.Stores;
using Razorwing.Framework.Platform;
using Razorwing.Framework.Threading;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using Terraria.Utilities;
using TwitchChat.Chat;
using TwitchChat.Events;
using TwitchChat.IRCClient;
using TwitchChat.Overrides;
using TwitchChat.Overrides.Razorwing;
using TwitchChat.Overrides.Razorwing.Timing;
using Item = Terraria.Item;
using Main = Terraria.Main;
using NetMessage = Terraria.NetMessage;
using Player = Terraria.Player;
using Projectile = Terraria.Projectile;

#endregion

namespace TwitchChat
{
    public class TwitchChat : Mod
    {
        public enum NetPacketType : byte
        {
            EventWasStarted,
            EventWaveUpdated,
            EventEnded,
            Custom
        }

        public const string TwitchColor = "942adf";
        public static readonly string Path;

        internal static int[] ShadowNpc = new int[256];
        private readonly UnifiedRandom rand = new UnifiedRandom();
        public Dictionary<string, Action> BossCommands = new Dictionary<string, Action>();

        private DateTimeOffset bossCooldown = DateTimeOffset.Now;

        public string ChatBoss = "";

        public Dictionary<string, Action<ChannelMessageEventArgs>> CurrentPool = null;
        public bool Fun;
        private bool inRestoringState;

        public Bindable<string> LastStatus = new Bindable<string>($"[c/{TwitchColor}: Client not connected]");
        public List<string> RecentChatters = new List<string>();
        public string Username = "";

        static TwitchChat() { Path = $@"{ModLoader.ModPath}\Cache\Twitch\"; }


        public TwitchChat()
        {
            LastStatus.ValueChanged += LastStatus_ValueChanged;
            if (Main.netMode != NetmodeID.Server)
                ChatManager.Register<EmoticonHandler>("emote", "e");


            try
            {
                if (File.Exists($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\My Games\Terraria\ModLoader\TwitchChat\Twitch.cfg") && !File.Exists($@"{Path}Twitch.ini"))
                    File.Move($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\My Games\Terraria\ModLoader\TwitchChat\Twitch.cfg", $@"{Path}Twitch.ini");
            }
            catch (Exception e)
            {
                Logger.Warn($"Failed to move config file to cache folder:\n{e}");
            }
        }

        public IEnumerable<string> KnownBots => ModContent.GetInstance<TwitchConfig>().UsersToIgnore;
        public string CommandPrefix => ModContent.GetInstance<TwitchConfig>().CommandPrefix;
        public bool IgnoreCommands => ModContent.GetInstance<TwitchConfig>().IgnoreCommands;


        public bool ShowDebug => ModContent.GetInstance<TwitchConfig>().ShowIRC;

        internal TwitchOldConfig OldConfig { get; set; }
        internal DesktopStorage Storage { get; set; }
        internal IrcClient Irc { get; set; }
        internal WebClient Web { get; set; } = new WebClient();
        internal ResourceStore<byte[]> Store { get; private set; }
        internal Texture2DStore Textures { get; private set; }
        public static List<IWorldEvent> EventsPool { get; private set; } = new List<IWorldEvent>();

        internal static TwitchChat Instance { get; private set; }

        public string Channel { get; private set; }

        private static void LastStatus_ValueChanged(string m) { Text(m); }

        /// <summary>
        ///     Write text in to chat/console.
        ///     Mostly used for debugging and sending client only related info
        /// </summary>
        /// <param name="m">Text to print</param>
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

        /// <summary>
        ///     Send message in chat. Can use locale strings and write text from server to clients even if client don't have this
        ///     mod
        /// </summary>
        /// <param name="m">Text to print</param>
        /// <param name="color">text color</param>
        public static void Post(string m, Color color)
        {
            switch (Main.netMode)
            {
                case NetmodeID.SinglePlayer:
                    Main.NewText(m);
                    break;
                case NetmodeID.Server:
                    NetMessage.BroadcastChatMessage(NetworkText.FromKey(m), color);
                    break;
            }
        }

        /// <summary>
        ///     Send message to twitch IRC server
        /// </summary>
        /// <param name="text">Text to print</param>
        public static void Send(string text)
        {
            if (text != string.Empty) Instance.Irc?.SendMessage(Instance.Channel, text);
        }

        public override void Load()
        {
            base.Load();

            RecentChatters = new List<string>();

#if DEBUG
            //Used for debugging
            RecentChatters.AddRange(new[]
            {
                "Nightbot",
                "KarmikKoalla",
                "Moobot",
                "SomeoneFromChat"
            });
#endif

            Instance = this;

            BossCommands = new Dictionary<string, Action>();

            EventsPool = new List<IWorldEvent>();

            LastStatus.Value = $"[c/{TwitchColor}: Client not connected]";

            OldConfig = new TwitchOldConfig(Storage = new ModStorage(@"Twitch"));

            Store = new ResourceStore<byte[]>(new StorageBackedResourceStore(Storage));
            if (ModLoader.version.Major == 10)
                Store.AddStore(new OnlineStore());
            else
                Store.AddStore(new WebStore("image/png"));

            Textures = new Texture2DStore(Store);

            EmoticonHandler.store = new EmoticonsStore(Store);

            if (Storage.Exists("EmoteIDs.json"))
                using (Stream p = Storage.GetStream("EmoteIDs.json"))
                using (StreamReader s = new StreamReader(p))
                {
                    try
                    {
                        EmoticonHandler.convertingEmotes =
                            JsonConvert.DeserializeObject<Dictionary<string, int>>(s.ReadToEnd());
                    }
                    catch (Exception e)
                    {
                        Logger.Warn($"Failed to load emotes id:\n{e}");
                    }
                }

            // Just to create file
            OldConfig.Save();


            Irc = new IrcClient(); // This client used in my twitch bot so class know all info about twitch irc server so we don't need to provide what info here 

            //Start migrating to new configs
            //ShowDebug = OldConfig.Get<bool>(TwitchCfg.ShowAllIrc);
            //IgnoreCommands = OldConfig.Get<bool>(TwitchCfg.IgnoreCommands);
            //CommandPrefix = OldConfig.Get<string>(TwitchCfg.IgnoreCommandPrefix);
            Username = OldConfig.Get<string>(TwitchCfg.Username);
            Fun = OldConfig.Get<bool>(TwitchCfg.EnableFun);
            Channel = OldConfig.Get<string>(TwitchCfg.Channel);

            if (ShowDebug)
                Razorwing.Framework.Logging.Logger.Storage =
                    Storage; //Thx tML 0.11 for adding "Mod.Logger" <3 Breaking all as all ways 

            if (Fun)
            {
                //Since it not work on server (Not affect clients) until i write packets for this Twitch boss is disabled for server
                if (Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.SinglePlayer)
                {
                    BossCommands.Add("heal", () =>
                    {
                        if (Main.netMode != NetmodeID.SinglePlayer)
                            foreach (Player it in Main.player)
                            {
                                if (it.active)
                                    for (int i = rand.Next(20); i > 0; i--)
                                    {
                                        Item.NewItem(it.position, ItemID.Heart, noGrabDelay: true);
                                        Item.NewItem(it.position, ItemID.Star, noGrabDelay: true);
                                    }
                            }
                        else
                            for (int i = rand.Next(20); i > 0; i--)
                            {
                                Item.NewItem(Main.LocalPlayer.position, ItemID.Heart, noGrabDelay: true);
                                Item.NewItem(Main.LocalPlayer.position, ItemID.Star, noGrabDelay: true);
                            }
                    });

                    BossCommands.Add("buff", () =>
                    {
                        if (Main.netMode != NetmodeID.SinglePlayer)
                        {
                            foreach (Player it in Main.player)
                                if (it.active)
                                {
                                    for (int i = rand.Next(3); i > 0; i--)
                                        Item.NewItem(it.position, ItemID.NebulaPickup1, noGrabDelay: true);
                                    for (int i = rand.Next(3); i > 0; i--)
                                        Item.NewItem(it.position, ItemID.NebulaPickup2, noGrabDelay: true);
                                    for (int i = rand.Next(3); i > 0; i--)
                                        Item.NewItem(it.position, ItemID.NebulaPickup3, noGrabDelay: true);
                                }
                        }
                        else
                        {
                            for (int i = rand.Next(3); i > 0; i--)
                                Item.NewItem(Main.LocalPlayer.position, ItemID.NebulaPickup1, noGrabDelay: true);
                            for (int i = rand.Next(3); i > 0; i--)
                                Item.NewItem(Main.LocalPlayer.position, ItemID.NebulaPickup2, noGrabDelay: true);
                            for (int i = rand.Next(3); i > 0; i--)
                                Item.NewItem(Main.LocalPlayer.position, ItemID.NebulaPickup3, noGrabDelay: true);
                        }
                    });

                    BossCommands.Add("death", () =>
                    {
                        if (Main.netMode != NetmodeID.SinglePlayer)
                            foreach (Player it in Main.player)
                            {
                                if (it.active)
                                    for (int i = rand.Next(20); i > 0; i--)
                                        Projectile.NewProjectile(it.position, new Vector2(0, 3), ProjectileID.EyeFire,
                                            400, 0);
                            }
                        else
                            for (int i = rand.Next(20); i > 0; i--)
                                Projectile.NewProjectile(Main.LocalPlayer.position, new Vector2(0, 3),
                                    ProjectileID.EyeFire, 400, 0);
                    });

                    BossCommands.Add("quit", () =>
                    {
                        Send($"@{ChatBoss} become a pussy and no more chat boss!");
                        ChatBoss = "";
                    });
                }


                //Register inner world event invasions
                foreach (Mod mod in ModLoader.Mods)
                foreach (TypeInfo it in mod.GetType().Assembly.DefinedTypes)
                    if (!it.IsAbstract && (
                            it.BaseType != typeof(object) && it.BaseType == typeof(IWorldEvent) ||
                            it.BaseType?.BaseType != typeof(object) && it.BaseType?.BaseType == typeof(IWorldEvent))
                    ) //In case if IWorldEvent is second parent
                        try
                        {
                            EventsPool.Add((IWorldEvent) Activator.CreateInstance(it));
                        }
                        catch (Exception e)
                        {
                            Logger.Error(
                                "Exception caught in Events register loop. Report mod author with related stacktrace: \n" +
                                $"{e.Message}\n" +
                                $"{e.StackTrace}\n");
                        }
            }


            if (ShowDebug)
                Irc.ServerMessage += (s, m) =>
                {
                    try
                    {
                        Text(m);
                    }
                    catch (Exception)
                    {
                        Logger.Warn("Failed to post message");
                    }
                };

            Irc.OnConnect += (s, e) =>
            {
                LastStatus.Value = $"[c/{TwitchColor}:Connected]";

                Irc.SendRaw("CAP REQ :twitch.tv/tags");
                Thread.Sleep(500);
                Irc.SendRaw("CAP REQ :twitch.tv/commands");
                Thread.Sleep(500);
                Irc.JoinChannel(Channel);

                inRestoringState = false;
            };

            Irc.ConnectionClosed += (s, e) =>
            {
                if (!inRestoringState)
                {
                    LastStatus.Value = $"[c/{TwitchColor}:Connection lost!]";
                    inRestoringState = true;
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
                        //In case you self bot, we ignore your own messages 
                        if (e.From == Username)
                            return;
                        //if message was send by known bot, we ignore it
                        if (KnownBots.Contains(e.From))
                            return;
                    }

                    string result = e.Message;

                    var parsed = new List<SEmote>();

                    foreach (string it in e.Badge.emotes)
                    {
                        if (it == string.Empty)
                            break;
                        string[] pair = it.Split(':');
                        string[] ind = pair[1].Split(',');
                        parsed.AddRange(ind.Select(p =>
                        {
                            string[] ip = p.Split('-');
                            return new SEmote(ip[0], ip[1], pair[0]);
                        }));
                    }

                    if (parsed.Count != 0)
                    {
                        var list = new Dictionary<int, string>();
                        foreach (SEmote it in parsed)
                        {
                            if (list.ContainsKey(it.Emote))
                                continue;
                            string st = e.Message.Substring(it.Start, it.End - it.Start + 1);

                            //Not perfect because if Kappa mentioned in msg KappaPride get broken,
                            //but it way faster what per glyph concat 
                            result = result.Replace(st, $"[e:{it.Emote}]");

                            list.Add(it.Emote, st);
                        }

                        foreach (KeyValuePair<int, string> em in list)
                            if (!EmoticonHandler.convertingEmotes.ContainsKey(em.Value))
                                EmoticonHandler.convertingEmotes.Add(em.Value, em.Key);
                    }
                    else
                    {
                        result = e.Message;
                    }


                    string prefix = "";
                    if (e.Badge.sub) prefix += $"[i:{ItemID.Star}] ";
                    if (e.Badge.mod) prefix += $"[i:{ItemID.Arkhalis}]";

                    //String format 
                    Main.NewText($@"{prefix} [c/{TwitchColor}:{e.Badge.DisplayName}]: {result}");

                    if (!RecentChatters.Contains(e.Badge.DisplayName))
                        RecentChatters.Add(e.Badge.DisplayName);
                }

                if ((Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.SinglePlayer) && Fun)
                {
                    if (e.Message.StartsWith(CommandPrefix))
                        return;
                    //In case you self bot, we ignore your own messages 
                    if (e.From == Username)
                        return;
                    //if message was sent by known bot, we ignore it
                    if (KnownBots.Contains(e.From))
                        return;


                    string word = e.Message.ToLower().Split(' ').First();

                    if (CurrentPool?.ContainsKey(word) ?? false)
                    {
                        CurrentPool[word]?.Invoke(e);
                    }
                    else if (e.From == ChatBoss && bossCooldown < DateTimeOffset.Now && BossCommands.ContainsKey(word))
                    {
                        bossCooldown = DateTimeOffset.Now.AddSeconds(20);
                        BossCommands[word]?.Invoke();
                    }
                }
            };

            if (OldConfig.Get<bool>(TwitchCfg.AutoConnect) && OldConfig.Get<string>(TwitchCfg.OAToken) !=
                                                           "https://twitchapps.com/tmi/"
                                                           && OldConfig.Get<string>(TwitchCfg.Username) != "missingno")
            {
                Irc.Username = OldConfig.Get<string>(TwitchCfg.Username);
                Irc.AuthToken = OldConfig.Get<string>(TwitchCfg.OAToken);
                Irc.Connect();
            }

            WorldGen.SpawnTownNPC += SpawnTownNpcHook;
        }

        private TownNPCSpawnResult SpawnTownNpcHook(WorldGen.orig_SpawnTownNPC orig, int x, int y)
        {
            //Avoid crush when we not in world
            if (Main.gameMenu)
                return TownNPCSpawnResult.Blocked;

            TownNPCSpawnResult v = orig?.Invoke(x, y) ?? TownNPCSpawnResult.Blocked;
            if (v != TownNPCSpawnResult.Successful)
                return v;

            //if game actually spawn new town npc
            TwitchWorld w = ModContent.GetInstance<TwitchWorld>();
            var r = new WeightedRandom<string>();
            IEnumerable<string> l = RecentChatters.Except(w.UsedNicks);

            foreach (string it in l)
                r.Add(it);

            //Then select random nick
            string username = r.Get();

            if (username == string.Empty)
                return v;

            //Go through shadow array
            for (int i = 0; i < Main.maxNPCs; i++)
                //If this is active town npc and has different typeID what this slot has before
                if (Main.npc[i].active && Main.npc[i].townNPC && ShadowNpc[i] != Main.npc[i].type)
                {
                    //Post a message
                    Post($"But wait, he's actually a [c/{TwitchColor}:{username}]!", Color.White);
                    w.UsedNicks.Add(username);
                    //Add a scheduled action since npc at this moment isn't get reset yet
                    ModContent.GetInstance<EventWorld>().WorldScheduler.AddDelayed(
                        () => { Main.npc[i].GivenName = username; },
                        5);
                    break;
                }

            //Update shadow array
            for (int i = 0; i < Main.npc.Length; i++) ShadowNpc[i] = Main.npc[i].type;

            return v;
        }

        public override void UpdateMusic(ref int music, ref MusicPriority priority)
        {
            if (ModContent.GetInstance<EventWorld>() != null &&
                ModContent.GetInstance<EventWorld>().CurrentEvent != null &&
                ModContent.GetInstance<EventWorld>().CurrentEvent.MusicId != -1)
            {
                music = ModContent.GetInstance<EventWorld>().CurrentEvent.MusicId;
                priority = MusicPriority.Environment;
            }
            else
            {
                base.UpdateMusic(ref music, ref priority);
            }
        }

        public override void Unload()
        {
            base.Unload();

            if (Storage != null)
                using (Stream p = Storage?.GetStream("EmoteIDs.json", FileAccess.Write))
                using (StreamWriter s = new StreamWriter(p))
                {
                    s.Write(JsonConvert.SerializeObject(EmoticonHandler.convertingEmotes, Formatting.Indented));
                }

            if (Irc?.Connected ?? false)
                Irc?.Disconnect();
            Irc?.Dispose();
            Irc = null;
            OldConfig?.Load();
            OldConfig?.Dispose();
            OldConfig = null;
            Storage = null;
            Store?.Dispose();
            Store = null;
            Textures?.Dispose();
            Textures = null;
            RecentChatters = null;
            WorldGen.SpawnTownNPC -= SpawnTownNpcHook;


            if (ModContent.GetInstance<EventWorld>() != null)
            {
                ModContent.GetInstance<EventWorld>().WorldScheduler = new Scheduler(Thread.CurrentThread,
                    new GameTickClock(ModContent.GetInstance<EventWorld>()));
                ModContent.GetInstance<EventWorld>().RealtimeScheduler = new Scheduler();
                ModContent.GetInstance<EventWorld>().CurrentEvent = null;
            }

            Web = null;
            Instance = null;
            EventsPool?.Clear();
            EventsPool = null;
            GlobalSpawnOverride.HandleCleanup();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            NetPacketType type = (NetPacketType) reader.ReadByte();

            if (type != NetPacketType.Custom)
                //Currently only server can said us what we should do
                if (whoAmI != 256)
                    return;

            if (type == NetPacketType.EventWasStarted)
            {
                string name = reader.ReadString();
                foreach (IWorldEvent it in EventsPool)
                    if (it.GetType().Name == name)
                    {
                        ModContent.GetInstance<EventWorld>().StartWorldEvent(it);
                        break;
                    }

                if (ModContent.GetInstance<EventWorld>().CurrentEvent == null)
                    Main.NewText(
                        $"WARNING! You ether or disable FunMode or you are using outdated version of mod, what haven't an {name} event! Switching to NoSync mode...");

                ModContent.GetInstance<EventWorld>().CurrentEvent.TimeLeft = reader.ReadInt32();
                InvasionType invType = (InvasionType) reader.ReadByte();
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
                if (ModContent.GetInstance<EventWorld>().CurrentEvent == null ||
                    ModContent.GetInstance<EventWorld>().CurrentEvent.GetType().Name != name)
                {
                    //if (ModContent.GetInstance<EventWorld>().CurrentEvent == null)
                    //    Main.NewText($"WARNING! Currently you wont have any executing event, but server send wave update for {name} event!");
                    if (ModContent.GetInstance<EventWorld>().CurrentEvent.GetType().Name != name)
                    {
                        Main.NewText(
                            $"ERROR! Currently executing event not the same what server sends! Executed event is: {ModContent.GetInstance<EventWorld>().CurrentEvent.GetType().Name}. Server has {name} event!");
                        ModContent.GetInstance<EventWorld>().CurrentEvent
                            .EventEnd(ModContent.GetInstance<EventWorld>(), this);
                        ModContent.GetInstance<EventWorld>().CurrentEvent = null;
                    }


                    foreach (IWorldEvent it in EventsPool)
                        if (it.GetType().Name == name)
                        {
                            ModContent.GetInstance<EventWorld>().StartWorldEvent(it);
                            break;
                        }

                    if (ModContent.GetInstance<EventWorld>().CurrentEvent == null)
                    {
                        //Main.NewText($"WARNING! You ether or disable FunMode or you are using outdated version of mod, what haven't an {name} event!");
                    }
                }

                ModContent.GetInstance<EventWorld>().CurrentEvent.TimeLeft = reader.ReadInt32();
                InvasionType invType = (InvasionType) reader.ReadByte();
                if (invType == InvasionType.Invasion)
                {
                    Main.invasionProgressWave = reader.ReadInt32();
                    Main.invasionSizeStart = reader.ReadInt32();
                    Main.invasionSize = reader.ReadInt32();
                    Main.invasionType = reader.ReadInt32();
                    Main.invasionX = reader.ReadDouble();
                    Main.invasionProgress = reader.ReadInt32();
                    ModContent.GetInstance<EventWorld>().CurrentEvent.OnWaveChange();
                }
            }
            else if (type == NetPacketType.EventEnded)
            {
                string name = reader.ReadString();
                if (ModContent.GetInstance<EventWorld>().CurrentEvent == null ||
                    ModContent.GetInstance<EventWorld>().CurrentEvent.GetType().Name != name)
                {
                    if (ModContent.GetInstance<EventWorld>().CurrentEvent == null)
                        Main.NewText(
                            $"WARNING! Currently you wont have any executing event, but server send event end for {name} event!");
                    if (ModContent.GetInstance<EventWorld>().CurrentEvent.GetType().Name != name)
                    {
                        Main.NewText(
                            $"ERROR! Currently executing event not the same what server sends! Executed event is: {ModContent.GetInstance<EventWorld>().CurrentEvent.GetType().Name}. Server has {name} event!");
                        ModContent.GetInstance<EventWorld>().CurrentEvent
                            .EventEnd(ModContent.GetInstance<EventWorld>(), this);
                        ModContent.GetInstance<EventWorld>().CurrentEvent = null;
                    }
                }
                else
                {
                    ModContent.GetInstance<EventWorld>().CurrentEvent
                        .EventEnd(ModContent.GetInstance<EventWorld>(), this);
                    ModContent.GetInstance<EventWorld>().CurrentEvent = null;
                }
            }
            else if (type == NetPacketType.Custom)
            {
                #region Constants

                const string lunarSky = "LunarSkies";
                const string netSendFix = "NetSend";

                #endregion

                string eve = reader.ReadString();
                if (eve == lunarSky)
                {
                    LunarSkies t = (LunarSkies) reader.ReadByte();
                    EventPlayer.LunarSky = t;
                }
                else if (eve == netSendFix)
                {
                    bool b = reader.ReadBoolean();
                    if (b)
                    {
                        ModPacket p = GetPacket();
                        p.Write((byte) NetPacketType.Custom);
                        p.Write(netSendFix);
                        p.Write(false);
                        ModContent.GetInstance<EventWorld>().WriteNetSendData(p);
                        p.Send(whoAmI);
                    }
                    else
                    {
                        ModContent.GetInstance<EventWorld>().NetReceive(reader);
                    }
                }
            }
        }


        #region SEmote

        private class SEmote
        {
            public readonly int Emote;
            public readonly int End;
            public readonly int Start;

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

            public int CompareTo(object obj) { return Start.CompareTo(obj); }
        }

        #endregion
    }
}