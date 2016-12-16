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
using MahApps.Metro.Controls;
using System.IO;
namespace ControlClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// Notice:Main只有页面交互逻辑，勿做后台业务逻辑工作
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        private ControlServerManage csm;
        private WindowState ws; //窗口状态
        private System.Windows.Forms.NotifyIcon notifyIcon; //任务栏图标
        private static int rowNum = 4;    //the number of game gridlist's row and cloumn
        private static int columnNum = 4;
        private string[,] gamePath = new string[rowNum, columnNum];//corresponding game's path

        public MainWindow()
        {
            InitializeComponent();
            InitGame();
            csm = ControlServerManage.GetInstance(cbb_port, lbl_gloveStatus, this);
            icon();
            contextMenu();
        }

        //        private void topTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //        {
        //            base.OnMouseLeftButtonDown(e);
        //            // 获取鼠标相对标题栏位置  
        //            Point position = e.GetPosition(topTitle);
        //            // 如果鼠标位置在标题栏内，允许拖动  
        //            if (e.LeftButton == MouseButtonState.Pressed)
        //            {
        //                if (position.X >= 0 && position.X < topTitle.ActualWidth && position.Y >= 0 && position.Y < topTitle.ActualHeight)
        //                {
        //                    this.DragMove();
        //                }
        //            }
        //        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown(-1);
            System.Environment.Exit(-1);
        }

        private void ShutdownAll(object sender, RoutedEventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Current.Shutdown(-1);
            System.Environment.Exit(-1);
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
            ws = this.WindowState;
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
            if (fileName.Contains(".exe"))
            {
                doUpdate(fileName);
            }
            else
            {
                MessageBox.Show("不支持的文件格式", "出错了");
            }
        }
        private void startGame(object sender, RoutedEventArgs e)
        {
            Image img = (Image)e.OriginalSource;
            String str = img.Name;
            int row = -1;
            int column = -1;
            int.TryParse(str.Substring(4, 1), out row);
            int.TryParse(str.Substring(5, 1), out column);
            try
            {
                Process.Start(@gamePath[row, column]);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("游戏已被修改或不存在\n请检查游戏路径，并重新添加", "出错了");
                DeleteGameByKey("gamepath" + row + column);
            }
            catch (Exception)
            {
                MessageBox.Show("不可解决的错误\n游戏可能已经损坏", "出错了");
            }
        }
        private void DeleteGame(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmToDel = MessageBox.Show("是否删除游戏？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmToDel == MessageBoxResult.Yes)
            {
                Image img = (Image)e.OriginalSource;
                String str = img.Name;
                int row = -1;
                int column = -1;
                int.TryParse(str.Substring(4, 1), out row);
                int.TryParse(str.Substring(5, 1), out column);
                DeleteGameByKey("gamepath" + row + column);
            }
        }
        public void InitGame()
        {
            string file = System.Windows.Forms.Application.ExecutablePath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(file);
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key.Contains("gamepath"))
                {
                    String path = config.AppSettings.Settings[key].Value.ToString();
                    doUpdate(path);
                }
            }

        }
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
            int nowColumn = Grid.GetColumn(addGame);
            int nowRow = Grid.GetRow(addGame);
            if (nowRow == columnNum - 1 && nowColumn == rowNum - 1)
            {
                MessageBox.Show("游戏数目已达上限", "出错了");
            }
            else if (nowColumn == columnNum - 1)  //换行
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
                img.Name = "game" + nowRow + nowColumn;
                Label l = new Label();
                l.Content = gameName;
                l.HorizontalAlignment = HorizontalAlignment.Center;
                g.Children.Add(l);
                Grid.SetRow(l, 1);
                g.VerticalAlignment = VerticalAlignment.Center;
                g.HorizontalAlignment = HorizontalAlignment.Center;
                gameContainer.Children.Add(g);
                Grid.SetColumn(g, nowColumn);
                Grid.SetRow(g, nowRow);
                gamePath[nowRow, nowColumn] = fileName;
                img.MouseLeftButtonUp += new MouseButtonEventHandler(this.startGame);  //add LeftMouseClick event 
                img.MouseRightButtonUp += new MouseButtonEventHandler(this.DeleteGame); //add rightMouseClick event 
                Grid.SetColumn(addGame, 0);
                Grid.SetRow(addGame, nowRow + 1);
                //  write the configuration file
                Utils.UpdateAppConfig("gamepath" + nowRow + nowColumn, fileName);
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
                img.Name = "game" + nowRow + nowColumn;
                Label l = new Label();
                l.Content = gameName;
                g.VerticalAlignment = VerticalAlignment.Center;
                l.HorizontalAlignment = HorizontalAlignment.Center;
                g.Children.Add(l);
                Grid.SetRow(l, 1);
                g.HorizontalAlignment = HorizontalAlignment.Center;
                gameContainer.Children.Add(g);
                Grid.SetColumn(g, nowColumn);
                Grid.SetRow(g, nowRow);
                gamePath[nowRow, nowColumn] = fileName;
                img.MouseLeftButtonUp += new MouseButtonEventHandler(this.startGame);  //add LeftMouseClick event 
                img.MouseRightButtonUp += new MouseButtonEventHandler(this.DeleteGame); //add rightMouseClick event    
                Grid.SetColumn(addGame, nowColumn + 1);
                //  write the configuration file
                Utils.UpdateAppConfig("gamepath" + nowRow + nowColumn, fileName);
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
        //delete selected game
        private void DeleteGameByKey(String selectkey)
        {
            string file = System.Windows.Forms.Application.ExecutablePath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(file);
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (selectkey != null && selectkey.Equals(key))
                    config.AppSettings.Settings.Remove(key);
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            Utils.FormatConfig(rowNum, columnNum);
            gameContainer.Children.Clear();
            gameContainer.Children.Add(addGame);
            Grid.SetColumn(addGame, 0);
            Grid.SetRow(addGame, 0);
            InitGame();
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
                    if (key.Contains("gamepath"))
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
            String loginName = LoginTb.Text;
            Label loginStatus = new Label();
            loginStatus.Content = loginName;
            Login login = new Login(loginStatus);
            login.Owner = this;
            login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            login.ShowDialog();
        }
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
            if (csm == null)
            {
                csm = ControlServerManage.GetInstance(cbb_port, lbl_gloveStatus, this);
            }
            if (csm == null)
            {
                MessageBox.Show("手套未连接", "出错了");
                return;
            }
            if (csm.getServerStatus())
            {
                try
                {
                    csm.endServer();
                    ControlTemplate template = serverBtn.FindName("serverBtnTemp") as ControlTemplate;
                    if (template != null)
                    {
                        Image img = template.FindName("imgWork", serverBtn) as Image;
                        img.Source = new BitmapImage(new Uri("./img/service_off.png",
                                                           UriKind.Relative));
                    }
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.ToString(), "出错了");
                    ControlServerManage.DestoryInstance();  //手套未连接，控制器无效，置null
                }
            }
            else
            {
                try
                {
                    csm.startServer();
                    ControlTemplate template = serverBtn.FindName("serverBtnTemp") as ControlTemplate;
                    if (template != null)
                    {
                        Image img = template.FindName("imgWork", serverBtn) as Image;
                        img.Source = new BitmapImage(new Uri("./img/service_on.png",
                                                           UriKind.Relative));
                    }
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.ToString(), "出错了");
                }
            }
        }

        //glove config
        private void btn_Config_Click(object sender, RoutedEventArgs e)
        {
            (new GloveConfigView()).Show();
        }

        private void gameBar_Click(object sender, RoutedEventArgs e)
        {
            GameBar g = GameBar.GetInstance();
            g.Show();
        }

        private void icon()
        {
            string path = System.IO.Path.GetFullPath("../../icLauncher.ico");
            if (File.Exists(path))
            {
                this.notifyIcon = new System.Windows.Forms.NotifyIcon();
                this.notifyIcon.BalloonTipText = "medical管理客户端"; //设置程序启动时显示的文本
                this.notifyIcon.Text = "管理客户端";//最小化到托盘时，鼠标点击时显示的文本
                System.Drawing.Icon icon = new System.Drawing.Icon(path);//程序图标 
                this.notifyIcon.Icon = icon;
                this.notifyIcon.Visible = true;
                notifyIcon.MouseDoubleClick += OnNotifyIconDoubleClick;
                this.notifyIcon.ShowBalloonTip(1000);
            }

        }
        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            this.WindowState = ws;
        }
        //任务栏动作捕捉
        private void contextMenu()
        {
            System.Windows.Forms.ContextMenuStrip cms = new System.Windows.Forms.ContextMenuStrip();


            //关联 NotifyIcon 和 ContextMenuStrip
            notifyIcon.ContextMenuStrip = cms;
            System.Windows.Forms.ToolStripMenuItem exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exitMenuItem.Text = "退出";
            exitMenuItem.Click += new EventHandler(exitMenuItem_Click);
            System.Windows.Forms.ToolStripMenuItem hideMenumItem = new System.Windows.Forms.ToolStripMenuItem();
            hideMenumItem.Text = "隐藏";
            hideMenumItem.Click += new EventHandler(hideMenumItem_Click);
            System.Windows.Forms.ToolStripMenuItem showMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            showMenuItem.Text = "显示";
            showMenuItem.Click += new EventHandler(showMenuItem_Click);
            cms.Items.Add(exitMenuItem);
            cms.Items.Add(hideMenumItem);
            cms.Items.Add(showMenuItem);
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Current.Shutdown(-1);
            System.Environment.Exit(-1);
        }

        private void hideMenumItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void showMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.Activate();
        } 
    }
}
