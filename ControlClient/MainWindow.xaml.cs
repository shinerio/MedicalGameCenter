using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ControlClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // WebSocket数据处理类
        public class GloveData : WebSocketBehavior
        {
            private static Random ran = new Random();
            static System.Timers.Timer _timer = new System.Timers.Timer
            {
                Enabled = false,
                AutoReset = true,
                Interval = _interval
            };

            protected override void OnOpen()
            {
                _timer.Elapsed += new System.Timers.ElapsedEventHandler(SendMsg);
                base.OnOpen();
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                // _timer.Elapsed += new System.Timers.ElapsedEventHandler(SendMsg);
                switch (e.Data)
                {
                    case "start":
                        _timer.Start();
                        break;
                    case "stop":
                        //Console.WriteLine("stop!!!!");
                        _timer.Stop();
                        //_timer.Dispose();
                        break;
                    default:
                        break;
                }
            }

            private void SendMsg(object source, ElapsedEventArgs e)
            {
                RefreshValue();
                Send(Convert.ToString(_value));
            }

            private static void RefreshValue()
            {
                int dv = 0;
                switch (_value)
                {
                    case 0:
                        dv = ran.Next(0, 2);
                        break;
                    case 1:
                        dv = ran.Next(-1, 2);
                        break;
                    case 99:
                        dv = ran.Next(-1, 2);
                        break;
                    case 100:
                        dv = ran.Next(-1, 1);
                        break;
                    default:
                        dv = ran.Next(-1, 2);
                        break;
                }
                _value = _value + dv;
            }
        }
        public class CommandData : WebSocketBehavior
        {
            // 标识评估状态
            enum EvaluateStatus
            {
                Idle,
                Ready,
                Running
            }

            static Regex digitRegex = new Regex(@"\d+");
            private static int EvaluateTime = 0;
            private static EvaluateStatus status = EvaluateStatus.Idle;
            public static bool isRunning = false;
            Random random = new Random();
            static System.Timers.Timer _timer = new System.Timers.Timer
            {
                Enabled = false,
                AutoReset = false
            };
            protected override void OnOpen()
            {
                _timer.Elapsed += new System.Timers.ElapsedEventHandler(StopRunning);
                base.OnOpen();
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                String data = e.Data;
                if (status == EvaluateStatus.Ready && digitRegex.IsMatch(data))
                {
                    EvaluateTime = String2Int(data);
                }
                switch (data)
                {
                    case "evaluate_request":
                        //TODO: 弹出评估请求框
                        MessageBoxResult result = MessageBox.Show("是否同意开始评估？", "请选择", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            status = EvaluateStatus.Ready;
                            Send("evaluate_request_accepted");
                        }
                        else
                        {
                            status = EvaluateStatus.Idle;
                            Send("evaluate_request_refused");
                        }
                        break;
                    case "evaluate_start":
                        if (status == EvaluateStatus.Ready)
                        {

                            Send(String.Format("evaluate_started time:{0}", EvaluateTime));
                            status = EvaluateStatus.Running;
                            isRunning = true;
                            _timer.Interval = EvaluateTime * 1000;
                            _timer.Start();
                            //TODO: 开始评估操作
                            WriteDBThread.run();    //Test
                        }
                        break;
                    default:
                        break;
                }
            }

            private void StopRunning(object source, ElapsedEventArgs e)
            {
                isRunning = false;
                Send("evaluate_stop");
                ResetStatus();
            }

            private void ResetStatus()
            {
                status = EvaluateStatus.Idle;
                _timer.Stop();
            }

            private int String2Int(String s)
            {
                int i;
                if (!Int32.TryParse(s, out i))
                {
                    i = 1;
                }
                return i;
            }

            private class WriteDBThread
            {
                public static void run()
                {
                    WriteDBThread t = new WriteDBThread();
                    Thread parameterThread = new Thread(new ParameterizedThreadStart(t.InsertData));
                    parameterThread.Name = "Write Database";
                    parameterThread.Start(50);
                }

                private void InsertData(object ms)
                {
                    int j = 10;
                    int.TryParse(ms.ToString(), out j); //这里采用了TryParse方法，避免不能转换时出现异常
                    // 结果先写入测试文件
                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(@"./test.txt", true))
                    {
                        while (CommandData.isRunning)
                        {
                            file.WriteLine(new Random().Next(0, 101));
                            Thread.Sleep(j); //让线程暂停  
                        }
                    }
                }
            }
        }
        static Socket server;  //数据源服务器
        static WebSocketServer WSServer;    //WebSocket服务端
        private static int _value = 50;  //默认发送手套的标量值
        private static String GloveDataServerName = "/GloveData";   //标量数据在WebSocket上的服务名
        private static String CommandDataServerName = "/CommandData";   //评估命令在WebSocket上的服务名
        private static int _interval = 50;  //[TEST]数据发送间隔

        static string msg = "hold";  //默认发送数据
        static int rowNum = 4;
        static int cloumnNum = 4;
        string[,] gamePath = new string[rowNum, cloumnNum];
        public MainWindow()
        {
            InitializeComponent();
            initGame(this);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(-1);
        }

        private void Scale_click(object sender, RoutedEventArgs e)
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

        private void zuixiaohua(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern int ExtractIconEx(string lpszFile, int niconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);
        private void AddGame(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.ShowDialog();
            String fileName = openFile.FileName;
            if (fileName != null && !"".Equals(fileName))
            {
                if (!fileName.EndsWith(".exe"))
                {
                    MessageBox.Show("文件格式错误", "出错了");
                }
                else
                {
                    doUpdate(this, fileName);
                }
            }
        }
        private void startGame(object sender, RoutedEventArgs e)
        {
            Image img = (Image)sender;
            int row = Grid.GetRow(img);
            int column = Grid.GetColumn(img);
            Process.Start(@gamePath[row, row]);

        }
        //初始化游戏
        public static void initGame(MainWindow main)
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
            System.Drawing.Icon newIcon = System.Drawing.Icon.FromHandle(largeIcons[0]);
            System.Drawing.Bitmap bmp = newIcon.ToBitmap();
            BitmapSource returnSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
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
                img.MouseLeftButtonUp += main.startGame;  //添加鼠标事件                        
                Grid.SetColumn(main.addGame, 0);
                Grid.SetRow(main.addGame, nowRow + 1);
                //写配置文件
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
                //写配置文件
                Utils.UpdateAppConfig(nowRow.ToString() + nowColumn, fileName);
            }
        }
        //游戏菜单
        private void GameMenu(object sender, RoutedEventArgs e)
        {
            DeviceHelpContainer.Visibility = Visibility.Hidden;
            gameContainer.Visibility = Visibility.Visible;
        }

        //设备支持
        private void DeviceHelp(object sender, RoutedEventArgs e)
        {
            gameContainer.Visibility = Visibility.Hidden;
            DeviceHelpContainer.Visibility = Visibility.Visible;
            localIP.Text = Utils.getConfig("localIP");
            targetIP.Text = Utils.getConfig("targetIP");
        }
        //清空游戏
        private void ClearGame(object sender, MouseEventArgs e)
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
        //登录窗口
        private void Login(object sender, RoutedEventArgs e)
        {
            Login login = new Login();
            login.Owner = this;
            login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            login.ShowDialog();
        }

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
        static void startServer()
        {
            server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            String localIP = Utils.getConfig("localIP");
            if (localIP != null)
            {
                try
                {
                    server.Bind(new IPEndPoint(IPAddress.Parse(localIP), 6001));//绑定端口号和IP
                    Thread t = new Thread(sendMsg);//开启发送消息线程
                    t.IsBackground = true;
                    t.Start();
                    WSServer = new WebSocketServer(String.Format("ws://{0}", localIP));//new WebSocket
                    WSServer.AddWebSocketService<GloveData>(GloveDataServerName);
                    WSServer.AddWebSocketService<CommandData>(CommandDataServerName);
                    WSServer.Start();
                }
                catch (Exception)
                {
                    MessageBox.Show("本机IP地址无效", "出错了");
                }
            }
            else
            {
                MessageBox.Show("本机IP地址不能为空", "出错了");
            }
        }

        static bool isServe = false;  //是否在服务中
        //发送数据
        static void sendMsg()
        {
            String targetIP = Utils.getConfig("targetIP");
            if (targetIP != null)
            {
                try
                {
                    EndPoint point = new IPEndPoint(IPAddress.Parse(targetIP), 6000);
                    while (isServe)
                    {
                        server.SendTo(Encoding.UTF8.GetBytes(msg), point);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("目标IP地址无效", "出错了");
                }
            }
            else
            {
                MessageBox.Show("目标IP地址不能为空", "出错了");
            }
        }

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


        //设备帮助修改配置确认
        private void settingsDer_Click(object sender, RoutedEventArgs e)
        {
            String slocalIP = localIP.Text.ToString();
            String stargetIP = targetIP.Text.ToString();
            Utils.UpdateAppConfig("localIP", slocalIP);
            Utils.UpdateAppConfig("targetIP", stargetIP);
            isServe = false;
            if (server != null)
            {
                server.Close();
            }
            server = null;
            ControlTemplate template = serverBtn.FindName("serverBtnTemp") as ControlTemplate;

            if (template != null)
            {
                Image img = template.FindName("imgWork", serverBtn) as Image;
                img.Source = new BitmapImage(new Uri("./img/service_off.png",
                                                   UriKind.Relative));
            }
            MessageBox.Show("修改成功,请重启服务", "success");
        }

        private void StartServe(object sender, RoutedEventArgs e)
        {
            if (isServe)
            {
                ControlTemplate template = serverBtn.FindName("serverBtnTemp") as ControlTemplate;

                if (template != null)
                {
                    Image img = template.FindName("imgWork", serverBtn) as Image;
                    img.Source = new BitmapImage(new Uri("./img/service_off.png",
                                                       UriKind.Relative));
                }
                isServe = false;
                if (server != null)
                {
                    server.Close();
                }
                if (WSServer != null)
                {
                    WSServer.Stop();
                }
                server = null;

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
                startServer();
                isServe = true;
            }
        }

        private void AboutUs_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
