var WebsocketTSClient;
(function (WebsocketTSClient) {
    class MsgPack {
        constructor() {
        }
    }
    class RequestPack extends MsgPack {
        constructor(clientId) {
            super();
            this.clientId = clientId;
            this.utcTicks = WebsocketTSClient.Utility.UTCNowSeconds();
            this.rid = ++RequestPack.requestId;
        }
        get type() {
            return "request";
        }
    }
    RequestPack.requestId = 0;
    WebsocketTSClient.RequestPack = RequestPack;
    class ResponsePack extends MsgPack {
        get type() {
            return "response";
        }
    }
    WebsocketTSClient.ResponsePack = ResponsePack;
    class NotifyPack extends MsgPack {
        get type() {
            return "notify";
        }
    }
    WebsocketTSClient.NotifyPack = NotifyPack;
})(WebsocketTSClient || (WebsocketTSClient = {}));
