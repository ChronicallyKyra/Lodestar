using System;
using System.Threading.Tasks;
using Mercurius.Modrinth;
using Mercurius.Commands;
using Mercurius.Configuration;
using Mercurius.Profiles;

public class SyncCommand : BaseCommand {
    public override string Name { get => "Sync"; }
    public override string Description { get => "Syncronises Mods with Profile"; }
    public override string Format { get => "Sync"; }

    private APIClient client = new APIClient();
    private List<Mod> installQueue = new List<Mod>();
    public override async Task Execute(string[] args) {
        if (ProfileManager.SelectedProfile is null) {
            Console.WriteLine("No Profile is Selected... ? (Create or Select One)");
            return;
        }

        SyncModsFiles();
        await Install();
    }

    private void SyncModsFiles() {
        List<string> existingFiles = Directory.GetFiles($"{SettingsManager.Settings.Minecraft_Directory}/mods/").ToList<string>();
        List<string> modPaths = new List<string>();

        foreach (Mod mod in ProfileManager.SelectedProfile.Mods) {
            modPaths.Add($"{SettingsManager.Settings.Minecraft_Directory}/mods/{mod.FileName}");

            foreach (Mod dependency in mod.Dependencies) {
                modPaths.Add($"{SettingsManager.Settings.Minecraft_Directory}/mods/{dependency.FileName}");
            }
        }
        
        // if (modPaths.Count <= 0 && existingFiles.Count > 0) {
            //TODO: Generate Version from Mod file
        // }

        List<string> keepers = existingFiles.Intersect<string>(modPaths).ToList<string>();

        foreach (string mod in keepers) {
            existingFiles.Remove(mod);
        }

        if (existingFiles.Count <= 0) {
            Console.WriteLine("There are no residiual unassociated mods to remove");
        } else {
            Console.WriteLine("Removing Unrecognized Mod jars...");
            foreach (string file in existingFiles)
                File.Delete(file);
        }

        if (ProfileManager.SelectedProfile.Mods.Count <= 0) {
            Console.WriteLine("There is nothing to do...");
            return;
        }
        List<Mod> preQueue = new List<Mod>();
        preQueue.AddRange(ProfileManager.SelectedProfile.Mods);
        foreach (Mod mod in ProfileManager.SelectedProfile.Mods) {
            preQueue.AddRange(mod.Dependencies);
        }
        

        // Queue mods for install
        foreach (Mod mod in preQueue) {
            if (File.Exists($"{SettingsManager.Settings.Minecraft_Directory}/mods/{mod.FileName}")) {
                Console.Write("{0}: {1} is already installed, reinstall? (y/N) >", mod.Title, mod.ModVersion);

                if (Console.ReadLine().ToLower().Equals("y")) {
                    installQueue.Add(mod);
                }
                    
            } else
                installQueue.Add(mod);
        }
    }
    private async Task<bool> Install() {
        if (installQueue.Count < 1) {
            Console.WriteLine("There is nothing to do");
            return false;
        }

        Console.WriteLine("Mods Queued for Install: ");
        foreach (Mod modToInstall in installQueue) {
            Console.WriteLine("- {0}", modToInstall.Title);
        }

        Console.Write("\nContinue with Operation? (Y/n) ");

        if (Console.ReadLine().ToLower().Equals("n")) {
            Console.WriteLine("Aborting...");
            return false;
        }
        
        foreach (Mod mod in installQueue) {
            await client.DownloadVersionAsync(mod);
        }
        return true;
    }
}