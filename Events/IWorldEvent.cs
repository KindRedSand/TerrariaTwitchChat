using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using TwitchChat.Overrides;
using static TwitchChat.TwitchChat;

namespace TwitchChat.Events
{
    public abstract class IWorldEvent
    {
        #region Locale
        /// <summary>
        /// Should we send a warning message for player until event was started?
        /// </summary>
        public virtual bool UseWarning => false;
        public virtual string Warning => "";

        //Now in world ticks
        public virtual int StartDelay { get; set; } = 0;
        public virtual string StartString => $"";
        public virtual string EndString => $"";
        public virtual Color WarnColor => Color.BlanchedAlmond;
        public virtual Color StartColor => Color.Azure;
        public virtual Color EndColor => Color.Green;

        /// <summary>
        /// Owerride this property if you want play specifc music during event
        /// For modded music return somthing like
        /// <code>
        /// TwitchChat.Instance.GetSoundSlot(SoundType.Music, "Sounds/Music/VeryImportantMusic"));
        /// </code>
        /// </summary>
        public virtual int MusicId { get; set; } = -1;

        #endregion

        #region Time
        /// <summary>
        /// Allow easy check for daytime
        /// </summary>
        public bool IsDaystateValid
        {
            get
            {
                if (Main.dayTime && Condition.HasFlag(TriggerCondition.Day))
                    return true;
                return (!Main.dayTime && Condition.HasFlag(TriggerCondition.Night));
            }
        }

        
        /// <summary>
        /// Cooldown betwen events that starts automatically
        /// Should be in ~seconds~ game ticks
        /// </summary>
        public abstract int Cooldown { get; set; }

        /// <summary>
        /// Lenght of event, if this starts not by using <see cref="TriggerCondition.OnDayBegin"/> or <see cref="TriggerCondition.OnNightBegin"/>.
        /// In game ticks, there 32400 -> night time, 54000 -> day time
        /// </summary>
        public virtual int Length { get; set; } = 32400;

        private double timeStart = 0;

        /// <summary>
        /// Cooldown between trying to start event.
        /// Event cooldowns saves localy per world.
        /// In ~seconds~ world ticks
        /// </summary>
        public virtual int ChanceCooldown => 120;

        /// <summary>
        /// Chance to this event "happen"
        /// In percents there 1.f = 100%
        /// </summary>
        public abstract float Chance { get; set; }

        /// <summary>
        /// Start condition of this event.
        /// If you want start your event only by using item/etc. left there <see cref="TriggerCondition.Both"/> and override <see cref="IWorldEvent.Chance"/> => 0
        /// </summary>
        public virtual TriggerCondition Condition => TriggerCondition.Both;

        /// <summary>
        /// This will be called if defined instead using default chance.
        /// Return true to start event, or false to get a chance cooldown
        /// </summary>
        public virtual Func<bool> ChanceAction => null;

        /// <summary>
        /// This will be called if defined instead using default <see cref="TriggerCondition"/>
        /// Return true if event can be started at this point, or false to get chance cooldown
        /// </summary>
        public virtual Func<bool> ConditionAction => null;

        private bool started = false;
        /// <summary>
        /// Return true if event is currently executing
        /// </summary>
        public bool IsStarted => started && (Type == InvasionType.Invasion || TimeLeft > 0) ? true : false;

        private int tLeft = 0;
        /// <summary>
        /// Return time, that left for this event.
        /// Not used by invasions
        /// </summary>
        public virtual int TimeLeft
        {
            get
            {
                return tLeft;
            }
            set
            {
                if (started)
                {
                    if (value <= 0 && Type != InvasionType.Invasion)
                        started = false;
                    tLeft = value;
                }
            }
        }

        /// <summary>
        /// Cooldown of this event, when after compleeteon the event they can't b started again automatically in world
        /// </summary>
        public virtual int Cd {get; set;}
        #endregion

        #region Invasion

        /// <summary>
        /// Dictionary of invaders, there first => mob id and second => chance to apear in percent
        /// To use mobs from others mods use <code>
        /// ModLoader.GetMod("modname").NPCType("npcname")
        /// </code>
        /// </summary>        
        // ID : Chance %
        public abstract IDictionary<int, float> Invaders { get; }

        /// <summary>
        /// Drop from all mobs in invasion, there first => item id and second => chance for dorps
        /// </summary>
        // ID : Chance %
        public virtual IDictionary<int, float> InvadersDrop => null;


