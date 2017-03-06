using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Timers;
using Newtonsoft.Json;
using GloveLib; //旧的驱动
//using SenseSDK;   //新的驱动

namespace ControlClient
{
    // WebSocket数据处理类
    public class GloveData : WebSocketBehavior
    {
        private static int _interval = 1000;  //[TEST]数据发送间隔
        private static Rehabilitation rhb = Rehabilitation.GetSingleton();
        private HandType handType = GloveModule.handType; //左右手
        private DataWarehouse dh;
        static System.Timers.Timer _timer = new System.Timers.Timer
        {
            Enabled = false,
            AutoReset = true,
            Interval = _interval
        };

        protected override void OnOpen()
        {
            dh = DataWarehouse.GetSingleton();
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(SendMsg);
            base.OnOpen();
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            // _timer.Elapsed += new System.Timers.ElapsedEventHandler(SendMsg);
            switch (e.Data)
            {
                case "start":
                    _timer.Start();
                    break;
                case "stop":
                    //Console.WriteLine("stop!!!!");
                    _timer.Stop();
                    //_timer.Dispose();
                    break;
                default:
                    break;
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            _timer.Stop();
            base.OnClose(e);
        }

        private void SendMsg(object source, ElapsedEventArgs e)
        {
            int score = rhb.GetScore();          
            var f_r = dh.GetFrameData(handType, Definition.MODEL_TYPE);
            WebSockData wsd = new WebSockData();
            wsd.nodes = f_r.Nodes;
            //Console.WriteLine(f_r.Nodes[2].Y);
            wsd.score = score;
            if (State == WebSocketState.Open)
            {
                String s = JsonConvert.SerializeObject(wsd);
//                Console.WriteLine(s);
                Send(s);
                // Send(wsd.score.ToString());
            }
        }
    }
}
