using System;
using System.Collections.Generic;
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
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void UserName_MouseEnter(object sender, MouseEventArgs e)
        {
            if("请输入用户名".Equals(userName.Text.ToString()))
            userName.Clear();
        }

        private void userName_MouseLeave(object sender, MouseEventArgs e)
        {
            if (userName.Text==null||"".Equals(userName.Text.ToString()))
            {
                userName.AppendText("请输入用户名");
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

        private void passWord_MouseEnter(object sender, MouseEventArgs e)
        {
            if ("请输入密码".Equals(passTip.Content.ToString())) { 
                passTip.Content = "";
                passTip.Visibility = Visibility.Hidden;
            }
        }

        private void passWord_MouseLeave(object sender, MouseEventArgs e)
        {
            if (passWord.Password ==null || "".Equals(passWord.Password.ToString()))
            {
                passTip.Content = "请输入密码";
                passTip.Visibility = Visibility.Visible;
            }
        }
        //登录操作
        private void Login_Click(object sender, RoutedEventArgs e)
        {

        }

        //取消操作
        private void Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
