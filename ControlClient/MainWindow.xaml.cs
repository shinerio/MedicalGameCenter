using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.Text.RegularExpressions;
using GloveLib;
namespace ControlClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// Notice:Main只有页面交互逻辑，勿做后台业务逻辑工作
    /// </summary>
    public partial class MainWindow : Window
    {
        private ControlServerManage csm;
        private static int rowNum = 4;    //the number of game gridlist's row and cloumn
        private static int cloumnNum = 4;
        private string[,] gamePath = new string[rowNum, cloumnNum];//corresponding game's path

        //fuyang all the class below are singleton pattern
        //glove module for some init function
        private GloveModule gloveModule;
        //glove controller class for all access to glove api
        private GloveController gc;
        public MainWindow()
        {
            InitializeComponent();
            InitModule();
            InitGame(this);
            csm = ControlServerManage.GetInstance(cbb_port, lbl_gloveStatus);
        }

        private void InitModule()
        {
            ConsoleManager.Show();
            gloveModule = GloveModule.GetSingleton(this);
            // gc = gloveModule.gc;
        }

        private void topTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // 获取鼠标相对标题栏位置  
            Point position = e.GetPosition(topTitle);

            // 如果鼠标位置在标题栏内，允许拖动  
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (position.X >= 0 && position.X < topTitle.ActualWidth && position.Y >= 0 && position.Y < topTitle.ActualHeight)
                {
                    this.DragMove();
                }
            }
        }

        private void ShutdownAll(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(-1);
        }

        private void ScaleWindow(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern int ExtractIconEx(string lpszFile, int niconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);
        private void AddGame(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFile = new System.Windows.Forms.OpenFileDialog();
            openFile.Title = "选择游戏";
            openFile.Filter = "可执行文件|*.exe";
            openFile.FilterIndex = 1;
            openFile.RestoreDirectory = true;
            openFile.DefaultExt = "exe";
            System.Windows.Forms.DialogResult result = openFile.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
            String fileName = openFile.FileName;
            doUpdate(this, fileName);
        }
        private void startGame(object sender, RoutedEventArgs e)
        {
            Image img = (Image)sender;
            int row = Grid.GetRow(img);
            int column = Grid.GetColumn(img);
            Process.Start(@gamePath[row, row]);

        }
        public static void InitGame(MainWindow main)
        {
            string file = System.Windows.Forms.Application.ExecutablePath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(file);
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (!key.Contains("IP"))
                {
                    String path = config.AppSettings.Settings[key].Value.ToString();
                    doUpdate(main, path);
                }
            }

        }
        //更新游戏
        public static void doUpdate(MainWindow main, String fileName)
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
            img.Source = returnSource;
            int nowColumn = Grid.GetColumn(main.addGame);
            int nowRow = Grid.GetRow(main.addGame);
            if (nowRow == cloumnNum - 1 && nowColumn == rowNum - 1)
            {
                MessageBox.Show("游戏数目已达上限", "出错了");
            }
            else if (nowColumn == cloumnNum - 1)  //换行
            {
                Grid g = new Grid();
                RowDefinition rw1 = new RowDefinition();
                RowDefinition rw2 = new RowDefinition();
                g.RowDefinitions.Add(rw1);
                g.RowDefinitions.Add(rw2);
                g.Children.Add(img);
                Grid.SetRow(img, 0);
                int subStart = fileName.LastIndexOf("\\");
                int subEnd = fileName.LastIndexOf(".exe");
                String gameName = fileName.Substring(subStart + 1, subEnd - subStart - 1);
                Label l = new Label();
                l.Content = gameName;
                l.HorizontalAlignment = HorizontalAlignment.Center;
                g.Children.Add(l);
                Grid.SetRow(l, 1);
                g.VerticalAlignment = VerticalAlignment.Center;
                g.HorizontalAlignment = HorizontalAlignment.Center;
                main.gameContainer.Children.Add(g);
                Grid.SetColumn(g, nowColumn);
                Grid.SetRow(g, nowRow);
                main.gamePath[nowRow, nowColumn] = fileName;
                img.MouseLeftButtonUp += main.startGame;  //add mouseClick event                       
                Grid.SetColumn(main.addGame, 0);
                Grid.SetRow(main.addGame, nowRow + 1);
                //  write the configuration file
                Utils.UpdateAppConfig(nowRow.ToString() + nowColumn, fileName);
            }
            else
            {
                Grid g = new Grid();
                RowDefinition rw1 = new RowDefinition();
                RowDefinition rw2 = new RowDefinition();
                g.RowDefinitions.Add(rw1);
                g.RowDefinitions.Add(rw2);
                g.Children.Add(img);
                Grid.SetRow(img, 0);
                int subStart = fileName.LastIndexOf("\\");
                int subEnd = fileName.LastIndexOf(".exe");
                String gameName = fileName.Substring(subStart + 1, subEnd - subStart - 1);
                Label l = new Label();
                l.Content = gameName;
                g.VerticalAlignment = VerticalAlignment.Center;
                l.HorizontalAlignment = HorizontalAlignment.Center;
                g.Children.Add(l);
                Grid.SetRow(l, 1);
                g.HorizontalAlignment = HorizontalAlignment.Center;
                main.gameContainer.Children.Add(g);
                Grid.SetColumn(g, nowColumn);
                Grid.SetRow(g, nowRow);
                main.gamePath[nowRow, nowColumn] = fileName;
                img.MouseLeftButtonUp += main.startGame;
                Grid.SetColumn(main.addGame, nowColumn + 1);
                //  write the configuration file
                Utils.UpdateAppConfig(nowRow.ToString() + nowColumn, fileName);
            }
        }
        //open game gridlist page
        private void GameMenu(object sender, RoutedEventArgs e)
        {
            DeviceHelpContainer.Visibility = Visibility.Hidden;
            gameContainer.Visibility = Visibility.Visible;
        }

        //opne device help page
        private void DeviceHelp(object sender, RoutedEventArgs e)
        {
            gameContainer.Visibility = Visibility.Hidden;
            DeviceHelpContainer.Visibility = Visibility.Visible;
            localIP.Text = Utils.getConfig("localIP");
            targetIP.Text = Utils.getConfig("targetIP");
        }
        //clear game list
        private void ClearGame(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmToDel = MessageBox.Show("确认要清空游戏吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmToDel == MessageBoxResult.Yes)
            {
                string file = System.Windows.Forms.Application.ExecutablePath;
                Configuration config = ConfigurationManager.OpenExeConfiguration(file);
                foreach (string key in config.AppSettings.Settings.AllKeys)
                {
                    if (!key.Contains("IP"))
                        config.AppSettings.Settings.Remove(key);
                }
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                gameContainer.Children.Clear();
                gameContainer.Children.Add(addGame);
                Grid.SetColumn(addGame, 0);
                Grid.SetRow(addGame, 0);
            }
        }
        //show login window
        private void Login(object sender, RoutedEventArgs e)
        {
            Login login = new Login(loginStatus);
            login.Owner = this;
            login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            login.ShowDialog();
        }
        /*
        private void ShowData_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                showData.Content = "hold";
                msg = "hold";
            }
            if (e.Key == Key.Left)
            {
                showData.Content = "hold";
                msg = "hold";
            }
        }
        private void ShowData_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                msg = "right";
                showData.Content = "right";
               
            }
            if (e.Key == Key.Left)
            {
                msg = "left";
                showData.Content = "left";
            }
        }
         */



        //top navigation bar in_out_effect
        private void ServiceToolIcon_OnMouseEnter(object sender, MouseEventArgs e)
        {
            StackPanel stackPanel = (StackPanel)sender;
            stackPanel.Background = Brushes.LightGray;
        }

        private void ServiceToolIcon_OnMouseLeave(object sender, MouseEventArgs e)
        {
            StackPanel stackPanel = (StackPanel)sender;
            stackPanel.Background = new SolidColorBrush(Color.FromRgb(242, 242, 242));
        }


        //confirm modify in device help
        private void settingsDer_Click(object sender, RoutedEventArgs e)
        {
            String slocalIP = localIP.Text.ToString();
            String stargetIP = targetIP.Text.ToString();
            Utils.UpdateAppConfig("localIP", slocalIP);
            Utils.UpdateAppConfig("targetIP", stargetIP);
            csm.endServer();
            ControlTemplate template = serverBtn.FindName("serverBtnTemp") as ControlTemplate;
            if (template != null)
            {
                Image img = template.FindName("imgWork", serverBtn) as Image;
                img.Source = new BitmapImage(new Uri("./img/service_off.png",
                                                   UriKind.Relative));
            }
            MessageBox.Show("修改成功,请重启服务", "success");
        }
        //switch serve
        private void SwitchServe(object sender, RoutedEventArgs e)
        {
            if (csm.getServerStatus())
            {
                ControlTemplate template = serverBtn.FindName("serverBtnTemp") as ControlTemplate;
                if (template != null)
                {
                    Image img = template.FindName("imgWork", serverBtn) as Image;
                    img.Source = new BitmapImage(new Uri("./img/service_off.png",
                                                       UriKind.Relative));
                }
                csm.endServer();
            }
            else
            {
                ControlTemplate template = serverBtn.FindName("serverBtnTemp") as ControlTemplate;
                if (template != null)
                {
                    Image img = template.FindName("imgWork", serverBtn) as Image;
                    img.Source = new BitmapImage(new Uri("./img/service_on.png",
                                                       UriKind.Relative));
                }
                csm.startServer();
            }
        }
        /*
         //glovec connection
        private void btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!gc.IsConnected(0))
                {
                    
                    var PortName = cbb_port.SelectedItem.ToString();
                    gc.Connect(PortName, 0);
                    btn_Connect.Content = "关闭";
                    lbl_gloveStatus.Content = "手套已接入";
                }
                else
                {
                    gc.Close(0);
                    btn_Connect.Content = "打开";
                    lbl_gloveStatus.Content = "手套未接入";
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString());
            }
        }*/

        //glove config
        private void btn_Config_Click(object sender, RoutedEventArgs e)
        {
            (new GloveConfigView()).Show();
        }
    }
}
