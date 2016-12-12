using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ControlClient
{
    /// <summary>
    /// Game.xaml 的交互逻辑
    /// </summary>
    public partial class GameBar : Window
    {
        private static GameBar gameBar;
        private static float scalingFactor = GetScalingFactor();
        private GameBar()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            int SH = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;  
            int SW = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            double dpiX, dpiY;
            PresentationSource presentationsource = PresentationSource.FromVisual(this);

            if (presentationsource != null) // make sure it's connected
            {
                dpiX = 96.0 * presentationsource.CompositionTarget.TransformToDevice.M11;
                dpiY = 96.0 * presentationsource.CompositionTarget.TransformToDevice.M22;
            }
            this.Left = SW / scalingFactor - this.Width;   //控件和屏幕分辨率值之间有差异
            // this.Left = 1500;
            this.Top = 0;       
        }

        public static GameBar getInstance()
        {
            if (gameBar == null)
            {
                gameBar = new GameBar();
            }
            return gameBar;
        }

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
        }
        private static float GetScalingFactor()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int logicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int physicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);

            float screenScalingFactor = (float)physicalScreenHeight / (float)logicalScreenHeight;

            return screenScalingFactor; // 1.25 = 125%
        }
    }
}
