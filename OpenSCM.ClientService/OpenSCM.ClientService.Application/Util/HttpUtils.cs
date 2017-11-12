using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    /// <summary>
    /// Http 请求帮助类
    /// </summary>
    public static class HttpUtils
    {

        static HttpUtils()
        {
            ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
        }

        private static bool CheckValidationResult(object sender,
                                                  X509Certificate certificate,
                                                  X509Chain chain,
                                                  SslPolicyErrors errors)
        {
            // 总是接受
            return true;
        }

        /// <summary>
        /// 从 远程Get一段结果
        /// </summary>
        /// <param name="url">请求的Url 如果有QueryString 请自己拼接到Url中</param>
        /// <param name="proxyAddress">代理服务器地址</param>
        /// <param name="statusCode">返回值 状态码</param>
        /// <param name="responseData">返回值 响应的数据 一律处理为String 暂时不考虑其他响应格式 例如Image 二进制数据等</param>
        /// <exception cref="WebException">如果响应Status 不是<see cref="HttpStatusCode"/>  HttpStatusCode 2XX 则会抛出异常</exception>
        public static void HttpGet(string url,
                                   string proxyAddress,
                                   out HttpStatusCode statusCode,
                                   out string responseData)
        {
            HttpCall(url, "GET", string.Empty, null, proxyAddress,
                out statusCode, out responseData);
        }

        /// <summary>
        /// 模拟表单提交 POST 一段数据到远程Http服务上
        /// </summary>
        /// <param name="url">请求的Url 如果有QueryString 请自己拼接到Url中</param>
        /// <param name="data">自己处理的数据 以application/x-www-form-urlencoded 形式Post</param>
        /// <param name="proxyAddress">代理服务器地址</param>
        /// <param name="statusCode">返回值 状态码</param>
        /// <param name="responseData">返回值 响应的数据 一律处理为String 暂时不考虑其他响应格式 例如Image 二进制数据等</param>
        /// <exception cref="WebException">如果响应Status 不是<see cref="HttpStatusCode"/>  HttpStatusCode 2XX 则会抛出异常</exception>
        public static void HttpFormPost(string url,
                                        string data,
                                        string proxyAddress,
                                        out HttpStatusCode statusCode,
                                        out string responseData)
        {
            HttpCall(url, "POST", data, "application/x-www-form-urlencoded;charset=UTF-8", proxyAddress,
                out statusCode, out responseData);
        }

        /// <summary>
        /// POST 一段数据到远程Http服务上
        /// </summary>
        /// <param name="url">请求的Url 如果有QueryString 请自己拼接到Url中</param>
        /// <param name="data">数据</param>
        /// <param name="proxyAddress">代理服务器地址</param>
        /// <param name="statusCode">返回值 状态码</param>
        /// <param name="responseData">返回值 响应的数据 一律处理为String 暂时不考虑其他响应格式 例如Image 二进制数据等</param>
        /// <exception cref="WebException">如果响应Status 不是<see cref="HttpStatusCode"/>  HttpStatusCode 2XX  则会抛出异常</exception>
        public static void HttpPost(string url,
                                    string data,
                                    string proxyAddress,
                                    out HttpStatusCode statusCode,
                                    out string responseData)
        {
            HttpCall(url, "POST", data, null, proxyAddress,
                out statusCode, out responseData);
        }

        /// <summary>
        /// POST 一段数据到远程Http服务上
        /// </summary>
        /// <param name="url">请求的Url 如果有QueryString 请自己拼接到Url中</param>
        /// <param name="data">数据</param>
        /// <param name="proxyAddress">代理服务器地址</param>
        /// <param name="timeOut">相应时间</param>
        /// <param name="statusCode">返回值 状态码</param>
        /// <param name="responseData">返回值 响应的数据 一律处理为String 暂时不考虑其他响应格式 例如Image 二进制数据等</param>
        /// <exception cref="WebException">如果响应Status 不是<see cref="HttpStatusCode"/>  HttpStatusCode 2XX  则会抛出异常</exception>
        public static void HttpPost(string url,
                                    string data,
                                    string proxyAddress,
                                    int timeOut,
                                    out HttpStatusCode statusCode,
                                    out string responseData)
        {
            HttpCall(url, "POST", data, null, proxyAddress, timeOut,
                out statusCode, out responseData);
        }

        /// <summary>
        ///  模拟表单提交 POST 一段数据到远程Http服务上
        /// </summary>
        /// <param name="url">请求的Url 如果有QueryString 请自己拼接到Url中</param>
        /// <param name="data">数据 以application/x-www-form-urlencoded 形式 PUT 提交</param>
        /// <param name="proxyAddress">代理服务器地址</param>
        /// <param name="statusCode">返回值 状态码</param>
        /// <param name="responseData">返回值 响应的数据 一律处理为String 暂时不考虑其他响应格式 例如Image 二进制数据等</param>
        /// <exception cref="WebException">如果响应Status 不是<see cref="HttpStatusCode"/>  HttpStatusCode 2XX 则会抛出异常</exception>
        public static void HttpFormPut(string url,
                                       string data,
                                       string proxyAddress,
                                       out HttpStatusCode statusCode,
                                       out string responseData)
        {
            HttpCall(url, "PUT", data, "application/x-www-form-urlencoded;charset=UTF-8", proxyAddress,
                out statusCode, out responseData);
        }

        /// <summary>
        /// PUT 一段数据到远程Http服务上
        /// </summary>
        /// <param name="url">请求的Url 如果有QueryString 请自己拼接到Url中</param>
        /// <param name="data">数据</param>
        /// <param name="proxyAddress">代理服务器地址</param>
        /// <param name="statusCode">返回值 状态码</param>
        /// <param name="responseData">返回值 响应的数据 一律处理为String 暂时不考虑其他响应格式 例如Image 二进制数据等</param>
        /// <exception cref="WebException">如果响应Status 不是<see cref="HttpStatusCode"/>  HttpStatusCode 2XX 则会抛出异常</exception>
        public static void HttpPut(string url,
                                   string data,
                                   string proxyAddress,
                                   out HttpStatusCode statusCode,
                                   out string responseData)
        {
            HttpCall(url, "PUT", data, null, proxyAddress,
                out statusCode, out responseData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">请求的Url 如果有QueryString，请自己拼接到Url中</param>
        /// <param name="method">GET POST PUT 等</param>
        /// <param name="requestData">Request中 添加的 Body 如果不需要 null</param>
        /// <param name="contentType"></param>
        /// <param name="proxyAddress">代理服务器地址</param>
        /// <param name="statusCode">返回值 状态码</param>
        /// <param name="responseData">返回值 响应的数据 一律处理为String 暂时不考虑其他响应格式 例如Image 二进制数据等</param>
        /// <exception cref="WebException">如果响应Status 不是<see cref="HttpStatusCode"/>  HttpStatusCode 2XX 则会抛出异常</exception>
        private static void HttpCall(string url,
                                     string method,
                                     string requestData,
                                     string contentType,
                                     string proxyAddress,
                                     out HttpStatusCode statusCode,
                                     out string responseData)
        {
            HttpCall(url, method, requestData, contentType, proxyAddress, 50000, out statusCode, out responseData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">请求的Url 如果有QueryString，请自己拼接到Url中</param>
        /// <param name="method">GET POST PUT 等</param>
        /// <param name="requestData">Request中 添加的 Body 如果不需要 null</param>
        /// <param name="contentType"></param>
        /// <param name="proxyAddress">代理服务器地址</param>
        /// <param name="timeOut">相应时间</param>
        /// <param name="statusCode">返回值 状态码</param>
        /// <param name="responseData">返回值 响应的数据 一律处理为String 暂时不考虑其他响应格式 例如Image 二进制数据等</param>
        /// <exception cref="WebException">如果响应Status 不是<see cref="HttpStatusCode"/>  HttpStatusCode 2XX 则会抛出异常</exception>
        private static void HttpCall(string url,
                                     string method,
                                     string requestData,
                                     string contentType,
                                     string proxyAddress,
                                     int timeOut,
                                     out HttpStatusCode statusCode,
                                     out string responseData)
        {
            //string callId = Guid.NewGuid().ToString();
            //DebugLog(string.Format("[{0}]Begin Call", callId));
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            if (!string.IsNullOrEmpty(proxyAddress))
            {
                //DebugLog(string.Format("[{0}]Proxy Address:{1}", callId, proxyAddress));
                //代理服务器地址 为 127.0.0.1:3128的形式
                //因此使用这种方式设置
                var proxy = new WebProxy(proxyAddress);
                httpWebRequest.Proxy = proxy;//设置网关
                httpWebRequest.UseDefaultCredentials = false;
            }
            httpWebRequest.Method = method;
            httpWebRequest.Timeout = timeOut;
            if (!String.IsNullOrEmpty(contentType))
            {
                httpWebRequest.ContentType = contentType;
            }
            if (!String.IsNullOrEmpty(requestData))
            {
                var btBodys = Encoding.UTF8.GetBytes(requestData);
                httpWebRequest.ContentLength = btBodys.Length;
                var reqStream = httpWebRequest.GetRequestStream();
                reqStream.Write(btBodys, 0, btBodys.Length);
                reqStream.Close();
            }
            HttpWebResponse response;
            try
            {
                response = httpWebRequest.GetResponse() as HttpWebResponse;
                GetValue(response, out statusCode, out responseData);
                if (response != null)
                {
                    response.Close();
                }
            }
            catch (WebException ex)
            {
                response = ex.Response as HttpWebResponse;
                GetValue(response, out statusCode, out responseData);
                if (response != null)
                {
                    response.Close();
                }
                throw;
            }
            //DebugLog(string.Format("[{0}]End Call:{1}:{2}-{3}", callId, method, url, statusCode));
        }

        /// <summary>
        /// 从 HttpWebResponse 获取StatusCode 和 responseData
        /// </summary>
        /// <param name="response"></param>
        /// <param name="statusCode"></param>
        /// <param name="responseData"></param>
        private static void GetValue(HttpWebResponse response, out HttpStatusCode statusCode, out string responseData)
        {
            //如果服务端没有响应 response则为空
            if (response == null)
            {
                statusCode = HttpStatusCode.NotFound;
                responseData = "Time Out";
                return;
            }
            using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                responseData = reader.ReadToEnd();
            }
            statusCode = response.StatusCode;
        }

    }
}
