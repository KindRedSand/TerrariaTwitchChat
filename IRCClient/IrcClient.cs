using System;
using System.ComponentModel;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using Razorwing.Framework.Logging;

namespace TwitchChat.IRCClient
{
    /// <summary>
    ///     IRC client writed specialy for using Twitch API.
    ///     It's include whisperings (WHISPER) and badges parsing.
    /// </summary>
    internal class IrcClient : IDisposable
    {
        public RoomStateBadge CurrentChannelBadge;

        private readonly Logger log = Logger.GetLogger("Irc");

        public Action<string, RoomStateBadge> RoomStateChanged;

        #region IDisposable support

        public void Dispose()
        {
            if (sslEnabled)
            {
                stream?.Dispose();
                ssl?.Dispose();
                writer?.Dispose();
                reader?.Dispose();
            }
            else
            {
                stream?.Dispose();
                writer?.Dispose();
                reader?.Dispose();
            }
        }

        #endregion

        /// <summary>
        ///     Connect to the IRC server
        /// </summary>
        public void Connect()
        {
            Thread t = new Thread(DoConnect) {IsBackground = true};
            t.Start();
        }

        private void DoConnect()
        {
            try
            {
                irc = new TcpClient(serverName, port);
                stream = irc.GetStream();
                if (sslEnabled)
                {
                    ssl = new SslStream(stream, false);
                    ssl.AuthenticateAsClient(serverName);
                    reader = new StreamReader(ssl);
                    writer = new StreamWriter(ssl);
                }
                else
                {
                    reader = new StreamReader(stream);
                    writer = new StreamWriter(stream);
                }

                if (!string.IsNullOrEmpty(serverPassword))
                    Send("PASS " + serverPassword);

                Send("NICK " + nick);
                Send("USER " + nick + " 0 * :" + nick);

                Listen();
            }
            catch (Exception ex)
            {
                ExceptionThrown?.Invoke(this, new ExceptionEventArgs(ex));
            }
        }

        /// <summary>
        ///     Disconnect from the IRC server
        /// </summary>
        public void Disconnect()
        {
            if (irc != null)
            {
                if (irc.Connected) Send("QUIT Client Disconnected");
                irc = null;
            }
        }

        /// <summary>
        ///     Sends the JOIN command to the server
        /// </summary>
        /// <param name="channel">Channel to join</param>
        public void JoinChannel(string channel)
        {
            if (irc != null && irc.Connected)
            {
                Send($"JOIN {channel}");
                log.Add($"JOIN {channel}");
            }
        }

        /// <summary>
        ///     Sends the PART command for a given channel
        /// </summary>
        /// <param name="channel">Channel to leave</param>
        public void PartChannel(string channel)
        {
            Send($"PART {channel}");
            log.Add($"PART {channel}");
        }

        /// <summary>
        ///     Send a notice to a user
        /// </summary>
        /// <param name="toNick">User to send the notice to</param>
        /// <param name="message">The message to send</param>
        public void SendNotice(string toNick, string message)
        {
            Send("NOTICE " + toNick + " :" + message);
            log.Add($"NOTICE {toNick} :{message}");
        }

        /// <summary>
        ///     Send a message to the channel
        /// </summary>
        /// <param name="channel">Channel to send message</param>
        /// <param name="message">Message to send</param>
        public void SendMessage(string channel, string message)
        {
            Send($"PRIVMSG {channel} :{message}");
            log.Add($"PRIVMSG {channel} :{message}");
        }

        /// <summary>
        ///     Send a message to the channel
        /// </summary>
        /// <param name="channel">User to send message</param>
        /// <param name="message">Message to send</param>
        public void SendWhisper(string toNick, string message)
        {
            Send($"WHISPER {toNick} :{message}");
            log.Add($"WHISPER {toNick} :{message}");
        }

        /// <summary>
        ///     Send RAW IRC commands
        /// </summary>
        /// <param name="message"></param>
        public void SendRaw(string message) { Send(message); }


        /// <summary>
        ///     Listens for messages from the server
        /// </summary>
        private void Listen()
        {
            string inputLine = null;
            while ((inputLine = reader.ReadLine()) != null)
                try
                {
                    if (inputLine[0] != '@')
                        ProcessData(inputLine, new Badge());
                    else
                        ParseBagedData(inputLine);
                }
                catch (Exception ex)
                {
                    ExceptionThrown?.Invoke(this, new ExceptionEventArgs(ex));
                }

            ConnectionClosed?.Invoke(this, new EventArgs());
        }

