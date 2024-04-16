var WebsocketTSClient;
(function (WebsocketTSClient) {
    WebsocketTSClient.ErrCode = {
        Internal_RetryTimesOut: -1000,
        Unkown: -100,
        OK: 0,
        // ClientGroupBroadcastService
        AlreadyInGroup: 10000,
        NotInGroup: 10001,
        // MasterSlavesGroupService
        AlreadyRegistered: 11000,
        MasterIdIsNull: 11001,
        MasterNameIsNull: 11002,
        MasterIsOffline: 11003,
        DataIsNull: 11004
    };
})(WebsocketTSClient || (WebsocketTSClient = {}));
