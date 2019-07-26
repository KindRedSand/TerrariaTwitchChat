﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using TwitchChat.Commands.Debug;
using TwitchChat.Events;
using Razorwing.Framework.Threading;
using TwitchChat.Razorwing.Overrides.Timing;

namespace TwitchChat
{
    public class EventWorld : ModWorld
    {
        public override TagCompound Save()
        {
            
            //Save events cooldown
            TagCompound tags = new TagCompound();
            try
            {
                //foreach(var it in TwitchChat.EventsPool)
                //{
                //    cooldowns.Add(it.Key.GetType().Name, it.Value);
                //}
                foreach(var it in TwitchChat.EventsPool)
                {
                    tags.Add($"Event.Cooldown.{it.GetType()}", it.Cd);
                }
            }
            catch (Exception e)
            {
                ErrorLogger.Log($"Exception caughted in {nameof(Load)} when saving events cooldowns:\n" +
                    $"{e.Message}\n" +
                    $"{e.StackTrace}\n");
            }
            //return new TagCompound()
            //{
            //    ["EventCooldows"] = cooldowns,
            //};
            
            return tags;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (CurrentEvent != null)
            {
                CurrentEvent.EventEnd(this, (TwitchChat)mod);
                CurrentEvent = null;
            }

            TickUpdate?.Invoke(false);
            ///Build new Scheduler based on main thread with clock based on world ticks (kind'a'like we root to game time instead real time schedule)
            WorldScheduler = new Scheduler(Thread.CurrentThread, new GameTickClock(this, true));

            RealtimeScheduler = new Scheduler(Thread.CurrentThread);

            lastDayState = Main.dayTime;
        }

        
        public override void Load(TagCompound tag)
        {
            if (tag != null && tag.ContainsKey("EventCooldows"))
            {
                try
                {
                    //var arr = tag.Get<Dictionary<string, int>>("EventCooldows");

                    //I know it is shit, very slow shit, but it is terraria what you want? The whole terraria code is shit so shut up
                    foreach(var it in TwitchChat.EventsPool)
                    {
                        foreach(var tg in tag)
                        {
                            if (tg.Key == $"Event.Cooldown.{it.GetType()}")
                                it.Cd = (int)tg.Value;
                        }
                    }
                }catch (Exception e)
                {
                    ErrorLogger.Log($"Exception caughted in {nameof(Load)} when reading events cooldowns:\n" +
                        $"{e.Message}\n" +
                        $"{e.StackTrace}\n");
                }

            }
        }

        public IWorldEvent CurrentEvent = null;

        private bool lastDayState = false;

        public void StartWorldEvent(IWorldEvent ev)
        {
            try
            {
                if (Main.invasionType > 0 || Main.bloodMoon || Main.eclipse || Main.slimeRain)
                    return;
                if (CurrentEvent != null)
                    CurrentEvent.EventEnd(this, (TwitchChat)mod);
                CurrentEvent = ev;
                ev.Cd = ev.Cooldown;
                if (!ev.UseWarning || ev.StartDelay == 0)
                    CurrentEvent?.EventStart(this, (TwitchChat)mod);
                else
                {
                    
                    TwitchChat.Post(ev.Warning, ev.WarnColor);

                    WorldScheduler.AddDelayed(() =>
                    {
                        CurrentEvent?.EventStart(this, (TwitchChat)mod);//We need to check couse we can get null for now
                    }, ev.StartDelay);
                }
            }catch(Exception e)
            {
                ErrorLogger.Log($"Warning! Exception caughted in {nameof(StartWorldEvent)} ! Report mod author with stacktrace:\n" +
                    $"{e.Message}\n" +
                    $"{e.StackTrace}\n");
                //If we executing it as command, rethrow if
                //if (e.StackTrace.Contains(typeof(StartEventCommand).Name))
                //{
                //    throw e;
                //}
            }

        }

        ~EventWorld()
        {
            //CurrentEvent.EventEnd(this, (TwitchChat)mod);
        }

        public override void NetSend(BinaryWriter writer)
        {
            if (CurrentEvent != null)
            {
                writer.Write(true);
                writer.Write(CurrentEvent.GetType().Name);
                CurrentEvent.WriteWaveData(ref writer);
            } else
                writer.Write(false);
        }

