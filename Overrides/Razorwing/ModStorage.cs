﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Razorwing.Framework;
using Razorwing.Framework.Platform;
using Terraria.ModLoader;

namespace TwitchChat.Overrides.Razorwing
{
    public class ModStorage : DesktopStorage
    {
        private string root;

        public ModStorage(string baseName = "") : base(baseName) { }

        protected override string LocateBasePath()
        {
            if (root == null)
            {
                string[] arr = ModLoader.ModPath.Split('\\');
                var i = -1;
                List<string> l = arr.Select(s =>
                {
                    i++;
                    return i < arr.Length - 1 ? arr[i] : null;
                }).ToList();
                l.Add("Mods");
                l.Add("Cache");
                root = string.Join("\\", l);
            }

            return root;
        }

        public override void OpenInNativeExplorer()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows) Process.Start("explorer.exe", GetFullPath(string.Empty));
        }

        public override Stream GetStream(string path, FileAccess access = FileAccess.Read, FileMode mode = FileMode.OpenOrCreate)
        {
            if (path.StartsWith("http")) ///TODO: remove this when HttpClient issue was fixed in tML
                return null;
            return base.GetStream(path, access, mode);
        }
    }
}