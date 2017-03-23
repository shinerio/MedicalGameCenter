using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Threading.Tasks;
using System.Windows.Forms;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Media.Animation;
using GloveLib;
using GloveLib;
using Binding = System.Windows.Data.Binding;

//旧的驱动
//using SenseSDK;   //新的驱动

namespace ControlClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// Notice:Main只有页面交互逻辑，勿做后台业务逻辑工作
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private SensorCalibrator sc;
        private SkeletonCalculator skc;
        private ControlServerManage csm;

        private Rehabilitation rhb;
        private DataWarehouse dh;
        private bool isMagneticAlignSuccess = false;

        private WindowState ws; //窗口状态
        //private System.Windows.Forms.NotifyIcon notifyIcon; //任务栏图标
        private int gameHwd;
        private static SynchronizationContext _syncContext = null;
        public static GloveStatus gloveStatus;
        public class GloveStatus : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public bool isGloveOK
            {
                set
                {
                    if (value)
                    {
                        status = "./img/ok.png";
                        label = "手套已接入";
                    }
                    else
                    {
                        status = "./img/error.png";
                        label = "手套未接入";
                    }
                }
            }

            private string img_path = "./img/error.png";
            private string lbl_text = "手套未接入";

            public string status
            {
                get { return img_path; }
                set
                {
                    img_path = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("status"));
                    }
                }
            }
            public string label
            {
                get { return lbl_text; }
                set
                {
                    lbl_text = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("label"));
                    }
                }
            }
        }

        public MainWindow()
        {

            InitializeComponent();
            ((Storyboard)FindResource("WaitStoryboard")).Begin();
            csm = ControlServerManage.GetInstance();
            this.WindowState = WindowState.Maximized;
            sc = SensorCalibrator.GetSingleton();
            skc = SkeletonCalculator.GetSingleton("");
            rhb = Rehabilitation.GetSingleton();
            dh = DataWarehouse.GetSingleton();
            //settingWindow = new Setting();
            _syncContext = SynchronizationContext.Current;
            InitGame();
            gloveStatus = new GloveStatus();
            img_gloveStatus.SetBinding(Image.SourceProperty, new Binding("status") { Source = gloveStatus });
            lbl_gloveStatus.SetBinding(Label.ContentProperty, new Binding("label") { Source = gloveStatus });
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
            ChromiumWebBrowser.Dispose();
            CefSharp.Cef.Shutdown();
            Utils.FinishGame(gameHwd);
            base.OnClosed(e);
            Application.Current.Shutdown(-1);
            System.Environment.Exit(-1);
        }

        private void ShutdownAll(object sender, RoutedEventArgs e)
        {
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

        /*
         *主窗口事件 
         */

        //        private void Goto_Click(object sender, RoutedEventArgs e)
        //        {
        //            ChromiumWebBrowser.Address = URLAddress.Text.ToString();
        //        }

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
                ConfigurationManager.RefreshSection("appSettings"); ;
            }
        }
        //show login window
        private void Login(object sender, RoutedEventArgs e)
        {
            Login login = new Login(LoginTb);
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
        //switch serve
        private void SwitchServe(object sender, RoutedEventArgs e)
        {
            //            Thread taskThread = new Thread(SwitchServiceTask);
            //            taskThread.Start();
            Console.WriteLine("SwitchServe");
            if (csm == null)
            {
                csm = ControlServerManage.GetInstance();
                MessageBox.Show("手套未连接", "出错了", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
                    Console.WriteLine(ee.ToString());
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
                    // 弹出调试信息
                    Console.WriteLine(ee.ToString());
                }

            }
        }

        private void SwitchServiceTask()
        {
            Console.WriteLine("SwitchServe");
            if (csm == null)
            {
                csm = ControlServerManage.GetInstance();
                MessageBox.Show("手套未连接", "出错了", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (csm.getServerStatus())
            {
                try
                {
                    csm.endServer();
                    this.Dispatcher.BeginInvoke((Action)delegate()
                    {
                        ControlTemplate template = serverBtn.FindName("serverBtnTemp") as ControlTemplate;
                        if (template != null)
                        {
                            Image img = template.FindName("imgWork", serverBtn) as Image;
                            img.Source = new BitmapImage(new Uri("./img/service_off.png",
                                                               UriKind.Relative));
                        }
                    });

                }
                catch (Exception ee)
                {
                    Console.WriteLine(ee.ToString());
                    ControlServerManage.DestoryInstance();  //手套未连接，控制器无效，置null
                }
            }
            else
            {
                try
                {
                    csm.startServer();
                    this.Dispatcher.BeginInvoke((Action)delegate()
                    {
                        ControlTemplate template = serverBtn.FindName("serverBtnTemp") as ControlTemplate;
                        if (template != null)
                        {
                            Image img = template.FindName("imgWork", serverBtn) as Image;
                            img.Source = new BitmapImage(new Uri("./img/service_on.png",
                                                               UriKind.Relative));
                        }
                    });

                }
                catch (Exception ee)
                {
                    // 弹出调试信息
                    Console.WriteLine(ee.ToString());
                }

            }
        }
        //glove config
        private void btn_Config_Click(object sender, RoutedEventArgs e)
        {
            // new GloveConfigView().Show();
            ShowAlignmentDialog(sender, e);
            // ShowCustomDialog(sender, e);
        }

        private void gameBar_Click(object sender, RoutedEventArgs e)
        {
            GameEdit g = new GameEdit();
            g.Owner = this;
            g.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            g.ShowDialog();
        }
        // 点击设置按钮
        private void setting_Click(object sender, RoutedEventArgs e)
        {
            Setting settingWindow = new Setting();
            settingWindow.Owner = this;
            settingWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settingWindow.Show();
        }

        private void alignment_Click(object sender, RoutedEventArgs e)
        {
            ShowAlignmentDialog(sender, e);
        }

        private void reset_Click(object sender, RoutedEventArgs e)
        {
            ResetPosture(sender, e);
        }
        //手模姿势重置
        private async void ResetPosture(object sender, RoutedEventArgs e)
        {
            GameArea.Visibility = Visibility.Hidden;
            await this.ShowMessageAsync("正在重置姿态", "提示：请将手掌置于前方，将五指自然张开，保持该姿势并点击确定按钮",
                    MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "确定" });
            var f_r = dh.GetFrameData(HandType.Right, Definition.MODEL_TYPE);
            var f_l = dh.GetFrameData(HandType.Left, Definition.MODEL_TYPE);
            skc.ResetHandShape(f_r, f_l);
            GameArea.Visibility = Visibility.Visible;
        }
        /*
         * 校准操作
         */

        private async void ShowCustomDialog(object sender, RoutedEventArgs e)
        {
            var dialog = (BaseMetroDialog)this.Resources["AlignmentDialog"];

            await this.ShowMetroDialogAsync(dialog);

            dialog.Title = "手套校准已完成";
            var textBlock = dialog.FindChild<TextBlock>("MessageTextBlock");
            textBlock.Text = "对话框将在3秒后关闭......";

            await Task.Delay(3000);

            await this.ShowMessageAsync("Secondary dialog", "This message is shown on top of another.");

            textBlock.Text = "The dialog will close in 2 seconds.";
            await Task.Delay(2000);

            await this.HideMetroDialogAsync(dialog);
        }

        private async void ShowAlignmentDialog(object sender, RoutedEventArgs e)
        {
            GameArea.Visibility = Visibility.Hidden;
            // 从配置中获取磁场校准信息
            String MagneticAlignSuccessConfig = Utils.getConfig("isMagneticAlignSuccess");
            if (MagneticAlignSuccessConfig != null)
            {
                int i;
                if (!Int32.TryParse(MagneticAlignSuccessConfig, out i))
                {
                    i = 0;
                }
                isMagneticAlignSuccess = i == 1;
            }
            else
            {
                isMagneticAlignSuccess = false;
            }
            if (ControlServerManage.GetInstance() == null)
            {
                var errorDialog = (BaseMetroDialog)this.Resources["AlignmentDialog"];

                await this.ShowMetroDialogAsync(errorDialog);

                errorDialog.Title = "手套未连接";
                var textBlock = errorDialog.FindChild<TextBlock>("MessageTextBlock");
                textBlock.Text = "此窗口将在2秒后关闭......";

                await Task.Delay(2000);
                await this.HideMetroDialogAsync(errorDialog);
                GameArea.Visibility = Visibility.Visible;
                return;
            }
            if (!ControlServerManage.GetInstance().getServerStatus())
            {
                var errorDialog = (BaseMetroDialog)this.Resources["AlignmentDialog"];

                await this.ShowMetroDialogAsync(errorDialog);

                errorDialog.Title = "请先启动服务";
                var textBlock = errorDialog.FindChild<TextBlock>("MessageTextBlock");
                textBlock.Text = "此窗口将在2秒后关闭......";

                await Task.Delay(2000);
                await this.HideMetroDialogAsync(errorDialog);
                GameArea.Visibility = Visibility.Visible;
                return;
            }
            var magneticAlignment = new MetroDialogSettings()
            {
                AffirmativeButtonText = "好的",
                NegativeButtonText = "跳过",
                FirstAuxiliaryButtonText = "关闭",
                MaximumBodyHeight = 100,
                ColorScheme = MetroDialogOptions.ColorScheme
            };

            MessageDialogResult doingMagneticAlignment = await this.ShowMessageAsync("是否进行磁场校准？", "提示：手套移动位置后需要重新进行磁场校准，一般只需要进行一次",
                MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, magneticAlignment);
            // 进行磁场校准
            if (doingMagneticAlignment == MessageDialogResult.Affirmative)
            {
                isMagneticAlignSuccess = false;
                HostCommander.EraseFlash();
                sc.StartCalibrate();
                MessageDialogResult result = await this.ShowMessageAsync("开始进行磁场校准", "请在各个方向上旋转手套10秒钟以上，完成后请点击下一步",
                    MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "下一步" });
                if (result == MessageDialogResult.Affirmative)
                {
                    try
                    {
                        Console.WriteLine("准备结束磁场校准！");
                        await CanTimeoutTask<CaliCallback>(sc.EndCalibrate, 5000, ShowData, FinishCallback);
                        isMagneticAlignSuccess = true;
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        isMagneticAlignSuccess = false;
                    }

                    // sc.EndCalibrate(ShowData, FinishCallback);
                    // await Task.Delay(5000);
                    // 写入磁场校准信息到配置文件
                    Utils.UpdateAppConfig("isMagneticAlignSuccess", (isMagneticAlignSuccess ? 1 : 0).ToString());
                    Console.WriteLine(isMagneticAlignSuccess);
                    if (isMagneticAlignSuccess)
                    {
                        await this.ShowMessageAsync("磁场校准成功", "请点击按钮进行姿态校准",
                            MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "好的" });
                    }
                    else
                    {
                        await this.ShowMessageAsync("磁场校准失败", "请检查手套连接是否正常",
                            MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "退出" });
                        GameArea.Visibility = Visibility.Visible;
                        return;
                    }
                }
            }
            if (doingMagneticAlignment != MessageDialogResult.FirstAuxiliary && isMagneticAlignSuccess)
            {
                var f_r = dh.GetFrameData(HandType.Right, Definition.MODEL_TYPE);
                var f_l = dh.GetFrameData(HandType.Left, Definition.MODEL_TYPE);
                skc.ResetHandShape(f_r, f_l);

                await this.ShowMessageAsync("正在进行姿态校准", "提示：请将手掌尽可能张开，保持该姿势并点击下一步",
                    MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "下一步" });
                rhb.ResetWorst();
                await this.ShowMessageAsync("正在进行姿态校准", "提示：请将手掌尽可能捏合，保持该姿势并点击下一步",
                    MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "下一步" });
                rhb.ResetBest();
                var dialog = (BaseMetroDialog)this.Resources["AlignmentDialog"];

                await this.ShowMetroDialogAsync(dialog);

                dialog.Title = "手套校准已完成";
                var textBlock = dialog.FindChild<TextBlock>("MessageTextBlock");
                textBlock.Text = "对话框将在2秒后关闭......";

                await Task.Delay(2000);
                await this.HideMetroDialogAsync(dialog);
            }
            if (!isMagneticAlignSuccess && doingMagneticAlignment == MessageDialogResult.Negative)
            {
                await this.ShowMessageAsync("还未进行磁场校准", "提示：请返回上一步重新进行磁场校准",
                    MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "上一步" });
                ShowAlignmentDialog(sender, e);
            }
            GameArea.Visibility = Visibility.Visible;
        }
        private async Task CanTimeoutTask<T>(Action<T, T> action, int timeout, params T[] args)
        {
            var task1 = Task.Run(() =>
            {
                if (args.Length == 2)
                {
                    action(args[0], args[1]);
                }
            });

            var task2 = Task.Delay(timeout);

            var firstTask = await Task.WhenAny(task1, task2);

            if (firstTask == task2)
            {
                throw new TimeoutException();
            }
        }

        private void ShowData()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("已收到数据个数：" + sc.Count + "  计算完成，开始发送。请稍等10秒钟");
            });

        }

        private void FinishCallback()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // isMagneticAlignSuccess = true;
                Console.WriteLine("已完成磁力计校准，请继续姿态校准");
            });

        }

        /*
         * 评估指示窗口触发
         */
        public static void StartEvaWindow(int time)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(ThreadStart));
            thread.IsBackground = true;
            thread.Start(time);//启动线程  
        }
        public static void EndEvaWindow()
        {
            Thread thread = new Thread(new ThreadStart(ThreadEnd));
            thread.IsBackground = true;
            thread.Start();//启动线程  

        }
        private static void ThreadStart(Object time)
        {
            _syncContext.Post(StartEvaluationWindow, time);//子线程中通过UI线程上下文更新UI  
        }
        private static void ThreadEnd()
        {
            _syncContext.Post(EndEvaluationWindow, null);//子线程中通过UI线程上下文更新UI  
        }
        private static void StartEvaluationWindow(Object text)
        {
            int i;
            if (!Int32.TryParse(text.ToString(), out i))
            {
                i = 6000;
            }
            EvaluationWindow ew = EvaluationWindow.GetInstance(i);
            ew.Start();
            ew.Show();
        }
        private static void EndEvaluationWindow(Object text)
        {
            EvaluationWindow ew = EvaluationWindow.GetInstance();
            ew.Close();
        }

        /*
         * 右侧游戏菜单栏控制
         */
        public static int gameNum = 0;
        public string[] gamepath;
        private float scaleSize = Utils.GetScaleSize();
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
            GameContainer.Children.Add(sp);
            gamepath[gameNum] = fileName;
            img.MouseLeftButtonUp += new MouseButtonEventHandler(this.startGame);  //add LeftMouseClick event  
            gameNum++;
        }
        private void startGame(object sender, RoutedEventArgs e)
        {
            Image img = (Image)e.OriginalSource;
            String str = img.Name;
            int row = -1;
            int.TryParse(str.Substring(4, str.Length - 4), out row);
            try
            {
                gameHwd = Utils.StartGame(GameArea, @gamepath[row], gameHwd, scaleSize);
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
            GameContainer.Children.Clear();
            gameNum = 0;
            gamepath = null;
            InitGame();
        }

        private void CommandBinding_RefreshWebpage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_RefreshWebpage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //            String address = ChromiumWebBrowser.Address;
            //            ChromiumWebBrowser.Address = address;
            ChromiumWebBrowser.Reload();
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            ChromiumWebBrowser.Reload();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // MessageBox.Show("Main Loaded");
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Task.Factory.StartNew(SchedulerWork);
        }
        private void SchedulerWork()
        {
            //fistr,second,three是三个TextBlock控件的名字
            Task task = new Task(() => ShowMainComponents(this.MainGrid, this.LoadingGrid));
            task.Start();
            Task.WaitAll(task);
        }

        private void ShowMainComponents(Grid mainGrid, Grid loadingGrid)
        {
            this.Dispatcher.BeginInvoke(new Action(async () =>
            {
                await Task.Delay(2000);
                loadingGrid.Visibility = Visibility.Collapsed;
                mainGrid.Visibility = Visibility.Visible;
            }));
        }
    }
}
