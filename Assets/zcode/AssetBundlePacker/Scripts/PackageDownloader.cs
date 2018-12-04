/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/03/14
 * Note  : AssetBundle包下载器
***************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace zcode.AssetBundlePacker
{
    public class PackageDownloader : MonoBehaviour
    {
        /// <summary>
        ///   状态
        /// </summary>
        public enum emState
        {
            None,               // 无
            VerifyURL,          // 验证有效的URL
            DownloadAssetBundle,// 下载AssetBundle
            Completed,          // 完成
            Failed,             // 失败
            Cancel,             // 取消
            Abort,              // 中断

            Max
        }

        /// <summary>
        ///   UpdateEvent
        /// </summary>
        public event System.Action<PackageDownloader> OnUpdate;

        /// <summary>
        ///   DoneEvent
        /// </summary>
        public event System.Action<PackageDownloader> OnDone;

        /// <summary>
        ///   是否结束
        /// </summary>
        public bool IsDone { get; private set; }

        /// <summary>
        ///   是否出错
        /// </summary>
        public bool IsFailed
        {
            get { return ErrorCode != emErrorCode.None; }
        }

        /// <summary>
        ///   错误代码
        /// </summary>
        public emErrorCode ErrorCode { get; private set; }

        /// <summary>
        ///   当前状态
        /// </summary>
        public emState CurrentState { get; private set; }

        /// <summary>
        ///   当前状态的完成度
        /// </summary>
        public float CurrentStateCompleteValue { get; private set; }

        /// <summary>
        ///   当前状态的总需完成度
        /// </summary>
        public float CurrentStateTotalValue { get; private set; }

        /// <summary>
        /// 下载地址列表
        /// </summary>
        private List<string> url_group_;

        /// <summary>
        ///   可用的URL
        /// </summary>
        private string current_url_;

        /// <summary>
        ///   资源包名
        /// </summary>
        private List<string> packages_name_;

        /// <summary>
        ///   
        /// </summary>
        private URLVerifier verifier_;

        /// <summary>
        ///   资源下载器
        /// </summary>
        private AssetBundleDownloader ab_download_;

        /// <summary>
        /// 
        /// </summary>
        protected PackageDownloader()
        { }

        /// <summary>
        ///   开始下载
        /// </summary>
        public bool StartDownload(List<string> url_group, List<string> pack_list)
        {
            if (!AssetBundleManager.Instance.IsReady)
                return false;
            if (!IsDone && CurrentState != emState.None)
                return false;

            Reset();

            url_group_ = url_group;
            packages_name_ = pack_list;

            if (AssetBundleManager.IsPlatformSupport)
            {
                StopAllCoroutines();
                StartCoroutine(Downloading());
            }
            else
            {
                IsDone = true;
            }

            return true;
        }

        /// <summary>
        ///   取消下载
        /// </summary>
        public void CancelDownload()
        {
            StopAllCoroutines();

            if (verifier_ != null)
            {
                verifier_.Abort();
                verifier_ = null;
            }
            if (ab_download_ != null)
            {
                ab_download_.Cancel();
                ab_download_ = null;
            }

            UpdateState(emState.Cancel);
            Done();
        }

        /// <summary>
        ///   中止下载
        /// </summary>
        public void AbortDownload()
        {
            StopAllCoroutines();

            if (verifier_ != null)
            {
                verifier_.Abort();
                verifier_ = null;
            }
            if (ab_download_ != null)
            {
                ab_download_.Abort();
                ab_download_ = null;
            }

            UpdateState(emState.Abort);
            Done();
        }

        IEnumerator Downloading()
        {
            UpdateState(emState.VerifyURL);
            yield return StartVerifyURL();
            UpdateState(emState.DownloadAssetBundle);
            yield return StartDownloadPack();
            UpdateState(ErrorCode == emErrorCode.None ? emState.Completed : emState.Failed);

            Done();
        }

        IEnumerator StartVerifyURL()
        {
            if (ErrorCode != emErrorCode.None)
                yield break;

            UpdateCompleteValue(0f, 1f);

            //下载地址重定向为根文件夹
            for (int i = 0; i < url_group_.Count; ++i)
                url_group_[i] = Common.CalcAssetBundleDownloadURL(url_group_[i]);

            //找到合适的资源服务器
            verifier_ = new URLVerifier(url_group_);
            verifier_.Start();
            while (!verifier_.IsDone)
            {
                yield return null;
            }
            current_url_ = verifier_.URL;
            if (string.IsNullOrEmpty(current_url_))
            {
                Error(emErrorCode.InvalidURL);
                yield break;
            }

            verifier_ = null;
            UpdateCompleteValue(1f, 1f);
        }

        /// <summary>
        ///   下载包资源
        /// </summary>
        IEnumerator StartDownloadPack()
        {
            if (ErrorCode != emErrorCode.None)
                yield break;

            UpdateCompleteValue(0f, 0f);

            if(packages_name_ == null)
            {
                Error(emErrorCode.InvalidPackageName);
                yield break;
            }

            //收集所有需要下载的AssetBundle
            List<string> ab_list = new List<string>();
            for (int i = 0; i < packages_name_.Count; ++i)
            {
                string pack_name = packages_name_[i];
                List<string> list = AssetBundleManager.Instance.FindAllAssetBundleFilesNameByPackage(pack_name);
                ab_list.AddRange(list);
            }
            if (ab_list == null)
            {
                Error(emErrorCode.NotFindAssetBundle);
                yield break;
            }

            //过滤已下载的资源
            ab_list.RemoveAll((assetbundle_name) =>
                {
                    return File.Exists(Common.GetFileFullName(assetbundle_name));
                });
            if (ab_list.Count == 0)
                yield break;

            //载入资源信息描述文件
            ResourcesManifest resources_manifest = AssetBundleManager.Instance.ResManifest;

            //开始下载
            ab_download_ = new AssetBundleDownloader(current_url_);
            ab_download_.Start(Common.PATH, ab_list, resources_manifest);
            while (!ab_download_.IsDone)
            {
                UpdateCompleteValue(ab_download_.CompletedSize, ab_download_.TotalSize);
                yield return null;
            }
            if (ab_download_.IsFailed)
            {
                Error(ab_download_.ErrorCode);
                yield break;
            }
            ab_download_ = null;

            yield return null;
        }

        /// <summary>
        /// 重置
        /// </summary>
        void Reset()
        {
            IsDone = false;
            ErrorCode = emErrorCode.None;
            CurrentState = emState.None;
            CurrentStateCompleteValue = 0f;
            CurrentStateTotalValue = 0f;
            current_url_ = "";
        }

        /// <summary>
        ///   结束
        /// </summary>
        void Done()
        {
            IsDone = true;
            OnDoneEvent();
        }

        /// <summary>
        ///   设置状态
        /// </summary>
        void UpdateState(emState state)
        {
            CurrentState = state;
            OnUpdateEvent();
        }

        /// <summary>
        ///   更新完成度
        /// </summary>
        void UpdateCompleteValue(float current)
        {
            UpdateCompleteValue(current, CurrentStateTotalValue);
        }
        /// <summary>
        ///   更新完成度
        /// </summary>
        void UpdateCompleteValue(float current, float total)
        {
            CurrentStateCompleteValue = current;
            CurrentStateTotalValue = total;
            OnUpdateEvent();
        }

        /// <summary>
        ///   更新
        /// </summary>
        void OnUpdateEvent()
        {
            if (OnUpdate != null)
                OnUpdate(this);
        }

        /// <summary>
        ///   结束事件
        /// </summary>
        void OnDoneEvent()
        {
            if (OnDone != null)
                OnDone(this);
        }

        /// <summary>
        ///   错误
        /// </summary>
        void Error(emErrorCode ec, string message = null)
        {
            ErrorCode = ec;

            string ms = string.IsNullOrEmpty(message) ?
                ErrorCode.ToString() : ErrorCode.ToString() + " - " + message;
            Debug.LogError(ms);
        }

        #region MonoBehaviour
        /// <summary>
        /// 
        /// </summary>
        void Awake()
        {
            Reset();
        }
        #endregion
    }
}