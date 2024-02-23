using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;
using System;
using UIFramework;

namespace WebSocketClient
{
    /// <summary>
    /// 基础数据包
    /// </summary>
    public abstract class MsgPack
    {
        public string clientId;
        public abstract string type { get; }
        public string serviceName;
        public string cmd;
        public JToken data;

        /// <summary>
        /// UTC Ticks，时间由服务器创建
        /// </summary>
        public long utcTicks;

        /// <summary>
        /// time of this data pack was created
        /// </summary>
        public DateTime date => new DateTime(utcTicks, DateTimeKind.Utc);

        public static MsgPack Parse(JObject rawData)
        {
            string dataType = JHelper.GetJsonString(rawData, "type");
            switch (dataType)
            {
                case RequestType:
                    return _Parse<RequestPack>(rawData);

                case ResponseType: 
                    return _Parse<ResponsePack>(rawData);

                case NotifyType: 
                    return _Parse<NotifyPack>(rawData);
                default: 
                    return null;
            }
        }

        protected abstract void ParseInternal(JObject rawData);

        private static T _Parse<T>(JObject rawData) where T : MsgPack
        {
            MsgPack pack = (MsgPack)Activator.CreateInstance(typeof(T));
            pack.clientId = JHelper.GetJsonString(rawData, "clientId");
            pack.serviceName = JHelper.GetJsonString(rawData, "serviceName");
            pack.cmd = JHelper.GetJsonString(rawData, "cmd");
            pack.data = JHelper.GetJsonObject(rawData, "data");
            pack.utcTicks = Utility.UTCNowSeconds();// 服务器创建对象的时间
            pack.ParseInternal(rawData);

            return pack as T;
        }

        public virtual JObject ToJson()
        {
            JObject obj = new JObject();
            obj.Add("clientId", clientId);
            obj.Add("serviceName", serviceName);
            obj.Add("cmd", cmd);
            obj.Add("data", data);
            obj.Add("utcTicks", utcTicks);
            obj.Add("type", type);

            return obj;
        }

        public override string ToString()
        {
            return ToJson().ToString().Replace("\r\n", string.Empty);
        }

        public const string RequestType = "requset";
        public const string ResponseType = "response";
        public const string NotifyType = "notify";
    }

    public class RequestPack : MsgPack
    {
        public int rid;

        public override string type => "requset";

        protected override void ParseInternal(JObject rawData)
        {
            this.rid = JHelper.GetJsonInt(rawData, "rid");
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj.Add("rid", rid);
            return obj;
        }

        public static int requestId = 0;

        public static int GetRequestId()
        {
            return ++requestId;
        }

        
    }

    public class ResponsePack : MsgPack
    {
        public override string type => "response";
        public int rid; // request id;
        public int errCode;

        public static ResponsePack CreateFromRequest(RequestPack request, JObject data, int errCode)
        {
            return new ResponsePack()
            {
                clientId = request.clientId,
                serviceName = request.serviceName,
                rid = request.rid,
                utcTicks = Utility.UTCNowSeconds(),
                data = data,
                errCode = errCode
            };
        }      

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj.Add("rid", rid);
            obj.Add("errCode", errCode);
            return obj;
        }

        protected override void ParseInternal(JObject rawData)
        {
            rid = JHelper.GetJsonInt(rawData, "rid");
            errCode = JHelper.GetJsonInt(rawData, "errCode");
        }
    }

    public class NotifyPack : MsgPack
    {
        public override string type => "notify";

        public override JObject ToJson()
        {
            return base.ToJson();
        }

        protected override void ParseInternal(JObject rawData)
        {
            // nothing specific
        }
    }
}
