using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Windows.Media.Imaging;

namespace Csdn评论.Server
{

    public class HttpTool
    {
        public CookieContainer cookieContainer { set; get; }

        public Action<HttpWebRequest> actionRequest;


        private String FormatPostData(Dictionary<string, string> parameters)
        {
            String formDate = string.Empty;
            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                formDate = formDate + (formDate != "" ? "&" : "") + string.Format("{0}={1}", parameter.Key, HttpUtility.UrlEncode(parameter.Value));
            }
            return formDate;
        }


        public string Get(string url, String encodingName = "utf-8")
        {
            Stream stream = GetResponseStream(url, null);
            using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(encodingName)))
            {
                return reader.ReadToEnd();
            }
        }

        public string Post(string url, Dictionary<string, string> parameters, String encodingName = "utf-8")
        {
            String formDate = FormatPostData(parameters);
            byte[] byteArray = Encoding.Default.GetBytes(formDate);
            Stream stream = GetResponseStream(url, byteArray);
            using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(encodingName)))
            {
                return reader.ReadToEnd();
            }
        }

        public BitmapImage GetImage(string url)
        {
            string content = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (cookieContainer != null)
            {
                request.CookieContainer = cookieContainer;
            }
            request.AllowWriteStreamBuffering = false;
            request.ContentType = "image/png";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
            request.KeepAlive = true;
            request.Method = WebRequestMethods.Http.Get;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream stream = response.GetResponseStream();
                if (stream.CanRead)
                {
                    Byte[] buffer = new Byte[response.ContentLength];
                    stream.Read(buffer, 0, buffer.Length);

                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = new MemoryStream(buffer);
                    bi.EndInit();

                    stream.Close();
                    return bi;
                }
            }
            return null;
        }


        private Stream GetResponseStream(string url, byte[] byteArray = null)
        {
            string content = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (cookieContainer != null)
            {
                request.CookieContainer = cookieContainer;
            }
            request.AllowWriteStreamBuffering = false;
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
            request.KeepAlive = true;

            if (byteArray == null)
            {
                request.Method = WebRequestMethods.Http.Get;
            }
            else
            {
                request.Method = WebRequestMethods.Http.Post;
                request.ContentLength = byteArray.Length;
            }


            if (actionRequest != null)
            {
                actionRequest(request);
            }

            if (byteArray != null)
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(byteArray, 0, byteArray.Length);
                }
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                return response.GetResponseStream();
            }
        }
    }
}
