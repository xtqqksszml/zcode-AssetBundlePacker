/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/22
 * Note  : 文件下载器
***************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace zcode
{
    /// <summary>
    ///   文件下载器
    /// </summary>
    public class FileDownload
    {
        /// <summary>
        ///   URL
        /// </summary>
        public string URL { get; private set; }

        /// <summary>
        ///   Root
        /// </summary>
        public string RootPath { get; private set; }

        /// <summary>
        ///   需要下载的资源
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        ///   是否结束
        /// </summary>
        public bool IsDone { get; private set; }

        /// <summary>
        ///   错误代码
        /// </summary>
        public HttpAsyDownload.emErrorCode ErrorCode { get; private set; }

        /// <summary>
        ///   是否出错
        /// </summary>
        public bool IsFailed
        {
            get { return ErrorCode != HttpAsyDownload.emErrorCode.None; }
        }

        /// <summary>
        ///   下载的大小
        /// </summary>
        public long CompletedSize { get; private set; }

        /// <summary>
        ///   总大小
        /// </summary>
        public long TotalSize { get; private set; }

        /// <summary>
        ///   Http下载器
        /// </summary>
        private HttpAsyDownload download_;

        /// <summary>
        ///   
        /// </summary>
        public FileDownload(string url, string root, string file_name)
        {
            Reset(url, root, file_name);
        }

        /// <summary>
        ///   重置
        /// </summary>
        public void Reset(string url, string root, string file_name)
        {
            URL = url;
            RootPath = root;
            FileName = file_name;
            IsDone = false;
            ErrorCode = HttpAsyDownload.emErrorCode.None;
            CompletedSize = 0;
            TotalSize = 0;
        }

        /// <summary>
        ///   开始下载
        /// </summary>
        public void Start()
        {
            //统计数据
            TotalSize = 0;
            CompletedSize = 0;
            UpdateState();

            //下载
            Download(FileName);
        }

        /// <summary>
        ///   取消下载
        /// </summary>
        public void Cancel()
        {
            if (download_ != null)
                download_.Cancel();
        }

        /// <summary>
        ///   中止下载
        /// </summary>
        public void Abort()
        {
            if (download_ != null)
                download_.Abort();
        }

        /// <summary>
        ///   更新
        /// </summary>
        void UpdateState()
        {
            if (TotalSize > 0)
            {
                IsDone = download_ != null && download_.IsDone;
            }
        }

        /// <summary>
        ///   下载
        /// </summary>
        bool Download(string file_name)
        {
            download_ = new HttpAsyDownload(URL);
            download_.Start(RootPath, file_name, _OnDownloadNotifyCallback, _OnDownloadErrorCallback);

            return true;
        }

        /// <summary>
        ///   下载进度通知回调
        /// </summary>
        void _OnDownloadNotifyCallback(HttpAsyDownload d, long size)
        {
            CompletedSize = d.CompletedLength;
            TotalSize = d.Length;
            UpdateState();
        }

        /// <summary>
        ///   下载错误回调
        /// </summary>
        void _OnDownloadErrorCallback(HttpAsyDownload d)
        {
            IsDone = true;
            ErrorCode = d.ErrorCode;
        }
    }
}