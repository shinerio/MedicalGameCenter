using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GloveLib; //旧的驱动
//using SenseSDK;   //新的驱动

namespace ControlClient
{
    class WebSockData
    {  
        public Node [] nodes{get;set;}
        public int score { get; set; }
    }
}
