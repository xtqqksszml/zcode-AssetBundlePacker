/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/03/11
 * Note  : AssetBundle资源更新器, 用于游戏启动时自动更新游戏资源
***************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace zcode.AssetBundlePacker
{
    public class Updater : MonoBehaviour
    {
        /// <summary>
        ///   状态
        /// </summary>
        public enum emState
        {
            None,               // 无
            Initialize,         // 初始化更新器
            VerifyURL,          // 验证有效的URL
            DownloadMainConfig, // 下载主要的配置文件
            UpdateAssetBundle,  // 更新AssetBundle
            CopyCacheFile,      // 复制缓存下的文件
            Dispose,            // 后备工作
            Completed,          // 完成
            Failed,             // 失败
            Cancel,             // 取消
            Abort,              // 中断

            Max
        }


        /// <summary>
        /// 各个状态所占进度比率
        /// </summary>
        /// <remarks>
        /// {当前状态所占比率， 上个状态累计总比率}
        /// </remarks>
        static readonly float[,] STATE_PROGRESS_RATIO = new float[,]
        {
            { 0f, 0f},              // None
            { 0.025f, 0f},           // Initialize
            { 0.025f, 0.25f},        // VerifyURL
            { 0.025f, 0.05f},         // DownloadMainConfig
            { 0.85f, 0.1f},         // UpdateAssetBundle
            { 0.025f, 0.95f},         // CopyCacheFile
            { 0.025f, 0.975f},        // Dispose
            { 0f, 1f},              // Completed
            { 0f, 1f},              // Failed
            { 0f, 1f},              // Cancel
            { 0f, 1f},              // Abort
        };

        /// <summary>
        ///   UpdateEvent
        /// </summary>
        public event System.Action<Updater> OnUpdate;

        /// <summary>
        ///   DoneEvent
        /// </summary>
        public event System.Action<Updater> OnDone;

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
        ///   当前的完成的进度[0f, 1f]
        /// </summary>
        public float CurrentProgress { get; private set; }

        /// <summary>
        /// 下载地址列表
        /// </summary>
        private List<string> url_group_;

        /// <summary>
        ///   当前可用的下载地址
        /// </summary>
        private string current_url_;

        /// <summary>
        ///   
        /// </summary>
        private URLVerifier verifier_;

        /// <summary>
        ///   文件下载器
        /// </summary>
        private FileDownload file_download_;

        /// <summary>
        ///   资源下载器
        /// </summary>
        private AssetBundleDownloader ab_download_;

        /// <summary>
        /// 
        /// </summary>
        protected Updater()
        { }

        /// <summary>
        ///   开始更新
        /// </summary>
        public bool StartUpdate(string url)
        {
            if (!AssetBundleManager.Instance.IsReady)
                return false;
            if (!IsDone && CurrentState != emState.None)
                return false;

            List<string> url_group = new List<string>();
            url_group.Add(url);
            return StartUpdate(url_group);
        }

        /// <summary>
        ///   开始更新
        /// </summary>
        public bool StartUpdate(List<string> url_group)
        {
            if (!AssetBundleManager.Instance.IsReady)
                return false;
            if (!IsDone && CurrentState != emState.None)
                return false;

            url_group_ = url_group;
            current_url_ = null;

            if(AssetBundleManager.IsPlatformSupport)
            {
                StopAllCoroutines();
                StartCoroutine(Updating());
            }
            else
            {
                IsDone = true;
            }

            return true;
        }

        /// <summary>
        /// 重新开始更新
        /// </summary>
        public bool RestartUpdate()
        {
            if(!IsDone)
            {
                return false;
            }

            Reset();
            return StartUpdate(url_group_);
        }

        /// <summary>
        ///   取消更新
        /// </summary>
        public void CancelUpdate()
        {
            StopAllCoroutines();

            SaveDownloadCacheData();

            if (verifier_ != null)
            {
                verifier_.Abort();
                verifier_ = null;
            }
            if (file_download_ != null)
            {
                file_download_.Cancel();
                file_download_ = null;
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
        ///   
        /// </summary>
        public void AbortUpdate()
        {
            StopAllCoroutines();

            SaveDownloadCacheData();

            if (verifier_ != null)
            {
                verifier_.Abort();
                verifier_ = null;
            }
            if (file_download_ != null)
            {
                file_download_.Abort();
                file_download_ = null;
            }
            if (ab_download_ != null)
            {
                ab_download_.Abort();
                ab_download_ = null;

            }
            UpdateState(emState.Abort);
            Done();
        }

        /// <summary>
        ///   更新
        /// </summary>
        IEnumerator Updating()
        {
            UpdateState(emState.Initialize);
            yield return UpdatingInitialize();
            UpdateState(emState.VerifyURL);
            yield return UpdatingVerifyURL();
            UpdateState(emState.DownloadMainConfig);
            yield return UpdatingDownloadAllConfig();
            UpdateState(emState.UpdateAssetBundle);
            yield return UpdatingUpdateAssetBundle();
            UpdateState(emState.CopyCacheFile);
            yield return UpdatingCopyCacheFile();
            UpdateState(emState.Dispose);
            yield return UpdatingDispose();
            UpdateState(ErrorCode == emErrorCode.None ? emState.Completed : emState.Failed);

            Done();
        }

        /// <summary>
        ///   初始化更新器
        /// </summary>
        IEnumerator UpdatingInitialize()
        {
            if (ErrorCode != emErrorCode.None)
                yield break;

            UpdateCompleteValue(0f, 1f);

            //创建缓存目录
            if (!Directory.Exists(Common.UPDATER_CACHE_PATH))
                Directory.CreateDirectory(Common.UPDATER_CACHE_PATH);

            UpdateCompleteValue(1f, 1f);
            yield return null;
        }

        /// <summary>
        ///   开始进行资源URL检测
        /// </summary>
        IEnumerator UpdatingVerifyURL()
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
                Error(emErrorCode.InvalidURL, "Can't find valid Resources URL");
            }
            verifier_ = null;
            UpdateCompleteValue(1f, 1f);
            yield return null;
        }

        /// <summary>
        ///   开始进行主要文件下载,下载至缓存目录
        /// </summary>
        IEnumerator UpdatingDownloadAllConfig()
        {
            if (ErrorCode != emErrorCode.None)
                yield break;

            //下载主配置文件
            for (int i = 0; i < Common.CONFIG_NAME_ARRAY.Length; ++i )
            {
                file_download_ = new FileDownload(current_url_
                                        , Common.UPDATER_CACHE_PATH
                                        , Common.CONFIG_NAME_ARRAY[i]);
                file_download_.Start();
                while (!file_download_.IsDone)
                {
                    yield return null;
                }
                if (file_download_.IsFailed)
                {
                    if (Common.CONFIG_REQUIRE_CONDITION_ARRAY[i])
                    {
                        Error(emErrorCode.DownloadMainConfigFileFailed
                        , Common.CONFIG_NAME_ARRAY[i] + " download failed!");
                        yield break;
                    }

                    if (file_download_.ErrorCode == HttpAsyDownload.emErrorCode.DiskFull)
                    {
                        Error(emErrorCode.DiskFull);
                        yield break;
                    }
                }
                file_download_ = null;
                UpdateCompleteValue(i, Common.CONFIG_NAME_ARRAY.Length);
            }
           
            yield return null;
        }

        /// <summary>
        ///   更新AssetBundle
        /// </summary>
        IEnumerator UpdatingUpdateAssetBundle()
        {
            if (ErrorCode != emErrorCode.None)
                yield break;

            UpdateCompleteValue(0f, 0f);

            //载入新的ResourcesManifest
            ResourcesManifest old_resource_manifest = AssetBundleManager.Instance.ResManifest;
            string file = Common.UPDATER_CACHE_PATH + "/" + Common.RESOURCES_MANIFEST_FILE_NAME;
            ResourcesManifest new_resources_manifest = Common.LoadResourcesManifestByPath(file);
            if (new_resources_manifest == null)
            {
                Error(emErrorCode.LoadNewResourcesManiFestFailed
                    , "Can't load new verion ResourcesManifest!");
                yield break;
            }

            //载入MainManifest
            AssetBundleManifest manifest = AssetBundleManager.Instance.MainManifest;
            file = Common.UPDATER_CACHE_PATH + "/" + Common.MAIN_MANIFEST_FILE_NAME;
            AssetBundleManifest new_manifest = Common.LoadMainManifestByPath(file);
            if (new_manifest == null)
            {
                Error(emErrorCode.LoadNewMainManifestFailed
                    , "Can't find new version MainManifest!");
                yield break;
            }

            //获取需下载的资源列表与删除的资源的列表
            List<string> download_files = new List<string>();
            List<string> delete_files = new List<string>();
            ComparisonUtils.CompareAndCalcDifferenceFiles(ref download_files, ref delete_files
                                        , manifest, new_manifest
                                        , old_resource_manifest, new_resources_manifest
                                        , ComparisonUtils.emCompareMode.All);

            // 进度控制
            float totalProgress = delete_files.Count + download_files.Count;
            float currentProgress = 0;

            //载入下载缓存数据, 过滤缓存中已下载的文件
            DownloadCache download_cache = new DownloadCache();
            download_cache.Load(Common.DOWNLOADCACHE_FILE_PATH);
            if (!download_cache.HasData())
                download_cache = null;
            if (download_cache != null)
            {
                var cache_itr = download_cache.Data.AssetBundles.GetEnumerator();
                while (cache_itr.MoveNext())
                {
                    DownloadCacheData.AssetBundle elem = cache_itr.Current.Value;
                    string name = elem.AssetBundleName;
                    string full_name = Common.GetFileFullName(name);
                    if (File.Exists(full_name))
                    {
                        string cache_hash = elem.Hash;
                        string new_hash = new_manifest.GetAssetBundleHash(name).ToString();
                        if (!string.IsNullOrEmpty(cache_hash)
                                && cache_hash.CompareTo(new_hash) == 0)
                        {
                            download_files.Remove(name);
                            ++currentProgress;
                            UpdateCompleteValue(currentProgress, totalProgress);
                            yield return null;
                        }
                    }
                }
            }

            //删除已废弃的文件
            if (delete_files.Count > 0)
            {
                for (int i = 0; i < delete_files.Count; ++i)
                {
                    string full_name = Common.GetFileFullName(delete_files[i]);
                    if (File.Exists(full_name))
                    {
                        File.Delete(full_name);
                        ++currentProgress;
                        UpdateCompleteValue(currentProgress, totalProgress);
                        yield return null;
                    }
                }
            }

            //更新所有需下载的资源
            ab_download_ = new AssetBundleDownloader(current_url_);
            ab_download_.Start(Common.PATH, download_files, new_resources_manifest);
            while (!ab_download_.IsDone)
            {
                UpdateCompleteValue(currentProgress + ab_download_.CompleteDownloadList.Count, totalProgress);
                yield return null;
            }
            if (ab_download_.IsFailed)
            {
                Error(ab_download_.ErrorCode);
                yield break;
            }
        }

        /// <summary>
        ///   拷贝文件并覆盖旧数据文件
        /// </summary>
        IEnumerator UpdatingCopyCacheFile()
        {
            if (ErrorCode != emErrorCode.None) { yield break; }

            //从缓存中剪切主配置文件覆盖旧文件
            for (int i = 0; i < Common.CONFIG_NAME_ARRAY.Length; ++i)
            {
                UpdateCompleteValue(i, Common.CONFIG_NAME_ARRAY.Length);
                try
                {
                    var file = Common.CONFIG_NAME_ARRAY[i];
                    var src = Common.GetUpdaterCacheFileFullName(file);
                    var dest = Common.GetFileFullName(file);
                    if (File.Exists(dest))
                    {
                        File.Delete(dest);
                    }
                    File.Move(src, dest);
                }
                catch (System.Exception ex)
                {
                    if (Common.CONFIG_REQUIRE_CONDITION_ARRAY[i])
                    {
                        var message = Common.CONFIG_NAME_ARRAY[i] + ", " + ex.Message;
                        ErrorWriteFile(emIOOperateCode.Fail, message);
                        break;
                    }
                }
                
            }

            // 拷贝失败则需要把本地配置文件删除
            // （由于部分配置文件拷贝失败，会导致本地的配置文件不匹配会引起版本信息错误， 统一全部删除则下次进入游戏会重新从安装包拷贝全部数据）
            if (IsFailed)
            {
                for (int i = 0; i < Common.CONFIG_NAME_ARRAY.Length; ++i)
                {
                    var fileFullName = Common.GetFileFullName(Common.CONFIG_NAME_ARRAY[i]);
                    if (File.Exists(fileFullName))
                    {
                        File.Delete(fileFullName);
                    }
                }
            }
        }

        /// <summary>
        ///   清理
        /// </summary>
        IEnumerator UpdatingDispose()
        {
            UpdateCompleteValue(0f, 1f);

            if (ErrorCode != emErrorCode.None)
            {
                //缓存已下载内容,便于下次继续下载
                SaveDownloadCacheData();
            }
            else
            {
                //删除缓存目录
                if (Directory.Exists(Common.UPDATER_CACHE_PATH))
                    Directory.Delete(Common.UPDATER_CACHE_PATH, true);

                //重启AssetBundleManager
                AssetBundleManager.Instance.Relaunch();
                var abMgr = AssetBundleManager.Instance;
                while(!abMgr.WaitForLaunch())
                {
                    yield return null;
                }
                if(abMgr.IsFailed)
                {
                    if (abMgr.ErrorCode == zcode.AssetBundlePacker.emErrorCode.DiskFull)
                    {
                        Error(emErrorCode.DiskFull);
                    }
                    else
                    {
                        Error(abMgr.ErrorCode);
                    }
                }
            }

            UpdateCompleteValue(1f, 1f);
            yield return null;
        }

        void CutCacheFileToNative(string file)
        {
            try
            {
                var src = Common.GetUpdaterCacheFileFullName(file);
                var dest = Common.GetFileFullName(file);
                if(!File.Exists(src))
                {
                    return;
                }
                if(File.Exists(dest))
                {
                    File.Delete(dest);
                }
                File.Move(src, dest);
            }
            catch (System.Exception ex)
            {
                ErrorWriteFile(emIOOperateCode.Fail, ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void ErrorWriteFile(emIOOperateCode resultCode, string message)
        {
            if (resultCode == emIOOperateCode.DiskFull)
            {
                string ms = string.IsNullOrEmpty(message) ?
                "Disk Full!" : "Disk Full, " + message;
                Error(emErrorCode.DiskFull, ms);
            }
            else if (resultCode == emIOOperateCode.Fail)
            {
                string ms = string.IsNullOrEmpty(message) ?
                "WriteException!" : "WriteException, " + message;
                Error(emErrorCode.WriteException, ms);
            }
        }

        #region Other
        /// <summary>
        /// 
        /// </summary>
        void Reset()
        {
            IsDone = false;
            ErrorCode = emErrorCode.None;
            CurrentState = emState.None;
            CurrentProgress = 0f;
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
        void UpdateCompleteValue(float current, float total)
        {
            float ratio = STATE_PROGRESS_RATIO[(int)CurrentState, 0];
            float min = STATE_PROGRESS_RATIO[(int)CurrentState, 1];
            CurrentProgress = (current / total) * ratio + min;
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

            StringBuilder sb = new StringBuilder("[Updater] - ");
            sb.Append(ErrorCode.ToString());
            if (!string.IsNullOrEmpty(message)) { sb.Append("\n"); sb.Append(message); }
            Debug.LogError(sb.ToString());
        }

        /// <summary>
        ///   写入下载缓存信息，用于断点续传
        /// </summary>
        void SaveDownloadCacheData()
        {
            if (CurrentState < emState.UpdateAssetBundle)
                return;

            if (!Directory.Exists(Common.UPDATER_CACHE_PATH))
                return;

            //载入新的Manifest
            string new_manifest_name = Common.UPDATER_CACHE_PATH + "/" + Common.MAIN_MANIFEST_FILE_NAME;
            AssetBundleManifest new_manifest = Common.LoadMainManifestByPath(new_manifest_name);
            if (new_manifest == null)
                return;

            //先尝试读取旧的缓存信息，再保存现在已经下载的数据
            //PS:由于只有版本完整更新完才会移动Cache目录，且玩家可能多次尝试下载更新，所以必须保留旧的缓存信息
            DownloadCache cache = new DownloadCache();
            cache.Load(Common.DOWNLOADCACHE_FILE_PATH);
            if (ab_download_ != null
                && ab_download_.CompleteDownloadList != null
                && ab_download_.CompleteDownloadList.Count > 0)
            {
                for (int i = 0; i < ab_download_.CompleteDownloadList.Count; ++i)
                {
                    string assetbundle_name = ab_download_.CompleteDownloadList[i];
                    Hash128 hash_code = new_manifest.GetAssetBundleHash(assetbundle_name);
                    if (hash_code.isValid && !cache.Data.AssetBundles.ContainsKey(assetbundle_name))
                    {
                        DownloadCacheData.AssetBundle elem = new DownloadCacheData.AssetBundle()
                        {
                            AssetBundleName = assetbundle_name,
                            Hash = hash_code.ToString(),
                        };
                        Debug.Log(cache.Data.AssetBundles.Count + " - Cache Add:" + assetbundle_name);
                        cache.Data.AssetBundles.Add(assetbundle_name, elem);
                    }

                }
            }
            if (cache.HasData())
                cache.Save(Common.DOWNLOADCACHE_FILE_PATH);
        }
        #endregion

        #region MonoBehaviour
        /// <summary>
        ///   
        /// </summary>
        void Awake()
        {
            Reset();
        }

        /// <summary>
        /// 
        /// </summary>
        void OnDestroy()
        {
            AbortUpdate();
        }
        #endregion
    }
}