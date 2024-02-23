using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.Utilities
{
    public class JHelper
    {
        public interface IParser
        {
            void ParseJson(JToken data);
        }

        public static void AddJsonNotNull(JObject jObject, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                jObject.Add(key, value);
            }
        }

        public static int GetJsonNonNegInt(JToken jObject, string key, int defValue = 0)
        {
            JToken? token = GetJsonByType(jObject, key, JTokenType.Integer);
            return token != null ? Math.Max(0, (int)token) : defValue;
        }

        public static int GetJsonInt(JToken jObject, string key, int defValue = 0)
        {
            JToken? token = GetJsonByType(jObject, key, JTokenType.Integer);
            return token != null ? (int)token : defValue;
        }

        public static long GetJsonNonNegLong(JToken jObject, string key, long defValue = 0)
        {
            JToken? token = GetJsonByType(jObject, key, JTokenType.Integer);
            return token != null ? ((long)token >= 0 ? (long)token : 0) : defValue;
        }

        public static long GetJsonLong(JToken jObject, string key, long defValue = 0)
        {
            JToken? token = GetJsonByType(jObject, key, JTokenType.Integer);
            return token != null ? (long)token : defValue;
        }

        public static float GetJsonFloat(JToken jObject, string key, float defValue = 0)
        {
            JToken? token = GetJsonByType(jObject, key, JTokenType.Float);
            if (token != null)
            {
                return (float)token;
            }

            token = GetJsonByType(jObject, key, JTokenType.Integer);
            if (token != null)
            {
                return (long)token;
            }

            return defValue;
        }

        public static bool GetJsonBool(JToken jObject, string key, bool defValue = false)
        {
            JToken? token = GetJsonByType(jObject, key, JTokenType.Boolean);
            return token != null ? (bool)token : defValue;
        }

        public static string? GetJsonString(JToken jObject, string key, string defValue = "")
        {
            JToken? token = GetJsonByType(jObject, key, JTokenType.String);
            return token != null ? (string?)token : defValue;
        }

        public static JArray? GetJsonArray(JToken jObject, string key)
        {
            return GetJsonByType(jObject, key, JTokenType.Array) as JArray;
        }

        public static JArray? GetJsonArrayOrEmpty(JToken jObject, string key)
        {
            JArray? a = GetJsonArray(jObject, key);
            if (a == null)
            {
                a = new JArray();
            }
            return a;
        }

        public static int[]? GetJsonIntArray(JToken token, string key, int[]? defaultArray = null)
        {
            JArray? a = GetJsonArray(token, key);
            if (a == null)
            {
                return defaultArray;
            }

            int[] arr = new int[a.Count];
            for (int i = 0; i < a.Count; ++i)
            {
                arr[i] = JsonIntValue(a[i]);
            }
            return arr;
        }

        public static int[] GetJsonIntArrayOrEmpty(JToken token, string key)
        {
            JArray? a = GetJsonArray(token, key);
            if (a == null)
            {
                return new int[0];
            }

            int[] arr = new int[a.Count];
            for (int i = 0; i < a.Count; ++i)
            {
                arr[i] = JsonIntValue(a[i]);
            }
            return arr;
        }

        public static long[]? GetJsonLongArray(JToken token, string key)
        {
            JArray? a = GetJsonArray(token, key);
            if (a == null)
            {
                return null;
            }

            long[] arr = new long[a.Count];
            for (int i = 0; i < a.Count; ++i)
            {
                arr[i] = JsonLongValue(a[i]);
            }
            return arr;
        }

        public static long[] GetJsonLongArrayOrEmpty(JToken token, string key)
        {
            JArray? a = GetJsonArray(token, key);
            if (a == null)
            {
                return new long[0];
            }

            long[] arr = new long[a.Count];
            for (int i = 0; i < a.Count; ++i)
            {
                arr[i] = JsonLongValue(a[i]);
            }
            return arr;
        }

        public static float[] GetJsonFloatArrayOrEmpty(JToken token, string key)
        {
            JArray? a = GetJsonArray(token, key);
            if (a == null)
            {
                return new float[0];
            }

            float[] arr = new float[a.Count];
            for (int i = 0; i < a.Count; ++i)
            {
                arr[i] = JsonFloatValue(a[i]);
            }
            return arr;
        }

        public static string?[]? GetJsonStringArray(JToken token, string key)
        {
            JArray? a = GetJsonArray(token, key);
            if (a == null)
            {
                return null;
            }

            string?[] arr = new string?[a.Count];
            for (int i = 0; i < a.Count; ++i)
            {
                arr[i] = JsonStringValue(a[i]);
            }
            return arr;
        }

        public static string?[] GetJsonStringArrayOrEmpty(JToken token, string key)
        {
            JArray? a = GetJsonArray(token, key);
            if (a == null)
            {
                return new string?[0];
            }

            string?[] arr = new string?[a.Count];
            for (int i = 0; i < a.Count; ++i)
            {
                arr[i] = JsonStringValue(a[i]);
            }
            return arr;
        }

        public static T GetJsonIntEnum<T>(JToken token, string key, T defValue) where T : struct
        {
            JToken? t = GetJsonByType(token, key, JTokenType.Integer);
            if (t != null)
            {
                T ret;
                if (System.Enum.TryParse(((int)t).ToString(), out ret))
                {
                    return ret;
                }
            }
            return defValue;
        }

        public static T[]? GetJsonIntEnumArray<T>(JToken token, string key, T defValue) where T : struct
        {
            int[]? intArray = GetJsonIntArray(token, key);
            if (intArray != null)
            {
                T[] enumArray = new T[intArray.Length];
                for (int i = 0; i < enumArray.Length; ++i)
                {
                    if (System.Enum.TryParse(intArray[i].ToString(), out T ret))
                    {
                        enumArray[i] = ret;
                    }
                    else
                    {
                        enumArray[i] = defValue;
                    }
                }
                return enumArray;
            }
            return null;
        }

        public static T GetJsonStringEnum<T>(JToken token, string key, T defValue) where T : struct
        {
            JToken? t = GetJsonByType(token, key, JTokenType.String);
            if (t != null)
            {
                T ret;
                if (System.Enum.TryParse((string?)t, out ret))
                {
                    return ret;
                }
            }
            return defValue;
        }

        public static JToken? GetJsonToken(JToken? jToken, string path)
        {
            if (jToken == null || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string[] segments = path.Split('/');
            if (segments != null && segments.Length > 0)
            {
                int idx = 0;
                while (idx < segments.Length)
                {
                    JObject? jobj = jToken as JObject;
                    if (jobj == null)
                    {
                        jToken = null;
                        break;
                    }

                    string s = segments[idx++].Trim();
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        jToken = null;
                        break;
                    }

                    if (!jobj.TryGetValue(s, out jToken))
                    {
                        jToken = null;
                        break;
                    }
                }
                return jToken;
            }
            return null;
        }

        public static JObject? GetJsonObject(JToken jObject, params string[] keys)
        {
            JObject? ret = jObject as JObject;
            if (keys != null)
            {
                for (int i = 0; i < keys.Length && ret != null; ++i)
                {
                    ret = GetJsonByType(ret, keys[i], JTokenType.Object) as JObject;
                }
            }
            return ret;
        }

        private static JToken? GetJsonByType(JToken jObject, string key, JTokenType type)
        {
            if (jObject is JObject)
            {
                ((JObject)jObject).TryGetValue(key, out JToken? token);
                if (token != null && token.Type == type)
                {
                    return token;
                }
            }
            return null;
        }

        public static int JsonIntValue(JToken token, int defValue = 0)
        {
            if (token != null && token.Type == JTokenType.Integer)
            {
                return (int)token;
            }
            return defValue;
        }

        public static long JsonLongValue(JToken token, long defValue = 0)
        {
            if (token != null && token.Type == JTokenType.Integer)
            {
                return (long)token;
            }
            return defValue;
        }

        public static float JsonFloatValue(JToken token, float defValue = 0)
        {
            if (token != null)
            {
                if (token.Type == JTokenType.Float)
                {
                    return (float)token;
                }
                else if (token.Type == JTokenType.Integer)
                {
                    return (long)token;
                }
            }
            return defValue;
        }

        public static double JsonDoubleValue(JToken token, float defValue = 0)
        {
            if (token != null)
            {
                if (token.Type == JTokenType.Float)
                {
                    return (double)token;
                }
                else if (token.Type == JTokenType.Integer)
                {
                    return (long)token;
                }
            }
            return defValue;
        }

        public static string? JsonStringValue(JToken token, string defValue = "")
        {
            if (token != null && token.Type == JTokenType.String)
            {
                return (string?)token;
            }
            return defValue;
        }

        public static bool JsonBooleanValue(JToken token, bool defValue = false)
        {
            if (token != null && token.Type == JTokenType.Boolean)
            {
                return (bool)token;
            }
            return defValue;
        }

        public static JArray? JsonArrayValue(JToken token)
        {
            if (token != null && token.Type == JTokenType.Array)
            {
                return token as JArray;
            }
            return null;
        }

        public static JObject? JsonObjectValue(JToken token)
        {
            if (token != null && token.Type == JTokenType.Object)
            {
                return token as JObject;
            }
            return null;
        }

        public struct PropPair
        {
            public string key;
            public JToken? value;

            public PropPair(string k, JToken v)
            {
                key = k;
                value = v;
            }
        }

        public static JArray? MakeIntArray(ICollection<int> arr)
        {
            JArray ja = new JArray();
            if (arr != null)
            {
                foreach (int a in arr)
                {
                    ja.Add(a);
                }
            }
            return ja;
        }

        public static JArray? MakeIntArray(int[] arr)
        {
            JArray ja = new JArray();
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; ++i)
                {
                    ja.Add(arr[i]);
                }
            }
            return ja;
        }

        public static JArray? MakeStringArray(string?[] arr)
        {
            JArray ja = new JArray();
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; ++i)
                {
                    ja.Add(arr[i]);
                }
            }
            return ja;
        }

        public static JObject MakeData(string p1, JToken v1)
        {
            JObject o = new JObject();
            o.Add(p1, v1);
            return o;
        }

        public static JObject MakeData(string p1, JToken v1, string p2, JToken v2)
        {
            JObject o = new JObject();
            o.Add(p1, v1);
            o.Add(p2, v2);
            return o;
        }

        public static JObject MakeData(string p1, JToken v1, string p2, JToken v2, string p3, JToken v3)
        {
            JObject o = new JObject();
            o.Add(p1, v1);
            o.Add(p2, v2);
            o.Add(p3, v3);
            return o;
        }

        public static JObject MakeData(string p1, JToken v1, string p2, JToken v2, string p3, JToken v3, string p4, JToken v4)
        {
            JObject o = new JObject();
            o.Add(p1, v1);
            o.Add(p2, v2);
            o.Add(p3, v3);
            o.Add(p4, v4);
            return o;
        }
    }

    public struct JObjectWrapper
    {
        public JObject jobject;

        public JObjectWrapper(JObject jObject)
        {
            jobject = jObject;
        }

        public int PropInt(string key, int defValue = 0)
        {
            return JHelper.GetJsonInt(jobject, key, defValue);
        }

        public long PropLong(string key, long defValue = 0)
        {
            return JHelper.GetJsonLong(jobject, key, defValue);
        }

        public string? PropString(string key, string defValue = "")
        {
            return JHelper.GetJsonString(jobject, key, defValue);
        }

        public bool PropBoolean(string key, bool defValue = false)
        {
            return JHelper.GetJsonBool(jobject, key, defValue);
        }

        public JObject? PropObject(string key)
        {
            return JHelper.GetJsonObject(jobject, key);
        }

        public JArray? PropArray(string key)
        {
            return JHelper.GetJsonArray(jobject, key);
        }

        public T PropStringEnum<T>(string key, T defValue) where T : struct
        {
            JToken? t = JHelper.GetJsonString(jobject, key);
            if (t != null)
            {
                T ret;
                if (System.Enum.TryParse((string?)t, out ret))
                {
                    return ret;
                }
            }
            return defValue;
        }
    }
}
