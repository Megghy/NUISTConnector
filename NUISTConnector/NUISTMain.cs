using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NUISTConnector
{
    public enum NUISTDomain
    {
        Unknown,
        CMCC = 2,
        ChinaNet = 3,
        ChinaUnicom = 4
    }
    public static class NUISTMain
    {
        public static readonly Dictionary<string, NUISTDomain> Domains = new()
        {
            { "中国移动", NUISTDomain.CMCC },
            { "中国电信", NUISTDomain.ChinaNet },
            { "中国联通", NUISTDomain.ChinaUnicom }
        };

        public const string URL_GET_IP = "http://10.255.255.34/api/v1/ip";
        public const string URL_LOGIN = "http://10.255.255.34/api/v1/login";
        public const string URL_LOGOUT = "http://10.255.255.34/api/v1/logout";
        public static readonly Color ErrorColor = Color.FromRgb(220, 170, 170);
        public static readonly Color InfoColor = Color.FromRgb(200, 200, 120);
        public static readonly Color SuccessColor = Color.FromRgb(120, 200, 120);
        public static readonly Ping _ping = new();

        private static string? _ip;
        private static bool _isLoggedIn = false;
        public static bool IsLoggedIn
        {
            get => _isLoggedIn; 
            internal set
            {
                _isLoggedIn = value;
                App.Current.Dispatcher.BeginInvoke(() =>
                {
                    App._window.LoginButton.IsEnabled = !value;
                    App._window.LogoutButton.IsEnabled = value;
                });
            }
        }
        public static bool ShouldStop { get; internal set; } = false;

        private static readonly HttpClient _client = new()
        {
            Timeout = new TimeSpan(0, 0, 5)
        };
        public static void Log(string log, Color color = default)
            => App.Current.Dispatcher.BeginInvoke(() => App._window.AppendLog(log, color));

        public static async Task<string?> GetIP()
        {
            try
            {
                _ip = (string)JsonNode.Parse(await _client.GetStringAsync(URL_GET_IP))["data"];
                return _ip;
            }
            catch (TaskCanceledException)
            {
                Log($"[Error] 从服务器获取IP超时", ErrorColor);
                return null;
            }
            catch (Exception ex)
            {
                Log($"[Error] 从服务器获取IP时发生错误\r\n{ex}", ErrorColor);
                return null;
            }
        }
        public static async Task<bool> Logout()
        {
            if (!IsLoggedIn)
            {
                Log($"[Error] 你尚未登录, 无法登出");
                return false;
            }
            if (await GetIP() is { } ip)
            {
                using var logoutContent = new StringContent(new NUISTRequestData()
                {
                    channel = "0",
                    ifautologin = "0",
                    pagesign = "thirdauth",
                    username = Config.Instance.UserName,
                    password = Config.Instance.PasswordInternal,
                    usripadd = await GetIP()
                }.Serialize(), Encoding.UTF8, "application/json");
                var logoutRespone = await _client.PostAsync(URL_LOGOUT, logoutContent);
                if (logoutRespone.IsSuccessStatusCode && JsonNode.Parse(await logoutRespone.Content.ReadAsStringAsync()) is { } secondNode)
                {
                    if ((int)secondNode["code"] == 200)
                    {
                        if (Config.Instance.ShowNotice)
                        {
                            await App.Current.Dispatcher.BeginInvoke(() =>
                            {
                                App._window.notifyIcon.ShowBalloonTip("NUIST Connector", "已登出校园网", BalloonIcon.Info);
                                App._window.notifyIcon.HideBalloonTip();
                            });
                        }
                        Log($"[Success] 已登出校园网", SuccessColor);
                        IsLoggedIn = false;
                        return true;
                    }
                    else
                        Log($"[Error] 登出失败. {secondNode["message"]}", ErrorColor);
                }
                else
                    Log($"[Error] 登出失败: {logoutRespone.StatusCode}");
            }
            else
                Log($"[Error] 未获取到IP, 无法登出");
            return false;
        }
        internal static bool IsConnecting { get; private set; } = false;
        public static async Task<bool> Login()
        {
            if (IsConnecting)
                return false;
            if (await IsConnected())
            {
                Log($"[Success] 已连接到网络", SuccessColor);
                IsLoggedIn = true;
                return true;
            }
            try
            {
                Log($"[Info] 正在尝试连接 i-NUIST...", InfoColor);
                if (await GetIP() is { } ip)
                {
                    using var firstContent = new StringContent(new NUISTRequestData()
                    {
                        channel = "_GET",
                        ifautologin = "0",
                        pagesign = "firstauth",
                        username = Config.Instance.UserName,
                        password = Config.Instance.PasswordInternal,
                        usripadd = ip
                    }.Serialize(), Encoding.UTF8, "application/json");
                    var firstRespone = await _client.PostAsync(URL_LOGIN, firstContent);
                    if (firstRespone.IsSuccessStatusCode && JsonNode.Parse(await firstRespone.Content.ReadAsStringAsync()) is { } firstNode)
                    {
                        if ((int)firstNode["code"] == 200)
                        {
                            Log($"[Info] 一步验证成功, 发送二步验证...", InfoColor);
                            using var secondContent = new StringContent(new NUISTRequestData()
                            {
                                channel = ((byte)Config.Instance.Domain).ToString(),
                                ifautologin = "0",
                                pagesign = "secondauth",
                                username = Config.Instance.UserName,
                                password = Config.Instance.PasswordInternal,
                                usripadd = ip
                            }.Serialize(), Encoding.UTF8, "application/json");
                            var secondRespone = await _client.PostAsync(URL_LOGIN, secondContent);
                            if (secondRespone.IsSuccessStatusCode && JsonNode.Parse(await secondRespone.Content.ReadAsStringAsync()) is { } secondNode)
                            {
                                if ((int)secondNode["code"] == 200)
                                {
                                    if (Config.Instance.ShowNotice)
                                    {
                                        await App.Current.Dispatcher.BeginInvoke(() =>
                                        {
                                            App._window.notifyIcon.ShowBalloonTip("NUIST Connector", "已成功连接校园网", BalloonIcon.Info);
                                            App._window.notifyIcon.HideBalloonTip();
                                        });
                                    }
                                    Log($"[Success] 二步验证完成, 成功登陆校园网", SuccessColor);
                                    IsLoggedIn = true;
                                    return true;
                                }
                                else
                                {
                                    Log($"[Error] 登陆失败. {secondNode["message"]}", ErrorColor);
                                    await App.Current.Dispatcher.BeginInvoke(() =>
                                    {
                                        App._window.notifyIcon.ShowBalloonTip("NUIST Connector", $"登陆失败. {secondNode["message"]}", BalloonIcon.Error);
                                        App._window.notifyIcon.HideBalloonTip();
                                    });
                                }
                            }
                            else
                                Log($"[Error] 二步请求失败: {firstRespone.StatusCode}", ErrorColor);
                        }
                        else
                        {
                            Log($"[Error] 登陆失败. {firstNode["message"]}", ErrorColor);
                            await App.Current.Dispatcher.BeginInvoke(() =>
                            {
                                App._window.notifyIcon.ShowBalloonTip("NUIST Connector", $"登陆失败. {firstNode["message"]}", BalloonIcon.Error);
                                App._window.notifyIcon.HideBalloonTip();
                            });
                        }
                    }
                    else
                        Log($"[Error] 一步请求失败: {firstRespone.StatusCode}", ErrorColor);
                }
                else
                    Log($"[Error] 未获取到IP", ErrorColor);
            }
            catch (Exception ex)
            {
                Log($"[Error] 登陆失败. {ex}", ErrorColor);
            }
            IsLoggedIn = false;
            return false;
        }
        public static async Task<bool> IsConnected()
            => (await _ping.SendPingAsync("baidu.com", 1500)).Status == IPStatus.Success;
        public static async void StartLoop()
        {
            if (await IsConnected())
            {
                Log($"[Success] 已连接到网络", SuccessColor);
                IsLoggedIn = true;
            }
            while (!ShouldStop)
            {
                if (Config.Instance.AutoConnect)
                {
                    if (!IsLoggedIn && ManagedNativeWifi.NativeWifi.EnumerateConnectedNetworkSsids().Any(s => s.ToString() == "i-NUIST") && !await IsConnected())
                    {
                        if (!Login().Result)
                        {
                            Log($"30 秒后尝试重新连接");
                            await Task.Delay(1000 * 30);
                        }
                    }
                    else
                    {
                        if (!await IsConnected())
                        {
                            IsLoggedIn = false;
                            Log($"似乎未连接到互联网, 尝试登陆");
                            if (!Login().Result)
                            {
                                Log($"30 秒后尝试重新连接");
                                await Task.Delay(1000 * 30);
                            }
                        }
                        else
                            IsLoggedIn = true;
                    }
                }
                await Task.Delay(1000);
            }
        }
    }
}
