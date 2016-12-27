/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/03/11
 * Note  : URL验证是否有效
***************************************************************/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net;

namespace zcode
{
    /// <summary>
    ///   URL验证器
    /// </summary>
    public class URLVerifier
    {
        /// <summary>
        ///   是否完成
        /// </summary>
        public bool IsDone { get; private set; }

        /// <summary>
        ///   有效的URL
        /// </summary>
        public string URL { get; private set; }

        /// <summary>
        ///   需验证的URL组
        /// </summary>
        private List<string> url_group_;

        /// <summary>
        ///   
        /// </summary>
        private Thread thread_;

        /// <summary>
        ///   
        /// </summary>
        public URLVerifier(List<string> url_group)
        {
            IsDone = false;
            URL = null;
            url_group_ = url_group;
        }

        /// <summary>
        ///   开始校验
        /// </summary>
        public void Start()
        {
            if (url_group_ == null || url_group_.Count == 0)
            {
                URL = null;
                IsDone = true;
                return;
            }

            if(thread_ == null)
            {
                thread_ = new Thread(new ThreadStart(_VerifyURLGroup));
                thread_.Start();
            }
        }

        /// <summary>
        ///   中止校验
        /// </summary>
        public void Abort()
        {
            if (thread_ != null)
            {
                thread_.Abort();
                thread_ = null;
            }

            URL = null;
            IsDone = true;
        }

        /// <summary>
        ///   校验操作
        /// </summary>
        void _VerifyURLGroup()
        {
            IsDone = false;
            URL = null;
            if(url_group_ != null)
            {
                for (int i = 0; i < url_group_.Count; ++i)
                {
                    string url = url_group_[i];
                    if(Verify(url))
                    {
                        URL = url;
                        break;
                    }
                }
            }

            IsDone = true;
        }

        /// <summary>
        ///   Verify
        /// </summary>
        static bool Verify(string url)
        {
            bool result = false;
            HttpWebRequest request = null;
            HttpWebResponse response = null;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.KeepAlive = false;
                request.Method = "HEAD";
                request.Timeout = 5000;
                request.AllowAutoRedirect = false;
                request.UseDefaultCredentials = true;
                response = request.GetResponse() as HttpWebResponse;
                result = response.StatusCode == HttpStatusCode.OK;
            }
            catch (System.Net.WebException)
            {
                result = false;
            }
            catch (System.Exception)
            {
                result = false;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
                if (request != null)
                {
                    request.Abort();
                    request = null;
                }
            }

            return result;
        }
    }
}
