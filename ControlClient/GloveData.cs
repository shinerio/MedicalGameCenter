using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Timers;
using GloveLib;
namespace ControlClient
{
    // WebSocket数据处理类
    public class GloveData : WebSocketBehavior
    {
        private static int _interval = 10;  //[TEST]数据发送间隔
        private static Rehabilitation rhb = Rehabilitation.GetSingleton();
        static System.Timers.Timer _timer = new System.Timers.Timer
        {
            Enabled = false,
            AutoReset = true,
            Interval = _interval
        };

        protected override void OnOpen()
        {
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

        private void SendMsg(object source, ElapsedEventArgs e)
        {
            Send(Convert.ToString(rhb.GetScore()));
        }
    }
}
