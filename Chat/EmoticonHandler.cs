using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI.Chat;

namespace TwitchChat.Chat
{
    public class EmoticonHandler : ITagHandler
    {
        internal static Dictionary<int, Texture2D> cache = new Dictionary<int, Texture2D>();
        internal static List<int> inProggres = new List<int>();
        internal static WebClient web = new WebClient();
        internal static int[] failsafe = { 0 };


        static EmoticonHandler()
        {
            web.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            web.Headers.Add("accept", "image/png");
        }

        public EmoticonHandler()
        {
           
        }

        /// <summary>
        /// Load texture from web. Now it only cache texture in RAM 
        /// </summary>
        /// <param name="id">Emoticon ID</param>
        public static void LoadTexture(int id)
        {
            if(inProggres.Contains(id))
            {
                return;
            }
            inProggres.Add(id);

            try
            {
                lock (web)
                using (MemoryStream ms = new MemoryStream())
                using (var str = web.OpenRead($@"http://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0"))
                {
                    //If result is different what png image we can't load it as Texture2D 
                    if (web.ResponseHeaders.Get("content-type") != "image/png")
                    {
                        inProggres.Remove(id);
                        return;
                    }

                    //Texture2D.FromStream requre stream with available seek operation, so we just copy all data (until EndOfStream) in to MemoryStream
                    str.CopyTo(ms);

                    Texture2D txt = Texture2D.FromStream(Main.graphics.GraphicsDevice, ms);

                    lock(cache)
                    {
                        cache.Add(id, txt);
                    }
                }

                inProggres.Remove(id);

            }catch(Exception e)
            {
                var list = failsafe.ToList();
                list.Add(id);
                failsafe = list.ToArray();
                inProggres.Remove(id);
            }

        }

        private Dictionary<string, int> convertingEmotes = new Dictionary<string, int>()
        {
            ["LUL"] = 425618,
            ["CoolStoryBob"] = 123171,
            ["BibleThump"] = 86,
            ["SeemsGood"] = 64138,
            ["SwiftRage"] = 34,
            ["NotLikeThis"] = 58765,
            ["4Head"] = 354,
            ["SMOrc"] = 52,
            ["ResidentSleeper"] = 245,
            ["Jebaited"] = 114836,
            ["PogChamp"] = 88,
            ["BabyRage"] = 22639,
            ["Kappa"] = 25,
            ["Kreygasm"] = 41,
            ["BlessRNG"] = 153556,
            ["KappaPride"] = 55338,
            ["<3"] = 9,
        };

        public TextSnippet Parse(string text, Color baseColor = default(Color), string options = null)
        {
            int i;
            if (int.TryParse(text, out i))
                return new EmoticonSnippet(i);
            else if (convertingEmotes.ContainsKey(text))
                return new EmoticonSnippet(convertingEmotes[text]);
            return new TextSnippet(text);
        }

        private class EmoticonSnippet : TextSnippet
        {
            private int id;

            public EmoticonSnippet(int id)
            {
                this.id = id;
            }

            public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default(Vector2), Color color = default(Color), float scale = 1)
            {
                if (failsafe.Contains(id))
                {
                    size = new Vector2(0, 0);
                    return false;
                }

                if(!cache.ContainsKey(id))
                {
                    if(!inProggres.Contains(id))
                    {
                        //In case not blocking game thread while we download image
                        Task.Run(() => { LoadTexture(id); });
                        //LoadTexture(id);
                    }
                    return base.UniqueDraw(justCheckingString, out size, spriteBatch, position, color, scale);
                }

                try
                {
                    if (color != null && color.A != 0)
                        spriteBatch.Draw(cache[id], new Rectangle((int)position.X, (int)position.Y, 28, 28), color);

                    size = new Vector2(28, 28);
                    return true;
                }catch(Exception e)
                {
                    var list = failsafe.ToList();
                    list.Add(id);
                    failsafe = list.ToArray();
                    size = new Vector2(0, 0);
                    return false;
                }

            }
        }

    }


}
