using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent); //该api用于嵌入到窗口中运行
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        public static int StartGame(Grid g, String path,int gameHwd,float scaleSize)
        {
            IntPtr hwd = (IntPtr)gameHwd;
            FinishGame(gameHwd);
            try
            {
                //External exe inside WPF Window 
                System.Windows.Forms.Panel _pnlSched = new System.Windows.Forms.Panel();

                WindowsFormsHost windowsFormsHost1 = new WindowsFormsHost();

                windowsFormsHost1.Child = _pnlSched;

                g.Children.Add(windowsFormsHost1);

                ProcessStartInfo psi = new ProcessStartInfo(path);

                psi.WindowStyle = ProcessWindowStyle.Maximized;
                
                Process PR = Process.Start(psi);

                // true if the associated process has reached an idle state:
                PR.WaitForInputIdle();

                //System.Threading.Thread.Sleep(3000);

                hwd = PR.MainWindowHandle;

                // loading exe to the wpf window:
                SetParent(PR.MainWindowHandle, _pnlSched.Handle);
                MoveWindow(hwd, 0, 0, (int)(g.ActualWidth * scaleSize), (int)(g.ActualHeight*scaleSize),true);             
            }
            catch (Exception ex)
            {
                //Nothing...
            }
            return (int)hwd;
        }
        [DllImport("User32.dll", EntryPoint = "PostMessage")]
        public static extern int PostMessage(
            IntPtr hWnd,        // 信息发往的窗口的句柄
            int Msg,            // 消息ID
            int wParam,         // 参数1
            int lParam            // 参数2
        );
        public static void FinishGame(int gameHwd)
        {
            IntPtr hwd = (IntPtr)gameHwd;
            if(hwd!=IntPtr.Zero)
            PostMessage(hwd, 0x0010, 0, 0);
        }
    }
}
