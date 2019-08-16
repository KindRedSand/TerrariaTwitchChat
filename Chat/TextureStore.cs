using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Razorwing.Framework.IO.Stores;
using System.IO;

namespace TwitchChat.Chat
{
    public class Texture2DStore : ResourceStore<Texture2D>
    {
        protected readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        protected IResourceStore<byte[]> store { get; }

        /// <summary>
        /// Decides at what resolution multiple this <see cref="TextureStore"/> is providing sprites at.
        /// ie. if we are providing high resolution (at 2x the resolution of standard 1366x768) sprites this should be 2.
        /// </summary>
        public readonly float ScaleAdjust;

        public Texture2DStore(IResourceStore<byte[]> store = null)
            : base()
        {
            this.store = store;
            (store as ResourceStore<byte[]>)?.AddExtension(@"png");
            AddExtension(@"png");
        }


        public new Task<Texture2D> GetAsync(string name) =>
            Task.Run(
                () => {
                    return Task.FromResult(Get(name));
                }
                );

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public new virtual Texture2D Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            lock (textureCache)
            {
                Texture2D tex;

                // refresh the texture if no longer available (may have been previously disposed).
                if (!textureCache.TryGetValue(name, out tex) || tex?.IsDisposed == true)
                {
                    using (var str = store.GetStream(name))
                    {
                        if (str == null)
                            return null;
                        textureCache[name] = tex = Texture2D.FromStream(Main.graphics.GraphicsDevice, str);
                        if (str is MemoryStream)///TODO: remove this when HttpClient issue was fixed in tML
                            str.Dispose();
                    }
                        
                }
                    

                return tex;
            }
        }
    }
}
