using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Csdn评论.Server;
using System.Windows.Threading;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using System.Runtime.InteropServices;

namespace Csdn评论
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private int startPage = 1;
        private int curPage = 0;
        private int endPage = 999;
        private int curStringIndex = 0;
        private int lastDiscusPage = 0;
        private Thread thread = null;
        private bool isStart = false;
        private string[] stringGood = new string[] { "很好，非常满意，感谢分享", "终于找到了这个，很强大，谢谢！", "资源还不错，值得学习", "很好的资源...值得借鉴...", "受益匪浅，谢谢分享，继续学习去",
        "真的很不错。很有用，好好学学","资源太详细了！！！谢谢楼主分享好东西！！！","可以，不错，谢谢了。过来学习下","很好，非常感谢，值得学习","收获颇丰，学到了许多啊",
        "不错，很实用。 学习，谢谢分享！","很好，努力学习中","正好用得到，谢谢","多谢！很有用。学习中。","确实对自己有些用，借鉴一下！！","好东西，你值得拥有",
        "正在学习中，资源不错","很有用啊，太感谢了","真是一个好东西，有参考价值的","很好，不错！值得学一学！","很好的资源啊，呵呵，多谢分享","这个资源不错，非常楼主感谢，学习了。"};
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            wb_show.Navigate("http://passport.csdn.net/account/login?from=http%3A%2F%2Fdownload.csdn.net%2Fmy%2Fdownloads");
        }

        //获取WebBrowser的Cookie
        private CookieContainer GetCookieContainer()
        {
            CookieContainer container = new CookieContainer();
            mshtml.IHTMLDocument2 ss = (mshtml.IHTMLDocument2)wb_show.Document;
            foreach (string cookie in ss.cookie.Split(';'))
            {
                string name = cookie.Split('=')[0];
                string value = cookie.Substring(name.Length + 1);
                string path = "/";
                string domain = ".csdn.net";
                container.Add(new Cookie(name.Trim(), value.Trim(), path, domain));
            }
            return container;
        }

        private void bt_start_Click(object sender, RoutedEventArgs e)
        {
            if (isStart == false)
            {
                CookieContainer myCookieContainer = GetCookieContainer();
                MyWebHttp.getInstance().setCookie(myCookieContainer);

                bt_start.Content = "结束";
                isStart = true;
                thread = new Thread(() => { DownList(); });
                thread.IsBackground = true;
                thread.Start();
            }
            else
            {
                bt_start.Content = "开始";
                isStart = false;
                if (thread != null)
                {
                    thread.Abort();
                }
            }

        }


        //private void Login(String name, String pass)
        //{
        //    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
        //    {
        //        tv_state.Text = "登录ing";
        //    });

        //    String url = "https://passport.csdn.net/account/login";
        //    String content = MyWebHttp.getInstance().DownUTF8WebSite(url);

        //    //获取its
        //    String regex = "name=\"lt\" value=\"(.{40})\"";
        //    Regex r = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        //    Match m = r.Match(content);
        //    String it = m.Groups[1].ToString();

        //    //获取execution
        //    regex = "name=\"execution\" value=\"(.{4,5})\"";
        //    r = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        //    m = r.Match(content);
        //    String execution = m.Groups[1].ToString();

        //    url = "http://passport.csdn.net/account/login";
        //    Dictionary<string, string> parameters = new Dictionary<string, string>();
        //    parameters.Add("username", "122560007@163.com");
        //    parameters.Add("password", "Csdn52800");
        //    parameters.Add("lt", it);
        //    parameters.Add("execution", execution);
        //    parameters.Add("_eventId", "submit");

        //    string host = "";
        //    content = MyWebHttp.getInstance().DownUTF8WebSite(url, parameters, host, "");

        //    DownList();
        //}

        //读取评论数据
        private void DownList()
        {
            curPage = startPage;
            while (curPage <= endPage)
            {
                String curPageUrl = "http://download.csdn.net/my/downloads/" + curPage;
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    tv_state.Text = "获取列表第" + curPage + "页";
                    wb_show.Navigate(curPageUrl);
                });

                String content = MyWebHttp.getInstance().DownUTF8WebSite(curPageUrl);

                if (content.Contains("请您先登录"))
                {
                    MessageBox.Show("请先登录", "提示");
                    isStart = false;
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                    {
                        tv_state.Text = "未登录";
                        bt_start.Content = "开始";
                    });
                    return;
                }

                //获取数据
                string pattern = "<a href=\"/detail/.{2,30}/(\\d{2,15})#comment\" class=\"btn-comment\">立即评价，通过可返分</a>";
                MatchCollection matches = Regex.Matches(content, pattern);
                foreach (Match match in matches)
                {
                    String sourceid = match.Groups[1].Value;
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                    {
                        tv_state.Text = "正在评论sourceid：" + sourceid;
                    });

                    while (WriteDiscuss(sourceid, curPage) == false)
                    {
                        Thread.Sleep(5 * 1000);
                    }

                }
                curPage++;

                if (System.Math.Abs(lastDiscusPage - curPage) >= 20)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                    {
                        tv_state.Text = "评论结束";
                        bt_start.Content = "开始";
                        isStart = false;
                    });
                    return;
                }
            }
        }

        private bool WriteDiscuss(String sourceid, int page)
        {
            bool isOk = false;
            if (string.IsNullOrEmpty(sourceid))
            {
                return isOk;
            }

            long epoch = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            StringBuilder sb = new StringBuilder();
            sb.Append("http://download.csdn.net/index.php/comment/post_comment?");
            sb.Append("jsonpcallback=jsonp=" + epoch + "112");
            sb.Append("&sourceid=" + sourceid);
            sb.Append("&content=" + stringGood[curStringIndex]);
            sb.Append("&rating=5");
            sb.Append("&t=" + epoch + "286");

            curStringIndex++;
            if (curStringIndex >= stringGood.Length)
            {
                curStringIndex = 0;
            }
            String url = sb.ToString();
            String content = MyWebHttp.getInstance().DownUTF8WebSite(url);

            if (content.Contains("\"succ\":1") == true)
            {
                lastDiscusPage = page;
                isOk = true;
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    tv_state.Text = "CSDN评论间隔时间，等待";
                    wb_show.Refresh();
                });
                Thread.Sleep(1000 * 60);
            }
            else
            {
                isOk = false;
            }
            return isOk;
        }

        private void wb_show_LoadCompleted(object sender, NavigationEventArgs e)
        {

        }

        static void SuppressScriptErrors(WebBrowser webBrowser, bool hide)
        {
            webBrowser.Navigating += (s, e) =>
            {
                var fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (fiComWebBrowser == null)
                    return;

                object objComWebBrowser = fiComWebBrowser.GetValue(webBrowser);
                if (objComWebBrowser == null)
                    return;

                objComWebBrowser.GetType().InvokeMember("Silent", System.Reflection.BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
            };
        }

        private void wb_show_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            SuppressScriptErrors(sender as WebBrowser, true);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (thread != null)
            {
                thread.Abort();
            }
        }

        private void bt_about_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }
    }
}
