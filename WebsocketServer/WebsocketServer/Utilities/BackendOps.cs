﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.Utilities
{
    static class BackendOps
    {
        /// <summary>
        /// 加入分组
        /// </summary>
        public const string Cmd_JoinGroup = "JoinGroup";

        /// <summary>
        /// 离开分组
        /// </summary>
        public const string Cmd_LeaveGroup = "LeaveGroup";

        /// <summary>
        /// 广播消息
        /// </summary>
        public const string Cmd_BroadcastMsg = "BroadcastMsg";

        /// <summary>
        /// 网络心跳
        /// </summary>
        public const string WSPing = "WSPing";
    }
}
