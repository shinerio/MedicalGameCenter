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
using GloveLib;
namespace ControlClient
{
    /// <summary>
    /// Interaction logic for GloveConfigView.xaml
    /// </summary>
    public partial class GloveConfigView : Window
    {
        private SensorCalibrator sc;
        private SkeletonCalculator skc;
        private Rehabilitation rhb;
        private DataWarehouse dh;
        public GloveConfigView()
        {
            InitializeComponent();
            sc = SensorCalibrator.GetSingleton();
            rhb = Rehabilitation.GetSingleton();
            skc = SkeletonCalculator.GetSingleton("");
            dh = DataWarehouse.GetSingleton();
        }

        private void btn_EraseCali_Click(object sender, RoutedEventArgs e)
        {
            HostCommander.EraseFlash();
        }

        private void btn_StartCali_Click(object sender, RoutedEventArgs e)
        {
            lbl_info.Content = "请开始旋转设备";
            sc.StartCalibrate();
        }

        private void btn_EndCali_Click(object sender, RoutedEventArgs e)
        {
            lbl_info.Content = "开始计算";
            sc.EndCalibrate(ShowData, FinishCallback);
        }

        private void btn_worst_Click(object sender, RoutedEventArgs e)
        {
            rhb.ResetWorst();

        }

        private void btn_best_Click(object sender, RoutedEventArgs e)
        {
            rhb.ResetBest();

        }

        public void GetScore()
        {
            var s = rhb.GetScore();
        }

        public void ShowData()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lbl_info.Content = "已收到数据个数：" + sc.Count + "  计算完成，开始发送。请稍等10秒钟";
            });
           
        }

        private void FinishCallback()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lbl_info.Content = "已完成磁力计校准，请继续姿态校准";
            });

        }

        private void btn_reset_Click(object sender, RoutedEventArgs e)
        {
            var f_r = dh.GetFrameData(HandType.Right, Definition.MODEL_TYPE);
            var f_l = dh.GetFrameData(HandType.Left, Definition.MODEL_TYPE);
            skc.ResetHandShape(f_r,f_l);
        }
    }
}
