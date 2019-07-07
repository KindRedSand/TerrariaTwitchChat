﻿using System;
using System.Globalization;

namespace TwitchChat.Razorwing.Framework.Timing
{
    public struct FrameTimeInfo
    {
        /// <summary>
        /// Elapsed time during last frame in milliseconds.
        /// </summary>
        public double Elapsed;

        /// <summary>
        /// Begin time of this frame.
        /// </summary>
        public double Current;

        public override string ToString() => Math.Truncate(Current).ToString(CultureInfo.InvariantCulture);
    }
}
