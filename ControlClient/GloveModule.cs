using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GloveLib;
using System.Timers;

namespace ControlClient
{
    public class GloveModule
    {
        private static GloveModule Instance;
        public static GloveModule GetSingleton(MainWindow mw)
        {
            if (Instance == null)
            {
                Instance = new GloveModule(mw);
            }
            return Instance;
        }

        public SocketManager sm;
        public  GloveController gc;
        private DataWarehouse dh;
        private SkeletonCalculator sc;
        private MainWindow mw;
        private Rehabilitation rhb;
        private Timer pullDataTimer;

        private GloveModule(MainWindow mw)
        {
            this.mw = mw;
            gc = GloveController.GetSingleton(ModelType.HandOnly);
            var mLogger = Logger.GetInstance(mw.txt_log);
            gc.RegisterLogger(mLogger);
            rhb = Rehabilitation.GetSingleton();

            dh = DataWarehouse.GetSingleton();
            string[] ports = gc.GetPorts();
            if (ports.Length > 0)
            {
                ports.ToList().ForEach(n => mw.cbb_port.Items.Add(n));
                mw.cbb_port.SelectedItem = mw.cbb_port.Items[mw.cbb_port.Items.Count - 1];
            }
            // socket module
            sm = SocketManager.GetInstance();
            sm.Start(10200);

            sc = SkeletonCalculator.GetSingleton("");

            pullDataTimer = new Timer(10);
            pullDataTimer.Elapsed += pullDataTimer_Tick;
            pullDataTimer.Start();


        }

        void pullDataTimer_Tick(object sender, EventArgs e)
        {
            //Console.WriteLine(DateTime.Now.Millisecond);
            SendQuatToClient();
        }

        private void SendQuatToClient()
        {
            try
            {
                //send right
                var f_r = dh.GetFrameData(HandType.Right, Definition.MODEL_TYPE);
                var s = sc.UpdateRaw(f_r);
                if (s != null)
                {
                    sm.Send(s.ToJson());
                }
                var score = rhb.GetScore();
                if (score!=-1)
                {
                    Console.WriteLine("得分:{0}",score);
                }
                ////send left
                //var f_l = dh.GetFrameData(HandType.Left, Definition.MODEL_TYPE);
                //s = sc.UpdateRaw(f_l);
                //if (s != null)
                //{
                //    sm.Send(s.ToJson());
                //}

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }



    }
}
