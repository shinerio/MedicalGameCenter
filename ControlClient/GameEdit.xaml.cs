using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Configuration;
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
using System.Windows.Shapes;

namespace ControlClient
{
    /// <summary>
    /// GameEdit.xaml 的交互逻辑
    /// </summary>
    public partial class GameEdit : MetroWindow
    {
        private static int rowNum = 4;    //the number of game gridlist's row and cloumn
        private static int columnNum = 4;
        private string[,] gamePath = new string[rowNum, columnNum];//corresponding game's path
        public GameEdit()
        {
            InitializeComponent();
            InitGame();
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
                img.MouseRightButtonUp += new MouseButtonEventHandler(this.DeleteGame); //add rightMouseClick event    
                Grid.SetColumn(addGame, nowColumn + 1);
                //  write the configuration file
                Utils.UpdateAppConfig("gamepath" + nowRow + nowColumn, fileName);
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
    }
}
