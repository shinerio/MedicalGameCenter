using System;
using System.Collections.Generic;
using System.Linq;
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
        private GameBar()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            int SH = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;  
            int SW = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Right;       
            this.Left = 0;   //控件和屏幕分辨率值之间有差异
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
    }
}
