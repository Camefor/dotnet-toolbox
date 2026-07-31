using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Camefor.Tools.Extensions
{
    /// <summary>
    /// 描   述  ： 扩展.json序列反序列化                         
    /// 版   本  ： V1.0.0                            
    /// 创 建 人 ： rhyswang                                  
    /// 日    期 ：                         
    /// 创 建 人 ：                                   
    /// 创建时间 ：                                  
    /// 修 改 人 ：                                   
    /// 修改时间 ：                                   
    /// 修改描述 ：                                   
    /// </summary> 

    public static partial class Extensions
    {

        /// <summary>
        ///  JToken对象转换具体值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="containerToken"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T FindFirstTokenValue<T>(this JToken containerToken, string name)
        {
            var r = FindFirstToken(containerToken, name);
            if (r == null) return default(T);
            return r.Value<T>();
        }

        /// <summary>
        /// JToken集合 对象转换具体值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="containerToken"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T FindTokensValues<T>(this JToken containerToken, string name)
        {
            var r = FindTokens(containerToken, name);
            if (r == null) return default(T);
            return r.Value<T>();
        }


        /// <summary>
        /// 根据Json的Key获取包含Value的JToken对象，返回对象集合中的默认一个，不存在返回Null。
        /// </summary>
        /// <param name="containerToken"></param>
        /// <param name="name"></param>
        /// <returns>返回对象集合中的默认一个 JToken </returns>
        public static JToken FindFirstToken(this JToken containerToken, string name)
        {
            return FindTokens(containerToken, name).FirstOrDefault();
        }

        /// <summary>
        /// 根据Json的Key获取包含Value的JToken对象，返回对象集合。
        /// </summary>
        /// <param name="containerToken"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static List<JToken> FindTokens(this JToken containerToken, string name)
        {
            List<JToken> matches = new List<JToken>();
            FindTokens(containerToken, name, matches);
            return matches;
        }

        private static void FindTokens(JToken containerToken, string name, List<JToken> matches)
        {
            if (containerToken.Type == JTokenType.Object)
            {
                foreach (JProperty child in containerToken.Children<JProperty>())
                {
                    if (child.Name == name)
                    {
                        matches.Add(child.Value);
                    }
                    FindTokens(child.Value, name, matches);
                }
            }
            else if (containerToken.Type == JTokenType.Array)
            {
                foreach (JToken child in containerToken.Children())
                {
                    FindTokens(child, name, matches);
                }
            }
        }

        /// <summary>
        /// 转成json对象
        /// </summary>
        /// <param name="json">json字串</param>
        /// <returns></returns>
        public static object ToJson(this string json)
        {
            return json == null ? null : JsonConvert.DeserializeObject(json);
        }
        /// <summary>
        /// 转成json字串
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns></returns>
        public static string ToJson(this object obj)
        {
            var timeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
            return JsonConvert.SerializeObject(obj, timeConverter);
        }
        /// <summary>
        /// 转成json字串
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="datetimeformats">时间格式化</param>
        /// <returns></returns>
        public static string ToJson(this object obj, string datetimeformats)
        {
            var timeConverter = new IsoDateTimeConverter { DateTimeFormat = datetimeformats };
            return JsonConvert.SerializeObject(obj, timeConverter);
        }
        /// <summary>
        /// 字串反序列化成指定对象实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="json">字串</param>
        /// <returns></returns>
        public static T ToObject<T>(this string json)
        {
            return json == null ? default(T) : JsonConvert.DeserializeObject<T>(json);
        }
        /// <summary>
        /// 字串反序列化成指定对象实体(列表)
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="json">字串</param>
        /// <returns></returns>
        public static List<T> ToList<T>(this string json)
        {
            return json == null ? null : JsonConvert.DeserializeObject<List<T>>(json);
        }
        /// <summary>
        /// 字串反序列化成DataTable
        /// </summary>
        /// <param name="json">字串</param>
        /// <returns></returns>
        public static DataTable ToTable(this string json)
        {
            return json == null ? null : JsonConvert.DeserializeObject<DataTable>(json);
        }
        /// <summary>
        /// 字串反序列化成linq对象
        /// </summary>
        /// <param name="json">字串</param>
        /// <returns></returns>
        public static JObject ToJObject(this string json)
        {
            return json == null ? JObject.Parse("{}") : JObject.Parse(json.Replace("&nbsp;", ""));
        }
    }
}
