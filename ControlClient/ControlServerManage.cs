using GloveLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WebSocketSharp.Server;

namespace ControlClient
{
    public class ControlServerManage
    {
        private static ControlServerManage instance;
        private  Socket server;  //游戏控制数据源
        private  WebSocketServer WSServer;    //WebSocket服务端 
        private  String GloveDataServerName = "/GloveData";   //标量数据在WebSocket上的服务名
        private  String CommandDataServerName = "/CommandData";   //评估命令在WebSocket上的服务名 
        private  bool isServe = false;  //是否在服务中
        //glove controller class for all access to glove api
        private GloveController gc;
        //port selector
        private static ComboBox cbb_port;
        //lable for glove connection
        private static Label lbl_gloveStatus;
        private ControlServerManage() { gc = GloveModule.GetSingleton().gc; }

        public static ControlServerManage GetInstance(ComboBox pbb_port, Label gloveStatus)
        {
            if (instance == null)
            {
                lbl_gloveStatus = gloveStatus;
                cbb_port = pbb_port;
                instance = new ControlServerManage();
            }
            return instance;
        }
        public  bool getServerStatus()
        {
            return isServe;
        }
        public void startServer()
        {
            if (server == null && WSServer==null)
            { 
            server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            String localIP = Utils.getConfig("localIP");
                if (localIP != null&&!"".Equals(localIP))
                 {
                    try
                     {
                    isServe = true;
                    if (!gc.IsConnected(0))        //接入手套
                    {
                        var PortName = cbb_port.SelectedItem.ToString();
                        gc.Connect(PortName, 0);
                        lbl_gloveStatus.Content = "手套已接入";
                    }
                    server.Bind(new IPEndPoint(IPAddress.Parse(localIP), 6001));//绑定端口号和IP
                    Thread t = new Thread(sendMsg);//开启发送消息线程
                    t.IsBackground = true;
                    t.Start();
                    WSServer = new WebSocketServer(String.Format("ws://{0}", localIP));//new WebSocket
                    WSServer.AddWebSocketService<GloveData>(GloveDataServerName);
                    WSServer.AddWebSocketService<CommandData>(CommandDataServerName);
                    WSServer.Start();             
                    } catch (Exception e)
                    {
                    MessageBox.Show(e.ToString(), "出错了");
                    }
                 } else
                 {
                MessageBox.Show("本机IP地址不能为空", "出错了");
                 }
            }
            else
            {
                MessageBox.Show("服务启动失败", "出错了");
            }
        }
        public void endServer()
        {
            isServe = false;
            try { 
            if (gc.IsConnected(0))        //接入手套
            {
                gc.Close(0);
                lbl_gloveStatus.Content = "手套未接入";
            }
                }catch(Exception e){
                    MessageBox.Show(e.ToString(), "出错了");
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
        private  string msg = "hold";  //默认发送数据
        //发送数据
        private  void sendMsg()
        {
            String targetIP = Utils.getConfig("targetIP");
            if (targetIP != null && !"".Equals(targetIP))
            {
                try
                {
                    EndPoint point = new IPEndPoint(IPAddress.Parse(targetIP), 6000);
                    Rehabilitation rhb = Rehabilitation.GetSingleton();
                    int now = 0;
                    while (isServe)
                    {
                        now = rhb.GetScore();
                        if (now > 80)
                        {
                            msg = "left";
                        }
                        else if (now < 30)
                        {
                            msg = "right";
                        }
                        else
                        {
                            msg = "hold";
                        }
                        if (server != null)
                        server.SendTo(Encoding.UTF8.GetBytes(msg), point);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "出错了");
                    isServe = false;
                }
            }
            else
            {
                MessageBox.Show("目标IP地址不能为空", "出错了");
            }
        }
    }
}
