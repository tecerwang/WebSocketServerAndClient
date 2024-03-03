var WebsocketTSClient;
(function (WebsocketTSClient) {
    var BackendOps = /** @class */ (function () {
        function BackendOps() {
        }
        /**
        * 加入分组
        */
        BackendOps.Cmd_JoinGroup = "JoinGroup";
        /**
        * 离开分组
        */
        BackendOps.Cmd_LeaveGroup = "LeaveGroup";
        /**
        * 广播消息
        */
        BackendOps.Cmd_BroadcastMsg = "BroadcastMsg";
        /**
        * 注册一个 Master
        */
        BackendOps.Cmd_RegisterAsMaster = "RegisterAsMaster";
        /**
        * 注销一个 Master
        */
        BackendOps.Cmd_UnregisterFromMaster = "UnregisterFromMaster";
        /**
        * 当Master集合发生变化，增加，删减，修改
        */
        BackendOps.Notify_OnMasterCollectionChanged = "OnMasterCollectionChanged";
        /**
        * 获取所有master
        */
        BackendOps.Cmd_GetAllMasters = "GetAllMasters";
        /**
        * 注册一个slave
        */
        BackendOps.Cmd_RegisterAsSlave = "RegisterAsSlave";
        /**
        * 注销一个slave
        */
        BackendOps.Cmd_UnregisterFromSlave = "UnregisterFromSlave";
        /**
        * 发送消息
        */
        BackendOps.Cmd_Broadcast = "Broadcast";
        /**
        * 网络心跳
        */
        BackendOps.WSPing = "WSPing";
        return BackendOps;
    }());
    WebsocketTSClient.BackendOps = BackendOps;
})(WebsocketTSClient || (WebsocketTSClient = {}));
