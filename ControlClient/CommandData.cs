using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using GloveLib;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ControlClient
{
    public class CommandData : WebSocketBehavior
    {
        // 标识评估状态
        enum EvaluateStatus
        {
            Idle,
            Ready,
            Running
        }

        static Regex digitRegex = new Regex(@"\d+");
        private static int EvaluateTime = 0;
        private static Rehabilitation rhb = Rehabilitation.GetSingleton();
        private static EvaluateStatus status = EvaluateStatus.Idle;
        public static bool isRunning = false;
        Random random = new Random();
        static System.Timers.Timer _timer = new System.Timers.Timer
        {
            Enabled = false,
            AutoReset = false
        };
        protected override void OnOpen()
        {
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(StopRunning);
            base.OnOpen();
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            String data = e.Data;
            if (status == EvaluateStatus.Ready && digitRegex.IsMatch(data))
            {
                EvaluateTime = String2Int(data);
            }
            switch (data)
            {
                case "evaluate_request":
                    //TODO: 弹出评估请求框
                    MessageBoxResult result = MessageBox.Show("是否同意开始评估？", "请选择", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        status = EvaluateStatus.Ready;
                        Send("evaluate_request_accepted");
                    }
                    else
                    {
                        status = EvaluateStatus.Idle;
                        Send("evaluate_request_refused");
                    }
                    break;
                case "evaluate_start":
                    if (status == EvaluateStatus.Ready)
                    {

                        Send(String.Format("evaluate_started time:{0}", EvaluateTime));
                        status = EvaluateStatus.Running;
                        isRunning = true;
                        _timer.Interval = EvaluateTime * 1000;
                        _timer.Start();
                        //TODO: 开始评估操作
                        WriteFileThread.Run();    //Test
                    }
                    break;
                default:
                    break;
            }
        }

        private void StopRunning(object source, ElapsedEventArgs e)
        {
            isRunning = false;
            Send("evaluate_stop");
            ResetStatus();
        }

        private void ResetStatus()
        {
            status = EvaluateStatus.Idle;
            _timer.Stop();
        }

        private int String2Int(String s)
        {
            int i;
            if (!Int32.TryParse(s, out i))
            {
                i = 1;
            }
            return i;
        }

        private static String GetCurrentTime()
        {
            return DateTime.Now.ToString("yyyy/MM/dd-HH:mm:ss.ffff");
        }

        private class WriteFileThread
        {
            public static void Run()
            {
                WriteFileThread t = new WriteFileThread();
                Thread parameterThread = new Thread(new ParameterizedThreadStart(t.InsertData));
                parameterThread.Name = "Write DataFile";
                parameterThread.Start(10);
            }

            private void InsertData(object ms)
            {
                int t = 10;
                int.TryParse(ms.ToString(), out t); //这里采用了TryParse方法，避免不能转换时出现异常
                // 结果先写入测试文件
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(@"./score.txt", true))
                {
                    while (CommandData.isRunning)
                    {
                        file.WriteLine(String.Format("{0}\t{1}\t{2}", GetCurrentTime(), Login.UserName, rhb.GetScore()));
                        Thread.Sleep(t); //让线程暂停  
                    }
                }
            }
        }
    }
}
