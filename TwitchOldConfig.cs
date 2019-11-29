using System.Collections.Generic;
using System.ComponentModel;
using Razorwing.Framework;
using Razorwing.Framework.Configuration;
using Razorwing.Framework.Platform;
using Razorwing.Framework.Platform.Linux;
using Razorwing.Framework.Platform.Windows;
using Terraria.ModLoader.Config;

namespace TwitchChat
{
    public class TwitchConfig : ModConfig
    {
        private bool autoReconnect = true;

        private string channel = "";

        private string commandPrefix = "!";


        private bool funMode;

        private bool ignoreCommands;

        private bool showIRC;

        private string username = "";
        public override ConfigScope Mode => ConfigScope.ClientSide;

        private bool available => mod != null && ((TwitchChat) mod).OldConfig != null;
        private TwitchOldConfig cfg => ((TwitchChat) mod)?.OldConfig;

        [Label("Twitch Username")]
        [Tooltip("Username used to authorize you in twitch IRC server. Lowercase will be forced automatically")]
        [DefaultValue("missingno")]
        public string Username
        {
            get => !available
                ? username
                : cfg?.Get<string>(TwitchCfg.Username);
            set
            {
                username = value;
                if (!available)
                    return;
                cfg?.Set(TwitchCfg.Username, value.ToLower());
            }
        }

        [Label("Target channel")]
        [Tooltip("IRC name of channel where to listen messages. In nearly any cases equal streamer username. Require mod reloading to confirm channel rejoining. Lowercase will be forced automatically")]
        [DefaultValue("#kindredthefox")]
        public string Channel
        {
            get => !available
                ? channel
                : cfg?.Get<string>(TwitchCfg.Channel);
            set
            {
                channel = value;
                if (!available)
                    return;
                cfg?.Set(TwitchCfg.Channel,
                    value.StartsWith("#") ? value.ToLower() : ("#" + value).ToLower());
            }
        }

        /// <summary>
        ///     Duplicate it to old config to prevent a token
        /// </summary>
        private string token
        {
            get => !available
                ? string.Empty
                : (cfg?.Get<string>(TwitchCfg.OAToken) ?? string.Empty).StartsWith("oauth:")
                    ? "oauth:"
                    : string.Empty;

            set => cfg?.Set(TwitchCfg.OAToken, value);
        }

        [Label("Copy OAToken from clipboard")]
        [Tooltip("Copy token directly from your OS clipboard. Works only for Windows and Linux. Not implemented for OSX, so use backup field")]
        public bool ButtonToken
        {
            get => token != string.Empty;
            set
            {
                switch (RuntimeInfo.OS)
                {
                    case RuntimeInfo.Platform.Windows:
                        WindowsClipboard cw = new WindowsClipboard();
                        var sw = cw.GetText();
                        if (sw != null && sw.StartsWith("oauth:") && sw != token)
                            token = sw;
                        break;
                    case RuntimeInfo.Platform.Linux:
                        LinuxClipboard cl = new LinuxClipboard();
                        var sl = cl.GetText();
                        if (sl != null && sl.StartsWith("oauth:") && sl != token)
                            token = sl;
                        break;
                    case RuntimeInfo.Platform.MacOsx:
                    default:
                        //RIP since i don't have any device with MacOS so idk how it works
                        //And i don't want to carry even more dependency to support apple things since it can be broken trought tML sandboxing
                        //And yeah, i hate apple devices
                        break;
                }
            }
        }

        [Label("Backup: manual token entry")]
        [Tooltip("Used if copy from clipboard won't work. Important note: mod use this config field only to reroute data to own config file. Changing fields here won't all ways mean changing actual settings!")]
        public string TokenEntry
        {
            get => "";
            set
            {
                if (value?.StartsWith("oauth:") ?? false)
                    token = value;
            }
        }

        [Label("Auto reconnect")]
        [Tooltip("Force client to automatically reconnect to IRC when connection get losted")]
        [DefaultValue(true)]
        public bool AutoReconnect
        {
            get => cfg?.Get<bool>(TwitchCfg.AutoConnect) ?? autoReconnect;
            set
            {
                autoReconnect = value;
                if (!available)
                    return;
                cfg?.Set(TwitchCfg.AutoConnect, value);
            }
        }

        [Label("Ignore Commands")]
        [Tooltip("Force client to ignore commands messages and messages from known bots including your own in case self botting")]
        [DefaultValue(false)]
        public bool IgnoreCommands
        {
            get => cfg?.Get<bool>(TwitchCfg.IgnoreCommands) ?? ignoreCommands;
            set
            {
                ignoreCommands = false;
                if (!available)
                    return;
                cfg?.Set(TwitchCfg.IgnoreCommands, value);
            }
        }

        [Label("Command prefix")]
        [DefaultValue("!")]
        public string CommandPrefix
        {
            get => !available
                ? commandPrefix
                : cfg?.Get<string>(TwitchCfg.IgnoreCommandPrefix);
            set
            {
                commandPrefix = value;
                if (!available)
                    return;
                cfg?.Set(TwitchCfg.IgnoreCommandPrefix, value);
            }
        }

        [Label("Known bots")]
        [Tooltip("List of bots usernames to ignore")]
        public List<string> UsersToIgnore { get; set; } = new List<string>
        {
            "nightbot",
            "moobot",
            "starbotttv",
            "stay_hydrated_bot"
        };

        [Label("Show Debug")]
        [Tooltip("Show all IRC data in chat and also enables internal mod logging.")]
        [DefaultValue(false)]
        public bool ShowIRC
        {
            get => cfg?.Get<bool>(TwitchCfg.ShowAllIrc) ?? showIRC;
            set
            {
                showIRC = value;
                if (!available)
                    return;
                cfg?.Set(TwitchCfg.ShowAllIrc, value);
            }
        }

        [Label("Enable Fun (Twitch Plays Terraria)")]
        [Tooltip("Enables experimental features.")]
        [DefaultValue(false)]
        public bool FunMode
        {
            get => cfg?.Get<bool>(TwitchCfg.EnableFun) ?? funMode;
            set
            {
                funMode = value;
                if (!available)
                    return;
                cfg?.Set(TwitchCfg.EnableFun, value);
            }
        }

        [Label("Don't forget to reload mod by /t r command!")]
        [Tooltip("Required in order to apply changes.")]
        [DefaultValue(false)]
        public bool ReloadBtn { get; set; }
    }


    /// <summary>
    ///     No more needed since ModLoader now has own config manager, but remain this for legacy loading
    ///     for update
    /// </summary>
    public class TwitchOldConfig : IniConfigManager<TwitchCfg>
    {
        public TwitchOldConfig(Storage storage)
            : base(storage)
        {
        }

        protected override string Filename => @"Twitch.ini";

        protected override void InitialiseDefaults()
        {
            Set(TwitchCfg.AutoConnect, true);
            Set(TwitchCfg.Channel, "#kindredthefox");
            Set(TwitchCfg.Username, "missingno");
            Set(TwitchCfg.OAToken, "https://twitchapps.com/tmi/");
            Set(TwitchCfg.ShowAllIrc, false);
            Set(TwitchCfg.IgnoreCommands, false);
            Set(TwitchCfg.IgnoreCommandPrefix, "!");
            Set(TwitchCfg.EnableFun, false);
        }
    }

    public enum TwitchCfg
    {
        Channel,
        OAToken,
        Username,
        AutoConnect,
        ShowAllIrc,
        IgnoreCommands,
        IgnoreCommandPrefix,
        EnableFun
    }
}