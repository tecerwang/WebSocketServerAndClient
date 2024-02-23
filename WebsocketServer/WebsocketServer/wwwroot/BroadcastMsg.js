class BroadcastMsg {
    constructor(groupId, cmd, msg) {       
        this.groupId = groupId;
        this.cmd = cmd;
        this.data = msg;
    }

    static serviceName = "ClientGroupBroadcastService";

    static joinGroup(gid) {
        return new RequestPack(clientId,
            BroadcastMsg.serviceName,
            new BroadcastMsg(gid, "JoinGroup", null),
            new Date());
    }

    static leaveGroup(gid) {
        return new RequestPack(clientId,
            BroadcastMsg.serviceName,
            new BroadcastMsg(gid, "LeaveGroup", null),
            new Date());
    }

    static broadcast(gid, msg)
    {
        return new RequestPack(clientId,
            BroadcastMsg.serviceName,
            new BroadcastMsg(gid, "BroadcastMsg", msg),
            new Date());
    }

    static handleBroadcastData(data)
    {
        if(data.type === "notify")
        {
            
        }
        else if(data.type === "response")
        {

        }
    }
}