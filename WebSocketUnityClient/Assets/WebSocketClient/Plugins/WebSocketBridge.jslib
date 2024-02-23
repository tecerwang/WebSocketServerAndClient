mergeInto(LibraryManager.library, {
    WebSocket_Connect: function(urlPtr, length) {
        var url = UTF8ToString(urlPtr, length); // Convert the pointer to a string
        var ws = new WebSocket(url);

        // Handle WebSocket events
        ws.onopen = function(event) {
            window.unityInstance.SendMessage('Test', 'OnWebSocketOpen', '');
        };

        ws.onmessage = function(event) {
            window.unityInstance.SendMessage('Test', 'OnWebSocketMessage', event.data);
        };

        ws.onerror = function(event) {
            window.unityInstance.SendMessage('Test', 'OnWebSocketError', event.message);
        };

        ws.onclose = function(event) {
            window.unityInstance.SendMessage('Test', 'OnWebSocketClose', event.code.toString());
        };
        return ws;
    },

    WebSocket_Send: function(ws, message) {
        ws.send(message);
    },

    WebSocket_Close: function(ws) {
        ws.close();
    }
});