        private void ParseBagedData(string data)
        {
            // split the data into parts
            string[] ircData = data.Split(' ');

            var ircCommand = ircData[2];
            var tagString = ircData[0].Substring(1);
            string[] tags = tagString.Split(';');

            // Actually, we can get 3 different tags compound, but we want to know only about two of them
            if (ircCommand == "ROOMSTATE")
            {
                tagString = ircData[0].Substring(1);
                //@broadcaster-lang=;emote-only=0;followers-only=-1;r9k=0;rituals=0;room-id=30773965;slow=0;subs-only=0 :tmi.twitch.tv ROOMSTATE #redcrafting
                RoomStateBadge badge = new RoomStateBadge();
                foreach (var it in tags)
                {
                    string[] pair = it.Split('=');
                    switch (pair[0])
                    {
                        case "broadcaster-lang":
                            badge.BroadcasterLang = pair[1];
                            break;
                        case "followers-only":
                            if (pair[1] == "1")
                                badge.FollowersOny = true;
                            break;
                        case "r9k":
                            if (pair[1] == "1")
                                badge.r9k = true;
                            break;
                        case "slow":
                            if (pair[1] == "1")
                                badge.SlowMode = true;
                            break;
                        case "subs-only":
                            if (pair[1] == "1")
                                badge.SubChat = true;
                            break;
                        case "room-id":
                            badge.ChannelID = pair[1];
                            break;
                    }
                }

                RoomStateChanged?.Invoke(data, badge);
            }
            else
            {
                Badge badge = new Badge(false);
                foreach (var it in tags)
                {
                    string[] pair = it.Split('=');
                    switch (pair[0])
                    {
                        case "display-name":
                            badge.DisplayName = pair[1];
                            break;
                        case "subscriber":
                            if (pair[1] == "1")
                                badge.sub = true;
                            break;
                        case "turbo":
                            if (pair[1] == "1")
                                badge.turbo = true;
                            break;
                        case "mod":
                            if (pair[1] == "1")
                                badge.mod = true;
                            break;
                        case "emotes":
                            badge.emotes = pair[1].Split('/');

                            break;
                    }
                }


                ProcessData(data, badge);
            }
        }

