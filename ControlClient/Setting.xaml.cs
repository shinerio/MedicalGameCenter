using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
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
using MahApps.Metro.Controls;

namespace ControlClient
{

    public partial class Setting : MetroWindow
    {
        private ControlServerManage csm;
        private MainWindow mainWindow;
        private GloveController gc;

        public Setting()
        {
            InitializeComponent();
            localIP.Text = Utils.getConfig("localIP");
            targetIP.Text = Utils.getConfig("targetIP");
            String port = Utils.getConfig("port");
            gc = GloveController.GetSingleton(ModelType.HandOnly);
            String[] ports = gc.GetPorts();
            if (ports.Length > 0)
            {
                ports.ToList().ForEach(n => cbb_port.Items.Add(n));
                if (ports.ToList().Contains(Utils.getConfig("port")))
                {
                    int i = ports.ToList().FindIndex(0, ports.ToList().Count, x=> x == port);
                    cbb_port.SelectedItem = cbb_port.Items[i];
                }
                else
                {
                    cbb_port.SelectedItem = cbb_port.Items[cbb_port.Items.Count - 1];
                }
                
            }

        }

        private void settingsDer_Click(object sender, RoutedEventArgs e)
        {
            mainWindow = (MainWindow)this.Owner;
            csm = ControlServerManage.GetInstance(mainWindow.lbl_gloveStatus);
            String slocalIP = localIP.Text.ToString();
            String stargetIP = targetIP.Text.ToString();
            String port = cbb_port.SelectedItem.ToString();
            Utils.UpdateAppConfig("localIP", slocalIP);
            Utils.UpdateAppConfig("targetIP", stargetIP);
            Utils.UpdateAppConfig("port", port);
            csm.endServer();
            ControlTemplate template = mainWindow.serverBtn.FindName("serverBtnTemp") as ControlTemplate;
            if (template != null)
            {
                Image img = template.FindName("imgWork", mainWindow.serverBtn) as Image;
                img.Source = new BitmapImage(new Uri("./img/service_off.png",
                                                   UriKind.Relative));
            }
            MessageBox.Show("修改成功,请重启服务", "success");
            //Console.WriteLine(Utils.getConfig("port"));
            this.Close();
        }

        //        protected override void OnClosed(EventArgs e)
        //        {
        //            this.Hide();
        //            //base.OnClosed(e);
        //        }
    }
}
