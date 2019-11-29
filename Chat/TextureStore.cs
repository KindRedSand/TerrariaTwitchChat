using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Razorwing.Framework.IO.Stores;
using Terraria;

namespace TwitchChat.Chat
{
    public class Texture2DStore : ResourceStore<Texture2D>
    {
        protected readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();

        public Texture2DStore(IResourceStore<byte[]> store = null)
        {
            Store = store;
            (store as ResourceStore<byte[]>)?.AddExtension(@"png");
            AddExtension(@"png");
        }

        protected IResourceStore<byte[]> Store { get; }


        public new Task<Texture2D> GetAsync(string name)
        {
            return Task.Run(
                () => Task.FromResult(Get(name)) 
            );
        }

        /// <summary>
        ///     Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public new virtual Texture2D Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            lock (TextureCache)
            {
                Texture2D tex;

                // refresh the texture if no longer available (may have been previously disposed).
                if (!TextureCache.TryGetValue(name, out tex) || tex?.IsDisposed == true)
                    using (Stream str = Store.GetStream(name))
                    {
                        if (str == null)
                            return null;
                        TextureCache[name] = tex = Texture2D.FromStream(Main.graphics.GraphicsDevice, str);
                        if (str is MemoryStream) //TODO: remove this when HttpClient issue was fixed in tML
                            str.Dispose();
                    }


                return tex;
            }
        }
    }
}