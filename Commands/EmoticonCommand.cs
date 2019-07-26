using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using TwitchChat.Chat;

namespace TwitchChat.Commands
{
    public class EmoticonCommand : ModCommand
    {
        public override string Command => "te";

        public override CommandType Type => CommandType.Chat;

        public override string Usage => "/te failsafe (fs) / cache (c)";

        public override string Description => "Allow to clear emotes cache. fs clear 'failsafe' list what used to prevent using bad stated emotes. If you think some emote what should work get in this list use /t fs " +
            "/te c clear whole image cache so all emotes need to be redownloaded. Not clear failsafe list. Expect lag spikes upon use!";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "failsafe" || args[0] == "fs")
                {
                    EmoticonHandler.failsafe = new int[] { 0 };
                    caller.Reply("Failsafe cache cleared.");
                }
                else if (args[0] == "cache" || args[0] == "c")
                {
                    EmoticonHandler.cache.Clear();
                    caller.Reply("Since now we use store, clearing cache only clear texture references.");
                }
                else
                    caller.Reply(Usage);
            }
            else
                caller.Reply(Usage);
        }
    }
}
