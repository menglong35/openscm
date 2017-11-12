using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    interface IDataUploadService
    {
        /// <summary>
        /// 重启上传服务
        /// </summary>
        void Restart();

        /// <summary>
        /// 服务启动
        /// </summary>
        void Start();

        /// <summary>
        /// 请求上传
        /// </summary>
        void RequestUpload();

        /// <summary>
        /// 服务停止
        /// </summary>
        void Stop();

        /// <summary>
        /// 请求直接上传
        /// </summary>
        /// <param name="typeKey"></param>
        /// <param name="data"></param>
        /// <param name="responseData"></param>
        void RequestDerictUpload(string typeKey, string data, out string responseData);

        /// <summary>
        /// 请求直接上传
        /// </summary>
        /// <param name="typeKey"></param>
        /// <param name="data"></param>
        /// <param name="timeOut"></param>
        /// <param name="responseData"></param>
        void RequestDerictUpload(string typeKey, string data, int timeOut, out string responseData);

        //20160620 ADD BY ZHANGHJE(01436) FOR 服务云集成客服系统，增加请求直接上传


        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeKey"></param>
        /// <param name="data"></param>
        /// <param name="customerCode"></param>
        /// <param name="accessToken"></param>
        void DerictUpload(string typeKey, string data, string customerCode, string accessToken);
    }
}
