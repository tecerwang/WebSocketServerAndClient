var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var WebsocketTSClient;
(function (WebsocketTSClient) {
    var MsgPack = /** @class */ (function () {
        function MsgPack() {
        }
        return MsgPack;
    }());
    var RequestPack = /** @class */ (function (_super) {
        __extends(RequestPack, _super);
        function RequestPack(clientId) {
            var _this = _super.call(this) || this;
            _this.clientId = clientId;
            _this.utcTicks = WebsocketTSClient.Utility.UTCNowSeconds();
            _this.rid = ++RequestPack.requestId;
            return _this;
        }
        Object.defineProperty(RequestPack.prototype, "type", {
            get: function () {
                return "request";
            },
            enumerable: false,
            configurable: true
        });
        RequestPack.requestId = 0;
        return RequestPack;
    }(MsgPack));
    WebsocketTSClient.RequestPack = RequestPack;
    var ResponsePack = /** @class */ (function (_super) {
        __extends(ResponsePack, _super);
        function ResponsePack() {
            return _super !== null && _super.apply(this, arguments) || this;
        }
        Object.defineProperty(ResponsePack.prototype, "type", {
            get: function () {
                return "response";
            },
            enumerable: false,
            configurable: true
        });
        return ResponsePack;
    }(MsgPack));
    WebsocketTSClient.ResponsePack = ResponsePack;
    var NotifyPack = /** @class */ (function (_super) {
        __extends(NotifyPack, _super);
        function NotifyPack() {
            return _super !== null && _super.apply(this, arguments) || this;
        }
        Object.defineProperty(NotifyPack.prototype, "type", {
            get: function () {
                return "notify";
            },
            enumerable: false,
            configurable: true
        });
        return NotifyPack;
    }(MsgPack));
    WebsocketTSClient.NotifyPack = NotifyPack;
})(WebsocketTSClient || (WebsocketTSClient = {}));
