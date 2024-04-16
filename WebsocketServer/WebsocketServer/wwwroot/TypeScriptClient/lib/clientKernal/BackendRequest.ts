namespace WebsocketTSClient
{

    export type BackendRequestResp = (errCode: number, data: any, context: any) => void;

    interface RequestContext
    {
        serviceName: string;
        cmd: string;
        data: any;
        context: any;
    }

    export class BackendRequest
    {
        private _responseDel: BackendRequestResp | undefined;
        private _requestContext: RequestContext | undefined;
        /// <summary>
        /// 默认为 -1，代表未发起有效请求
        /// </summary>
        private _rid: number = -1;
        private _retryTimes: number = 0;
        private _retryCount: number = 0;

        constructor()
        {
            WSBackend.singleton.OnStateChanged.AddListener(this.Singleton_OnBackendStateChanged);
            WSBackend.singleton.OnResponse.AddListener(this.Singleton_OnBackendResponse);
        }

        private Singleton_OnBackendStateChanged = (state: boolean): void =>
        {
            if (this._rid < 0 && state)
            {
                this.Request(this._requestContext!);
            }
        };

        private Singleton_OnBackendResponse = (resp: ResponsePack): void =>
        {
            if (this._rid > 0 && resp.rid === this._rid)
            {
                this._responseDel?.(resp.errCode, resp.data, this._requestContext?.context);
                this.Release();
            }
        };

        private Request(context: RequestContext): void
        {
            if (this._retryTimes < 0 || this._retryCount < this._retryTimes)
            {
                const rid = WSBackend.singleton.CreateBackendRequest(context.serviceName, context.cmd, context.data);
                this._retryCount++;
                this._rid = rid ?? -1;
            } else
            {
                this._responseDel?.(ErrCode.Internal_RetryTimesOut, null, this._requestContext?.context);
                this.Release();
            }
        }

        private Release(): void
        {
            WSBackend.singleton.OnStateChanged.RmListener(this.Singleton_OnBackendStateChanged);
            WSBackend.singleton.OnResponse.RmListener(this.Singleton_OnBackendResponse);
        }

        public static CreateRetry(serviceName: string, cmd: string, data: any, context: any, onResponse: BackendRequestResp, retryTimes: number = -1): boolean
        {
            if (WSBackend.singleton != null && WSBackend.singleton != undefined && WSBackend.singleton.IsConnected())
            {
                const request = new BackendRequest();
                request._responseDel = onResponse;
                request._retryTimes = retryTimes;
                request._requestContext = {
                    serviceName,
                    cmd,
                    data,
                    context
                };
                try
                {
                    request.Request(request._requestContext);
                    return true;
                }
                catch (ex)
                {
                    // 如果出错需要释放掉这个 request
                    request.Release();
                    Utility.LogDebug(ex);
                }
            }
            onResponse?.(ErrCode.Internal_RetryTimesOut, null, context);
            return false;
        }
    }
}