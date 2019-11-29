using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TwitchChat.IRCClient;

namespace TwitchChat.Events.ZergInvasion
{
    public class ZergVoteEvent : VoteEvent
    {
        public override int Cooldown { get; set; } = 1500;

        public override float Chance { get; set; } = NPC.downedMechBossAny ? 0.1f : 0;

        public override IDictionary<int, float> Invaders => null;

        public override string StartString => "Didn't you noticed what where not enough enemies?";

        public override int Length => 3600;

        public override string Description => "Zerg invasion incoming! Hom many monsters should invade the world";

        public override Dictionary<string, Action<ChannelMessageEventArgs>> VoteSuggestion { get; } = new Dictionary<string, Action<ChannelMessageEventArgs>>
        {
            ["more"] = m =>
            {
                EventWorld world = ModContent.GetInstance<EventWorld>();
                TwitchChat.Send("More enemy");
                world.WorldScheduler.Add(() =>
                {
                    world.StartWorldEvent(new ZergRushEvent
                        {Mul = 1000});
                });
            },
            ["less"] = m =>
            {
                EventWorld world = ModContent.GetInstance<EventWorld>();
                TwitchChat.Send("Less enemy");
                world.WorldScheduler.Add(() =>
                {
                    world.StartWorldEvent(new ZergRushEvent
                        {Mul = 1});
                });
            },
            ["nochange"] = m =>
            {
                EventWorld world = ModContent.GetInstance<EventWorld>();
                TwitchChat.Send("No spawn changing");
                world.WorldScheduler.Add(() => { world.StartWorldEvent(new ZergRushEvent()); });
            }
        };

        public override VoteMode VoteMode => VoteMode.EndAction;
    }
}