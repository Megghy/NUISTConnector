using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace NUISTConnector
{
    public class Config
    {
        public static readonly JsonSerializerOptions DefaultSerializerOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        };
        internal static Config? _instance;
        public static Config Instance { get { _instance ??= Load(); return _instance; } }
        public static string ConfigPath => Path.Combine(Environment.CurrentDirectory, "Config.json");
        public static Config Load()
        {
            if (File.Exists(ConfigPath))
                return JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigPath))!.Save();
            else
                return new Config().Save();
        }
        public Config Save()
        {
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, DefaultSerializerOptions));
            return this;
        }

        public string UserName { get; set; }
        public string Password { get; set; }
        [JsonIgnore]
        public string PasswordInternal
            => Encoding.Default.GetString(Convert.FromBase64String(Password));
        public NUISTDomain Domain { get; set; } = NUISTDomain.Unknown;
        public bool ShowNotice { get; set; } = true;
        public bool AutoConnect { get; set; } = true;
    }
}
