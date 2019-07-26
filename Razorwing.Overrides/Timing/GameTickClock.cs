using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Razorwing.Framework.Timing;


namespace TwitchChat.Razorwing.Overrides.Timing
{
    public class GameTickClock : IAdjustableClock, IDisposable
    {
        protected readonly EventWorld world;
        
        public GameTickClock(EventWorld world, bool autosub = true)
        {
            this.world = world;
            if (autosub)
                world.TickUpdate += SubTick;
            Rate = 1;
        }

        

        public double Rate { get; set; }
        public double CurrentTime { get; private set; } = 0;

        //We consider what we still updates every tick, and if game freeze, clock also freeze, but not stops.
        //Fix later
        public bool IsRunning => true;

        //private double tickRate = 1;
        //public override double Rate => tickRate;

        public void Reset() => CurrentTime = 0;

        public void ResetSpeedAdjustments() => Rate = 1;

        public bool Seek(double position)
        {
            CurrentTime = position;
            return true;
        }

        public void Start()
        {
            //What you're waiting for in manual clock?
        }

        public void Stop()
        {
            //What you're waiting for in manual clock?
        }

        /// <summary>
        /// Allow manual manage of how many ticks we add
        /// </summary>
        /// <param name="amouth">The amouth of ticks</param>
        public void AddTick(double amouth = 1)
        {
            CurrentTime += amouth * Rate;
        }

        /// <summary>
        /// Used for auto subscribe
        /// </summary>
        internal void SubTick(bool s)
        {
            if (s)
                AddTick();
            else
                //Unsub from updates to get peace
                Dispose();
        }

        public void Dispose()
        {
            //We should remove reference, so GB can know what we no more need this obj
            world.TickUpdate -= SubTick;
        }
    }
}
