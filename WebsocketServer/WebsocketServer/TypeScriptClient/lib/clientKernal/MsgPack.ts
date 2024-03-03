namespace WebsocketTSClient
{
    abstract class MsgPack
    {
        public clientId: string;
        public serviceName?: string | null;
        public data?: JSON | null;
        public cmd?: string | null;
        public utcTicks: number;

        public abstract type: string;

        constructor()
        {
        }
    }
    export class RequestPack extends MsgPack
    {

        rid: number;
        private static requestId: number = 0

        constructor(clientId: string)
        {
            super();
            this.clientId = clientId;
            this.utcTicks = Utility.UTCNowSeconds();
            this.rid = ++RequestPack.requestId;
        }

        get type(): string
        {
            return "request";
        }
    }

    export class ResponsePack extends MsgPack
    {
        rid?: number | null;
        errCode?: number | null;

        get type(): string
        {
            return "response";
        }
    }

    export class NotifyPack extends MsgPack
    {
        get type(): string
        {
            return "notify";
        }
    }
}

