using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace NUISTConnector
{
    public static class Utils
    {
        public static readonly JsonSerializerOptions jsonOption = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        public static T? Deserialize<T>(this string text)
        {
            try
            {
                if (text == "{}")
                    return default;
                return JsonSerializer.Deserialize<T>(text, jsonOption);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ((MainWindow)App.Current.MainWindow).AppendLog(ex.ToString()));
                return default;
            }
        }
        public static string Serialize(this object o, JsonSerializerOptions option = null) => JsonSerializer.Serialize(o, option ?? jsonOption);
    }
}
