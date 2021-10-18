using FanaticalLibrary.Services;
using Playnite;
using Playnite.SDK;
using Playnite.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using Playnite.SDK.Data;






namespace FanaticalLibrary
{


    public class FanaticalLibrarySettings : ObservableObject
    {
        private bool optionThatWontBeSaved = false;
        public int Version { get; set; }
        //public bool ConnectAccount { get; set; } = false;
        public bool ImportRedeemdGames { get; set; } = false;

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        [DontSerialize]
        public bool OptionThatWontBeSaved { get => optionThatWontBeSaved; set => SetValue(ref optionThatWontBeSaved, value); }
    }

    public class FanaticalLibrarySettingsViewModel : PluginSettingsViewModel<FanaticalLibrarySettings, FanaticalLibrary> //ObservableObject, ISettings
    {
        /*private readonly FanaticalLibrary plugin;
        private FanaticalLibrarySettings editingClone { get; set; }

        private FanaticalLibrarySettings settings;
        public FanaticalLibrarySettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }*/
        public bool IsUserLoggedIn
        {
            get
            {
                return new FanaticalAccountClient(PlayniteApi, Plugin.TokensPath).GetIsUserLoggedIn();
            }
        }

        public RelayCommand<object> LoginCommand
        {
            get => new RelayCommand<object>((a) =>
            {
                Login();
            });
        }

        public FanaticalLibrarySettingsViewModel(FanaticalLibrary library, IPlayniteAPI api) : base(library, api)
        {
            var savedSettings = LoadSavedSettings();
            if (savedSettings != null)
            {
                savedSettings.Version = 1;
                Settings = savedSettings;
            }
            else
            {
                Settings = new FanaticalLibrarySettings { Version = 1 };
            }
        }
        /*
        public FanaticalLibrarySettingsViewModel(FanaticalLibrary plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<FanaticalLibrarySettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new FanaticalLibrarySettings();
            }
        }
        

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
        */
  
        private void Login()
        {
            try
            {
                var clientApi = new FanaticalAccountClient(PlayniteApi, Plugin.TokensPath);
                clientApi.Login();
                OnPropertyChanged(nameof(IsUserLoggedIn));
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(PlayniteApi.Resources.GetString("LOCNotLoggedInError"), "");
                Logger.Error(e, "Failed to authenticate user.");
            }
        }

    }
}