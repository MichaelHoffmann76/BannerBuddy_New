using System;
using System.IO;
using System.Web.Script.Serialization;
using BannerBuddy.AddIn.Models;

namespace BannerBuddy.AddIn.Storage
{
    public class ConfigService
    {
        private readonly string _configPath;

        private static string PrettyJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            var indent = 0;
            var inString = false;
            var escape = false;
            var sb = new System.Text.StringBuilder(json.Length * 2);

            foreach (var ch in json)
            {
                if (inString)
                {
                    sb.Append(ch);
                    if (escape)
                    {
                        escape = false;
                        continue;
                    }
                    if (ch == '\\')
                        escape = true;
                    else if (ch == '"')
                        inString = false;
                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inString = true;
                        sb.Append(ch);
                        break;
                    case '{':
                    case '[':
                        sb.Append(ch);
                        sb.AppendLine();
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case '}':
                    case ']':
                        sb.AppendLine();
                        indent = Math.Max(0, indent - 1);
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(ch);
                        break;
                    case ',':
                        sb.Append(ch);
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case ':':
                        sb.Append(ch);
                        sb.Append(' ');
                        break;
                    default:
                        if (!char.IsWhiteSpace(ch))
                            sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }

        public ConfigService()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "BannerBuddy",
                "config.json"
            );
        }

        public BannerBuddyConfig Load()
        {
            if (!File.Exists(_configPath))
                return null;

            var json = File.ReadAllText(_configPath);
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<BannerBuddyConfig>(json);
        }

        public void Save(BannerBuddyConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(config);
            File.WriteAllText(_configPath, PrettyJson(json));
        }
    }
}
