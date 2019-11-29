using System;
using System.Collections.Generic;
using System.Linq;
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
        internal static EmoticonsStore store;
        internal static List<int> inProggres = new List<int>();
        internal static int[] failsafe = {0};

        public static Dictionary<string, int> convertingEmotes = new Dictionary<string, int>
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
            ["<3"] = 9
        };


        static EmoticonHandler() { }

        public TextSnippet Parse(string text, Color baseColor = default, string options = null)
        {
            int i;
            if (int.TryParse(text, out i))
                return new EmoticonSnippet(i)
                {
                    CheckForHover = true
                };
            if (convertingEmotes.ContainsKey(text))
                return new EmoticonSnippet(convertingEmotes[text])
                {
                    CheckForHover = true
                };
            return new TextSnippet(text);
        }

        /// <summary>
        ///     Load texture from web. Now it only cache texture in RAM
        /// </summary>
        /// <param name="id">Emoticon ID</param>
        public static void LoadTexture(int id)
        {
            if (inProggres.Contains(id)) return;
            inProggres.Add(id);

            try
            {
                Texture2D t = store.Get(id);

                if (t != null)
                    lock (cache)
                    {
                        cache.Add(id, t);
                    }

                inProggres.Remove(id);
            }
            catch (Exception e)
            {
                inProggres.Remove(id);
                if (e is ArgumentException)
                    return;
                List<int> list = failsafe.ToList();
                list.Add(id);
                failsafe = list.ToArray();
            }
        }

        private class EmoticonSnippet : TextSnippet
        {
            private readonly int id;

            public EmoticonSnippet(int id) { this.id = id; }

            public override void OnHover()
            {
                var w = "";
                foreach (KeyValuePair<string, int> p in convertingEmotes)
                    if (p.Value == id)
                    {
                        w = p.Key;
                        break;
                    }

                if (w != "")
                    Main.instance.MouseText(w);
                else
                    Main.instance.MouseText($"{id}");
            }

            public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default, Color color = default, float scale = 1)
            {
                if (failsafe.Contains(id))
                {
                    size = new Vector2(0, 0);
                    return false;
                }

                if (!cache.ContainsKey(id))
                {
                    if (!inProggres.Contains(id))
                        //In case not blocking game thread while we download image
                        Task.Run(() => { LoadTexture(id); });
                    return base.UniqueDraw(justCheckingString, out size, spriteBatch, position, color, scale);
                }

                try
                {
                    if (color != null && color.A != 0)
                        spriteBatch.Draw(cache[id], new Rectangle((int) position.X, (int) position.Y, (int) (cache[id].Width / 2.5), (int) (cache[id].Height / 2.5)), color);

                    size = new Vector2((int) (cache[id].Width / 2.5), (int) (cache[id].Height / 2.5));
                    return true;
                }
                catch (Exception e)
                {
                    List<int> list = failsafe.ToList();
                    list.Add(id);
                    failsafe = list.ToArray();
                    size = new Vector2(0, 0);
                    return false;
                }
            }
        }
    }
}