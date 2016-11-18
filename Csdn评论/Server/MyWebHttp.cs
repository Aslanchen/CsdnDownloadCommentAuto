using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Windows.Media.Imaging;

namespace Csdn评论.Server
{

    public class MyWebHttp
    {
        private static MyWebHttp instance;
        private CookieContainer gCookieContainer;

        public static MyWebHttp getInstance()
        {
            if (instance == null)
            {
                instance = new MyWebHttp();
            }
            return instance;
        }

        public void setCookie(CookieContainer cc)
        {
            this.gCookieContainer = cc;
        }

        private string getFormData(Dictionary<string, string> parameters)
        {
            String formDate = string.Empty;
            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                formDate = formDate + (formDate != "" ? "&" : "") + string.Format("{0}={1}", parameter.Key, HttpUtility.UrlEncode(parameter.Value));
            }
            return formDate;
        }

        public string DownUTF8WebSite(string url)
        {
            string value = String.Empty;
            try
            {
                value = getWebSiteGet(url, "utf-8");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return value;
        }

        public string DownUTF8WebSite(string url, string ip)
        {
            string value = String.Empty;
            try
            {
                value = getWebSiteGet(url, "utf-8", ip);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return value;
        }

        public string DownUTF8WebSite(string url, Dictionary<string, string> parameters, string Host, string Referer)
        {
            String formDate = getFormData(parameters);
            byte[] byteArray = Encoding.Default.GetBytes(formDate);
            string value = String.Empty;
            try
            {
                value = getWebSitePost(url, "utf-8", byteArray, Host, Referer);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return value;
        }

        public string DownGBKWebSite(string url)
        {
            string value = String.Empty;
            try
            {
                value = getWebSiteGet(url, "GBK");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return value;
        }

        public string DownGBKWebSite(string url, Dictionary<string, string> parameters, string Host, string Referer)
        {
            String formDate = getFormData(parameters);
            byte[] byteArray = Encoding.Default.GetBytes(formDate);
            string value = String.Empty;
            try
            {
                value = getWebSitePost(url, "GBK", byteArray, Host, Referer);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return value;
        }

        /// <summary>
        /// Get方式
        /// </summary>
        /// <param name="url">Url地址</param>
        /// <param name="unicode">编码形式</param>
        /// <returns></returns>
        private string getWebSiteGet(string url, string unicode)
        {
            string content = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (gCookieContainer != null)
            {
                request.CookieContainer = gCookieContainer;
            }
            request.AllowWriteStreamBuffering = false;
            request.Headers["Accept-Language"] = "zh-CN";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
            request.KeepAlive = true;
            //request.
            request.Method = WebRequestMethods.Http.Get;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(unicode)))
                {
                    content = reader.ReadToEnd();
                }
            }
            return content;
        }

        public BitmapImage GetImage(string url)
        {
            string content = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (gCookieContainer != null)
            {
                request.CookieContainer = gCookieContainer;
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

        private string getWebSiteGet(string url, string unicode, String ip)
        {
            string content = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add("X_FORWARDED_FOR", ip);
            request.CookieContainer = gCookieContainer;
            request.AllowWriteStreamBuffering = false;
            request.Headers["Accept-Language"] = "zh-CN";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
            request.KeepAlive = true;
            //request.
            request.Method = WebRequestMethods.Http.Get;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(unicode)))
                {
                    content = reader.ReadToEnd();
                }
            }
            return content;
        }

        /// <summary>
        /// Post方式
        /// </summary>
        /// <param name="url">Url地址</param>
        /// <param name="unicode">编码格式</param>
        /// <param name="byteArray">formData字节数字</param>
        /// <param name="Host">如果没有则为空</param>
        /// <param name="Referer">如果没有则为空</param>
        /// <returns></returns>
        private string getWebSitePost(string url, string unicode, byte[] byteArray, string Host, string Referer)
        {
            string content = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "*/*";
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.CookieContainer = gCookieContainer;
            request.ContentType = "application/x-www-form-urlencoded";

            if (string.IsNullOrEmpty(Host) == false)
            {
                //request.Host = Host;
            }
            if (string.IsNullOrEmpty(Referer) == false)
            {
                request.Referer = Referer;
            }
            request.AllowWriteStreamBuffering = false;
            request.Headers["Accept-Language"] = "zh-CN";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
            request.KeepAlive = true;
            request.Method = WebRequestMethods.Http.Post;
            request.ContentLength = byteArray.Length;
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(byteArray, 0, byteArray.Length);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(unicode)))
                    {
                        content = reader.ReadToEnd();
                    }
                }
            }
            return content;
        }
    }
}