        public override void NetReceive(BinaryReader reader)
        {
            CurrentEvent = null;
            if (reader.ReadBoolean())
            {
                var st = reader.ReadString();
                
                foreach(var it in TwitchChat.EventsPool)
                {
                    if (it.GetType().Name == st)
                    {
                        StartWorldEvent(it);

                        if (it.Type == InvasionType.Invasion)
                        {
                            Main.invasionProgressWave = reader.ReadInt32();
                            Main.invasionSizeStart = reader.ReadInt32();
                            Main.invasionSize = reader.ReadInt32();
                            Main.invasionType = reader.ReadInt32();
                            Main.invasionX = reader.ReadDouble();
                            Main.invasionProgress = reader.ReadInt32();
                        }
                        break;
                    }
                }
                if (CurrentEvent == null)
                {
                    Main.NewTextMultiline($"You use outdated version of mod, were {st} event not registered!", c: Color.Red);
                }
            }
        }

        internal Action<bool> TickUpdate;
        public Scheduler WorldScheduler;
        public Scheduler RealtimeScheduler;

        private int pucd = 1500;

        public override void PostUpdate()
        {
            if (WorldScheduler == null)
                //Mod in bad state and not loaded
                Load(null);

            TickUpdate?.Invoke(true);

            if (CurrentEvent == null)
                pucd -= 1;

            WorldScheduler.Update();

            try
            {

                //CurrentEvent?.PerformTick(this, (TwitchChat)mod);

                if (Main.netMode != 1 && (CurrentEvent == null && (pucd <= 0 || Main.dayTime != lastDayState)))
                {
                    pucd = 1500;
                    foreach (var ev in TwitchChat.EventsPool)
                    {
                        //Decrease one tick
                        ev.Cd -= pucd;
                        if (ev.Cd <= 0)
                            ev.Cd = 0;
                        else continue;

                        if (lastDayState != Main.dayTime && Main.time < 30)//If day tranfer to night or rev.  Main.time < 30 to not to trigger in midnight when join world
                        {
                            if (ev.Condition.HasFlag(TriggerCondition.SwitchTriggered))
                            {
                                if (ev.IsDaystateValid && (ev.ConditionAction != null ? ev.ConditionAction.Invoke() : true))
                                {
                                    if (ev.ChanceAction != null)//In case event has own start logic
                                    {
                                        if (ev.ChanceAction.Invoke())
                                        {
                                            StartWorldEvent(ev);
                                            break;
                                        }
                                    }
                                    else
                                    if (Main.rand.Next(1000) > (1000 - (1000 * ev.Chance)))
                                    {
                                        StartWorldEvent(ev);
                                        break;
                                    }
                                }
                            }
                        }
                        else if (!ev.Condition.HasFlag(TriggerCondition.SwitchTriggered))
                        {

                            if (ev.IsDaystateValid && (ev.ConditionAction != null ? ev.ConditionAction.Invoke() : true))
                            {
                                if (ev.ChanceAction != null)//In case event has own start logic
                                {
                                    if (ev.ChanceAction.Invoke())
                                    {
                                        StartWorldEvent(ev);
                                        break;
                                    }
                                }
                                else
                                if (Main.rand.Next(1000) > (1000 - (1000 * ev.Chance)))
                                {
                                    StartWorldEvent(ev);
                                    break;
                                }
                            }
                        }
                        ev.Cd = ev.ChanceCooldown;
                    }

                }
                else if (CurrentEvent != null && CurrentEvent.IsStarted)
                {
                    if (CurrentEvent.Condition.HasFlag(TriggerCondition.SwitchTriggered) && CurrentEvent.Type != InvasionType.Invasion)
                    {
                        if ((CurrentEvent.Condition.HasFlag(TriggerCondition.Day) || CurrentEvent.Condition.HasFlag(TriggerCondition.Night)))
                        {
                            if (lastDayState != Main.dayTime)
                            {
                                CurrentEvent.EventEnd(this, (TwitchChat)mod);
                                CurrentEvent = null;
                            }
                            else
                            {
                                CurrentEvent.PerformTick(this, (TwitchChat)mod);
                            }
                        }
                    }
                    else
                    {
                        if (CurrentEvent.Type != InvasionType.Invasion)
                            CurrentEvent.TimeLeft -= 1;
                        if (CurrentEvent.TimeLeft <= 0 && CurrentEvent.Type != InvasionType.Invasion)
                        {
                            CurrentEvent.EventEnd(this, (TwitchChat)mod);
                            CurrentEvent = null;
                        }
                        else
                        {
                            CurrentEvent.PerformTick(this, (TwitchChat)mod);
                        }
                    }
                }
                //else if(CurrentEvent!=null && !CurrentEvent.IsStarted)
                //{
                //    CurrentEvent.EventEnd(this, (TwitchChat)mod);
                //    CurrentEvent = null;
                //}
            }catch (Exception e)
            {
                ErrorLogger.Log($"Exception caught in event update! {e.Message}\n{e.StackTrace}\n");
            }
           
            


            lastDayState = Main.dayTime;
        }
    }
}