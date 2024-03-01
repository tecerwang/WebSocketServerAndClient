namespace WebSocketServer.ServerKernal
{
    public interface IWebSocketLogic
    {
        /// <summary>
        /// 连接打开
        /// </summary>
        /// <param name="cliendId"></param>
        public Task OnClientOpen(string clientId, WebSocketMiddleWare ws);

        /// <summary>
        /// 连接关闭
        /// </summary>
        /// <param name="cliendId"></param>
        public Task OnClientClose(string clientId);

        /// <summary>
        /// 当收到消息
        /// </summary>
        /// <param name="cliendId"></param>
        public Task OnMessageRecieved(string msg);
    }
}
