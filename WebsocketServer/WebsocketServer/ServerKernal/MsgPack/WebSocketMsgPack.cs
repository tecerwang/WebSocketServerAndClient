using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketServer.Utilities;

namespace WebSocketServer.ServerKernal.MsgPack
{
    /// <summary>
    /// 基础数据包
    /// </summary>
    internal abstract class MsgPack
    {
        public required string clientId;
        public abstract string type { get; }
        public string? serviceName;
        public JToken? data;
        public string? cmd;
        /// <summary>
        /// UTC Ticks
        /// </summary>
        public long utcTicks;

        /// <summary>
        /// time of this data pack was created
        /// </summary>
        public DateTime date => new DateTime(utcTicks, DateTimeKind.Utc);

        public static T? Parse<T>(JObject rawData) where T : MsgPack
        {
            try
            {
                var clientId = JHelper.GetJsonString(rawData, "clientId");
                if (string.IsNullOrEmpty(clientId))
                {
                    return null;
                }
                MsgPack? pack = (MsgPack?)Activator.CreateInstance(typeof(T));
                if (pack != null)
                {
                    pack.clientId = clientId;
                    pack.serviceName = JHelper.GetJsonString(rawData, "serviceName");
                    pack.cmd = JHelper.GetJsonString(rawData, "cmd");
                    pack.data = JHelper.GetJsonObject(rawData, "data");
                    pack.utcTicks = Utility.UTCNowSeconds();// 服务器创建对象的时间

                    return pack as T;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                DebugLog.Print($"[DataPack] Parse Exception {ex}");
                return null;
            }
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
    }

    internal class RequestPack : MsgPack
    {
        public int rid { get; private set; }

        public override string type => "requset";

        public static RequestPack? Parse(JObject rawData)
        {
            RequestPack? pack = Parse<RequestPack>(rawData);
            if (pack != null)
            {
                pack.rid = JHelper.GetJsonInt(rawData, "rid");
            }
            return pack;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj.Add("rid", rid);
            return obj;
        }
    }

    internal class ResponsePack : MsgPack
    {
        public override string type => "response";
        public int? rid; // request id;
        public int? errCode;

        public static ResponsePack CreateFromRequest(RequestPack request, JToken? data, int errCode)
        {
            return new ResponsePack()
            {
                clientId = request.clientId,
                serviceName = request.serviceName,
                cmd = request.cmd,
                rid = request.rid,
                utcTicks = Utility.UTCNowSeconds(),
                data = data,
                errCode = errCode
            };
        }

        public static ResponsePack? Parse(JObject rawData)
        {
            ResponsePack? pack = Parse<ResponsePack>(rawData);
            if (pack != null)
            {
                pack.rid = JHelper.GetJsonInt(rawData, "rid");
                pack.errCode = JHelper.GetJsonInt(rawData, "errCode");
            }
            return pack;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj.Add("rid", rid);
            obj.Add("errCode", errCode);
            return obj;
        }
    }

    internal class NotifyPack : MsgPack
    {
        public override string type => "notify";
        public static NotifyPack? Parse(JObject rawData)
        {
            NotifyPack? pack = Parse<NotifyPack>(rawData); ;
            return pack;
        }

        public override JObject ToJson()
        {
            return base.ToJson();
        }
    }
}
