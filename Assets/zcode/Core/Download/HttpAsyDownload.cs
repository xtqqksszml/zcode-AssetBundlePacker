/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/22
 * Note  : Http下载(支持断点续传, 暂不支持多线程下载)
***************************************************************/
using UnityEngine;
using System;
using System.Collections;
using System.Threading;
using System.Net;
using System.IO;

namespace zcode
{
    /// <summary>
    ///   下载内容
    /// </summary>
    internal class DownloadContent
    {
        /// <summary>
        /// 状态
        /// </summary>
        public enum emState
        {
            Downloading,        // 正在下载
            Canceling,          // 正在取消
            Completed,          // 已完成
            Failed,             // 已失败
        }

        /// <summary>
        /// 下载文件缓存的Last-Modified字符串大小
        /// </summary>
        public const int FILE_LAST_MODIFIED_SIZE = 32;

        /// <summary>
        ///   缓存大小
        /// </summary>
        public const int BUFFER_SIZE = 1024;

        /// <summary>
        ///   下载中间文件名
        /// </summary>
        public const string TEMP_EXTENSION_NAME = ".download";

        /// <summary>
        /// 当前状态
        /// </summary>
        public emState State;

        /// <summary>
        ///   文件名
        /// </summary>
        public string FileFullName;

        /// <summary>
        ///   上次已下载的大小
        /// </summary>
        public long LastTimeCompletedLength;

        /// <summary>
        ///   数据缓存
        /// </summary>
        public byte[] Buffer;

        /// <summary>
        /// 
        /// </summary>
        public DateTime LastModified;

        /// <summary>
        ///   
        /// </summary>
        public FileStream FS;

        /// <summary>
        /// 返回的数据流
        /// </summary>
        public Stream ResponseStream { get; private set; }

        /// <summary>
        ///   
        /// </summary>
        private HttpWebResponse web_response_;
        public HttpWebResponse WebResponse
        {
            get
            {
                return web_response_;
            }
            set
            {
                web_response_ = value;
                ResponseStream = web_response_.GetResponseStream();
            }
        }

        /// <summary>
        ///   临时文件名（用于下载时写入数据）
        /// </summary>
        public string TempFileFullName
        {
            get
            {
                return FileFullName + TEMP_EXTENSION_NAME;
            }
        }

        /// <summary>
        ///   
        /// </summary>
        public DownloadContent(string file_name, bool is_new = true)
        {
            FileFullName = file_name;
            State = emState.Downloading;
            Buffer = new byte[BUFFER_SIZE];

            OpenFile(is_new);
        }

