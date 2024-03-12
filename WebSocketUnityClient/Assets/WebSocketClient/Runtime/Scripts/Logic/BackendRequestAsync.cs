using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketClient;

/// <summary>
/// BackendRequest 的异步版本
/// </summary>
public class BackendRequestAsync : IDisposable
{
    private struct RequestContext
    {
        public string serviceName;
        public string cmd;
        public JToken data;
        /// <summary>
        /// 默认为 -1，代表未发起有效请求
        /// </summary>
        public int rid;
    }

    public struct Response
    {
        public int errCode;
        public JToken data;
    }

    /// <summary>
    /// 记录请求信息，断线重连后使用
    /// </summary>
    private RequestContext _requestContext;

    private TaskCompletionSource<Response> _requestCompleteSource = null;

    public BackendRequestAsync(string serviceName, string cmd, JToken data)
    {
        WSBackend.singleton.OnBackendStateChanged += Singleton_OnBackendStateChanged;
        WSBackend.singleton.OnBackendResponse += Singleton_OnBackendResponse;

        _requestContext = new RequestContext()
        {
            serviceName = serviceName,
            cmd = cmd,
            data = data,
            rid = -1
        };
    }

    public void Dispose()
    {
        WSBackend.singleton.OnBackendStateChanged -= Singleton_OnBackendStateChanged;
        WSBackend.singleton.OnBackendResponse -= Singleton_OnBackendResponse;     
    }

    private void Singleton_OnBackendStateChanged()
    {
        /// 连接断开，等待消息的上下文需要知道
        if (WSBackend.singleton.State == WSBackend.WSBackendState.Close && _requestCompleteSource != null)
        {
            _requestCompleteSource.SetResult(new Response()
            {
                errCode = ErrCode.Internal_ConnectionClose,
                data = null
            });
            _requestCompleteSource = null;
        }
    }

    private void Singleton_OnBackendResponse(ResponsePack response)
    {
        if (response.rid == _requestContext.rid && _requestCompleteSource != null)
        {
            _requestCompleteSource.SetResult(new Response() 
            {
                 data = response.data,
                 errCode = response.errCode
            });
        }
    }

    public async Task<Response> Request()
    {
        if (WSBackend.singleton != null && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
        {
            var errCode = ErrCode.Internal_Error;
            var result = await WSBackend.singleton.CreateBackendRequest(_requestContext.serviceName, _requestContext.cmd, _requestContext.data);
            if (result.state == WebSocketClient.WebSocketClient.SendMsgState.Sent)
            {
                _requestContext.rid = result.rid;
                _requestCompleteSource = new TaskCompletionSource<Response>();
                /// 如果消息发出 等待回执，或者连接可能断开
                return await _requestCompleteSource.Task;
            }
            else
            {
                switch (result.state)
                {
                    case WebSocketClient.WebSocketClient.SendMsgState.Timeout:
                        {
                            errCode = ErrCode.Internal_RetryTimeout;
                            break;
                        }
                    case WebSocketClient.WebSocketClient.SendMsgState.InteruptByConnectionClose:
                        {
                            errCode = ErrCode.Internal_ConnectionClose;
                            break;
                        }
                }
                return new Response()
                {
                    errCode = errCode,
                    data = null
                };
            }
        }
        else
        {
            // 告诉前端，连接已经断开
            return new Response()
            {
                errCode = ErrCode.Internal_ConnectionClose,
                data = null
            };
        }
    }
}
