using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
        private static float scaleSize = 1.25f;
        public static   int gameNum = 0;
        public string[] gamepath;
        private GameBar()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            int SH = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;  
            int SW = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Right;
            this.Left = SW / scaleSize - this.Width;   //控件和屏幕分辨率值之间有差异
            this.Top = (SH / scaleSize - this.Height) / 2;
            InitGame();
        }

        public static GameBar getInstance()
        {
            if (gameBar == null)
            {
                gameBar = new GameBar();
            }
            return gameBar;
        }

        public void InitGame()
        {    
            string file = System.Windows.Forms.Application.ExecutablePath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(file);
            gamepath = new string[config.AppSettings.Settings.AllKeys.Length];   //生成最大游戏数组
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key.Contains("gamepath"))
                {
                    String path = config.AppSettings.Settings[key].Value.ToString();
                    doUpdate(path);
                }
            }

        }
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern int ExtractIconEx(string lpszFile, int niconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);
        //更新游戏
        public void doUpdate(String fileName)
        {
            IntPtr[] largeIcons, smallIcons;  //存放大/小图标的指针数组  
            string appPath = @fileName;
            //第一步：获取程序中的图标数         
            int IconCount = ExtractIconEx(appPath, -1, null, null, 0);
            //第二步：创建存放大/小图标的空间    
            largeIcons = new IntPtr[IconCount];
            smallIcons = new IntPtr[IconCount];
            //第三步：抽取所有的大小图标保存到largeIcons和smallIcons中        
            ExtractIconEx(appPath, 0, largeIcons, smallIcons, IconCount);
            //  for (int i = 0; i < IconCount; i++)
            //    {
            BitmapSource returnSource = null;
            if (IconCount > 0)
            {
                System.Drawing.Icon newIcon = System.Drawing.Icon.FromHandle(largeIcons[0]);
                System.Drawing.Bitmap bmp = newIcon.ToBitmap();
                returnSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            Image img = new Image();
            if (returnSource == null)
            {
                img.Source = new BitmapImage(new Uri("./img/defaultGameICO.png",
                                                   UriKind.Relative));
            }
            else
            {
                img.Source = returnSource;
            }
            StackPanel sp = new StackPanel();
            sp.Children.Add(img);
            int subStart = fileName.LastIndexOf("\\");
            int subEnd = fileName.LastIndexOf(".exe");
            String gameName = fileName.Substring(subStart + 1, subEnd - subStart - 1);
            img.Name = "game" + gameNum;
            System.Windows.Controls.Label l = new System.Windows.Controls.Label();
            l.Content = gameName;
            sp.Children.Add(l);
            gameContainer.Children.Add(sp); 
            gamepath[gameNum] = fileName;
            img.MouseLeftButtonUp += new MouseButtonEventHandler(this.startGame);  //add LeftMouseClick event  
            gameNum++;
        }
        private void startGame(object sender, RoutedEventArgs e)
        {
            Image img = (Image)e.OriginalSource;
            String str = img.Name;
            int row = -1;
            int.TryParse(str.Substring(4,str.Length-4), out row);
            try
            {
                Process.Start(@gamepath[row]);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                System.Windows.MessageBox.Show("游戏已被修改或不存在\n请检查游戏路径，并重新添加", "出错了");
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("不可解决的错误\n游戏可能已经损坏", "出错了");
            }
        }

        private void RefreshGame_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            gameContainer.Children.Clear();
            gameNum = 0;
            gamepath = null;
            InitGame();
        }

        private void CloseBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
            gameBar = null;
        }
    }
}
