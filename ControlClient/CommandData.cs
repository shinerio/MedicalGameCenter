using System;
using System.Collections.Generic;
using System.IO;
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
using System.Data;
using MySql.Data.MySqlClient;

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

        // 指令
        private const String EvaluateRequest = "evaluate_request";
        private const String EvaluateStart = "evaluate_start";
        private const String EvaluateRequestAccepted = "evaluate_request_accepted";
        private const String EvaluateStop = "evaluate_stop";

        static readonly Regex DigitRegex = new Regex(@"\d+");
        private static int _evaluateTime = 0;
        private static long _timeStamp = GetCurrentTimeLong();
        private static long _startTime = GetCurrentTimeLong();
        private static long _endTime = GetCurrentTimeLong();
        private static int _evaluationId = -1;  // 当前评估id
        // 数据库配置
        private static string _connectionString = "server=10.103.238.28;" +
                                         "user id=root; " +
                                         "pwd=bigdata;" +
                                         "database=medical_manage;" +
                                         "allowuservariables=True;" +
                                         "Allow Zero Datetime=True";
        // 建立以UserID为名的文件夹
        private static String dir = String.Format(@"{0}", Patient.GetInstance().id);
        private static String filePath;
        private static Rehabilitation rhb = Rehabilitation.GetSingleton();
        public static DataWarehouse dh = DataWarehouse.GetSingleton();
        private static EvaluateStatus status = EvaluateStatus.Idle;
        public static bool isRunning = false;
        Random random = new Random();

        private static System.Timers.Timer _timer = new System.Timers.Timer
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
            if (status == EvaluateStatus.Ready && DigitRegex.IsMatch(data))
            {
                _evaluateTime = String2Int(data);
            }
            switch (data)
            {
                case EvaluateRequest:
                    // 不弹出评估请求框
                    status = EvaluateStatus.Ready;
                    Send(EvaluateRequestAccepted);
                    break;
                case EvaluateStart:
                    if (status == EvaluateStatus.Ready)
                    {

                        Send(String.Format("evaluate_started time:{0}", _evaluateTime));
                        Console.WriteLine(String.Format("evaluate_started time:{0}", _evaluateTime));
                        status = EvaluateStatus.Running;
                        isRunning = true;
                        _timer.Interval = _evaluateTime * 1000;
                        _timer.Start();
                        //TODO: 开始评估操作
                        WriteFileThread.Run(); //Test
                    }
                    break;
                default:
                    break;
            }
        }

        private void StopRunning(object source, ElapsedEventArgs e)
        {
            isRunning = false;
            Send(EvaluateStop);
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
        private static long GetCurrentTimeLong()
        {
            return
                (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
        // 向数据库插入评估信息
        private static void InsertEvaluationInfo(int userId, long startTime, long endTime)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "insert into EVALUATION_INFO(uid, start_time, end_time) values (@uid, @start_time, @end_time)";
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@start_time", startTime);
                    cmd.Parameters.AddWithValue("@end_time", endTime);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (MySqlException ex)
                    {
                        // Console.WriteLine(ex.Message);
                    }
                }
            }
        }
        // 根据uid和start_time查找对应的评估id
        private static int GetEvaluationId(int userId, long startTime)
        {
            int evaluationId = -1;
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "select id from EVALUATION_INFO where uid=@uid and start_time=@start_time;";
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@start_time", startTime);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                evaluationId = reader.GetInt32(0);
                                //Console.WriteLine(reader.GetString(0));
                            }
                        }
                    }

                }
            }
            return evaluationId;
        }
        // 插入原始数据
        private static void InsertRawData(MySqlConnection connection, MySqlCommand cmd, int evaluationId,
            long timeStamp, String jsonString)
        {
            cmd.CommandText = "insert into RAWDATA(evaluation_id, time_stamp, json_string) values (@evaluation_id, @time_stamp, @json_string);";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@evaluation_id", evaluationId);
            cmd.Parameters.AddWithValue("@time_stamp", timeStamp);
            cmd.Parameters.AddWithValue("@json_string", jsonString);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Console.WriteLine(ex.Message);
            }
        }
        // 写文件进程
        private class WriteFileThread
        {
            public static ManualResetEvent Event = new ManualResetEvent(false);
            private static SkeletonCalculator sc = SkeletonCalculator.GetSingleton("");
            public static FrameData fd = dh.GetFrameData(HandType.Right, Definition.MODEL_TYPE);

            public static void Run()
            {
                WriteFileThread t = new WriteFileThread();
                Thread parameterThread = new Thread(new ParameterizedThreadStart(t.InsertData));
                parameterThread.IsBackground = true;
                parameterThread.Name = "WriteFile";
                // 建立用户文件夹
                if (!Directory.Exists(dir))
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("无法建立文件夹，评估失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                }
                _startTime = GetCurrentTimeLong();
                parameterThread.Start(10);
                Console.WriteLine("Write File Start!");
            }

            private void InsertData(object ms)
            {
                int t = 10;
                int.TryParse(ms.ToString(), out t); //这里采用了TryParse方法，避免不能转换时出现异常
                filePath = String.Format(@"{0}/nodes{1}.txt", dir, GetCurrentTimeLong());
                // 结果先写入测试文件
                using (System.IO.StreamWriter nodesFile = new System.IO.StreamWriter(filePath, false))
                {
                    while (CommandData.isRunning)
                    {
                        fd = dh.GetFrameData(HandType.Right, Definition.MODEL_TYPE);
                        _timeStamp = GetCurrentTimeLong();
                        // scoreFile.WriteLine(String.Format("{0}\t{1}\t{2}", TimeStamp, Patient.GetInstance().id, rhb.GetScore()));
                        nodesFile.WriteLine(String.Format("{0}\t{1}", _timeStamp, sc.UpdateRaw(fd).ToJson()));
                        // Event.Set();
                        Thread.Sleep(t); //让线程暂停 
                    }
                }
                _endTime = GetCurrentTimeLong();
                Console.WriteLine("Write File Done!");
                WriteFileToDatabase.Run();
            }
        }
        // 将文件写入数据库
        private class WriteFileToDatabase
        {
            public static void Run()
            {
                WriteFileToDatabase t = new WriteFileToDatabase();
                Thread thread = new Thread(new ThreadStart(t.Excute));
                thread.IsBackground = true;
                thread.Name = "WriteFileToDatabaseThread";
                thread.Start();
                Console.WriteLine("Write DB Start!");
            }

            private void Excute()
            {
                InsertEvaluationInfo(Patient.GetInstance().id, _startTime, _endTime);
                _evaluationId = GetEvaluationId(Patient.GetInstance().id, _startTime);
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = conn.CreateCommand())
                    {
                        using (System.IO.StreamReader file = new System.IO.StreamReader(filePath))
                        {
                            string line;
                            long timeStamp = _timeStamp;
                            while ((line = file.ReadLine()) != null)
                            {
                                
                                String[] tokens = line.Split('\t');
                                
                                if (tokens.Length == 2)
                                {
                                    long.TryParse(tokens[0], out timeStamp);
                                    InsertRawData(conn, cmd, _evaluationId, timeStamp, tokens[1]);
                                }
                            }  
                        }
                    }
                }
                Console.WriteLine("Write DB Done!");
            }
        }

        private class WriteAFile
        {
            public static void Run(int type)
            {
                WriteAFile t = new WriteAFile();
                Thread parameterThread = new Thread(new ParameterizedThreadStart(t.Excute));
                parameterThread.IsBackground = true;
                parameterThread.Name = String.Format("WriteFileThread{0}", type);
                parameterThread.Start(type);
            }

            private void Excute(object o)
            {
                int t = 1;
                int.TryParse(o.ToString(), out t);
                using (StreamWriter dataFile = new StreamWriter(String.Format(@"{0}/node{1}.txt", dir, t)))
                {
                    while (CommandData.isRunning)
                    {
                        WriteFileThread.Event.WaitOne(); // 阻塞子线程，并等待主线程通知
                        dataFile.WriteLineAsync(String.Format("{0}\t{1}", _timeStamp, WriteFileThread.fd.Nodes[t]));
                        WriteFileThread.Event.Reset();
                    }
                }
            }
        }
    }
}
