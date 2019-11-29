using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Razorwing.Framework.IO.Stores;

namespace TwitchChat.Overrides.Razorwing
{
    /// <summary>
    ///     Temp solution what uses <see cref="WebClient" /> instead of <see cref="HttpClient" />
    ///     It way slower since we can only download one file per time instead of default 3 streams in
    ///     <see cref="HttpClient" />
    /// </summary>
    public class WebStore : IResourceStore<byte[]> //TODO: remove this when HttpClient issue was fixed in tML
    {
        private readonly string accept = "image/png";
        private readonly WebClient web = new WebClient();


        public WebStore(string accept = null)
        {
            if (accept != null) //image/png
                web.Headers.Add("accept", this.accept = accept);
        }

        public byte[] Get(string name)
        {
            return ((MemoryStream) GetStream(name)).GetBuffer(); //Just reuse code
        }

        public Task<byte[]> GetAsync(string name)
        {
            return Task.FromResult(Get(name)); //Just reuse code
        }

        public Stream GetStream(string name)
        {
            lock (web)
            {
                using (Stream str = web.OpenRead(name))
                {
                    MemoryStream ms = new MemoryStream();
                    if (web.ResponseHeaders.Get("content-type") != accept) return null;

                    str?.CopyTo(ms);

                    return ms;
                }
            }
        }

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed) isDisposed = true;
        }

        ~WebStore() { Dispose(false); }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}