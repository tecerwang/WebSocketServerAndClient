var WebsocketTSClient;
(function (WebsocketTSClient) {
    class EventHandler {
        constructor() {
            this.event = [];
        }
        AddListener(handler) {
            this.event.push(handler);
        }
        RmListener(handler) {
            const index = this.event.indexOf(handler);
            if (index !== -1) {
                this.event.splice(index, 1);
            }
        }
        Trigger(...args) {
            this.event.forEach(handler => handler(...args));
        }
    }
    WebsocketTSClient.EventHandler = EventHandler;
})(WebsocketTSClient || (WebsocketTSClient = {}));
