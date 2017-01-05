using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;

namespace ControlClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EvaluationWindow : MetroWindow, INotifyPropertyChanged
    {
        private DispatcherTimer timer;
        private int currentRate = 0;
        private bool UpOrDown = true;
        private static float scaleSize = 1f;
        private static EvaluationWindow instance;
        public static EvaluationWindow GetInstance(int period)
        {
            if (instance == null)
            {
                instance = new EvaluationWindow(period);
            }
            else
            {
                instance.Destory();
                instance = new EvaluationWindow(period);
            }
            return instance;
        }
        public static EvaluationWindow GetInstance()
        {
            if (instance == null)
            {
                instance = new EvaluationWindow(1000);//为指定peroid，默认1秒
            }
            return instance;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="period">手张合周期，毫秒为单位</param>
        private EvaluationWindow(int period)
        {
            period = period / 100;
            this.Topmost = true;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            int SH = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            int SW = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Right;
            this.Left = (SW / scaleSize - this.Width) / 2;   //控件和屏幕分辨率值之间有差异
            this.Top = (SH / scaleSize - this.Height) / 2;
            this.DataContext = this;
            int minutes = period / (1000 * 60);
            int seconds = period / 1000;
            int miliSeconds = period % 1000;
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, minutes, seconds, miliSeconds);
            timer.Tick += timer_Tick;
            InitializeComponent();
        }
        public void Destory()
        {
            if (timer != null)
                timer.Stop();
        }

        #region Properties

        private int successRate = 100;
        public int SuccessRate
        {
            get
            {
                return successRate;
            }
            set
            {
                if (value != successRate)
                {
                    successRate = value;
                    OnPropertyChanged("SuccessRate");
                }
            }
        }

        #endregion
        public void Start()
        {
            if (timer != null)
            timer.Start();
        }
        public void Stop()
        {
            if (timer != null)
            timer.Stop();  
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (UpOrDown)
            {
                currentRate++;
                SuccessRate = currentRate;
            }
            else
            {
                currentRate--;
                SuccessRate = currentRate;
            }
            if (SuccessRate == 100)
            {
                UpOrDown = false;
            }
            if (SuccessRate == 0)
            {
                UpOrDown = true;
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

    }
}
