using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Razorwing.Framework.IO.Stores;

namespace TwitchChat.Chat
{
    public class EmoticonsStore : Texture2DStore
    {
        public EmoticonsStore(IResourceStore<byte[]> store) : base(store) { }

        public async Task<Texture2D> GetAsync(int id)
        {
            if (!TextureCache.ContainsKey($@"http://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0") && File.Exists($@"{TwitchChat.Path}\emoticons\{id}.png")) return await base.GetAsync($@"emoticons\{id}.png");

            Texture2D s = await GetAsync($@"http://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0");

            WriteAllData(s, id);

            return s;
        }

        public Texture2D Get(int id)
        {
            if (!TextureCache.ContainsKey($@"http://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0") && File.Exists($@"{TwitchChat.Path}\emoticons\{id}.png"))
            {
                return base.Get($@"emoticons\{id}.png");
            }

            Texture2D s = Get($@"http://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0");

            Task.Run(() => WriteAllData(s, id));

            return s;
        }

        private static void WriteAllData(Texture2D s, int id)
        {
            if (s == null)
                return;
            try
            {
                if (!TwitchChat.Instance.Storage.Exists($@"emoticons\{id}.png"))
                    s.SaveAsPng(TwitchChat.Instance.Storage.GetStream($@"emoticons\{id}.png", FileAccess.Write), s.Width, s.Height);
            }
            catch (Exception e)
            {
                Razorwing.Framework.Logging.Logger.Error(e, "Exception caught while saving emote on disc");
            }
        }
    }

    public enum LoadState
    {
        NotLoaded = 0,
        Loading,
        Loaded
    }
}