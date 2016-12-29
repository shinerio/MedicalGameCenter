using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Timers;
using GloveLib;
using Newtonsoft.Json;
namespace ControlClient
{
    // WebSocket数据处理类
    public class ScoreData : WebSocketBehavior
    {
        private static int _interval = 50;  //[TEST]数据发送间隔
        private static Rehabilitation rhb = Rehabilitation.GetSingleton();
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
            
            if (State == WebSocketState.Open)
            {
                Send(score.ToString());
                // Send(wsd.score.ToString());
            }
        }
    }
}
