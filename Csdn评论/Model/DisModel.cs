using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Csdn评论.Model
{
    public class DisModel
    {
        public int succ { get; set; }

        public string msg { get; set; }

        public override string ToString()
        {
            return string.Format("succ:{0} msg:{1}", succ, msg);
        }
    }
}