        private void ProcessData(string data, Badge badge)
        {
            string[] ircData = data.Split(' ');

            var indx = 1;
            if (badge.DisplayName == string.Empty || badge.DisplayName == null)
                indx = 1;
            else
                indx = 2;
            var ircCommand = ircData[indx];


            if (data.Length > 4)
                if (data.Substring(0, 4) == "PING")
                {
                    // hardcoded to twitch server 
                    Send("PONG :tmi.twitch.tv");
                    return;
                }

            // re-act according to the IRC Commands
            switch (ircCommand)
            {
                case "001": // server welcome message, after this we can join
                    Send("MODE " + nick + " +B");
                    log.Add($"Connected to {serverName}");
                    OnConnect?.Invoke(this, new EventArgs());
                    break;
                case "353": // member list
                {
                    var channel = ircData[5];
                    if (ircData.Length > 5)
                    {
                        string[] userList = JoinArray(ircData, 5).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
                    break;
                case "433":
                    var takenNick = ircData[4];
                    break;
                case "JOIN": // someone joined
                {
                    var channel = ircData[2];
                    var user = ircData[0].Substring(1, ircData[1].IndexOf("!", StringComparison.Ordinal) - 1);
                }
                    break;
                case "MODE": // MODE was set
                {
                    var channel = ircData[3];
                    if (channel != Username)
                    {
                        string from;
                        if (ircData[0].Contains("!"))
                            from = ircData[1].Substring(1, ircData[1].IndexOf("!", StringComparison.Ordinal) - 1);
                        else
                            from = ircData[1].Substring(1);

                        var to = ircData[5];
                        var mode = ircData[4];
                    }

                    // TODO: event for userMode's
                }
                    break;
                case "NICK": // someone changed their nick
                    var oldNick = ircData[1].Substring(1, ircData[1].IndexOf("!", StringComparison.Ordinal) - 1);
                    var newNick = JoinArray(ircData, 4);

                    //Fire_NickChanged(new UserNickChangedEventArgs(oldNick, newNick));
                    break;
                case "NOTICE": // someone sent a notice
                {
                    var from = ircData[1];
                    var message = JoinArray(ircData, 4);
                    if (from.Contains("!"))
                    {
                        from = from.Substring(1, ircData[1].IndexOf('!') - 1);
                        NoticeMessage?.Invoke(this, new NoticeMessageEventArgs(from, message));
                    }
                    else
                    {
                        NoticeMessage?.Invoke(this, new NoticeMessageEventArgs(serverName, message));
                    }

                    log.Add($"We get NOTICE: {message}");
                }
                    break;
                case "PRIVMSG": // message was sent to the channel or as private
                {
                    var from = ircData[1].Substring(1, ircData[1].IndexOf('!') - 1);
                    var to = ircData[3];
                    var message = JoinArray(ircData, 4);

                    // if it's a private message
                    if (string.Equals(to, nick, StringComparison.CurrentCultureIgnoreCase))
                        PrivateMessage?.Invoke(this, new PrivateMessageEventArgs(from, message));
                    else
                        ChannelMessage?.Invoke(this, new ChannelMessageEventArgs(to, from, message, badge));
                }
                    break;
                case "WHISPER":
                {
                    var from = ircData[1].Substring(1, ircData[1].IndexOf('!') - 1);
                    var to = ircData[3];
                    var message = JoinArray(ircData, 4);

                    WhisperMessage?.Invoke(this, new PrivateMessageEventArgs(from, message));
                }
                    break;
                case "PART":
                case "QUIT": // someone left
                {
                    var channel = ircData[3];
                    var user = ircData[1].Substring(1, data.IndexOf("!") - 1);

                    //Fire_UserLeft(new UserLeftEventArgs(channel, user));
                    Send("NAMES " + ircData[3]);
                    log.Add("We QUIT");
                }
                    break;
                default:
                    // still using this while debugging

                    if (ircData.Length > 4)
                        ServerMessage?.Invoke(this, JoinArray(ircData, 3));

                    break;
            }
        }

        /// <summary>
        ///     Strips the message of unnecessary characters
        /// </summary>
        /// <param name="message">Message to strip</param>
        /// <returns>Stripped message</returns>
        private static string StripMessage(string message)
        {
            // remove IRC Color Codes
            foreach (Match m in new Regex((char) 3 + @"(?:\d{1,2}(?:,\d{1,2})?)?").Matches(message))
                message = message.Replace(m.Value, "");

            // if there is nothing to strip
            if (message == "")
                return "";
            if (message.Substring(0, 1) == ":" && message.Length > 2)
                return message.Substring(1, message.Length - 1);
            return message;
        }

        /// <summary>
        ///     Joins the array into a string after a specific index
        /// </summary>
        /// <param name="strArray">Array of strings</param>
        /// <param name="startIndex">Starting index</param>
        /// <returns>String</returns>
        private static string JoinArray(string[] strArray, int startIndex) { return StripMessage(string.Join(" ", strArray, startIndex, strArray.Length - startIndex)); }

        /// <summary>
        ///     Send message to server
        /// </summary>
        /// <param name="message">Message to send</param>
        private void Send(string message)
        {
            if (writer == null)
                return;
            writer.WriteLine(message);
            writer.Flush();
        }

        #region Properties and fields

        /// <summary>
        ///     The hostname of server. Couse we want use it for twitch so Twitch IRC server was setted for default.
        /// </summary>
        private string serverName = "irc.chat.twitch.tv";

        /// <summary>
        ///     The hostname of server.
        /// </summary>
        public string ServerAdress
        {
            get => serverName;

            set
            {
                if (serverName == value)
                    return;
                serverName = value;
            }
        }

        /// <summary>
        ///     Default port used for IRC.
        /// </summary>
        private int port = 6667;

        /// <summary>
        ///     Set a port for server connection.
        /// </summary>
        public int ServerPort
        {
            get => port;
            set
            {
                if (port == value)
                    return;
                port = value;
            }
        }


        private string serverPassword = string.Empty;

        /// <summary>
        ///     By default it is server password, but in Twitch case it is OAuth token genereted by twitchapps. You can get it
        ///     using https://twitchapps.com/tmi/ service.
        ///     For some purposes getting a password from there is not allowed.
        /// </summary>
        public string AuthToken
        {
            set
            {
                if (serverPassword == value) return;
                serverPassword = value;
            }
        }

        private string nick = "NotAuthtorisedYet";

        /// <summary>
        ///     Username from Twitch.
        /// </summary>
        public string Username
        {
            get => nick;
            set
            {
                if (nick == value) return;
                nick = value;
            }
        }

        private bool sslEnabled = true;

        /// <summary>
        ///     Do we use a SSL connection? True by default.
        /// </summary>
        public bool SSL
        {
            get => sslEnabled;
            set
            {
                if (sslEnabled == value) return;
                sslEnabled = value;
            }
        }

        /// <summary>
        ///     Returns true if the client is connected.
        /// </summary>
        public bool Connected
        {
            get
            {
                if (irc != null)
                    return irc.Connected;
                return false;
            }
        }

        /// <summary>
        ///     TcpClient used to talk to the server.
        /// </summary>
        private TcpClient irc;

        /// <summary>
        ///     Private network stream used to read/write from/to.
        /// </summary>
        private NetworkStream stream;

        /// <summary>
        ///     Private ssl stream used to read/write from/to.
        /// </summary>
        private SslStream ssl;

        /// <summary>
        /// Global variable used to read input from the client.
        /// [If you want to ask me wtf it is => idk, it just placed exactly here in more solutions, but used only for 1 method. Tell me a secret if you know why people do it]
        /// </summary>
        //private string inputLine;

        /// <summary>
        ///     Stream reader to read from the network stream.
        /// </summary>
        private StreamReader reader;

        /// <summary>
        ///     Stream writer to write to the stream.
        /// </summary>
        private StreamWriter writer;

        /// <summary>
        ///     AsyncOperation used to handle cross-thread wonderness.
        /// </summary>
        private AsyncOperation op;

        #endregion

        #region Constructor

        /// <summary>
        ///     IrcClient used to connect to an IRC Server (default port: 443 and enabled SSL)
        /// </summary>
        /// <param name="Server">IRC Server</param>
        public IrcClient(string Server) : this(Server, 443, true) { }

        /// <summary>
        ///     IrcClient used to connect to an IRC Server (default port: 443, default hostname is Twitch server and enabled SSL)
        /// </summary>
        /// <param name="Server">IRC Server</param>
        public IrcClient() : this("irc.chat.twitch.tv", 443, true) { }

        /// <summary>
        ///     IrcClient used to connect to an IRC Server
        /// </summary>
        /// <param name="Server">IRC Server</param>
        /// <param name="Port">IRC Port (6667 if you are unsure and don't use SSL)</param>
        /// <param name="EnableSSL">Decides wether you wish to use a SSL connection or not.</param>
        public IrcClient(string Server, int Port, bool EnableSSL)
        {
            op = AsyncOperationManager.CreateOperation(null);
            serverName = Server;
            port = Port;
            sslEnabled = EnableSSL;
        }

        #endregion

        #region Events

        public event EventHandler<string> Pinged;
        public event EventHandler OnConnect;
        public event EventHandler<ExceptionEventArgs> ExceptionThrown;

        public event EventHandler<ChannelMessageEventArgs> ChannelMessage;
        public event EventHandler<NoticeMessageEventArgs> NoticeMessage;
        public event EventHandler<PrivateMessageEventArgs> PrivateMessage;
        public event EventHandler<PrivateMessageEventArgs> WhisperMessage;
        public event EventHandler<string> ServerMessage;

        public event EventHandler ConnectionClosed;

        #endregion
    }


    public class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception x) { Exception = x; }

        public Exception Exception { get; internal set; }

        public override string ToString() { return Exception.ToString(); }
    }

    public class ChannelMessageEventArgs : EventArgs
    {
        public ChannelMessageEventArgs(string channel, string from, string message, Badge badge)
        {
            Channel = channel;
            From = from;
            Message = message;
            Badge = badge;
        }

        public string Channel { get; internal set; }
        public string From { get; internal set; }
        public string Message { get; internal set; }
        public Badge Badge { get; }

        public char this[int i]
        {
            get
            {
                if (i > Message.Length)
                    return ' ';
                return Message[i];
            }
        }
    }

    public class NoticeMessageEventArgs : EventArgs
    {
        public NoticeMessageEventArgs(string from, string message)
        {
            From = from;
            Message = message;
        }

        public string From { get; internal set; }
        public string Message { get; internal set; }
    }

    public class PrivateMessageEventArgs : EventArgs
    {
        public PrivateMessageEventArgs(string from, string message)
        {
            From = from;
            Message = message;
        }

        public string From { get; internal set; }
        public string Message { get; internal set; }
    }

    public class StringEventArgs : EventArgs
    {
        public StringEventArgs(string s) { Result = s; }

        public string Result { get; internal set; }

        public override string ToString() { return Result; }
    }
}