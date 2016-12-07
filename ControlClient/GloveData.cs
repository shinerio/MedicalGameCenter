using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Timers;
namespace ControlClient
{
    // WebSocket数据处理类
    public class GloveData : WebSocketBehavior
    {
        private static int _interval = 20;  //[TEST]数据发送间隔
        private static int _value = 50;  //发送手套的标量值
        private static Random ran = new Random();
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
            RefreshValue();
            Send(Convert.ToString(_value));
        }

        public static void SetValue(int value)
        {
            _value = value;
        }

        private static void RefreshValue()
        {
            int dv = 0;
            switch (_value)
            {
                case 0:
                    dv = ran.Next(0, 2);
                    break;
                case 1:
                    dv = ran.Next(-1, 2);
                    break;
                case 99:
                    dv = ran.Next(-1, 2);
                    break;
                case 100:
                    dv = ran.Next(-1, 1);
                    break;
                default:
                    dv = ran.Next(-1, 2);
                    break;
            }
            _value = _value + dv;
        }
    }
}
