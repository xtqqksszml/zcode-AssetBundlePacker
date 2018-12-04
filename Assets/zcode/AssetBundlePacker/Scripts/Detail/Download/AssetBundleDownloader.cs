/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/18 16:21:22
 * Note  : AssetBundle下载器
***************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace zcode.AssetBundlePacker
{
    /// <summary>
    ///   AssetBundle下载器
    /// </summary>
    public class AssetBundleDownloader
    {
        /// <summary>
        ///   并发下载最大数量
        ///   如果需要>2，则需修改System.Net.ServicePointManager.DefaultConnectionLimit
        /// </summary>
        public const int CONCURRENCE_DOWNLOAD_NUMBER = 2;

        /// <summary>
        ///   URL
        /// </summary>
        public string URL;

        /// <summary>
        ///   下载根路径
        /// </summary>
        public string Root;

        /// <summary>
        ///   是否结束
        /// </summary>
        public bool IsDone { get; private set; }

        /// <summary>
        ///   是否出错
        /// </summary>
        public bool IsFailed
        {
            get{ return ErrorCode != emErrorCode.None; }
        }

        /// <summary>
        ///   错误代码
        /// </summary>
        public emErrorCode ErrorCode { get; private set; }

        /// <summary>
        ///   下载的大小
        /// </summary>
        public long CompletedSize { get; private set; }

        /// <summary>
        ///   总大小
        /// </summary>
        public long TotalSize { get; private set; }

        /// <summary>
        ///   需要下载的资源
        /// </summary>
        public List<string> UncompleteDownloadList { get; private set; }

        /// <summary>
        ///   正在下载的资源
        /// </summary>
        public List<string> DownloadingList { get; private set; }

        /// <summary>
        ///   已下载的资源
        /// </summary>
        public List<string> CompleteDownloadList { get; private set; }

        /// <summary>
        ///   下载失败的资源
        /// </summary>
        public List<string> FailedDownloadList { get; private set; }

        /// <summary>
        ///   http下载
        /// </summary>
        private List<HttpAsyDownload> downloads_;

        /// <summary>
        ///   资源描述数据
        /// </summary>
        private ResourcesManifest resources_manifest_;

        /// <summary>
        ///   锁对象，用于保证多线程下载安全
        /// </summary>
        readonly object lock_obj_ = new object();

        /// <summary>
        ///   下载资源
        /// </summary>
        public AssetBundleDownloader(string url
            , int concurrence_download_number = CONCURRENCE_DOWNLOAD_NUMBER)
        {
            URL = url;
            IsDone = false;
            ErrorCode = emErrorCode.None;
            CompletedSize = 0;
            TotalSize = 0;
            UncompleteDownloadList = new List<string>();
            CompleteDownloadList = new List<string>();
            FailedDownloadList = new List<string>();
            DownloadingList = new List<string>();

            downloads_ = new List<HttpAsyDownload>();

            System.Net.ServicePointManager.DefaultConnectionLimit = concurrence_download_number;
        }

        /// <summary>
        /// 开始下载
        /// </summary>
        public bool Start(string root
            , string assetbundlename
            , ResourcesManifest resources_manifest)
        {
            List<string> list = new List<string>();
            list.Add(assetbundlename);

            return Start(root, list, resources_manifest);
        }

        /// <summary>
        ///   开始下载
        /// </summary>
        public bool Start(string root
            , List<string> assetbundles
            , ResourcesManifest resources_manifest)
        {
            Abort();

            if (resources_manifest == null)
            {
                Error(emErrorCode.ParameterError);
                return false;
            }
            if (assetbundles == null || assetbundles.Count == 0)
            {
                IsDone = true;
                return true;
            }

            IsDone = false;
            ErrorCode = emErrorCode.None;

            Root = root;
            resources_manifest_ = resources_manifest;
            UncompleteDownloadList = assetbundles;
            CompleteDownloadList.Clear();
            FailedDownloadList.Clear();

            //统计下载数据
            TotalSize = 0;
            CompletedSize = 0;
            for (int i = 0; i < UncompleteDownloadList.Count; ++i)
            {
                var ab = resources_manifest_.Find(UncompleteDownloadList[i]);
                if (ab != null)
                {
                    if (ab.IsCompress)
                        TotalSize += ab.CompressSize;
                    else
                        TotalSize += ab.Size;
                }
            }

            //开始下载
            for (int i = 0; i < System.Net.ServicePointManager.DefaultConnectionLimit; ++i)
            {
                HttpAsyDownload d = new HttpAsyDownload(URL);
                downloads_.Add(d);
                var assetbundlename = GetImcomplete();
                if (!string.IsNullOrEmpty(assetbundlename))
                {
                    Download(downloads_[i], assetbundlename);
                }
            }

            return true;
        }

        /// <summary>
        ///   取消下载
        /// </summary>
        public void Cancel()
        {
            for (int i = 0; i < downloads_.Count; ++i)
            {
                downloads_[i].Cancel();
            }
            downloads_.Clear();
        }

        /// <summary>
        ///   终止下载
        /// </summary>
        public void Abort()
        {
            for (int i = 0; i < downloads_.Count; ++i)
            {
                downloads_[i].Abort();
            }
            downloads_.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        string GetImcomplete()
        {
            if (UncompleteDownloadList == null || UncompleteDownloadList.Count == 0)
                return null;

            var name = UncompleteDownloadList[UncompleteDownloadList.Count - 1];
            UncompleteDownloadList.RemoveAt(UncompleteDownloadList.Count - 1);
            return name;
        }

        /// <summary>
        ///   
        /// </summary>
        void Error(emErrorCode ec, string message = null)
        {
            lock (lock_obj_)
            {
                string ms = string.IsNullOrEmpty(message) ? ec.ToString() : ec.ToString() + " - " + message;
                Debug.LogError(ms);

                ErrorCode = ec;
                IsDone = true;
                Abort();
            }
        }

        /// <summary>
        ///   下载
        /// </summary>
        bool Download(HttpAsyDownload d, string assetbundlename)
        {
            lock (lock_obj_)
            {
                if (string.IsNullOrEmpty(assetbundlename))
                {
                    return false;
                }
                var ab = resources_manifest_.Find(assetbundlename);
                if (ab == null)
                {
                    Debug.LogWarning("AssetBundleDownloader.Download - AssetBundleName is invalid.");
                    return true;
                }

                DownloadingList.Add(assetbundlename);

                string file_name = ab.IsCompress ? Compress.GetCompressFileName(assetbundlename) : assetbundlename;
                d.Start(Root, file_name, _DownloadNotify, _DownloadError);
                return true;
            }
        }

        /// <summary>
        /// 下载完成
        /// </summary>
        void DownloadSucceed(string file_name)
        {
            lock (lock_obj_)
            {
                bool is_compress = Compress.IsCompressFile(file_name);
                string assetbundlename = is_compress ?  Compress.GetDefaultFileName(file_name) : file_name;
                CompleteDownloadList.Add(assetbundlename);
                DownloadingList.Remove(assetbundlename);

                //判断是否需要解压文件
                if (is_compress)
                {
                    // 解压文件
                    string in_file = Root + "/" + file_name;
                    string out_file = Root + "/" + assetbundlename;
                    Compress.DecompressFile(in_file, out_file);
                    // 删除压缩包
                    System.IO.File.Delete(in_file);
                }
            }
        }

        /// <summary>
        ///   
        /// </summary>
        void _DownloadNotify(HttpAsyDownload d, long size)
        {
            lock (lock_obj_)
            {
                CompletedSize += size;

                if (d.IsDone)
                {
                    DownloadSucceed(d.LocalName);

                    if(UncompleteDownloadList.Count == 0 && DownloadingList.Count == 0)
                    {
                        IsDone = true;
                    }
                    else
                    {
                        var assetbundlename = GetImcomplete();
                        if (!string.IsNullOrEmpty(assetbundlename))
                        {
                            Download(d, assetbundlename);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   
        /// </summary>
        void _DownloadError(HttpAsyDownload d)
        {
            lock (lock_obj_)
            {
                //加入失败列表
                string file_name = d.LocalName;
                bool is_compress = Compress.IsCompressFile(file_name);
                string assetbundlename = is_compress ? Compress.GetDefaultFileName(file_name) : file_name;
                FailedDownloadList.Add(assetbundlename);
                DownloadingList.Remove(assetbundlename);

                if(d.ErrorCode == HttpAsyDownload.emErrorCode.DiskFull)
                {
                    Error(emErrorCode.DiskFull, assetbundlename);
                }
                else
                {
                    Error(emErrorCode.DownloadFailed, assetbundlename);
                }
            }
        }
    }
}