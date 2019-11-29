﻿using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Utilities;
using TwitchChat.IRCClient;

namespace TwitchChat.Events
{
    public abstract class IVoteEvent : IWorldEvent
    {

        public abstract string Description { get; }

        /// <summary>
        /// Return list of available votes.
        /// <see cref="Action{ChannelMessageEventArgs}"/> will be called with argument if
        /// <see cref="VoteMode"/> return <see cref="VoteMode.InstantAction"/>.
        /// Otherwise it will be called without argument (it will be null")
        /// </summary>
        public abstract Dictionary<string, Action<ChannelMessageEventArgs>> VoteSuggestion { get; }

        protected Dictionary<string, string> Votes = new Dictionary<string, string>();

        public abstract VoteMode VoteMode { get; }

        protected override void OnStart()
        {
            Votes.Clear();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                TwitchChat.Instance.Irc.ChannelMessage += CountVote;

                var commands = "";
                foreach (var it in VoteSuggestion)
                    commands += it.Key + ", ";
                commands = commands.Substring(0, commands.Length - 2);
                TwitchChat.Send($"{Description}. Available commands: {commands}");
            }
        }

        protected override void OnEnd()
        {
            TwitchChat.Instance.Irc.ChannelMessage -= CountVote;

            if (VoteMode != VoteMode.EndAction|| Main.netMode == NetmodeID.MultiplayerClient)
                return;

            var votesCount = new SortedDictionary<string, int>();
            foreach (var it in Votes)
                if (votesCount.ContainsKey(it.Value))
                    votesCount[it.Value]++;
                else
                    votesCount.Add(it.Value, 1);

            var bigger = 0;
            string index = "", draftIndex = "";
            foreach (var it in votesCount)
                if (it.Value > bigger)
                {
                    bigger = it.Value;
                    index = it.Key;
                    draftIndex = "";
                }
                else if (it.Value == bigger)
                {
                    draftIndex = it.Key;
                }



            if (index == draftIndex && index != "")
            {
                var rand = new WeightedRandom<string>();
                rand.Add(index);
                rand.Add(draftIndex);

                index = rand.Get();
            }
            if(index != string.Empty)
                VoteSuggestion[index].Invoke(null);
            else
            {
                TwitchChat.Send("No votes...");
            }

        }

        private void CountVote(object sender, ChannelMessageEventArgs m)
        {
            if (Votes.ContainsKey(m.From))
                return;
            foreach (var it in VoteSuggestion)
            {
                if (m.Message.ToLower().StartsWith(it.Key.ToLower()))
                {
                    Votes.Add(m.From, it.Key);
                    if (VoteMode == VoteMode.InstantAction)
                        it.Value.Invoke(m);
                    return;
                }
            }
        }

    }

    public enum VoteMode
    {
        InstantAction,
        EndAction,
    }
}
