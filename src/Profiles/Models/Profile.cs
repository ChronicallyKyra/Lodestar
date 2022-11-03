using Mercurius.Configuration;
using Mercurius.DBus;
using Tmds.DBus;
using System.Threading.Tasks;
using NLog;

namespace Mercurius.Profiles {
    public class Profile : IProfile {
        // Make a reference to it's respective json file, with methods to update, delete, etc.
        public string Name { get; set; }
        public string MinecraftVersion { get; set; }
        public bool ServerSide { get; set; }
        public string Loader { get; set; }
        public List<Mod> Mods { get; set; }
        public ObjectPath ObjectPath { get => _objectPath; }
        private ObjectPath _objectPath;

        public string Path { get => string.Format("{0}{1}.profile.json", SettingsManager.Settings.Profile_Directory, Name); } //"{SettingsManager.Settings.Profile_Directory}/{this.Name}.profile.json";
        // private bool _disposed = false;
        private ILogger logger;

        internal static async Task<Profile> CreateNewAsync(string name, string minecraftVersion, string loader, bool serverSide, bool select = false) {
            Profile profile = new Profile {
                Name = name,
                MinecraftVersion = minecraftVersion,
                ServerSide = serverSide,
                Loader = loader,
                Mods = new List<Mod>(),
                logger = LogManager.GetCurrentClassLogger(),
                _objectPath = new ObjectPath($"/org/mercurius/profile/{name}")
            };
            await ProfileManager.WriteProfileAsync(profile);
            await ProfileManager.LoadProfileAsync(profile.Name);
            if (select) ProfileManager.SelectProfile(profile.Name);

            return profile;
        }

        internal async Task<Profile> UpdateAsync(Profile oldProfile, Profile newProfile) {
            if (oldProfile.Equals(newProfile)) return oldProfile;

            await ProfileManager.OverwriteProfileAsync(newProfile, newProfile.Name);
            await ProfileManager.LoadProfileAsync(newProfile.Name);
            return ProfileManager.GetLoadedProfile(newProfile.Name);
        }
        internal async Task UpdateModListAsync(List<Mod> mods) {
            if (Mods is null) {
                Mods = mods;
            } else {
                foreach (Mod mod in mods)
                    Mods.Add(mod);
            }

            await ProfileManager.OverwriteProfileAsync(this, this.Name);
        }
        internal async Task UpdateModListAsync(Mod mod) {
            Mods.Add(mod);

            await ProfileManager.OverwriteProfileAsync(this, this.Name);
        }
        internal async Task RemoveModFromListAsync(Mod modToRemove) {

            // List<Mod> parentsWithRemoveableDependency = Mods.Where<Mod>((mod) => mod.Dependencies.Where<Mod>((dependency) => dependency.Title.ToLower().Equals(modToRemove.Title))).ToList<Mod>();

            List<Mod> parentsWithRemoveableDependency = new List<Mod>();
            foreach (Mod mod in Mods) {
                bool containsRemoveableDep = false;
                foreach (Mod dependency in mod.Dependencies) {
                    if (dependency.Title.ToLower().Equals(modToRemove.Title.ToLower())) {
                        containsRemoveableDep = true;
                    }
                }
                if (containsRemoveableDep) {
                    parentsWithRemoveableDependency.Add(mod);
                }
            }

            if (parentsWithRemoveableDependency.Count() <= 0) {
                logger.Debug("Removing {0}...", modToRemove.Title);
                Mods.Remove(modToRemove);

                await ProfileManager.OverwriteProfileAsync(this, this.Name);
                return;
            }
            
            foreach (Mod parent in parentsWithRemoveableDependency) {
                logger.Debug("Removing {0} as dependency of mod {1}", modToRemove.Title, parent.Title);
                Mods.Remove(parent);
                parent.Dependencies.Remove(modToRemove);
                await UpdateModListAsync(parent);
            }

            // foreach (Mod parent in Mods) {
            //     if (parent.Dependencies.Contains(modToRemove)) {
            //         Mods.Remove(parent);

            //         parent.Dependencies.Remove(modToRemove);
            //         await UpdateModListAsync(parent);
            //         Console.WriteLine(Mods);
            //         return;
            //     }
            // }

            // Mods.Remove(modToRemove);
            // await ProfileManager.OverwriteProfileAsync(this, this.Name);
        }
        internal void Delete() {
            if (File.Exists(Path))
                ProfileManager.DeleteProfileFile(Name);
            else {
                logger.Debug($"No file exists for profile {Name}... Unloading...");
                ProfileManager.UnloadProfile(this);
                return;
            }

            // Dipose/unload
            ProfileManager.UnloadProfile(this);            
        }

        // public void Dispose() {
        //     Dispose(false);
        //     GC.SuppressFinalize(this);
        // }
        // private void Dispose(bool disposing) {
        //     if (_disposed) return;
        //     if (disposing) {
        //         ProfileManager.UnloadProfile(this);
        //     }

        //     _disposed = true;
        // }
    }
    public enum ClientType {
        ClientSide, ServerSide
    }
}