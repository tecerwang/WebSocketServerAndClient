using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.Utilities
{
    static class ErrCode
    {
        public static int Unkown = -100;

        public static int OK = 0;

        #region ClientGroupBroadcastService start from 10000
        public static int AlreadyInGroup = 10000;
        public static int NotInGroup = 10001;
        #endregion

        #region MasterSlavesGroupService start from 11000
        /// <summary>
        /// client 已经注册过
        /// </summary>
        public static int AlreadyRegistered = 11000;
        /// <summary>
        /// masterId 为空
        /// </summary>
        public static int MasterIdIsNull = 11001;
        /// <summary>
        /// master name 为空
        /// </summary>
        public static int MasterNameIsNull = 11002;
        /// <summary>
        /// master 已经下线
        /// </summary>
        public static int MasterIsOffline = 11003;
        /// <summary>
        /// 缺失数据包
        /// </summary>
        public static int DataIsNull = 11004;
        #endregion
    }
}