        public virtual float SpawnRateMul => 1;
        public virtual float MaxSpawnMul => 1;

        /// <summary>
        /// Disable or not others mobs spawning during this event
        /// </summary>
        public virtual bool DisableOthers => false;

        /// <summary>
        /// Type of this event
        /// </summary>
        public virtual InvasionType Type => InvasionType.WorldEvent;

        /// <summary>
        /// In wave mode must return wave size
        /// </summary>
        public virtual int InvasionSize => 100;

        /// <summary>
        /// Does invasion score should be scaled by player cound?
        /// </summary>
        public virtual bool MultiplyByPlayers => true;

        /// <summary>
        /// Invaders with invasion
        /// Wave, ID, Chance, Kill Value(proggress points)
        /// </summary>
        public virtual IDictionary<int, List<Tuple<int, float, int>>> InvasionList => null;

        /// <summary>
        /// Return dictionary for current invasion wave, there data ordered as
        /// NPC ID, Chance and mob value
        /// </summary>
        public IDictionary<int, Tuple<float, int>> GetListForCurWave
        {
            get
            {
                var d = new Dictionary<int, Tuple<float, int>>();
                if (InvasionList.ContainsKey(Main.invasionProgressWave))
                {
                    foreach (var it in InvasionList[Main.invasionProgressWave])
                    {
                        if (!d.ContainsKey(it.Item1))
                        {
                            d.Add(it.Item1, new Tuple<float, int>(it.Item2, it.Item3));
                        }else
                        {
                            if (Main.netMode == 2) // Server
                            {
                                NetMessage.BroadcastChatMessage(NetworkText.FromKey($"Trying start wave with overlaping invader!\n" +
                                    $"Event name: {GetType().Name} Wave: {Main.invasionProgressWave}"), Color.MediumVioletRed);
                            }
                            else if (Main.netMode == 0) // Single Player
                            {
                                Main.NewText($"Trying start wave with overlaping invader!\n" +
                                    $"Event name: {GetType().Name} Wave: {Main.invasionProgressWave}", Color.MediumVioletRed);
                            }
                        }
                    }
                }

                if (d.Count == 0)
                {
                    if (Main.netMode == 2) // Server
                    {
                        NetMessage.BroadcastChatMessage(NetworkText.FromKey($"Trying start invasion event WITHOUT invaders wave!\n" +
                            $"Event name: {GetType().Name}, Wave: {Main.invasionProgressWave}"), Color.MediumVioletRed);
                    }
                    else if (Main.netMode == 0) // Single Player
                    {
                        Main.NewText(Language.GetTextValue($"Trying start invasion event WITHOUT invaders in wave!\n" +
                            $"Event name: {GetType().Name}, Wave: {Main.invasionProgressWave}"), Color.MediumVioletRed);
                    }
                    Main.invasionProgress = 100;
                }

                return d;
            }
        }
        #endregion

        private int numPlayers = 0;


        /// <summary>
        /// Used by mod internally! Better use <see cref="EventWorld.StartWorldEvent(IWorldEvent)"/> to start events! Use only if you KNOW what you do!!
        /// Start event starting sequence, and send event data for all players. Can break things if called outside <see cref="EventWorld"/>!
        /// </summary>
        /// <param name="world"></param>
        /// <param name="mod"></param>
        internal void EventStart(EventWorld world, TwitchChat mod)
        {
            Post(StartString, StartColor);

            started = true;
            TimeLeft = Length;
            timeStart = Main.time;

            //Count Players
            for (int i = 0; i < 255; i++)
            {
                if (Main.player[i] != null && Main.player[i].active && Main.player[i].statLifeMax >= 200)
                {
                    numPlayers++;
                }
            }

            if (Type == InvasionType.Invasion)
            {
                Main.invasionSize = MultiplyByPlayers ? InvasionSize * numPlayers : InvasionSize;
                Main.invasionProgressDisplayLeft = Main.invasionSize - Main.invasionProgress;
                Main.invasionSizeStart = Main.invasionSize;
                TimeLeft = Main.invasionSize;
                Main.invasionProgressWave = 1;
                Main.invasionProgress = 0;
                Main.invasionX = Main.spawnTileX;
                Main.invasionProgressIcon = 0 + 8;
                Main.invasionProgressMax = Main.invasionSizeStart;
                Main.invasionType = -1;
                OnWaveChange();
            }           

            OnStart();

            if (Main.netMode != 1)//If we NOT the server client
            {

                GlobalSpawnOverride.StartOverrideSpawnRate(SpawnRateMul, MaxSpawnMul);
                if (Type == InvasionType.WorldEvent && Invaders != null)
                    GlobalSpawnOverride.OverridePool(Invaders, DisableOthers, true);
                else
                {
                    if (InvasionList!= null && InvasionList.Count != 0 )
                    {
                        GlobalSpawnOverride.OverridePool(GetListForCurWave, DisableOthers, true);
                    }
                    else
                    {
                        //Post($"Trying start invasion event WITHOUT invaders list!\n" +
                        //        $"Event name: {GetType().Name}", Color.MediumVioletRed);
                    }
                }
                if (InvadersDrop != null)
                    GlobalSpawnOverride.OverrideItemPool(InvadersDrop);
            }
            if (Main.netMode == 2)
            {
                var netMessage = mod.GetPacket();
                netMessage.Write((byte)NetPacketType.EventWasStarted);
                netMessage.Write(GetType().Name);
                netMessage.Write(TimeLeft);
                netMessage.Write((byte)Type);
                if (Type == InvasionType.Invasion)
                {
                    WriteWaveData(ref netMessage);
                }
                netMessage.Send();
            }

        }

