using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlClient
{
    class Utils
    {
        public static String getConfig(String newkey)
        {
            string file = System.Windows.Forms.Application.ExecutablePath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(file);
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key == newkey)
                {
                    return config.AppSettings.Settings[key].Value.ToString();
                }
            }
            return null;
        }
        public static void UpdateAppConfig(string newKey, string newValue)
        {
            string file = System.Windows.Forms.Application.ExecutablePath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(file);
            bool exist = false;
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key == newKey)
                {
                    exist = true;
                }
            }
            if (exist)
            {
                config.AppSettings.Settings.Remove(newKey);
            }
            config.AppSettings.Settings.Add(newKey, newValue);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        public static void FormatConfig(int maxRow, int maxColumn)
        {
            string file = System.Windows.Forms.Application.ExecutablePath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(file);
            int row = 0;
            int column = 0;
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key.Contains("gamepath"))
                {
                    String value = config.AppSettings.Settings[key].Value.ToString();
                    config.AppSettings.Settings.Remove(key);
                    config.AppSettings.Settings.Add("gamepath" + row + column, value);
                    if (column == maxColumn - 1)
                    {
                        row++;
                        column = 0;
                    }
                    else
                    {
                        column++;
                    }
                }

            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
