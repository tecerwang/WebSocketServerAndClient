using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketClient
{
    public static class ErrCode
    {
        public static int Unkown = -100;
        /// <summary>
        /// 请求超出重试次数
        /// </summary>
        public static int Internal_RetryTimesOut = -1000;

        public static int OK = 0;

        public static int AlreadyInGroup = 1000;
        public static int NotInGroup = 1000;
    }
}