using System;
using System.Globalization;
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
                if (Extensions.Extensions.GetValueByName(uiElem.Name.Substring(2), Bot.GlobalSettings, out val))
                    uiElem.Text = val;
            }

            foreach (var uiElem in settings_grid.GetLogicalChildCollection<PasswordBox>())
            {
                string val;
                if (Extensions.Extensions.GetValueByName(uiElem.Name.Substring(2), Bot.GlobalSettings, out val))
                    uiElem.Password = val;
            }

            foreach (var uiElem in settings_grid.GetLogicalChildCollection<CheckBox>())
            {
                bool val;
                if (Extensions.Extensions.GetValueByName(uiElem.Name.Substring(2), Bot.GlobalSettings, out val))
                    uiElem.IsChecked = val;
            }

            LoadingUi = false;
        }

        private void InitWindowsControlls()
        {
            authBox.ItemsSource = Enum.GetValues(typeof(AuthType));
        }

        private void HandleUiElementChangedEvent(object uiElement)
        {
            var box = uiElement as TextBox;
            if (box != null)
            {
                var propName = box.Name.Replace("c_", "");
                Extensions.Extensions.SetValueByName(propName, box.Text, Bot.GlobalSettings);
                return;
            }
            var chB = uiElement as CheckBox;
            if (chB != null)
            {
                var propName = chB.Name.Replace("c_", "");
                Extensions.Extensions.SetValueByName(propName, chB.IsChecked, Bot.GlobalSettings);
            }
            var passBox = uiElement as PasswordBox;
            if (passBox != null)
            {
                var propName = passBox.Name.Replace("c_", "");
                Extensions.Extensions.SetValueByName(propName, passBox.Password, Bot.GlobalSettings);
            }
        }

        private void BotPropertyChanged(object sender, EventArgs e)
        {
            if (Bot == null || LoadingUi) return;
            HandleUiElementChangedEvent(sender);
        }

        private void authBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Bot == null || LoadingUi) return;
            var comboBox = sender as ComboBox;
            if (comboBox != null)
                Bot.GlobalSettings.Auth.AuthType = (AuthType)comboBox.SelectedItem;
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
            c_DeviceId.Text = Bot.GlobalSettings.Device.DeviceId = dd.DeviceId;
            c_AndroidBoardName.Text = Bot.GlobalSettings.Device.AndroidBoardName = dd.AndroidBoardName;
            c_AndroidBootLoader.Text = Bot.GlobalSettings.Device.AndroidBootLoader = dd.AndroidBootloader;
            c_DeviceBrand.Text = Bot.GlobalSettings.Device.DeviceBrand = dd.DeviceBrand;
            c_DeviceModel.Text = Bot.GlobalSettings.Device.DeviceModel = dd.DeviceModel;
            c_DeviceModelIdentifier.Text = Bot.GlobalSettings.Device.DeviceModelIdentifier = dd.DeviceModelIdentifier;
            c_HardwareManufacturer.Text = Bot.GlobalSettings.Device.HardwareManufacturer = dd.HardwareManufacturer;
            c_HardWareModel.Text = Bot.GlobalSettings.Device.HardWareModel = dd.HardwareModel;
            c_FirmwareBrand.Text = Bot.GlobalSettings.Device.FirmwareBrand = dd.FirmwareBrand;
            c_FirmwareTags.Text = Bot.GlobalSettings.Device.FirmwareTags = dd.FirmwareTags;
            c_FirmwareType.Text = Bot.GlobalSettings.Device.FirmwareType = dd.FirmwareType;
            c_FirmwareFingerprint.Text = Bot.GlobalSettings.Device.FirmwareFingerprint = dd.FirmwareFingerprint;
        }

        private void btn_textProxy_Click(object sender, RoutedEventArgs e)
        {
            if (!Bot.GlobalSettings.Auth.UseProxy)
            {
                MessageBox.Show("Proxy disabled!", "Proxy test", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                WebClient wc = new WebClient();
                wc.Proxy = Bot.Session.Proxy;
                wc.DownloadString("http://google.com/ncr");
                MessageBox.Show("Proxy works fine!", "Proxy test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { MessageBox.Show("Proxy failed!", "Proxy test", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
    }
}
