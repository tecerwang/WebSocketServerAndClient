using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.Utilities
{
    static class BackendOps
    {
        #region ClientGroupBroadcastService
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
        #endregion

        #region CentralControllerService
        /// <summary>
        /// 注册成为监听者，监听服务事件
        /// </summary>
        public static string Cmd_RegisterAsListener = "RegisterAsListener";
        /// <summary>
        /// 从监听者注销
        /// </summary>
        public static string Cmd_UnregisterFromListener = "UnregisterFromListener";
        /// <summary>
        /// 注册一个 Master
        /// </summary>
        public static string Cmd_RegisterAsMaster = "RegisterAsMaster";
        /// <summary>
        /// 注销一个 Master
        /// </summary>
        public static string Cmd_UnregisterFromMaster = "UnregisterFromMaster";
        /// <summary>
        /// 当Master集合发生变化，增加，删减，修改
        /// </summary>
        public static string Notify_OnMasterCollectionChanged = "OnMasterCollectionChanged";
        /// <summary>
        /// 获取所有master
        /// </summary>
        public static string Cmd_GetAllMasters = "GetAllMasters";
        /// <summary>
        /// 注册一个slave
        /// </summary>
        public static string Cmd_RegisterAsSlave = "RegisterAsSlave";
        /// <summary>
        /// 注销一个slave
        /// </summary>
        public static string Cmd_UnregisterFromSlave = "UnregisterFromSlave";
        /// <summary>
        /// 发送消息
        /// </summary>
        public static string Cmd_Broadcast = "Broadcast";
        #endregion

        /// <summary>
        /// 网络心跳
        /// </summary>
        public const string WSPing = "WSPing";
    }
}
