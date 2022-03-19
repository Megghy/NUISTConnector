using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace NUISTConnector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal TaskbarIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            DataContext = Config.Instance;

            DomainComboBox.ItemsSource = NUISTMain.Domains;
            DomainComboBox.SelectedValue = Config.Instance.Domain;
            PasswordBox.Password = Config.Instance.Password;

            notifyIcon = new();
            notifyIcon.Icon = (System.Drawing.Icon)Resources["Icon"];
            notifyIcon.ToolTipText = "NUIST Connector";

            if (CheckInfo(false))
                LoginButton.IsEnabled = true;

            NUISTMain.StartLoop();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!NUISTMain.ShouldStop)
            {
                e.Cancel = true;
                string title = "NUIST Connector";
                string text = "已最小化到托盘";

                //show balloon with built-in icon
                notifyIcon.ShowBalloonTip(title, text, BalloonIcon.Info);
                //hide balloon
                notifyIcon.HideBalloonTip();
                Visibility = Visibility.Hidden;
            }
        }
        public void AppendLog(string log, Color color = default)
        {
            if (color == default)
                color = Color.FromRgb(245, 245, 245);
            if (LogBox.Document.Blocks.Count <= 0)
            {
                LogBox.Document.Blocks.Add(new Paragraph());
            }
            Run run = new("\r\n" + log);
            SolidColorBrush solidColorBrush = new(color);
            solidColorBrush.Freeze();
            run.Foreground = solidColorBrush;
            run.FontFamily = new FontFamily("Consolas");
            (LogBox.Document.Blocks.LastBlock as Paragraph)?.Inlines.Add(run);
        }
        public static bool CheckInfo(bool notice = true)
        {
            if (string.IsNullOrEmpty(Config.Instance.UserName))
            {
                if (notice)
                    MessageBox.Show("未填写用户名");
                return false;
            }
            if (string.IsNullOrEmpty(Config.Instance.Password))
            {
                if (notice)
                    MessageBox.Show("未填写密码");
                return false;
            }
            if (Config.Instance.Domain == NUISTDomain.Unknown)
            {
                if (notice)
                    MessageBox.Show("未选择运营商");
                return false;
            }
            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UserNameBox.Text))
            {
                MessageBox.Show("未填写用户名");
                return;
            }
            if (string.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageBox.Show("未填写密码");
                return;
            }
            if (DomainComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("未选择运营商");
                return;
            }
            Config.Instance.Domain = (NUISTDomain)DomainComboBox.SelectedValue;
            Config.Instance.Password = PasswordBox.Password;
            Config.Instance.Save();
            AppendLog($"[Success] 已保存信息", NUISTMain.SuccessColor);
            if (Config.Instance.ShowNotice)
            {
                string title = "NUIST Connector";
                string text = "已保存账号信息";
                App._window.notifyIcon.ShowBalloonTip(title, text, BalloonIcon.None);
                App._window.notifyIcon.HideBalloonTip();
            }

        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (NUISTMain.IsLoggedIn)
                AppendLog($"[Info]你已处于登陆状态", NUISTMain.InfoColor);
            else if (CheckInfo())
            {
                LoginButton.IsEnabled = false;
                Task.Run(NUISTMain.Login);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (!NUISTMain.IsLoggedIn)
                AppendLog($"[Info]你未处于登陆状态", NUISTMain.InfoColor);
            else
            {
                LogoutButton.IsEnabled = false;
                Task.Run(NUISTMain.Logout);
            }
        }

        private void CleanLog_Click(object sender, RoutedEventArgs e)
        {
            LogBox.Document.Blocks.Clear();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.Save();
            AppendLog($"[Success] 已保存更改", NUISTMain.SuccessColor);
            if (Config.Instance.ShowNotice)
            {
                string title = "NUIST Connector";
                string text = "已保存更改";
                App._window.notifyIcon.ShowBalloonTip(title, text, BalloonIcon.None);
                App._window.notifyIcon.HideBalloonTip();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            NUISTMain.ShouldStop = true;
            notifyIcon.Visibility = Visibility.Hidden;
            notifyIcon.Dispose();
            Environment.Exit(0);
        }
    }
}
