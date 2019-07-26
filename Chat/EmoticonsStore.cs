using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using Razorwing.Framework.IO.Stores;

namespace TwitchChat.Chat
{
    public class EmoticonsStore : Texture2DStore
    {

        public EmoticonsStore(IResourceStore<byte[]> store) : base(store)
        {

        }

        public async Task<Texture2D> GetAsync(int id)
        {
            if (!textureCache.ContainsKey($@"http://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0") && File.Exists($@"{TwitchChat.Path}\emoticons\{id}.png"))
            {
                return await base.GetAsync($@"emoticons\{id}.png");
            }
            else
            {
                var s = await GetAsync($@"http://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0");

                WriteAllData(s, id);

                return s;
            }
        }

        public Texture2D Get(int id)
        {
            if (!textureCache.ContainsKey($@"http://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0") && File.Exists($@"{TwitchChat.Path}\emoticons\{id}.png"))
            {
                return base.Get($@"emoticons\{id}.png");
            }
            else
            {
                var s = Get($@"http://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0");

                WriteAllData(s, id);

                return s;
            }
        }

        private async void WriteAllData(Texture2D s, int id)
        {
            if (s == null)
                return;
            try
            {
                if(!TwitchChat.Instance.Storage.Exists($@"emoticons\{id}.png"))
                    s.SaveAsPng(TwitchChat.Instance.Storage.GetStream($@"emoticons\{id}.png", FileAccess.Write, FileMode.OpenOrCreate), s.Width, s.Height);
            }catch (Exception e)
            {

            }
        }

    }

    public enum LoadState
    {
        NotLoaded = 0,
        Loading,
        Loaded,
    }
}
