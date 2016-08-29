using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Catchem.Classes;
using Catchem.Extensions;
using Catchem.Interfaces;
using PoGo.PokeMobBot.Logic;
using PokemonGo.RocketAPI.Enums;
using System.Net;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : IBotPage
    {
        public BotWindowData Bot;
        public bool LoadingUi;
        public string SubPath;
        private CatchemSettings _globalSettings;

        public void SetGlobalSettings(CatchemSettings settings)
        {
            _globalSettings = settings;
            CustomRouteComboBox.ItemsSource = _globalSettings.Routes;
        }

        public SettingsPage()
        {
            InitializeComponent();
            InitWindowsControlls();
        }

        public void ClearData()
        {
            //ignored
        }

        public void UpdateCoordBoxes()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                c_DefaultLatitude.Text =
                    Bot.GlobalSettings.LocationSettings.DefaultLatitude.ToString(CultureInfo.CurrentCulture);
                c_DefaultLongitude.Text =
                    Bot.GlobalSettings.LocationSettings.DefaultLongitude.ToString(CultureInfo.CurrentCulture);
                c_DefaultAltitude.Text =
                    Bot.GlobalSettings.LocationSettings.DefaultAltitude.ToString(CultureInfo.CurrentCulture);
            }));
        }

        public void SetBot(BotWindowData bot)
        {
            LoadingUi = true;
            Bot = bot;
            authBox.SelectedItem = Bot.GlobalSettings.Auth.AuthType;
            if (Bot.GlobalSettings.Auth.AuthType == AuthType.Google)
            {
                loginBox.Text = Bot.GlobalSettings.Auth.GoogleUsername;
                passwordBox.Password = Bot.GlobalSettings.Auth.GooglePassword;
            }
            else
            {
                loginBox.Text = Bot.GlobalSettings.Auth.PtcUsername;
                passwordBox.Password = Bot.GlobalSettings.Auth.PtcPassword;
            }

            foreach (var uiElem in settings_grid.GetLogicalChildCollection<TextBox>())
            {
                string val;
                if (UiHandlers.GetValueByName(uiElem.Name.Substring(2), Bot.GlobalSettings, out val))
                    uiElem.Text = val;
            }

            foreach (var uiElem in settings_grid.GetLogicalChildCollection<PasswordBox>())
            {
                string val;
                if (UiHandlers.GetValueByName(uiElem.Name.Substring(2), Bot.GlobalSettings, out val))
                    uiElem.Password = val;
            }

            foreach (var uiElem in settings_grid.GetLogicalChildCollection<CheckBox>())
            {
                bool val;
                if (UiHandlers.GetValueByName(uiElem.Name.Substring(2), Bot.GlobalSettings, out val))
                    uiElem.IsChecked = val;
            }

            foreach (var uiElem in settings_grid.GetLogicalChildCollection<ComboBox>())
            {
                Enum val;
                if (UiHandlers.GetValueByName(uiElem.Name.Substring(2), Bot.GlobalSettings, out val))
                {
                    var valType = val.GetType();
                    uiElem.ItemsSource = Enum.GetValues(valType);
                    uiElem.SelectedItem = val;
                }
            }

            CustomRouteComboBox.SelectedItem =
                _globalSettings.Routes.FirstOrDefault(x => x.Name == Bot.GlobalSettings.LocationSettings.CustomRouteName);


            LoadingUi = false;
        }

        private void InitWindowsControlls()
        {
            authBox.ItemsSource = Enum.GetValues(typeof(AuthType));
        }

        private void BotPropertyChanged(object sender, EventArgs e)
        {
            if (Bot == null || LoadingUi) return;
            UiHandlers.HandleUiElementChangedEvent(sender, Bot.GlobalSettings);
        }

        private void authBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Bot == null || LoadingUi) return;
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;
            if (Equals(Bot.GlobalSettings.Auth.AuthType, (AuthType) comboBox.SelectedItem)) return;
            if (Bot.GlobalSettings.Auth.AuthType == AuthType.Google)
            {
                Bot.GlobalSettings.Auth.AuthType = (AuthType)comboBox.SelectedItem;
                loginBox.Text = Bot.GlobalSettings.Auth.PtcUsername;
                passwordBox.Password = Bot.GlobalSettings.Auth.PtcPassword;
            }
            else
            {
                Bot.GlobalSettings.Auth.AuthType = (AuthType)comboBox.SelectedItem;
                loginBox.Text = Bot.GlobalSettings.Auth.GoogleUsername;
                passwordBox.Password = Bot.GlobalSettings.Auth.GooglePassword;
            }
            
        }

        private void loginBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Bot == null || LoadingUi) return;
            var box = sender as TextBox;
            if (box == null) return;
            if (Bot.GlobalSettings.Auth.AuthType == AuthType.Google)
                Bot.GlobalSettings.Auth.GoogleUsername = box.Text;
            else
                Bot.GlobalSettings.Auth.PtcUsername = box.Text;
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (Bot == null || LoadingUi) return;
            var box = sender as PasswordBox;
            if (box == null) return;
            if (Bot.GlobalSettings.Auth.AuthType == AuthType.Google)
                Bot.GlobalSettings.Auth.GooglePassword = box.Password;
            else
                Bot.GlobalSettings.Auth.PtcPassword = box.Password;
        }

        private void b_generateRandomDeviceId_Click(object sender, RoutedEventArgs e)
        {
            c_DeviceId.Text = DeviceSettings.RandomString(16, "0123456789abcdef");
        }

        private void b_getDataFromRealPhone_Click(object sender, RoutedEventArgs e)
        {
            StartFillFromRealDevice();
        }

        private void btn_SaveBot_Click(object sender, RoutedEventArgs e)
        {
            Bot.GlobalSettings.StoreData(SubPath + "\\" + Bot.ProfileName);
        }

        private async void StartFillFromRealDevice()
        {
            var dd = await Adb.GetDeviceData();
            Bot.GlobalSettings.Device.DeviceId = dd.DeviceId;
            Bot.GlobalSettings.Device.AndroidBoardName = dd.AndroidBoardName;
            Bot.GlobalSettings.Device.AndroidBootLoader = dd.AndroidBootloader;
            Bot.GlobalSettings.Device.DeviceBrand = dd.DeviceBrand;
            Bot.GlobalSettings.Device.DeviceModel = dd.DeviceModel;
            Bot.GlobalSettings.Device.DeviceModelIdentifier = dd.DeviceModelIdentifier;
            Bot.GlobalSettings.Device.HardwareManufacturer = dd.HardwareManufacturer;
            Bot.GlobalSettings.Device.HardWareModel = dd.HardwareModel;
            Bot.GlobalSettings.Device.FirmwareBrand = dd.FirmwareBrand;
            Bot.GlobalSettings.Device.FirmwareTags = dd.FirmwareTags;
            Bot.GlobalSettings.Device.FirmwareType = dd.FirmwareType;
            Bot.GlobalSettings.Device.FirmwareFingerprint = dd.FirmwareFingerprint;
            FillBoxesFromSettings();
        }

        private void FillBoxesFromSettings()
        {
            c_DeviceId.Text = Bot.GlobalSettings.Device.DeviceId;
            c_AndroidBoardName.Text = Bot.GlobalSettings.Device.AndroidBoardName;
            c_AndroidBootLoader.Text = Bot.GlobalSettings.Device.AndroidBootLoader;
            c_DeviceBrand.Text = Bot.GlobalSettings.Device.DeviceBrand;
            c_DeviceModel.Text = Bot.GlobalSettings.Device.DeviceModel;
            c_DeviceModelIdentifier.Text = Bot.GlobalSettings.Device.DeviceModelIdentifier;
            c_HardwareManufacturer.Text = Bot.GlobalSettings.Device.HardwareManufacturer;
            c_HardWareModel.Text = Bot.GlobalSettings.Device.HardWareModel;
            c_FirmwareBrand.Text = Bot.GlobalSettings.Device.FirmwareBrand;
            c_FirmwareTags.Text = Bot.GlobalSettings.Device.FirmwareTags;
            c_FirmwareType.Text = Bot.GlobalSettings.Device.FirmwareType;
            c_FirmwareFingerprint.Text = Bot.GlobalSettings.Device.FirmwareFingerprint;
        }

        private void btn_textProxy_Click(object sender, RoutedEventArgs e)
        {
            if (Bot == null || !Bot.GlobalSettings.Auth.UseProxy)
            {
                MessageBox.Show("Proxy disabled!", "Proxy test", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                var wc = new WebClient {Proxy = Bot.Session.Proxy};
                wc.DownloadString("http://google.com/ncr");
                MessageBox.Show("Proxy works fine!", "Proxy test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { MessageBox.Show("Proxy failed!", "Proxy test", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void b_GenerateNewAndroid_Click(object sender, RoutedEventArgs e)
        {
            if (Bot?.GlobalSettings?.Device == null) return;
            Bot.GlobalSettings.Device.NewRandomPhone();
            FillBoxesFromSettings();
        }

        private void CustomRouteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Bot?.GlobalSettings?.LocationSettings == null || LoadingUi) return;
            var cb = sender as ComboBox;
            var route = cb?.SelectedItem as BotRoute;
            if (route == null) return;
            Bot.GlobalSettings.LocationSettings.CustomRouteName = route.Name;
            Bot.GlobalSettings.LocationSettings.CustomRoute = route.Route;
        }
    }
}
