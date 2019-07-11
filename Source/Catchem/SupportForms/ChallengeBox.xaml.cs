using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Windows;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;

namespace Catchem.SupportForms
{
    /// <summary>
    /// Interaction logic for ChallengeBox.xaml
    /// </summary>
    public partial class ChallengeBox
    {
        private string _captchaUrl;
        private ISession _session;
        public bool InProgress;

        public ChallengeBox()
        {
            InitializeComponent();
        }

        public void DoChallenge(ISession session, string url)
        {
            _captchaUrl = url;
            _session = session;
            InProgress = true;
            Show();
        }

        private async void SendToken(string token)
        {
            if (!token.Contains("unity:")) return;
            token = token.Replace("unity:", "");
            await VerifyChallengeTask.Execute(_session, token);
            Close();
        }

        private void ChromeBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (IWebDriver driver = new ChromeDriver())
                {
                    //Notice navigation is slightly different than the Java version
                    //This is because 'get' is a keyword in C#
                    driver.Navigate().GoToUrl(_captchaUrl);

                    // Google's search is rendered dynamically with JavaScript.
                    // Wait for the page to load, timeout after 10 seconds
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(360));
                    wait.Until(d => d.Url.StartsWith("unity", StringComparison.OrdinalIgnoreCase));
                    SendToken(driver.Url);
                }
            }
            catch
            {
                //ignore
            }
            InProgress = false;
        }

        private void FirefoxBtn_Click(object sender, RoutedEventArgs e)
        {
            var path = Environment.CurrentDirectory;
            if (IntPtr.Size == 4)
                path += "/x86";
            else
                path += "/amd64";

            try
            {
                var service = FirefoxDriverService.CreateDefaultService(path, "geckodriver.exe");
                using (IWebDriver driver = new FirefoxDriver(service))
                {
                    //Notice navigation is slightly different than the Java version
                    //This is because 'get' is a keyword in C#
                    driver.Navigate().GoToUrl(_captchaUrl);

                    // Google's search is rendered dynamically with JavaScript.
                    // Wait for the page to load, timeout after 10 seconds
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(360));
                    wait.Until(d => d.Url.StartsWith("unity", StringComparison.OrdinalIgnoreCase));
                    SendToken(driver.Url);
                }
            }
            catch
            {
                //ignore
            }
            InProgress = false;
        }
    }
}


  
