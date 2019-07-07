using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchChat.Razorwing.Framework.Extensions
{
    public static class StringExtensions
    {
        public static bool Contains(this string collection, params string[] words)
        {
            foreach(var w in words)
            {
                if (collection.Contains(w))
                    return true;
            }
            return false;
        }


    }
}
