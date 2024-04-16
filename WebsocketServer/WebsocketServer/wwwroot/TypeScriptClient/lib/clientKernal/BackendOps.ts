namespace WebsocketTSClient
{
    export class BackendOps
    {
        /**
        * 加入分组
        */
        public static Cmd_JoinGroup: string = "JoinGroup";

        /**
        * 离开分组
        */
        public static Cmd_LeaveGroup: string = "LeaveGroup";

        /**
        * 广播消息
        */
        public static Cmd_BroadcastMsg: string = "BroadcastMsg";

        /**
          * 注册一个 Master
          */
        public static Cmd_RegisterAsListener: string = "RegisterAsListener";

        /**
        * 注销一个 Master
        */
        public static Cmd_UnregisterFromListener: string = "UnregisterFromListener";

        /**
        * 注册一个 Master
        */
        public static Cmd_RegisterAsMaster: string = "RegisterAsMaster";

        /**
        * 注销一个 Master
        */
        public static Cmd_UnregisterFromMaster: string = "UnregisterFromMaster";

        /**
        * 当Master集合发生变化，增加，删减，修改
        */
        public static Notify_OnMasterCollectionChanged: string = "OnMasterCollectionChanged";

        /**
        * 获取所有master
        */
        public static Cmd_GetAllMasters: string = "GetAllMasters";

        /**
        * 注册一个slave
        */
        public static Cmd_RegisterAsSlave: string = "RegisterAsSlave";

        /**
        * 注销一个slave
        */
        public static Cmd_UnregisterFromSlave: string = "UnregisterFromSlave";

        /**
        * 发送消息
        */
        public static Cmd_Broadcast: string = "Broadcast";

        /**
        * 网络心跳
        */
        public static WSPing: string = "WSPing";
    }
}