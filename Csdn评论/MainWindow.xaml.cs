using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Csdn评论.Server;
using System.Windows.Threading;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using Csdn评论.Model;
using Newtonsoft.Json;
using System.IO;
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
        private bool isPause = false;
        private string txt_validcode;
        private string[] stringGood = new string[] { "很好，非常满意，感谢分享", "终于找到了这个，很强大，谢谢！", "资源还不错，值得学习", "很好的资源...值得借鉴...", "受益匪浅，谢谢分享，继续学习去",
        "真的很不错。很有用，好好学学","资源太详细了！！！谢谢楼主分享好东西！！！","可以，不错，谢谢了。过来学习下","很好，非常感谢，值得学习","收获颇丰，学到了许多啊",
        "不错，很实用。 学习，谢谢分享！","很好，努力学习中","正好用得到，谢谢","多谢！很有用。学习中。","确实对自己有些用，借鉴一下！！","好东西，你值得拥有",
        "正在学习中，资源不错","很有用啊，太感谢了","真是一个好东西，有参考价值的","很好，不错！值得学一学！","很好的资源啊，呵呵，多谢分享","这个资源不错，非常楼主感谢，学习了。"};
        private HttpTool httptool = new HttpTool();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex = 0;
            //wb_show.Navigate("http://passport.csdn.net/account/login?from=http%3A%2F%2Fdownload.csdn.net%2Fmy%2Fdownloads");
            wb_show.Navigate("http://download.csdn.net/my/downloads/1");
        }

        //获取WebBrowser的Cookie
        private CookieContainer GetCookieContainer()
        {
            mshtml.IHTMLDocument2 ss = (mshtml.IHTMLDocument2)wb_show.Document;
            if (ss == null)
            {
                return null;
            }

            CookieContainer container = new CookieContainer();
            foreach (string cookie in ss.cookie.Split(';'))
            {
                string[] values = cookie.Split('=');
                string name = values[0].Trim();
                string value = values[1].Trim();
                string domain = ".csdn.net";
                try
                {
                    container.Add(new Cookie(name, value, "/", domain));
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
            return container;
        }

        private void bt_start_Click(object sender, RoutedEventArgs e)
        {
            if (isStart == false)
            {
                CookieContainer myCookieContainer = GetCookieContainer();
                httptool.cookieContainer = myCookieContainer;

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

        //读取评论数据
        private void DownList()
        {
            curPage = startPage;
            while (curPage <= endPage)
            {
                String curPageUrl = "http://download.csdn.net/my/downloads/" + curPage;
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    AddHistory("获取列表第" + curPage + "页");
                    wb_show.Navigate(curPageUrl);
                });

                String content = null;
                try
                {
                    content = httptool.Get(curPageUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Thread.Sleep(5 * 1000);
                    continue;
                }

                if (content.Contains("请您先登录"))
                {
                    MessageBox.Show("请先登录", "提示");
                    isStart = false;
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                    {
                        bt_start.Content = "开始";
                    });
                    return;
                }

                //开始解析HTML
                HtmlAgilityPack.HtmlDocument hd = new HtmlAgilityPack.HtmlDocument();
                hd.LoadHtml(content);
                HtmlAgilityPack.HtmlNode nodes = hd.DocumentNode.SelectSingleNode("//*[@id='wrap']/div[2]/div[2]/div[2]/dl");

                int index = 0;
                foreach (HtmlAgilityPack.HtmlNode value in nodes.ChildNodes)
                {
                    index++;
                    HtmlAgilityPack.HtmlNode nodebtns = value.SelectSingleNode(String.Format("//dt[{0}]/div[2]/span", index));
                    if (nodebtns != null)
                    {
                        string btns = nodebtns.InnerHtml;
                        if (string.Equals("已评价", btns) == true)
                        {
                            continue;
                        }
                    }

                    HtmlAgilityPack.HtmlNode nodeSourceid = value.SelectSingleNode(String.Format("//dt[{0}]/h3/a", index));
                    if (nodeSourceid == null)
                    {
                        continue;
                    }

                    string href = nodeSourceid.Attributes["href"].Value;

                    string pattern = "/(\\d{2,15})";
                    MatchCollection matches = Regex.Matches(href, pattern);
                    foreach (Match match in matches)
                    {
                        String sourceid = match.Groups[1].Value;
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddHistory(string.Format("正在评论sourceid：{0} {1}", sourceid, nodeSourceid.InnerHtml));
                        }));

                        while (WriteDiscuss(sourceid, curPage) == false)
                        {
                            Thread.Sleep(5 * 1000);
                        }
                    }
                }
                curPage++;

                if (System.Math.Abs(lastDiscusPage - curPage) >= 20)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                    {
                        AddHistory("评论结束");
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
            sb.Append("&sourceid=" + sourceid);
            sb.Append("&content=" + stringGood[curStringIndex]);
            if (string.IsNullOrEmpty(txt_validcode) == false)
            {
                sb.Append("&txt_validcode=" + txt_validcode);

            }
            sb.Append("&rating=5");
            sb.Append("&t=" + epoch + "286");

            curStringIndex++;
            if (curStringIndex >= stringGood.Length)
            {
                curStringIndex = 0;
            }
            String url = sb.ToString();
            String content = httptool.Get(url);

            if (string.IsNullOrEmpty(txt_validcode) == false)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    txt_validcode = "";
                    tb_check.Text = "";
                });
            }

            if (string.IsNullOrEmpty(content) == true)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    AddHistory("评论失败-没有返回内容");
                    wb_show.Refresh();
                });
                isOk = false;
                return isOk;
            }

            content = content.Trim();
            if (content.Length < 2)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    AddHistory("评论失败-返回格式错误");
                    wb_show.Refresh();
                });
                isOk = false;
                return isOk;
            }

            content = content.Substring(1, content.Length - 2);
            DisModel model = JsonConvert.DeserializeObject<DisModel>(content);
            if (model == null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    AddHistory("评论失败-返回格式错误");
                    wb_show.Refresh();
                });
                isOk = false;
                return isOk;
            }

            if (model.succ == 1)
            {
                lastDiscusPage = page;
                isOk = true;
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    AddHistory("评论成功，等待CSDN评论间隔时间65秒");
                    wb_show.Refresh();
                });
                Thread.Sleep(1000 * 65);
            }
            else if (model.succ == -3 || model.succ == -4)
            {
                isOk = false;
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    FlashWindow(hwnd, true);
                    AddHistory("评论失败-" + model.ToString());
                    //获取验证码
                    string urlCheck = "http://download.csdn.net/index.php/rest/tools/validcode/comment_validate/10.1187602692602725";
                    image.Source = httptool.GetImage(urlCheck);
                });

                isPause = true;
                while (isPause == true)
                {
                    Thread.Sleep(1000 * 5);
                }
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    FlashWindow(hwnd, true);
                    AddHistory("评论失败-" + model.ToString());
                    wb_show.Refresh();
                });
                isOk = false;
            }
            return isOk;
        }

        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr handle, bool invert);


        private void AddHistory(String value)
        {
            tb_history.AppendText(value + System.Environment.NewLine);
            tb_history.ScrollToEnd(); //自动滚动到底部
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

        private int failCheck = 0;
        private void bt_check_Click(object sender, RoutedEventArgs e)
        {
            txt_validcode = tb_check.Text.Trim();

            if (string.IsNullOrEmpty(txt_validcode) == true)
            {
                MessageBox.Show("请先输入验证码", "提示");
                return;
            }

            // 检查验证码是否正确
            string urlcheck = "http://download.csdn.net/index.php/comment/check_validcode/" + txt_validcode;
            string content = null;
            try
            {
                content = httptool.Get(urlcheck);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (string.IsNullOrEmpty(content) == true)
            {
                isPause = false;
                tb_check_state.Text = "验证码正确";
                image.Source = null;
                AddHistory("验证码校验正确");
            }
            else
            {
                failCheck++;
                txt_validcode = "";
                tb_check_state.Text = content;
                if (failCheck >= 4)
                {
                    failCheck = 0;
                    //获取验证码
                    string urlCheck = "http://download.csdn.net/index.php/rest/tools/validcode/comment_validate/10.1187602692602725";
                    try
                    {
                        image.Source = httptool.GetImage(urlCheck);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private void tb_check_TextChanged(object sender, TextChangedEventArgs e)
        {
            tb_check_state.Text = "";
        }
    }
}
