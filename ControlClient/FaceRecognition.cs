using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace ControlClient
{

    class FaceRecognition
    {
        private static String api_key = "5W-BJb6yjHg83NS1nVnAI9g6NgMurLq8";
        private static String api_value = "zGcehF-OVy2DNMxkHuPH1p0DxysmClcz";
        public static FaceSearchResult Search()
        {
            NameValueCollection data = new NameValueCollection();
            data.Add("api_key", api_key);
            data.Add("api_secret", api_value);
            data.Add("outer_id", "shinerio");
            BitmapImage img = new BitmapImage(new Uri("./img/faceToLogin.png",
                                                   UriKind.Relative));
            String mainPath = Application.ExecutablePath;
            String filePath = "F:/test.jpg";
            String jsonText = "";
            if (img != null)
            {
                HttpWebResponse response = HttpHelper.HttpUploadFile("https://api-cn.faceplusplus.com/facepp/v3/search", new string[] { "image_file" }, new String[] { filePath }, data);
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    jsonText = stream.ReadToEnd();
                }
            }
            FaceSearchResult res = JsonConvert.DeserializeObject<FaceSearchResult>(jsonText);
            return res;
//            if (res != null && res.results != null && res.results[0]!=null&&res.results[0].confidence> 80)
//            {
//                return true;
//            }
//            return false;
        }
    }
}
