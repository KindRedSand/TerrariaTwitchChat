using System.Collections.Generic;

namespace TwitchChat.Chat
{
    public class ChatData
    {
        public int chatter_count;
        public TwitchChatters chatters;
    }


    public class TwitchChatters
    {
        public List<string> moderators;
        public List<string> staff;
        public List<string> viewers;
    }
}