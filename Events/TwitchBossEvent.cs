using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.Utilities;
using TwitchChat.IRCClient;

namespace TwitchChat.Events
{
    public class TwitchBossEvent : IWorldEvent
    {
        public override int Cooldown => 1000;

        public override float Chance => 0.1f;

        public override IDictionary<int, float> Invaders => null;

        public override string StartString => "It's time to select new chat boss!";

        public override string EndString => $"New boss now is... ";

        private DateTimeOffset assignTime = DateTimeOffset.Now;

        public override int Lengt => 60 * 30;

        public override Func<bool> ChanceAction => () => 
        {
            //Disabled for server currently
            if (Main.netMode != 0)
                return false;

            if (TwitchChat.Instance.ChatBoss == string.Empty || TwitchChat.Instance.ChatBoss == null)
                return true;

            if(Main.rand.NextFloat() < (Chance *((DateTimeOffset.Now - assignTime).TotalMinutes * 0.15)))
            {
                return true;
            }

            return false;
        };

        private List<string> part = new List<string>();
        private WeightedRandom<string> rand = new WeightedRandom<string>();

        protected override void OnStart()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            rand.Clear();

            part.Clear();
            if (TwitchChat.Instance.CurrentPool == null)
                TwitchChat.Instance.CurrentPool = new Dictionary<string, Action<ChannelMessageEventArgs>>();
            TwitchChat.Instance.CurrentPool.Clear();
            TwitchChat.Instance.CurrentPool.Add("boss", Handle);

            TwitchChat.Send(StartString + " Quick write boss in chat!");
        }

        protected override void OnEnd()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (rand.elements.Count == 0)
            {
                TwitchChat.Send("Noone was selected to become chat boss");
                TwitchChat.Instance.ChatBoss = TwitchChat.Instance.Username;
                TwitchChat.Post("Noone...", Color.White);
                return;
            }


            var t = rand.Get();

            TwitchChat.Instance.ChatBoss = t;

            assignTime = DateTimeOffset.Now;
            TwitchChat.Post($" @{TwitchChat.Instance.ChatBoss}", Color.Purple);

            if (Main.netMode != NetmodeID.MultiplayerClient)
                TwitchChat.Send(EndString + $" @{TwitchChat.Instance.ChatBoss} you can use heal, buff, death or quit");
        }


        private void Handle(ChannelMessageEventArgs msg)
        {
            if(!part.Contains(msg.From))
            {
                part.Add(msg.From);
                rand.Add(msg.From, msg.Badge.sub ? 2 : 1);
            }
        }

    }
}
