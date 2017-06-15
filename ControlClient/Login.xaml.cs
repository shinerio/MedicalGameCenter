using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using AForge.Video.DirectShow;
using System.Threading;
using MySql.Data.MySqlClient;

namespace ControlClient
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : MetroWindow
    {
        // 数据库配置
        private static string _connectionString = "server=localhost;" +
                                         "user id=root; " +
                                         "pwd=admin;" +
                                         "database=medical_manage;" +
                                         "allowuservariables=True;" +
                                         "Allow Zero Datetime=True";

        public static String UserName = "tom";
        private TextBlock loginstatus;
        public Login(TextBlock loginStatus)
        {
            InitializeComponent();
            this.loginstatus = loginStatus;
        }

        private void UserName_MouseEnter(object sender, MouseEventArgs e)
        {
            if ("请输入用户名".Equals(userNameText.Text.ToString()))
                userNameText.Clear();
        }

        private void userName_MouseLeave(object sender, MouseEventArgs e)
        {
            if (userNameText.Text == null || "".Equals(userNameText.Text.ToString()))
            {
                userNameText.AppendText("请输入用户名");
            }
        }

        //窗口拖动函数
        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

        //登录操作
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (loginMethod)
            {
                String username = userNameText.Text.ToString();
                UserName = username;
                String password = passwordText.Password.ToString();
                if (username == "" || password == "")
                {
                    MessageBox.Show("用户名或密码不能为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                String url = "http://localhost:8080/patient/login";
                IDictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("username", username);
                parameters.Add("password", password);
                HttpWebResponse response = null;
                StreamReader readStream = null;
                try
                {
                    response = HttpWebResponseUtility.CreatePostHttpResponse(url, parameters, null, null,
                        Encoding.UTF8, null);
                    Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                    readStream = new StreamReader(response.GetResponseStream(), encode);
                    Char[] read = new Char[256];
                    int count = readStream.Read(read, 0, 256);
                    String str = "";
                    while (count > 0)
                    {
                        str += new String(read, 0, count);
                        // Console.Write(str);
                        count = readStream.Read(read, 0, 256);
                    }
                    if (!str.Equals(""))
                    {
                        str = str.Replace("\"", "'"); //java和c#的json格式转化
                        Patient.SetPatient(JsonConvert.DeserializeObject<Patient>(str));
                        loginstatus.Text = "你好！" + Patient.GetInstance().realname;
                        Console.WriteLine(String.Format("{0}:{1}", Patient.GetInstance().id, Patient.GetInstance().realname));
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("用户名或密码错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show("登陆失败，请检查网络设置", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (response != null)
                    {
                        response.Close();
                    }
                    if (readStream != null)
                    {
                        readStream.Close();
                    }
                }
            }
            else      //face to login
            {

            }
        }

        //取消操作
        private void Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            Image img = (Image)sender;
            img.Source = new BitmapImage(new Uri("./img/switchLogin_enter.png", UriKind.Relative));
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            Image img = (Image)sender;
            img.Source = new BitmapImage(new Uri("./img/switchLogin_Leave.png", UriKind.Relative));
        }

        bool loginMethod = true;
        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (loginMethod)
            {
                faceLogin.Visibility = Visibility.Visible;
                content.Visibility = Visibility.Hidden;
                loginMethod = false;
                Camera_Init();
            }
            else
            {
                faceLogin.Visibility = Visibility.Hidden;
                content.Visibility = Visibility.Visible;
                loginMethod = true;
                sourcePlayer.Stop();
            }
        }

        private void userNameText_GotFocus(object sender, RoutedEventArgs e)
        {
            if ("请输入用户名".Equals(userNameText.Text.ToString()))
            {
                userNameText.Text = "";
            }
        }
        private void userNameText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (userNameText.Text.ToString() == null || userNameText.Text.ToString().Equals(""))
            {
                userNameText.Text = "请输入用户名";
            }
        }

        private void Camera_Init()
        {
            // 设定初始视频设备  
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count > 0)
            {   // 默认设备  
                sourcePlayer.VideoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                sourcePlayer.Start();
                Thread.Sleep(3000);
                for (int i = 0; i < 5&& ContinueAuth; i++) {
                Thread.Sleep(1000);
                StartAuthenticate();
                }
            }
            else
            {
                MessageBox.Show("未检测到摄像头！");
            }
            sourcePlayer.Stop();
            this.Close();
        }
        public  bool ContinueAuth = true;  //成功验证后置false
        public  int tryCount = 0;       //尝试次数
        private void Authenticate()   //异步验证线程
        {
            tryCount++;
            try
            {
                FaceSearchResult res = FaceRecognition.Search();
                string user_face_id = null;
                if (res != null && res.results != null && res.results[0] != null && res.results[0].confidence > 80)
                {
                    user_face_id = res.results[0].user_id;
                }
                if (user_face_id != null)
                {
                    LoginBody body = GetUserByFaceId(user_face_id);
                    if (!loginMethod && body != null)
                    {
                        String username = body.username;
                        UserName = username;
                        String password = body.password;
                        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                        {
                            return;
                        }
                        String url = "http://localhost:8080/patient/login";
                        IDictionary<string, string> parameters = new Dictionary<string, string>();
                        parameters.Add("username", username);
                        parameters.Add("password", password);
                        HttpWebResponse response = null;
                        StreamReader readStream = null;
                        try
                        {
                            response = HttpWebResponseUtility.CreatePostHttpResponse(url, parameters, null, null,
                                Encoding.UTF8, null);
                            Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                            readStream = new StreamReader(response.GetResponseStream(), encode);
                            Char[] read = new Char[256];
                            int count = readStream.Read(read, 0, 256);
                            String str = "";
                            while (count > 0)
                            {
                                str += new String(read, 0, count);
                                Console.Write(str);
                                count = readStream.Read(read, 0, 256);
                            }
                            if (!str.Equals(""))
                            {
                                str = str.Replace("\"", "'"); //java和c#的json格式转化
                                Patient.SetPatient(JsonConvert.DeserializeObject<Patient>(str));
                                loginstatus.Text = "你好！" + Patient.GetInstance().realname;
                                tryCount = 0;
                                sourcePlayer.Stop();
                                ContinueAuth = false;
                                this.Close();
                            }
                        }
                        catch (Exception exception)
                        {
                            // MessageBox.Show("登陆失败，请检查网络设置", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            if (response != null)
                            {
                                response.Close();
                            }
                            if (readStream != null)
                            {
                                readStream.Close();
                            }
                        }
                    }
                }
                if (tryCount > 4)
                {
                    tryCount = 0;
                    MessageBox.Show("人脸登录失败!", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                //                 if (faceloginRes)
                //                 {
                //                     tryCount = 0;               
                //                     MessageBox.Show("认证成功");
                //                     sourcePlayer.Stop();
                //                     ContinueAuth = false;
                //                  }
                //                  else if(tryCount>4)
                //                 {
                //                     tryCount = 0;
                //                     MessageBox.Show("认证失败");
                //                }else
                //                {
                //                    MessageBox.Show("认证失败");
                //                }
            }
            catch (Exception)
            {
                tryCount = 0;
            }
        }
        public void StartAuthenticate()
        {
            // 判断视频设备是否开启  
            if (sourcePlayer.IsRunning)
            {   // 进行拍照  
                BitmapSource b = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                 sourcePlayer.GetCurrentVideoFrame().GetHbitmap(),
                                 IntPtr.Zero,
                                 Int32Rect.Empty,
                                 BitmapSizeOptions.FromEmptyOptions());
                string ProImgPath = "F:/tmp.png";//要保存的图片的地址，包含文件名
                PngBitmapEncoder PBE = new PngBitmapEncoder();
                PBE.Frames.Add(BitmapFrame.Create(b));
                using (Stream stream = File.Create(ProImgPath))
                {
                    PBE.Save(stream);
                }
            }
            Thread thread = new Thread(new ThreadStart(Authenticate));
            thread.IsBackground = true;
            thread.Start();//启动线程  
        }

        // 根据userFaceId获取登录体
        private static LoginBody GetUserByFaceId(string userFaceId)
        {
            LoginBody body = new LoginBody();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "select username,password from patient where user_face_id = @user_face_id;";
                    cmd.Parameters.AddWithValue("@user_face_id", userFaceId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                body.username = reader.GetString(0);
                                body.password = reader.GetString(1);
                                Console.WriteLine(body);
                            }
                        }
                    }

                }
            }
            return body;
        }

        private class LoginBody
        {
            public string username { set; get; }
            public string password { set; get; }
            public override string ToString()
            {
                return String.Format("username:{0} password:{1}", username, password);
            }
        }
    }
}