        /// <summary>
        ///   关闭
        /// </summary>
        public void Close()
        {
            if (web_response_ != null)
                CloseFile(web_response_.LastModified);
            else
                CloseFile();

            if (ResponseStream != null)
            {
                ResponseStream.Close();
                ResponseStream = null;
            }

            if (web_response_ != null)
            {
                web_response_.Close();
                web_response_ = null;
            }
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        void OpenFile(bool is_new)
        {
            try
            {
                //创建路径，保存路径存在
                string parent = Path.GetDirectoryName(FileFullName);
                Directory.CreateDirectory(parent);

                //写入到临时文件中，下载完成后改回来
                if (is_new || !File.Exists(TempFileFullName))
                {
                    //创建新的文件
                    FS = new FileStream(TempFileFullName, FileMode.Create, FileAccess.ReadWrite);
                    LastTimeCompletedLength = 0;
                    LastModified = DateTime.MinValue;
                }
                else
                {
                    //断点续传
                    FS = new FileStream(TempFileFullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    LastTimeCompletedLength = FS.Length;
                    if (LastTimeCompletedLength > FILE_LAST_MODIFIED_SIZE
                        && ReadLastModified(ref LastModified))
                    {
                        FS.Seek(LastTimeCompletedLength - FILE_LAST_MODIFIED_SIZE, SeekOrigin.Begin);
                        LastTimeCompletedLength -= FILE_LAST_MODIFIED_SIZE;
                    }
                    else
                    {
                        FS.Seek(0, SeekOrigin.Begin);
                        LastTimeCompletedLength = 0;
                        LastModified = DateTime.MinValue;
                    }
                }

                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            if (FS != null)
            {
                FS.Close();
                FS = null;
            }
        }

        /// <summary>
        /// 关闭文件
        /// </summary>
        void CloseFile()
        {
            if (FS != null)
            {
                FS.Close();
                FS = null;
            }
            
            if (File.Exists(TempFileFullName))
            {
                if (State == emState.Completed)
                {
                    //如果下载完成修正文件名
                    if (File.Exists(FileFullName))
                        File.Delete(FileFullName);
                    File.Move(TempFileFullName, FileFullName);
                }
                else
                {
                    //未下载完成，删除缓存文件
                    File.Delete(TempFileFullName);
                }
            }
        }

        /// <summary>
        /// 关闭文件,写入Last-Modified
        /// </summary>
        void CloseFile(DateTime last_modified)
        {
            if (State == emState.Failed) 
                WriteLastModified(last_modified);

            if (FS != null)
            {
                FS.Close();
                FS = null;
            }

            //如果下载完成修正文件名
            if (File.Exists(TempFileFullName))
            {
                if (State == emState.Completed)
                {
                    if (File.Exists(FileFullName))
                        File.Delete(FileFullName);
                    File.Move(TempFileFullName, FileFullName);
                }
            }
        }

        /// <summary>
        /// 写入Last-Modified
        /// </summary>
        bool WriteLastModified(DateTime last_modified)
        {
            if (FS != null )
            {
                //写入Last-Modified
                string str = last_modified.Ticks.ToString("d" + FILE_LAST_MODIFIED_SIZE);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
                FS.Write(bytes, 0, bytes.Length);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 读取Last-Modified
        /// </summary>
        bool ReadLastModified(ref DateTime last_modified)
        {
            if (FS != null && FS.Length > FILE_LAST_MODIFIED_SIZE)
            {
                byte[] bytes = new byte[FILE_LAST_MODIFIED_SIZE];
                FS.Seek(LastTimeCompletedLength - FILE_LAST_MODIFIED_SIZE, SeekOrigin.Begin);
                FS.Read(bytes, 0, FILE_LAST_MODIFIED_SIZE);
                long ticks = long.Parse(System.Text.Encoding.Default.GetString(bytes));
                last_modified = new DateTime(ticks);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    ///   Http下载
    /// </summary>
    public class HttpAsyDownload
    {
        /// <summary>
        ///   错误代码
        /// </summary>
        public enum emErrorCode
        {
            None,           // 无
            Cancel,         // 取消下载
            NoResponse,     // 服务器未响应
            DownloadError,  // 下载出错
            TimeOut,        // 超时
            Abort,          // 强制关闭
        }

        /// <summary>
        ///   超时时间(毫秒)
        /// </summary>
        public const int TIMEOUT_TIME = 20000;

        /// <summary>
        ///   下载地址
        /// </summary>
        public string URL { get; private set; }

        /// <summary>
        ///   存放的根路径
        /// </summary>
        public string Root { get; private set; }

        /// <summary>
        ///   LocalName
        /// </summary>
        public string LocalName { get; private set; }

        /// <summary>
        /// FullName
        /// </summary>
        public string FullName
        {
            get { return string.IsNullOrEmpty(Root) || string.IsNullOrEmpty(LocalName) ?
                null : Root  + "/" + LocalName; }
        }

        /// <summary>
        ///   是否结束
        /// </summary>
        public bool IsDone { get; private set; }

        /// <summary>
        ///   错误代码
        /// </summary>
        public emErrorCode ErrorCode;

        /// <summary>
        /// 总下载大小
        /// </summary>
        public long Length { get; private set; }

        /// <summary>
        /// 获得当前已下载大小
        /// </summary>
        public long CompletedLength { get; private set; }

        /// <summary>
        ///   下载通知回调
        /// </summary>
        private Action<HttpAsyDownload, long> notify_callback_;

        /// <summary>
        ///   错误回调
        /// </summary>
        private Action<HttpAsyDownload> error_callback_;

        /// <summary>
        ///   
        /// </summary>
        private DownloadContent content_ = null;

        /// <summary>
        ///   
        /// </summary>
        private HttpWebRequest http_request_ = null;

        /// <summary>
        ///   锁对象，用于保证线程安全
        /// </summary>
        object lock_obj_ = new object();

        /// <summary>
        ///   
        /// </summary>
        public HttpAsyDownload(string url)
        {
            URL = url;
        }

        /// <summary>
        ///   开始下载
        /// </summary>
        public void Start(string root, string local_file_name
                        , Action<HttpAsyDownload, long> notify = null
                        , Action<HttpAsyDownload> error_cb = null)
        {
            lock (lock_obj_)
            {
                Abort();

                Root = root;
                LocalName = local_file_name;
                IsDone = false;
                ErrorCode = emErrorCode.None;
                notify_callback_ = notify;
                error_callback_ = error_cb;
                content_ = new DownloadContent(FullName, false);
                CompletedLength = 0;
                Length = 0;
                _Download();
            }
        }

        /// <summary>
        ///   取消下载（优雅的）
        /// </summary>
        public void Cancel()
        {
            lock (lock_obj_)
            {
                if (content_ != null && content_.State == DownloadContent.emState.Downloading)
                {
                    content_.State = DownloadContent.emState.Canceling;
                }
                else
                {
                    IsDone = true;
                }
            }
        }

        /// <summary>
        ///   中止下载
        /// </summary>
        public void Abort()
        {
            lock (lock_obj_)
            {
                if (content_ != null && content_.State == DownloadContent.emState.Downloading)
                {
                    OnFailed(emErrorCode.Abort);
                }
            }
        }

        /// <summary>
        ///   下载完成
        /// </summary>
        void OnFinish()
        {
            lock (lock_obj_)
            {
                if (content_ != null)
                {
                    content_.State = DownloadContent.emState.Completed;
                    content_.Close();
                    content_ = null;
                }
                    
                if (http_request_ != null)
                {
                    http_request_.Abort();
                    http_request_ = null;
                }

                IsDone = true;
            }
        }

        /// <summary>
        ///   下载失败
        /// </summary>
        void OnFailed(emErrorCode code)
        {
            lock (lock_obj_)
            {
                if (content_ != null)
                {
                    content_.State = DownloadContent.emState.Failed;
                    content_.Close();
                    content_ = null;
                }

                if (http_request_ != null)
                {
                    http_request_.Abort();
                    http_request_ = null;
                }

                IsDone = true;
                ErrorCode = code;

                if (error_callback_ != null)
                    error_callback_(this);
            }
        }

        /// <summary>
        /// 下载
        /// </summary>
        void _Download()
        {
            try
            {
                lock (lock_obj_)
                {
                    //尝试下载资源，携带If-Modified-Since
                    http_request_ = WebRequest.Create(URL + LocalName) as HttpWebRequest;
                    http_request_.Timeout = TIMEOUT_TIME;
                    http_request_.KeepAlive = false;
                    http_request_.IfModifiedSince = content_.LastModified;
                    IAsyncResult result = (IAsyncResult)http_request_.BeginGetResponse(new AsyncCallback(_OnResponseCallback), http_request_);
                    RegisterTimeOut(result.AsyncWaitHandle);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + LocalName + "\" download failed!"
                                    + "\nMessage:" + e.Message);
                UnregisterTimeOut();
                OnFailed(emErrorCode.NoResponse);
            }
        }

        /// <summary>
        ///   
        /// </summary>
        void _OnResponseCallback(IAsyncResult ar)
        {
            try
            {
                UnregisterTimeOut();

                lock (lock_obj_)
                {
                    HttpWebRequest req = ar.AsyncState as HttpWebRequest;
                    if (req == null) return;
                    HttpWebResponse response = req.BetterEndGetResponse(ar) as HttpWebResponse;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Length = response.ContentLength;
                        content_.WebResponse = response;
                        _BeginRead(new AsyncCallback(_OnReadCallback));
                    }
                    else if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        //表示资源未修改开启断点续传
                        if (http_request_ != null)
                        {
                            http_request_.Abort();
                            http_request_ = null;
                        }
                        _PartialDownload();
                        return;
                    }
                    else
                    {
                        response.Close();
                        OnFailed(emErrorCode.NoResponse);
                        return;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + LocalName + "\" download failed!"
                                    + "\nMessage:" + e.Message);
                OnFailed(emErrorCode.DownloadError);
            }
        }

        /// <summary>
        ///   断点续传
        /// </summary>
        void _PartialDownload()
        {
            try
            {
                lock (lock_obj_)
                {
                    http_request_ = WebRequest.Create(URL + LocalName) as HttpWebRequest;
                    http_request_.Timeout = TIMEOUT_TIME;
                    http_request_.KeepAlive = false;
                    http_request_.AddRange((int)content_.LastTimeCompletedLength);
                    IAsyncResult result = (IAsyncResult)http_request_.BeginGetResponse(new AsyncCallback(_OnDownloadPartialResponseCallback), http_request_);
                    RegisterTimeOut(result.AsyncWaitHandle);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + LocalName + "\" download failed!"
                                    + "\nMessage:" + e.Message);
                UnregisterTimeOut();
                OnFailed(emErrorCode.NoResponse);
            }
        }

        /// <summary>
        ///   
        /// </summary>
        void _OnDownloadPartialResponseCallback(IAsyncResult ar)
        {
            try
            {
                UnregisterTimeOut();

                lock (lock_obj_)
                {
                    HttpWebRequest req = ar.AsyncState as HttpWebRequest;
                    if (req == null) return;
                    HttpWebResponse response = req.BetterEndGetResponse(ar) as HttpWebResponse;
                    if (response.StatusCode == HttpStatusCode.PartialContent)
                    {
                        Length = content_.LastTimeCompletedLength + response.ContentLength;
                        content_.WebResponse = response;
                        _BeginRead(new AsyncCallback(_OnReadCallback));
                    }
                    else if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        OnFailed(emErrorCode.Abort);
                        return;
                    }
                    else
                    {
                        response.Close();
                        OnFailed(emErrorCode.NoResponse);
                        return;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + LocalName + "\" download failed!"
                                    + "\nMessage:" + e.Message);
                OnFailed(emErrorCode.DownloadError);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IAsyncResult _BeginRead(AsyncCallback callback)
        {
            if (content_ == null)
                return null;

            if (content_.State == DownloadContent.emState.Canceling)
            {
                OnFailed(emErrorCode.Cancel);
                return null;
            }

            return content_.ResponseStream.BeginRead(content_.Buffer
                , 0
                , DownloadContent.BUFFER_SIZE
                , callback
                , content_);
        }

        /// <summary>
        ///   
        /// </summary>
        void _OnReadCallback(IAsyncResult ar)
        {
            try
            {
                lock (lock_obj_)
                {
                    DownloadContent rs = ar.AsyncState as DownloadContent;
                    if (rs.ResponseStream == null)
                        return;

                    int read = rs.ResponseStream.EndRead(ar);
                    if (read > 0)
                    {
                        rs.FS.Write(rs.Buffer, 0, read);
                        rs.FS.Flush();
                        CompletedLength += read;

                        if (notify_callback_ != null)
                            notify_callback_(this, (long)read);
                    }
                    else
                    {
                        OnFinish();

                        if (notify_callback_ != null)
                            notify_callback_(this, (long)read);
                        return;
                    }

                    _BeginRead(new AsyncCallback(_OnReadCallback));
                }
            }
            catch (WebException e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + LocalName + "\" download failed!"
                                    + "\nMessage:" + e.Message);
                OnFailed(emErrorCode.DownloadError);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + LocalName + "\" download failed!"
                                    + "\nMessage:" + e.Message);
                OnFailed(emErrorCode.DownloadError);
            }
        }

        #region Timeout
        /// <summary>
        /// 
        /// </summary>
        RegisteredWaitHandle registered_wait_handle_;

        /// <summary>
        /// 
        /// </summary>
        WaitHandle wait_handle_;

        /// <summary>
        /// 
        /// </summary>
        void RegisterTimeOut(WaitHandle handle)
        {
            wait_handle_ = handle;
            registered_wait_handle_ = ThreadPool.RegisterWaitForSingleObject(handle
                                                 , new WaitOrTimerCallback(_OnTimeoutCallback)
                                                 , http_request_
                                                 , TIMEOUT_TIME
                                                 , true);
        }

        /// <summary>
        /// 
        /// </summary>
        void UnregisterTimeOut()
        {
            if (registered_wait_handle_ != null && wait_handle_ != null)
                registered_wait_handle_.Unregister(wait_handle_);
        }

        /// <summary>
        ///   
        /// </summary>
        void _OnTimeoutCallback(object state, bool timedOut)
        {
            lock (lock_obj_)
            {
                if (timedOut)
                {
                    OnFailed(emErrorCode.TimeOut);
                }

                UnregisterTimeOut();
            }
        }
        #endregion
    }
}