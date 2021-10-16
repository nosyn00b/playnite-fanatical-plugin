using FanaticalLibrary.Models;
using FanaticalLibrary.Services;
using PlayniteExtensions.Common;
using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FanaticalLibrary
{
    [LoadPlugin]
    public class FanaticalLibrary : LibraryPluginBase<FanaticalLibrarySettingsViewModel>
    {

        internal readonly string TokensPath;

        /*private FanaticalLibrarySettingsViewModel settings { get; set; }
        

        private static readonly ILogger logger = LogManager.GetLogger();
        public override Guid Id { get; } = Guid.Parse("ef17cc27-95d4-45e7-bd49-214ba2e5f4b2");

        // Change to something more appropriate
        public override string Name => "Fanatical Library";

        // Implementing Client adds ability to open it via special menu in playnite.
        public override LibraryClient Client { get; } = new FanaticalLibraryClient();
        */

        public FanaticalLibrary(IPlayniteAPI api) : base(
            "Fanatical Library",
            Guid.Parse("ef17cc27-95d4-45e7-bd49-214ba2e5f4b2"),
            new LibraryPluginProperties { CanShutdownClient = true, HasSettings = true },
            new FanaticalLibraryClient(),
            null,
            (_) => new FanaticalLibrarySettingsView(),
            api)
            {
                SettingsViewModel = new FanaticalLibrarySettingsViewModel(this, api);
                TokensPath = Path.Combine(GetPluginUserDataPath(), "tokens.json");
            }
        
       /*
        public FanaticalLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new FanaticalLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = true
            };
            TokensPath = Path.Combine(GetPluginUserDataPath(), "tokens.json");
        }*/

        public string GetCachePath(string dirName)
        {
            return Path.Combine(GetPluginUserDataPath(), dirName);
        }

        internal List<FanaticalLibraryItem> GetLibraryItems()
        {
            var cacheDir = GetCachePath("catalogcache");
            var games = new List<FanaticalLibraryItem>();
            var accountApi = new FanaticalAccountClient(PlayniteApi, TokensPath);
            var assets = accountApi.GetLibraryItems();
            if (!assets?.Any() == true)
            {
                Logger.Warn("Found no assets on Fanatical accounts.");
            }

            //TODO Manage cacheing
            /*
            foreach (var gameAsset in assets) //assets.Where(a => a.@namespace != "ue")
            {

                var cacheFile = Paths.GetSafePathName($"{gameAsset._id}_{gameAsset.iid}.json");//gameAsset.@namespace_
                cacheFile = Path.Combine(cacheDir, cacheFile);
                //TODO filter key 
                var newGame = new FanaticalLibraryItem
                {
                    Source = new MetadataNameProperty("Fanatical"),
                    GameId = gameAsset.appName,
                    Name = catalogItem.title.RemoveTrademarks(),
                    Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") }
                };


                games.Add(newGame);
            }

            return games;*/
            return assets;
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            // Return list of user's games.
            /*return new List<GameMetadata>()
            {
                new GameMetadata()
                {
                    Name = "Notepad",
                    GameId = "notepad",
                    GameActions = new List<GameAction>
                    {
                        new GameAction()
                        {
                            Type = GameActionType.File,
                            Path = "notepad.exe",
                            IsPlayAction = true
                        }
                    },
                    IsInstalled = true,
                    Icon = new MetadataFile(@"c:\Windows\notepad.exe")
                },
                new GameMetadata()
                {
                    Name = "Calculator",
                    GameId = "calc",
                    GameActions = new List<GameAction>
                    {
                        new GameAction()
                        {
                            Type = GameActionType.File,
                            Path = "calc.exe",
                            IsPlayAction = true
                        }
                    },
                    IsInstalled = true,
                    Icon = new MetadataFile(@"https://playnite.link/applogo.png"),
                    BackgroundImage = new MetadataFile(@"https://playnite.link/applogo.png")
                }
            };*/

            var allGames = new List<GameMetadata>();
            Exception importError = null;

            
            try
            {
                var libraryGames = GetLibraryItems();
                Logger.Debug($"Found {libraryGames.Count} library Fantical items.");
                var nogames = 0;
           
                foreach (var item in libraryGames)
                {
                    if (item.status == "fulfilled")
                    { //only unredeemd
                        switch (item.type)
                        {
                            case "game":
                                Logger.Debug("Game found:" + item.name + ", status is [" + item.status + "]");
                                allGames.Add(fanaticalItemtoGame(item));
                                break;
                            case "dlc":
                                Logger.Debug("DLC found:" + item.name + ", status is [" + item.status + "]");
                                nogames++;
                                break;
                            case "book":
                                Logger.Debug("Book found:" + item.name + ", status is [" + item.status + "]");
                                nogames++;
                                break;
                            case "audio":
                                Logger.Debug("Audio found:" + item.name + ", status is [" + item.status + "]");
                                nogames++;
                                break;
                            default: //old games had not type and were games 
                                allGames.Add(fanaticalItemtoGame(item));
                                Logger.Debug("Undefined Type found:" + item.name + ", type is [" + item.type + "] status is [" + item.status + "]");
                                break;
                        }
                    }
                }

                Logger.Debug($"Skipped {nogames} items that are non-games.");


            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to import linked account Fanatical games details.");
                importError = e;
            }
            

            if (importError != null)
            {
                PlayniteApi.Notifications.Add(new NotificationMessage(
                    ImportErrorMessageId,
                    string.Format(PlayniteApi.Resources.GetString("LOCLibraryImportError"), Name) +
                    System.Environment.NewLine + importError.Message,
                    NotificationType.Error,
                    () => OpenSettingsView()));
            }
            else
            {
                PlayniteApi.Notifications.Remove(ImportErrorMessageId);
            }

            return allGames;
        }

        private GameMetadata fanaticalItemtoGame( FanaticalLibraryItem SourceGameMetadata)
        {

            var newGame = new GameMetadata
            { 
                Source = new MetadataNameProperty("Fanatical"),
                GameId = SourceGameMetadata._id,
                Name = SourceGameMetadata.name,
                Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") },
                IsInstalled = false //,
                //Icon = new MetadataFile(@"https://playnite.link/applogo.png"),
                //BackgroundImage = new MetadataFile(@"https://playnite.link/applogo.png")
            };
            return newGame;
        }
        
        /*

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new FanaticalLibrarySettingsView();
        }
        */
        /*
                public override LibraryMetadataProvider GetMetadataDownloader()
                {
                    return new IGDBLazyMetadataProvider(PlayniteApi);
                }
        */
    }
}