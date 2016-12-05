using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GloveLib;
using System.Windows.Controls;
using System.Windows;

namespace ControlClient
{
    public class Logger : ILogger
    {
        private TextBox txt;
        private const int _maxCapacity = 1000;
        private Queue<string> messageQueue = new Queue<string>(_maxCapacity);

        private Logger(TextBox tb)
        {
            txt = tb;
        }

        private static Logger Instance = null;

        public static Logger GetInstance(TextBox tb)
        {
            if (Instance == null)
            {
                Instance = new Logger(tb);
            }
            return Instance;
        }

        public static Logger GetInstance()
        {
            return Instance;
        }

        public void Log(string msg)
        {
            if (Instance != null)
            {
                Instance.Log_p(msg);
            }
        }

        private string EnumerateString(Queue<string> q)
        {
            return q.Aggregate((i, j) => i + " " + j);
        }

        public void Log_p(string msg)
        {
            if (messageQueue.Count >= _maxCapacity)
            {
                messageQueue.Dequeue();
            }
            var l = String.Format("{0:MM-dd hh:mm:ss}: {1} \r\n", DateTime.Now, msg);
            messageQueue.Enqueue(l);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (txt != null)
                {
                    txt.Text = EnumerateString(messageQueue);
                    txt.ScrollToEnd();
                }
            });
        }
    }
}
