﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WebSocketSharp.Server;
using GloveLib; //旧的驱动
//using SenseSDK;   //新的驱动

namespace ControlClient
{
    public class ControlServerManage
    {
        //fuyang all the class below are singleton pattern
        //glove module for some init function
        private static GloveModule gloveModule;
        //glove controller class for all access to glove api
        private GloveController gc;
        private static ControlServerManage instance;
        private Socket server;  //游戏控制数据源
        private WebSocketServer WSServer;    //WebSocket服务端 
        private String GloveDataServerName = "/GloveData";   //四元数据在WebSocket上的服务名
        private String ScoreDataServerName = "/ScoreData";   //四元数据在WebSocket上的服务名
        private String CommandDataServerName = "/CommandData";   //评估命令在WebSocket上的服务名
        public static bool isServe = false;  //是否在服务中
        private HandType handType = GloveModule.handType; //设置左右手
        private ControlServerManage() { gc = GloveModule.GetSingleton().gc; }

        public static ControlServerManage GetInstance()
        {
            if (instance == null)
            {
                ConsoleManager.Show();
                gloveModule = GloveModule.GetSingleton();
                if (gloveModule.gc.GetPorts() != null)
                {
                    String selected_port = Utils.getConfig("port");
                    if (selected_port == null || selected_port.Equals(""))
                    {
                        Utils.UpdateAppConfig("port", gloveModule.gc.GetPorts().Last());
                    }
                    instance = new ControlServerManage();
                }
                else   //无效手套模块，置空
                {
                    GloveModule.DestoryInstance();
                }
            }
            return instance;
        }
        public static void DestoryInstance()
        {
            instance = null;
        }
        public bool getServerStatus()
        {
            return isServe;
        }
        public void startServer()
        {
            if (server == null && WSServer == null)
            {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                String localIP = Utils.getConfig("localIP");
                //Console.WriteLine(Utils.getConfig("port"));
                if (localIP == null || "".Equals(localIP))
                {
                    localIP = "127.0.0.1";         //默认本地地址
                }
                try
                {
                    isServe = true;
                    if (!gc.IsConnected((int)handType)) //接入手套
                    {
                        var PortName = Utils.getConfig("port").ToString();
                        gc.Connect(PortName, (int)handType);
                        // MainWindow.gloveStatus.label = "手套已接入";
                        // img_gloveStatus.Source = new BitmapImage(new Uri("./img/ok.png", UriKind.Relative));
                        // MainWindow.gloveStatus.status = "./img/ok.png";
                        MainWindow.gloveStatus.isGloveOK = true;
                    }
                    server.Bind(new IPEndPoint(IPAddress.Parse(localIP), 6001)); //绑定端口号和IP
                    Thread t = new Thread(sendMsg); //开启发送消息线程
                    t.IsBackground = true;
                    t.Start();
                    WSServer = new WebSocketServer(String.Format("ws://{0}", localIP)); //new WebSocket
                    WSServer.AddWebSocketService<GloveData>(GloveDataServerName);
                    WSServer.AddWebSocketService<ScoreData>(ScoreDataServerName);
                    WSServer.AddWebSocketService<CommandData>(CommandDataServerName);
                    WSServer.Start();
                }
                catch (NullReferenceException)
                {
                    isServe = false;
                    MessageBox.Show("请检查手套是否连接正常", "出错了", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    throw new Exception("这是已处理错误");
                }
                catch (IOException)
                {
                    isServe = false;
                    MessageBox.Show("请检查手套是否连接正常", "出错了", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    throw new Exception("这是已处理错误");
                }
                catch (FormatException)
                {
                    isServe = false;
                    MessageBox.Show("源IP地址无效", "出错了", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    throw new Exception("这是已处理错误");
                }
                catch (Exception e)
                {
                    isServe = false;
                    Console.WriteLine(e.ToString());
                }
                finally
                {
                    if (!isServe)
                    {
                        endServer();
                    }
                }
            }
            else
            {
                MessageBox.Show("服务启动失败，端口已被占用", "出错了", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        public void endServer()
        {
            isServe = false;
            try
            {
                if (gc.IsConnected((int)handType))        //接入手套
                {
                    gc.Close((int)handType);
                    // MainWindow.gloveStatus.label = "手套未接入";
                    // img_gloveStatus.Source = new BitmapImage(new Uri("./img/error.png", UriKind.Relative));
                    // MainWindow.gloveStatus.status = "./img/error.png";
                    MainWindow.gloveStatus.isGloveOK = false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            if (server != null)
            {
                server.Close();
            }
            if (WSServer != null)
            {
                WSServer.Stop();
            }
            server = null;
            WSServer = null;
        }
        // private  string msg = "hold";  //默认发送数据
        //发送数据
        private void sendMsg()
        {
            String targetIP = Utils.getConfig("targetIP");
            if (targetIP == null || "".Equals(targetIP))
            {
                targetIP = "127.0.0.1"; //默认本地地址
            }
            try
            {
                EndPoint point = new IPEndPoint(IPAddress.Parse(targetIP), 6000);
                Rehabilitation rhb = Rehabilitation.GetSingleton();
                int now = 0;
                while (isServe)
                {
                    if (server != null && (now = rhb.GetScore()) != -1)
                    {
                        server.SendTo(Encoding.UTF8.GetBytes(now.ToString()), point);
                        // Console.WriteLine(now.ToString());
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                isServe = false;
            }
        }
    }
}
