using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using TwitchChat.IRCClient;
using TwitchChat.Overrides;

namespace TwitchChat.Events
{
    public class SpawnRateChangingVote : VoteEvent
    {
        public override int Cooldown { get; set; } = 1500;

        public override float Chance { get; set; } = 0.1f;

        public override IDictionary<int, float> Invaders => null;

        public override string StartString => "Didn't you noticed what where not enough enemies?";

        public override int Length => 3600;

        public override string Description => "Time to change how many enemy will spawn";

        public override Dictionary<string, Action<ChannelMessageEventArgs>> VoteSuggestion { get; } = new Dictionary<string, Action<ChannelMessageEventArgs>>
        {
            ["more"] = m =>
            {
                EventWorld world = ModContent.GetInstance<EventWorld>();
                TwitchChat.Send("More enemy!");
                world.WorldScheduler.Add(() => { GlobalSpawnOverride.StartOverrideSpawnRate(2.5f, 2f); });
            },
            ["less"] = m =>
            {
                EventWorld world = ModContent.GetInstance<EventWorld>();
                TwitchChat.Send("Less enemy");
                world.WorldScheduler.Add(() => { GlobalSpawnOverride.StartOverrideSpawnRate(0.01f, 0.3f); });
            },
            ["nochange"] = m =>
            {
                EventWorld world = ModContent.GetInstance<EventWorld>();
                TwitchChat.Send("No spawn changing");
                world.WorldScheduler.Add(GlobalSpawnOverride.EndOverride);
            }
        };

        public override VoteMode VoteMode => VoteMode.EndAction;
    }
}