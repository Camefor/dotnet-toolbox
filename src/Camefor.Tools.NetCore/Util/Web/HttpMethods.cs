using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Camefor.Tools.Util.Web
{
    /// <summary>
    /// 描   述  ： Http请求方法                         
    /// 版   本  ： V1.0.0                            
    /// 创 建 人 ： rhyswang                                  
    /// 日    期 ：                         
    /// 创 建 人 ：                                   
    /// 创建时间 ：                                  
    /// 修 改 人 ：                                   
    /// 修改时间 ：                                   
    /// 修改描述 ：                                   
    /// </summary> 

    public class HttpMethods
    {

        /// <summary>
        /// 创建HttpClient
        /// </summary>
        /// <returns></returns>
        public static HttpClient CreateHttpClient(string url, IDictionary<string, string> cookies = null)
        {
            HttpClient httpclient;
            HttpClientHandler handler = new HttpClientHandler();
            var uri = new Uri(url);
            if (cookies != null)
            {
                foreach (var key in cookies.Keys)
                {
                    string one = key + "=" + cookies[key];
                    handler.CookieContainer.SetCookies(uri, one);
                }
            }
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                httpclient = new HttpClient(handler);
            }
            else
            {
                httpclient = new HttpClient(handler);
            }
            return httpclient;
        }
        /// <summary>
        /// post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="jsonData">请求参数</param>
        /// <returns></returns>
        public static string Post(string url, string jsonData)
        {
            HttpClient httpClient = CreateHttpClient(url);
            var postData = new StringContent(jsonData);
            postData.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            Task<string> result = httpClient.PostAsync(url, postData).Result.Content.ReadAsStringAsync();
            return result.Result;
        }
        /// <summary>
        /// post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <returns></returns>
        public static string Post(string url)
        {
            HttpClient httpClient = CreateHttpClient(url);
            var postData = new StringContent("");
            postData.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            Task<string> result = httpClient.PostAsync(url, postData).Result.Content.ReadAsStringAsync();
            return result.Result;
        }

        /// <summary>
        /// post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="req">请求参数</param>
        /// <returns></returns>
        public static string Post(string url, byte[] req)
        {
            HttpClient httpClient = CreateHttpClient(url);
            var postData = new ByteArrayContent(req);
            Task<string> result = httpClient.PostAsync(url, postData).Result.Content.ReadAsStringAsync();
            return result.Result;
        }

        /// <summary>
        /// get 请求 异步方式
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> GetString(string url)
        {
            HttpClient httpClient = CreateHttpClient(url);
            var responseMessage = await httpClient.GetAsync(url);
            return await responseMessage.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// get 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <returns></returns>
        public static string Get(string url)
        {
            HttpClient httpClient = CreateHttpClient(url);
            Task<string> result = httpClient.GetAsync(url).Result.Content.ReadAsStringAsync();
            return result.Result;
        }


        /// <summary>
        /// using WebResut
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<dynamic> GetForDetail(string url)
        {
            WebRequest request = WebRequest.Create(url);

            request.Method = "GET";
            var response = await request.GetResponseAsync();

            var ContentType = response.ContentType;
            var ContentLength = response.ContentLength;
            var headerCollection = response.Headers;


            List<string> headers = new List<string>();
            foreach (var item in headerCollection.AllKeys)
            {
                headers.Add($" key:{item}  ==== value:{headerCollection[item]}");
            }


            foreach (var item in headers)
            {
                Console.WriteLine(item);
            }
            return response.GetResponseStream();
        }


    }
}
