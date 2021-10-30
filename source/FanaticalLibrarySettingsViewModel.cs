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
        public bool ImportRedeemdItems { get; set; } = false;
        public bool ImportAlsoDLC { get; set; } = false;
        public bool ImportAlsoBooks { get; set; } = false;
        public bool ImportAlsoComics { get; set; } = false;
        public bool ImportAlsoSoftware { get; set; } = false;
        public bool ImportAlsoElearning { get; set; } = false;
        public bool ImportAlsoAudio { get; set; } = false;

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        [DontSerialize]
        public bool OptionThatWontBeSaved { get => optionThatWontBeSaved; set => SetValue(ref optionThatWontBeSaved, value); }
    }

    public class FanaticalLibrarySettingsViewModel : PluginSettingsViewModel<FanaticalLibrarySettings, FanaticalLibrary> 
    {

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