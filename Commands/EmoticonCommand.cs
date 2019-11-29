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
                switch (args[0])
                {
                    case "fs":
                    case "failsafe":
                        EmoticonHandler.failsafe = new[] {0};
                        caller.Reply("Failsafe cache cleared.");
                        break;
                    case "cache":
                    case "c":
                        EmoticonHandler.cache.Clear();
                        caller.Reply("Since now we use store, clearing cache only clear texture references.");
                        break;
                    default:
                        caller.Reply(Usage);
                        return;
                }
            else
                caller.Reply(Usage);
        }
    }
}