        public void EventEnd(EventWorld world, TwitchChat mod)
        {
            Post(EndString, EndColor);

            GlobalSpawnOverride.EndOverride();
            GlobalSpawnOverride.DisablePoolOverride();
            GlobalSpawnOverride.DisableItemPool();

            if(Type == InvasionType.Invasion)
            {
                Main.invasionSize = 0;
                Main.invasionSizeStart = Main.invasionSize;
                Main.invasionProgress = 0;
                Main.invasionX = Main.spawnTileX;
                Main.invasionProgressIcon = 0;
                Main.invasionProgressWave = 0;
                Main.invasionProgressMax = Main.invasionSizeStart;
                Main.invasionWarn = 0;
                Main.invasionType = 0;
                Main.invasionDelay = 0;
                numPlayers = 0;
            }

            OnEnd();

            if (Main.netMode == 2)
            {
                var netMessage = mod.GetPacket();
                netMessage.Write((byte)NetPacketType.EventEnded);
                netMessage.Write(GetType().Name);
                netMessage.Send();
            }
        }

        private int oldProggressData = 0;

        public void PerformTick(EventWorld world, TwitchChat mod)
        {
            
            if (Main.invasionProgress >= 100)
            {
                if (Main.invasionProgressWave >= InvasionList.Count)
                {
                    if (Main.netMode != 1)
                    {
                        EventEnd(world, mod);
                        world.CurrentEvent = null;
                    }
                    Main.invasionProgressDisplayLeft = 20;
                    return;
                }
                else
                {
                    if (Main.netMode != 1)
                    {
                        if (Main.invasionProgressWave == 0)
                            return;
                        Main.invasionProgressWave++;
                        Main.invasionProgress = 0;
                        OnWaveChange();
                        Main.invasionSize = MultiplyByPlayers ? InvasionSize * numPlayers : InvasionSize;
                        Main.invasionSizeStart = Main.invasionSize;
                        tLeft = Main.invasionSize;
                        GlobalSpawnOverride.OverridePool(GetListForCurWave, DisableOthers, true);

                        if (Main.netMode == 2)
                        {
                            var netMessage = mod.GetPacket();
                            netMessage.Write((byte)NetPacketType.EventWaveUpdated);
                            netMessage.Write(GetType().Name);
                            netMessage.Write(TimeLeft);
                            netMessage.Write((byte)Type);
                            if (Type == InvasionType.Invasion)
                            {
                                WriteWaveData(ref netMessage);
                            }
                            netMessage.Send();
                        }
                    }
                }
            }else if (Type == InvasionType.Invasion)
            {
                if (Main.netMode == 2)
                {
                    if (TimeLeft != oldProggressData)
                    {
                        var netMessage = mod.GetPacket();
                        netMessage.Write((byte)NetPacketType.EventWaveUpdated);
                        netMessage.Write(GetType().Name);
                        netMessage.Write(TimeLeft);
                        netMessage.Write((byte)Type);
                        if (Type == InvasionType.Invasion)
                        {
                            WriteWaveData(ref netMessage);
                        }
                        netMessage.Send();
                    }
                    oldProggressData = TimeLeft;
                }

                Main.invasionProgress = (int)(100 * (((float)Main.invasionSize - (float)TimeLeft) / (float)Main.invasionSize));
                Main.invasionProgressDisplayLeft = 1000;

                
            }

            OnTick();
        }

