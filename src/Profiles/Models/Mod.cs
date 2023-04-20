using Mercurius.Configuration;
using System.Runtime.InteropServices;

using Mercurius.API.Modrinth;

namespace Mercurius.Profiles {
    [StructLayout(LayoutKind.Sequential)]
    public struct Mod {
        public string Title { get; set; }
        public string FileName { get; set; }
        public string DownloadURL { get; set; }
        public string ProjectId { get; set; }
        public string VersionId { get; set; }
        public string MinecraftVersion { get; set; }
        public string ModVersion { get; set; }
        public Remote Repo { get; set; }
        public ModLoader[] Loaders { get; set; }
        public Dictionary<string, Remote> DependencyVersions { get; set; }
        public RequiredBy ClientDependency { get; set; }

        internal void AddDependency(string id) {
            DependencyVersions.Add(id, Repo);
        }
        internal bool CheckFileExists() {
            return File.Exists($"{SettingsManager.Settings.Minecraft_Directory}/mods/{FileName}"); 
        }
    }
}