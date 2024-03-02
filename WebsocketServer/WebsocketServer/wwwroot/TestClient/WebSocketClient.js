class WebSocketClient {
    constructor(url, clientId, onOpen, onMessage, onClose, onError) {
        this.url = `${url}?clientId=${clientId}`;
        this.clientId = clientId;
        this.onOpen = onOpen;
        this.onMessage = onMessage;
        this.onClose = onClose;
        this.onError = onError;
        this.connect();
    }

    /// 请求索引
    static rid = 1;

    connect() {
        this.socket = new WebSocket(this.url);

        this.socket.addEventListener("open", (event) => {
            console.log("WebSocket connection opened:", event);
            if (this.onOpen) {
                this.onOpen(event);
            }
        });

        this.socket.addEventListener("message", (event) => {       
            if (this.onMessage) {                
                this.onMessage(event.data);
            }
        });

        this.socket.addEventListener("close", (event) => {
            console.log("WebSocket connection closed:", event);
            if (this.onClose) {
                this.onClose(event);
            }
        });

        this.socket.addEventListener("error", (event) => {
            console.error("WebSocket connection error:", event);
            if (this.onError) {
                this.onError(event);
            }
        });
    }

    isConnected() {
        return this.socket && this.socket.readyState === WebSocket.OPEN;
    }

    sendMessage(message) {
        if (this.isConnected()) {
            try {
                this.socket.send(message);
                WebSocketClient.rid++;
                console.log("Message sent:", message);
            } catch (error) {
                console.error("Error sending message:", error.message);
            }
        } else {
            console.error("WebSocket connection is not open.");
        }
    }

    disconnect() {
        if (this.isConnected()) {
            this.socket.close();
        } else {
            console.warn("WebSocket is not connected.");
        }
    }    
}

class DataPack {
    constructor(clientId, type, serviceName, msg, timeStamp) {
      this.clientId = clientId;
      this.type = type;
      this.serviceName = serviceName;
      this.data = msg;
      this.timeStamp = timeStamp;
    }
  }
  
  class RequestPack extends DataPack {
    constructor(clientId, serviceName, msg, timeStamp) {
      super(clientId, "request", serviceName, msg, timeStamp);
      this.rid = WebSocketClient.rid;
    }
  
    static deserialize(rawMsg) {
      try {
        return JSON.parse(rawMsg);
      } catch (ex) {
        console.error(`Error deserializing message: ${ex.message}`);
        return null;
      }
    }
  
    static serialize(pack) {
      try {
        return JSON.stringify(pack);
      } catch (ex) {
        console.error(`Error serializing message: ${ex.message}`);
        return null;
      }
    }
  }
  
  class ResponsePack extends DataPack {
    constructor(clientId, serviceName, msg, timeStamp, rid, errCode) {
      super(clientId, "response", serviceName, msg, timeStamp);
      this.rid = rid;
      this.errCode = errCode;
    }
  
    static createFromRequest(request, msg, errCode) {
      return new ResponsePack(
        request.clientId,
        request.serviceName,
        msg,
        new Date().toISOString(),
        request.rid,
        errCode
      );
    }
  
    static deserialize(rawMsg) {
      try {
        return JSON.parse(rawMsg);
      } catch (ex) {
        console.error(`Error deserializing message: ${ex.message}`);
        return null;
      }
    }
  
    static serialize(pack) {
      try {
        return JSON.stringify(pack);
      } catch (ex) {
        console.error(`Error serializing message: ${ex.message}`);
        return null;
      }
    }
  }
  
  class NotifyPack extends DataPack {
    constructor(clientId, serviceName, msg, timeStamp) {
      super(clientId, "notify", serviceName, msg, timeStamp);
    }
  
    static deserialize(rawMsg) {
      try {
        return JSON.parse(rawMsg);
      } catch (ex) {
        console.error(`Error deserializing message: ${ex.message}`);
        return null;
      }
    }
  
    static serialize(pack) {
      try {
        return JSON.stringify(pack);
      } catch (ex) {
        console.error(`Error serializing message: ${ex.message}`);
        return null;
      }
    }
  }
  