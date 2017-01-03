using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    /// LightIndicator.xaml 的交互逻辑
    /// </summary>
    public partial class LightIndicator : MetroWindow
    {
        private static LightIndicator instance;

        public static LightIndicator GetInstance(int period)
        {
            if (instance == null)
            {
                instance = new LightIndicator(period);
            }
            else if(instance.period!=period)
            {
                instance.Destory();
                instance = new LightIndicator(period);
            }
            return instance;
        }
        public static LightIndicator GetInstance()   //默认1秒为周期
        {
            if (instance == null)
            {
                instance = new LightIndicator(1000);
            }
            return instance;
        }
        public double period { set; get; }
        private Image[] light = new Image[5];
        private Timer timer = new Timer();
        private int flag = 0; //当前操作灯标志
        private Boolean OnOrOff; //true for turn on
        private System.Threading.SynchronizationContext _syncContext = null; //UI线程上下文
        /// <summary>
        /// 
        /// </summary>
        /// <param name="period">灯亮灭周期，毫秒为单位</param>
        public LightIndicator(double period)
        {    
            InitializeComponent();
            light = new Image[5];
            light[0] = light0;
            light[1] = light1;
            light[2] = light2;
            light[3] = light3;
            light[4] = light4;
            this.period = period;
            _syncContext = System.Threading.SynchronizationContext.Current;  
        }

        public void Start()
        {
            OnOrOff = true;
            flag = 0;
            //每次周期灯需要亮灭10次，平均每次操作耗时period/10
            timer.Interval = period / 10;
            timer.Enabled = true;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Timers_Timer_Elapsed);
            timer.Start();
        }

        private void Timers_Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _syncContext.Post(SetImageSource,null);//同步异步线程消息
        }
        private void SetImageSource(object text)
        {
            if (flag == 5)
            {
                OnOrOff = false;
                flag = 4;
            }
            if (flag == -1)
            {
                OnOrOff = true;
                flag = 0;
            }
            if (OnOrOff)
            {
                light[flag].Source = new BitmapImage(new Uri("./img/light_on.png",
                                                               UriKind.Relative));
                flag++;
            }
            else
            {
                light[flag].Source = new BitmapImage(new Uri("./img/light_off.png",
                                                               UriKind.Relative));
                flag--;
            }
        }
        public void Stop()
        {
            timer.Stop();
        }

        public void Destory()
        {
            timer.Stop();
            instance = null;

        }
    }
}