        /// <summary>
        /// Called on event egining (not when warning message apears!)
        /// </summary>
        protected virtual void OnStart()
        {

        }

        /// <summary>
        /// Called on event end
        /// </summary>
        protected virtual void OnEnd()
        {

        }

        /// <summary>
        /// Called every game tick
        /// </summary>
        protected virtual void OnTick()
        {

        }

        /// <summary>
        /// Called during invasion when wave was changed
        /// </summary>
        public virtual void OnWaveChange()//Made public couse we need to sync this with server
        {

        }

        /// <summary>
        /// Used internally for multiplayer syncs
        /// </summary>
        /// <param name="writer"></param>
        public void WriteWaveData(ref ModPacket writer)
        {
            writer.Write(Main.invasionProgressWave);
            //TwitchChat.Post($"Main.invasionProgressWave = {Main.invasionProgressWave}", Color.Wheat);
            writer.Write(Main.invasionSizeStart);
            //TwitchChat.Post($"Main.invasionSizeStart = {Main.invasionSizeStart}", Color.Wheat);
            writer.Write(Main.invasionSize);
            //TwitchChat.Post($"Main.invasionSize = {Main.invasionSize}", Color.Wheat);
            writer.Write(Main.invasionType);
            //TwitchChat.Post($"Main.invasionType = {Main.invasionType}", Color.Wheat);
            writer.Write(Main.invasionX);
            //TwitchChat.Post($"Main.invasionX = {Main.invasionX}", Color.Wheat);
            writer.Write(Main.invasionProgress);
            //TwitchChat.Post($"Main.invasionProgress = {Main.invasionProgress}", Color.Wheat);
        }

        /// <summary>
        /// Used internally for multiplayer syncs
        /// </summary>
        /// <param name="writer"></param>
        public void WriteWaveData(ref BinaryWriter writer)
        {
            writer.Write(Main.invasionProgressWave);
            //TwitchChat.Post($"Main.invasionProgressWave = {Main.invasionProgressWave}", Color.Wheat);
            writer.Write(Main.invasionSizeStart);
            //TwitchChat.Post($"Main.invasionSizeStart = {Main.invasionSizeStart}", Color.Wheat);
            writer.Write(Main.invasionSize);
            //TwitchChat.Post($"Main.invasionSize = {Main.invasionSize}", Color.Wheat);
            writer.Write(Main.invasionType);
            //TwitchChat.Post($"Main.invasionType = {Main.invasionType}", Color.Wheat);
            writer.Write(Main.invasionX);
            //TwitchChat.Post($"Main.invasionX = {Main.invasionX}", Color.Wheat);
            writer.Write(Main.invasionProgress);
            //TwitchChat.Post($"Main.invasionProgress = {Main.invasionProgress}", Color.Wheat);
        }
    }

    public enum InvasionType : byte
    {
        Invasion,
        WorldEvent,
    }

    /// <summary>
    /// Litle help with percentages
    /// </summary>
    public static class Chances
    {
        public const float Never = 0f;
        public const float TooRare = 0.05f;
        public const float Rare = 0.1f;
        public const float RareUncommon = 0.15f;
        public const float Uncommon = 0.3f;
        public const float Common = 0.5f;
        public const float FreqCommon = 0.6f;
        public const float Frequently = 0.7f;
        public const float Many = 0.8f;
        public const float Alot = 0.9f;
        public const float Allways = 1f;
    }

    [Flags]
    public enum TriggerCondition
    {
        OnDayBegin = SwitchTriggered | Day,
        OnNightBegin = SwitchTriggered | Night,
        BothBegin = SwitchTriggered | Day | Night,

        OnDay = Day,
        OnNight = Night,
        Both = Day | Night,

        //Prereq
        /// <summary>
        /// Trigger only on day/night end/begin
        /// </summary>
        SwitchTriggered = 1 << 0,
        /// <summary>
        /// Trigger during day
        /// </summary>
        Day = 1 << 1,
        /// <summary>
        /// Trigger during night
        /// </summary>
        Night = 1 << 2,

    }

    public enum LenghtEnum : byte
    {
        EndOfDay,
        EndOfNigth,
        CustomLengt,
        UntileNight,
        UntilDay,
    }
}
