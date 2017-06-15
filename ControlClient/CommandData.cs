using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Windows.Threading;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

//using SenseSDK;

namespace ControlClient
{
    public class CommandData : WebSocketBehavior
    {
        // URL
        private static String URL = "http://47.94.172.143:8080/shinerio/";
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
        private const String EvaluatePlayback = "evaluate_playback";
        private const String EvaluatePlaybackAck = "evaluate_playback_ack";

        static readonly Regex DigitRegex = new Regex(@"\d+");
        private static int _evaluateTime = 1;
        private static long _timeStamp = GetCurrentTimeLong();
        private static long _startTime = GetCurrentTimeLong();
        private static long _endTime = GetCurrentTimeLong();
        private static int _evaluationId = -1;  // 当前评估id
        private static int _evalustionSuccess = 0;  // 评估成功次数
        private static int _evaluationTestSpeed = 10;   // 评估时每分钟开合次数
        private static SocketManager sm = SocketManager.GetInstance();    // 评估再现数据发送socket
        private static Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Socket bindedSocket;
        // 数据库配置
        private static string _connectionString = "server=localhost;" +
                                         "user id=root; " +
                                         "pwd=admin;" +
                                         "database=medical_manage;" +
                                         "allowuservariables=True;" +
                                         "Allow Zero Datetime=True";
        // 建立以UserID为名的文件夹
        private static String dir = String.Format(@"{0}", Patient.GetInstance().id);
        private static String filePath;
        private static Rehabilitation rhb = Rehabilitation.GetSingleton();
        private static DataWarehouse dh = DataWarehouse.GetSingleton();
        private static HandType handType = GloveModule.handType; //左右手
        private static EvaluateStatus status = EvaluateStatus.Idle;
        private static EvaluationWindow ew;
        public static bool isRunning = false;
        private static bool waitingPlaybackParam = false;    //等待传递评估参数
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
                int d = String2Int(data, 110);
                _evaluateTime = d / 100;
                _evaluateTime = _evaluateTime > 5 ? 5 : _evaluateTime;
                _evaluationTestSpeed = d % 100;
                _evaluationTestSpeed = _evaluationTestSpeed > 10 ? 10 : _evaluationTestSpeed;
            }
            if (waitingPlaybackParam && DigitRegex.IsMatch(data))
            {
                EvaluationPlaybackThread.Run(String2Int(data, 110));
                waitingPlaybackParam = false;
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
                        _timer.Interval = _evaluateTime * 60000; // 更改时长
                        _timer.Start();
                        // 弹出评估窗
                        int time = 60000 / _evaluationTestSpeed;
                        MainWindow.StartEvaWindow(time);
                        // 开始评估操作
                        WriteFileThread.Run(); //Test
                    }
                    break;
                case EvaluatePlayback:
                    waitingPlaybackParam = true;
                    Console.WriteLine("准备评估再现");
                    Send(EvaluatePlaybackAck);
                    //Console.WriteLine(EvaluatePlaybackAck);
                    break;
                default:
                    break;
            }
        }

        private void StopRunning(object source, ElapsedEventArgs e)
        {
            isRunning = false;
            MainWindow.EndEvaWindow();
            Send(EvaluateStop);
            ResetStatus();
        }

        private void ResetStatus()
        {
            status = EvaluateStatus.Idle;
            _timer.Stop();
        }

        private int String2Int(String s, int def)
        {
            int i;
            if (!Int32.TryParse(s, out i))
            {
                i = def;
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
        private static void InsertEvaluationInfo(int userId, long startTime, long endTime, int successRatio)
        {
            //            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            //            {
            //                conn.Open();
            //                using (MySqlCommand cmd = conn.CreateCommand())
            //                {
            //                    cmd.CommandText = "insert into evaluation_info(patient_id, start_time, end_time, success_ratio) values (@patient_id, @start_time, @end_time, @success_ratio)";
            //                    cmd.Parameters.AddWithValue("@patient_id", userId);
            //                    cmd.Parameters.AddWithValue("@start_time", startTime);
            //                    cmd.Parameters.AddWithValue("@end_time", endTime);
            //                    cmd.Parameters.AddWithValue("@success_ratio", successRatio);
            //                    try
            //                    {
            //                        cmd.ExecuteNonQuery();
            //                    }
            //                    catch (MySqlException ex)
            //                    {
            //                        // Console.WriteLine(ex.Message);
            //                    }
            //                    finally
            //                    {
            //                        _evalustionSuccess = 0;
            //                    }
            //                }
            //            }
            Console.WriteLine("adding evaluation record......");
            String url = String.Format("{0}evaluation/addEvaluation_info", URL);
            IDictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("patientID", userId.ToString());
            parameters.Add("start_time", startTime.ToString());
            parameters.Add("end_time", endTime.ToString());
            parameters.Add("success_ratio", successRatio.ToString());
            HttpWebResponse response = null;
            try
            {
                response = HttpWebResponseUtility.CreatePostHttpResponse(url, parameters, null, null, Encoding.UTF8, null);
            }
            catch (Exception exception)
            {
                MessageBox.Show("评估信息上传失败，请检查网络设置", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }
        // 根据uid和start_time查找对应的评估id
        private static int GetEvaluationId(int userId, long startTime)
        {
            int evaluationId = -1;
//            using (MySqlConnection conn = new MySqlConnection(_connectionString))
//            {
//                conn.Open();
//                using (MySqlCommand cmd = conn.CreateCommand())
//                {
//                    cmd.CommandType = CommandType.Text;
//                    cmd.CommandText = "select id from evaluation_info where patient_id=@patient_id and start_time=@start_time;";
//                    cmd.Parameters.AddWithValue("@patient_id", userId);
//                    cmd.Parameters.AddWithValue("@start_time", startTime);
//                    using (MySqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            if (reader.HasRows)
//                            {
//                                evaluationId = reader.GetInt32(0);
//                                //Console.WriteLine(reader.GetString(0));
//                            }
//                        }
//                    }
//
//                }
//            }
            String url = String.Format("{0}evaluation/getEvaluationId", URL);
            IDictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("patient_id", userId.ToString());
            parameters.Add("start_time", startTime.ToString());
            HttpWebResponse response = null;
            StreamReader readStream = null;
            try
            {
                response = HttpWebResponseUtility.CreatePostHttpResponse(url, parameters, null, null, Encoding.UTF8, null);
                Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                readStream = new StreamReader(response.GetResponseStream(), encode);
                Char[] read = new Char[256];
                int count = readStream.Read(read, 0, 256);
                String str = "";
                while (count > 0)
                {
                    str += new String(read, 0, count);
                    // Console.Write(str);
                    count = readStream.Read(read, 0, 256);
                }
                if (!str.Equals(""))
                {
                    str = str.Replace("\"", "'"); //java和c#的json格式转化
                    if (!Int32.TryParse(str, out evaluationId))
                    {
                        evaluationId = -1;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
            return evaluationId;
        }
        // 插入原始数据
        private static void InsertRawData(MySqlConnection connection, MySqlCommand cmd, int evaluationId,
            long timeStamp, String jsonString, int score)
        {
            cmd.CommandText = "insert into rawdata(evaluation_id, time_stamp, json_string, score) values (@evaluation_id, @time_stamp, @json_string, @score);";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@evaluation_id", evaluationId);
            cmd.Parameters.AddWithValue("@time_stamp", timeStamp);
            cmd.Parameters.AddWithValue("@json_string", jsonString);
            cmd.Parameters.AddWithValue("@score", score);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Console.WriteLine(ex.Message);
            }
        }
        // 原始数据文件上传
        private static void SendRawData(String upload, int evaluationId)
        {
            NameValueCollection data = new NameValueCollection();
            data.Add("evaluation_info_id", evaluationId.ToString());
            HttpWebResponse response = HttpHelper.HttpUploadFile(String.Format("{0}evaluation/addRawdatas", URL),
                new string[] { "upload" }, new String[] { upload }, data);
        }

        // 写文件进程
        private class WriteFileThread
        {
            public static ManualResetEvent Event = new ManualResetEvent(false);
            private static SkeletonCalculator sc = SkeletonCalculator.GetSingleton("");
            private static SkeletonCalculator skc = SkeletonCalculator.GetSingleton("");
            public static FrameData fd = dh.GetFrameData(handType, Definition.MODEL_TYPE);

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
                bool waitingBest = false;   // 等待手掌闭合
                int score = rhb.GetScore();
                int t = 10;
                int.TryParse(ms.ToString(), out t); //这里采用了TryParse方法，避免不能转换时出现异常
                filePath = String.Format(@"{0}/nodes{1}.txt", dir, GetCurrentTimeLong());
                // 结果先写入测试文件
                using (System.IO.StreamWriter nodesFile = new System.IO.StreamWriter(filePath, false))
                {
                    while (CommandData.isRunning)
                    {
                        score = rhb.GetScore();
                        if (score > 97 && waitingBest == false)
                        {
                            waitingBest = true;
                        }
                        if (score < 3 && waitingBest == true)
                        {
                            _evalustionSuccess++;   // 统计成功一次
                            waitingBest = false;
                        }
                        fd = dh.GetFrameData(handType, Definition.MODEL_TYPE);
                        _timeStamp = GetCurrentTimeLong();
                        // scoreFile.WriteLine(String.Format("{0}\t{1}\t{2}", TimeStamp, Patient.GetInstance().id, rhb.GetScore()));
                        nodesFile.WriteLine(String.Format("{0}\t{1}\t{2}", _timeStamp, rhb.GetScore(), sc.UpdateRaw(fd).ToJson()));
                        // Event.Set();
                        Thread.Sleep(t); //让线程暂停 
                    }
                }
                _endTime = GetCurrentTimeLong();
                Console.WriteLine("Write File Done!");
                // WriteFileToDatabase.Run();
                UploadFileToServer.Run();
            }
        }
        // 将文件上传到服务器
        private class UploadFileToServer
        {
            public static void Run()
            {
                UploadFileToServer t = new UploadFileToServer();
                Thread thread = new Thread(new ThreadStart(t.Excute));
                thread.IsBackground = true;
                thread.Name = "UploadFileToServerThread";
                thread.Start();
                Console.WriteLine("RawData is Uploading!");
            }

            private void Excute()
            {
                int max = _evaluationTestSpeed * _evaluateTime;
                int evaluationSuccessRatio = (_evalustionSuccess > max ? max : _evalustionSuccess) * 100 / (_evaluationTestSpeed * _evaluateTime);
                // InsertEvaluationInfo(Patient.GetInstance().id, _startTime, _endTime, evaluationSuccessRatio);
                InsertEvaluationInfo(1, _startTime, _endTime, evaluationSuccessRatio);
                // _evaluationId = GetEvaluationId(Patient.GetInstance().id, _startTime);
                _evaluationId = GetEvaluationId(1, _startTime); // TODO 此处的用户编号为测试编号
                Console.WriteLine(String.Format("{0}:{1}", filePath, _evaluationId));
                if (!filePath.IsNullOrEmpty() && _evaluationId != -1)
                {
                    SendRawData(filePath, _evaluationId);
                    Console.WriteLine("Upload Done!");
                }
                else
                {
                    Console.WriteLine("Get Param Fail!");
                }
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
                int max = _evaluationTestSpeed * _evaluateTime;
                int evaluationSuccessRatio = (_evalustionSuccess > max ? max : _evalustionSuccess) * 100 / (_evaluationTestSpeed * _evaluateTime);
                // InsertEvaluationInfo(Patient.GetInstance().id, _startTime, _endTime, evaluationSuccessRatio);
                InsertEvaluationInfo(1, _startTime, _endTime, evaluationSuccessRatio);
                // _evaluationId = GetEvaluationId(Patient.GetInstance().id, _startTime);
                _evaluationId = GetEvaluationId(1, _startTime); // TODO 测试用
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = conn.CreateCommand())
                    {
                        using (System.IO.StreamReader file = new System.IO.StreamReader(filePath))
                        {
                            string line;
                            long timeStamp = _timeStamp;
                            int score;
                            while ((line = file.ReadLine()) != null)
                            {

                                String[] tokens = line.Split('\t');

                                if (tokens.Length == 3)
                                {
                                    int.TryParse(tokens[1], out score);
                                    long.TryParse(tokens[0], out timeStamp);
                                    InsertRawData(conn, cmd, _evaluationId, timeStamp, tokens[2], score);
                                }
                            }
                        }
                    }
                }
                Console.WriteLine("Write DB Done!");
            }
        }

        private class EvaluationPlaybackThread
        {
            private static int EvaluationId;
            private static bool isAccepted = false; // 客户端是否接入

            public static void Run(int id)
            {

                EvaluationId = id;
                Console.WriteLine(id);
                EvaluationPlaybackThread t = new EvaluationPlaybackThread();
                Thread thread = new Thread(new ThreadStart(t.Excute));
                thread.IsBackground = true;
                thread.Name = "EvaluationPlaybackThread";
                thread.Start();
                Thread timingThread = new Thread(new ThreadStart(t.connectTiming));
                timingThread.IsBackground = true;
                timingThread.Name = "Timing Thread";
                timingThread.Start();
            }

            private void Excute()
            {
                HttpWebResponse response = null;
                StreamReader readStream = null;
                try
                {
                    // sm.Start(10201);    // 开启socket
                    String localIP = Utils.getConfig("localIP");
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    server.Bind(new IPEndPoint(IPAddress.Parse(localIP), 10201)); //绑定IP和端口号
                    server.Listen(1);
                    Console.WriteLine("等待客户端连接");
                    bindedSocket = server.Accept();
                    if (bindedSocket != null)
                    {
                        isAccepted = true;
                        Console.WriteLine("客户端已连接");
                    }
                    List<String> rawDatas = GetRawDataById(EvaluationId);
                    SendRawdataToSocket(rawDatas);
//                    using (MySqlConnection conn = new MySqlConnection(_connectionString))
//                    {
//                        conn.Open();
//                        using (MySqlCommand cmd = conn.CreateCommand())
//                        {
//                            //Console.WriteLine(EvaluationId);
//                            SendRawdataToSocket(conn, cmd, EvaluationId);
//                        }
//                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("客户端连接超时");
                }
                finally
                {
                    isAccepted = false;
                }
            }
            private void connectTiming()
            {
                Thread.Sleep(100);
                Thread startHand = new Thread(new ThreadStart(StartHandModel));
                startHand.IsBackground = true;
                startHand.Name = String.Format("Starting Hand Model");
                startHand.Start();
                Thread.Sleep(20000);
                if (!isAccepted)
                {
                    server.Close(); // 超时后关闭连接
                }
            }
        }
        // 将原始数据通过socket发送出去(直接读取数据库)
        private static void SendRawdataToSocket(MySqlConnection connection, MySqlCommand cmd, int evaluationId)
        {
            cmd.CommandText = "select json_string from rawdata where evaluation_id=@evaluation_id order by time_stamp asc;";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@evaluation_id", evaluationId);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.HasRows)
                    {
                        Thread.Sleep(10);
                        if (bindedSocket != null)
                        {
                            bindedSocket.Send(Encoding.ASCII.GetBytes(reader.GetString(0) + "<EOF>"));
                            // Console.WriteLine("SendRawdataToSocket:" + evaluationId);
                        }
                    }
                }
            }
            bindedSocket.Send(Encoding.ASCII.GetBytes("<AFK><EOF>"));
            Console.WriteLine("评估再现完成！");
            server.Close();
            //server.Disconnect();
        }
        // 获得原始数据
        private static List<String> GetRawDataById(int EvaluationId)
        {
            Console.WriteLine(String.Format("Evaluation ID: {0}",EvaluationId));
            HttpWebResponse response = null;
            Stream resStream = null;
            try
            {
                String url = String.Format("{0}patient/getRawData", URL);
                IDictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("evaluation_id", EvaluationId.ToString());

                response = HttpWebResponseUtility.CreatePostHttpResponse(url, parameters, null, null,
                    Encoding.UTF8, null);
                Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                //readStream = new StreamReader(response.GetResponseStream(), encode);
                StringBuilder sb = new StringBuilder();
                Byte[] buf = new byte[8192];
                resStream = response.GetResponseStream();
                int count = buf.Length;
                do
                {
                    count = resStream.Read(buf, 0, buf.Length);
                    if (count != 0)
                    {
                        sb.Append(Encoding.UTF8.GetString(buf, 0, count)); // just hardcoding UTF8 here
                    }
                } while (count > 0);
                String str = sb.ToString();
                if (!str.Equals(""))
                {
                    str = str.Replace("\"", "'"); //java和c#的json格式转化
                    List<String> rawdatas = str.Split('\n').ToList();
                    Console.WriteLine(String.Format("Get Raw Datas: {0}", rawdatas.Count));
                    return rawdatas;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
                if (resStream != null)
                {
                    resStream.Close();
                }
            }
            return null;
        }
        // 将原始数据通过socket发送出去
        private static void SendRawdataToSocket(List<String> rawdatas)
        {
            foreach (var rawdata in rawdatas)
            {
                Thread.Sleep(10);
                if (bindedSocket != null)
                {
                    bindedSocket.Send(Encoding.ASCII.GetBytes(rawdata + "<EOF>"));
                    // Console.WriteLine("SendRawdataToSocket:" + evaluationId);
                }
            }
            bindedSocket.Send(Encoding.ASCII.GetBytes("<AFK><EOF>"));
            Console.WriteLine("评估再现完成！");
            server.Close();
            //server.Disconnect();
        }

        private static void StartHandModel()
        {
            String path = Utils.getConfig("modelPath");
            Console.WriteLine(path);
            try
            {
                if (path != null && !"exe".Equals(Path.GetExtension(path)))
                {
                    Process.Start(path);
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("手模已被修改或不存在\n请点击设置，并重新添加路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception)
            {
                MessageBox.Show("发生了未知的错误，请重试", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class EvaluationWindowThread
        {
            private static Thread threadRun;
            private static Thread threadStop;
            public static void Run()
            {
                int time = 60000 / _evaluationTestSpeed;
                //                if (threadRun != null)
                //                {
                //                    threadRun.Abort();
                //                    threadRun = null;
                //                }
                //                if (threadStop != null)
                //                {
                //                    threadStop.Abort();
                //                    threadStop = null;
                //                }
                threadRun = new Thread(delegate()
                {
                    ew = EvaluationWindow.GetInstance(time);
                    ew.Start();
                    System.Windows.Threading.Dispatcher.Run();
                });
                threadRun.Name = "EvaluationWindowThread Run";
                threadRun.IsBackground = true;
                threadRun.SetApartmentState(ApartmentState.STA);
                threadRun.Start();
            }
            public static void Stop()
            {
                //                if (threadRun != null)
                //                {
                //                    threadRun.Abort();
                //                    threadRun = null;
                //                }
                //                if (threadStop != null)
                //                {
                //                    threadStop.Abort();
                //                    threadStop = null;
                //                }
                threadStop = new Thread(delegate()
                {
                    ew.Stop();
                    System.Windows.Threading.Dispatcher.Run();
                });
                threadStop.Name = "EvaluationWindowThread Stop";
                threadStop.IsBackground = true;
                threadStop.SetApartmentState(ApartmentState.STA);
                threadStop.Start();
            }
        }
    }
}
