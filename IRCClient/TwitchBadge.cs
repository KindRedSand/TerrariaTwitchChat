using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TwitchChat.IRCClient
{
    public struct Badge
    {
        public bool sub;
        public bool mod;
        public string DisplayName;
        public bool turbo;
        public Badge(bool somthing)
        {
            this.sub = false;
            this.mod = false;
            this.DisplayName = "";
            this.turbo = false;
        }
    }
}