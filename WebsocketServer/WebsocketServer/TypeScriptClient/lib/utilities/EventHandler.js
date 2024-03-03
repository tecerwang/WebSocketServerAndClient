var WebsocketTSClient;
(function (WebsocketTSClient) {
    var EventHandler = /** @class */ (function () {
        function EventHandler() {
            this.event = [];
        }
        EventHandler.prototype.AddListener = function (handler) {
            this.event.push(handler);
        };
        EventHandler.prototype.RmListener = function (handler) {
            var index = this.event.indexOf(handler);
            if (index !== -1) {
                this.event.splice(index, 1);
            }
        };
        EventHandler.prototype.Trigger = function () {
            var args = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                args[_i] = arguments[_i];
            }
            this.event.forEach(function (handler) { return handler.apply(void 0, args); });
        };
        return EventHandler;
    }());
    WebsocketTSClient.EventHandler = EventHandler;
})(WebsocketTSClient || (WebsocketTSClient = {}));
