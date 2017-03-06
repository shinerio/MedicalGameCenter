using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Timers;
using System.Windows.Controls;
using GloveLib; //旧的驱动
//using SenseSDK;   //新的驱动

namespace ControlClient
{
    public class GloveModule
    {
        private static GloveModule Instance;
        public static GloveModule GetSingleton()
        {
            if (Instance == null)
            {
                Instance = new GloveModule();
            }
            return Instance;
        }
        public static void DestoryInstance()
        {
            Instance = null;
        }
        public SocketManager sm;
        public  GloveController gc;
        private DataWarehouse dh;
        private SkeletonCalculator sc;
        private Rehabilitation rhb;
        private Timer pullDataTimer;
        public static HandType handType = HandType.Right; //左右手

        private GloveModule()
        {
            gc = GloveController.GetSingleton(ModelType.HandOnly);
//            var mLogger = Logger.GetInstance(mw.txt_log);
//            gc.RegisterLogger(mLogger);
            rhb = Rehabilitation.GetSingleton();
            dh = DataWarehouse.GetSingleton();
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
                var f_r = dh.GetFrameData(handType, Definition.MODEL_TYPE);          

                var s = sc.UpdateRaw(f_r);
                if (s != null)
                {
                    sm.Send(s.ToJson());
                }
                // gc.GetCurrentAngle(handType);
                var score = rhb.GetScore();
                if (score!=-1)
                {
                    // Console.WriteLine("得分:{0}",score);
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
