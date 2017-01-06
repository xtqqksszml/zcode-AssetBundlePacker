/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/18
 * Note  : AssetBundle资源管理
 *         负责游戏中的AssetBundle资源加载
***************************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace zcode.AssetBundlePacker
{
    /// <summary>
    ///   资源管理器
    /// </summary>
    public class AssetBundleManager : MonoSingleton<AssetBundleManager>
    {
        /// <summary>
        ///   最新的资源版本
        /// </summary>
        public uint Version;

        /// <summary>
        ///   是否准备完成
        /// </summary>
        public bool IsReady { get; private set; }

        /// <summary>
        ///   是否出错
        /// </summary>
        public bool IsFailed
        {
            get { return ErrorCode != emErrorCode.None; }
        }

        /// <summary>
        /// 
        /// </summary>
        public emErrorCode ErrorCode { get; private set; }

        /// <summary>
        ///   主AssetBundleMainfest
        /// </summary>
        public AssetBundleManifest MainManifest { get; private set; }

        /// <summary>
        ///   资源描述数据
        /// </summary>
        public ResourcesManifest ResourcesManifest { get; private set; }

        /// <summary>
        ///   资源包数据
        /// </summary>
        public ResourcesPackages ResourcesPackages { get; private set; }

        /// <summary>
        ///   常驻的AssetBundle
        /// </summary>
        private Dictionary<string, AssetBundle> assetbundle_permanent_;

        /// <summary>
        ///   缓存的AssetBundle
        /// </summary>
        private Dictionary<string, AssetBundle> assetbundle_cache_;

        /// <summary>
        /// 临时的AssetBundle
        /// </summary>
        private Dictionary<string, AssetBundle> assetbundle_temporary_;

        protected AssetBundleManager()
        { }

        /// <summary>
        /// 重启
        /// </summary>
        public bool Relaunch()
        {
            //必须处于启动状态或者异常才可以重启
            if (!(IsReady || IsFailed))
                return false;

            ShutDown();
            Launch();

            return true;
        }

        /// <summary>
        /// 等待启动完毕，启动完毕返回True,
        /// </summary>
        public bool WaitForLaunch()
        {
            if (IsReady || IsFailed)
                return true;

            return false;
        }

        /// <summary>
        ///   加载一个资源
        /// </summary>
        public T LoadAsset<T>(string asset, bool unload_assetbundle = true, bool unload_all_loaded_objects = false)
                where T : Object
        {
            try
            {
                if (!IsReady)
                    return null;

                asset = asset.ToLower();

                T result = null;
                string[] assetbundlesname = FindAllAssetBundleNameByAsset(asset);
                if (assetbundlesname != null)
                {
                    for (int i = 0; i < assetbundlesname.Length; ++i)
                    {
                        AssetBundle ab = LoadAssetBundleAndDependencies(assetbundlesname[i]);
                        if (ab != null)
                        {
                            result = ab.LoadAsset<T>(asset);
                            break;
                        }
                    }

                    DisposeAssetBundleTemporary(unload_assetbundle, unload_all_loaded_objects);
                }

                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("AssetBundleManager.LoadAsset is falid!\n" + ex.Message);
            }

            return null;
        }

        /// <summary>
        ///   异步加载一个资源
        /// </summary>
        public AssetBundleRequest LoadAssetAsync<T>(string asset, bool unload_assetbundle = true, bool unload_all_loaded_objects = false)
                where T : Object
        {
            try
            {
                if (!IsReady)
                    return null;

                asset = asset.ToLower();

                AssetBundleRequest result = null;
                string[] assetbundlesname = FindAllAssetBundleNameByAsset(asset);
                if (assetbundlesname != null)
                {
                    for (int i = 0; i < assetbundlesname.Length; ++i)
                    {
                        AssetBundle ab = LoadAssetBundleAndDependencies(assetbundlesname[i]);
                        if (ab != null)
                        {
                            result = ab.LoadAssetAsync<T>(asset);
                            break;
                        }
                    }

                    DisposeAssetBundleTemporary(unload_assetbundle, unload_all_loaded_objects);
                }

                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("AssetBundleManager.LoadAsset is falid!\n" + ex.Message);
            }

            return null;
        }

        /// <summary>
        ///   加载场景
        /// </summary>
        [System.Obsolete("Use AssetBundleManager.LoadSceneAsync, Because this function has bug!(UnityEngine.SceneManagement.SceneManager.LoadScene is not synchronization completed! )")]
        public bool LoadScene(string scene_name
                                , LoadSceneMode mode = LoadSceneMode.Single
                                , bool unload_assetbundle = true
                                , bool unload_all_loaded_objects = false)
        {
            try
            {
                if (!IsReady)
                    return false;

                string assetbundlesname = FindAssetBundleNameByScene(scene_name);
                if (!string.IsNullOrEmpty(assetbundlesname))
                {
                    AssetBundle ab = LoadAssetBundleAndDependencies(assetbundlesname);
                    if (ab == null)
                    {
                        Debug.LogWarning("AssetBundleManager.LoadScene() - Can't Load AssetBundle(" + assetbundlesname + ")");
                        return false;
                    }

                    if (!Application.CanStreamedLevelBeLoaded(scene_name))
                        return false;

                    UnityEngine.SceneManagement.SceneManager.LoadScene(scene_name);

                    DisposeAssetBundleTemporary(unload_assetbundle, unload_all_loaded_objects);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("AssetBundleManager.LoadAsset is falid!\n" + ex.Message);
            }

            return false;
        }

        /// <summary>
        ///   异步加载场景
        /// </summary>
        public AsyncOperation LoadSceneAsync(string scene_name
                                                , LoadSceneMode mode = LoadSceneMode.Single
                                                , bool unload_assetbundle = true
                                                , bool unload_all_loaded_objects = false)
        {
            try
            {
                if (!IsReady)
                    return null;

                string assetbundlesname = FindAssetBundleNameByScene(scene_name);
                if (!string.IsNullOrEmpty(assetbundlesname))
                {
                    AssetBundle ab = LoadAssetBundleAndDependencies(assetbundlesname);
                    if (ab == null)
                    {
                        Debug.LogWarning("AssetBundleManager.LoadScene() - Can't Load AssetBundle(" + assetbundlesname + ")");
                        return null;
                    }

                    AsyncOperation result = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene_name, mode);
                    DisposeAssetBundleTemporary(unload_assetbundle, unload_all_loaded_objects);
                    return result;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("AssetBundleManager.LoadAsset is falid!\n" + ex.Message);
            }

            return null;
        }

        /// <summary>
        ///   判断一个AssetBundle是否存在缓存
        /// </summary>
        public bool IsExist(string assetbundlename)
        {
            if (!IsReady)
                return false;

            if (string.IsNullOrEmpty(assetbundlename))
                return false;

            return File.Exists(Common.GetFileFullName(assetbundlename));
        }

        /// <summary>
        ///   判断一个资源是否存在于AssetBundle中
        /// </summary>
        public bool IsAssetExist(string asset)
        {
            if (!IsReady)
                return false;

            string[] assetbundlesname = FindAllAssetBundleNameByAsset(asset);
            if (assetbundlesname != null)
            {
                for (int i = 0; i < assetbundlesname.Length; ++i)
                    if (IsExist(assetbundlesname[i])) return true;
            }

            return false;
        }

        /// <summary>
        ///   判断场景是否存在于AssetBundle中
        /// </summary>
        public bool IsSceneExist(string scene_name)
        {
            return !string.IsNullOrEmpty(FindAssetBundleNameByScene(scene_name));
        }

        /// <summary>
        ///   获得AssetBundle中的所有资源
        /// </summary>
        public string[] FindAllAssetNames(string assetbundlename)
        {
            AssetBundle bundle = LoadAssetBundle(assetbundlename);
            if (bundle != null)
                return bundle.GetAllAssetNames();
            return null;
        }

        /// <summary>
        ///   获得包含某个资源的所有AssetBundle
        /// </summary>
        public string[] FindAllAssetBundleNameByAsset(string asset)
        {
            if (!IsReady)
                return null;

            if (ResourcesManifest == null)
                return null;

            return ResourcesManifest.GetAllAssetBundleName(asset);
        }

        /// <summary>
        ///   获得一个场景的包名
        /// </summary>
        public string FindAssetBundleNameByScene(string scene_name)
        {
            if (!IsReady)
                return null;

            if (ResourcesManifest == null)
                return null;

            return ResourcesManifest.GetAssetBundleNameBySceneLevelName(scene_name);
        }

        /// <summary>
        ///   获得指定资源包的AssetBundle列表
        /// </summary>
        public List<string> FindAllAssetBundleFilesNameByPackage(string package_name)
        {
            if (!IsReady)
                return null;

            if (ResourcesPackages == null)
                return null;

            ResourcesPackagesData.Package pack = ResourcesPackages.Find(package_name);
            if (pack == null)
                return null;

            List<string> result = new List<string>();
            for (int i = 0; i < pack.AssetList.Count; ++i)
            {
                string[] assetbundlename = FindAllAssetBundleNameByAsset(pack.AssetList[i]);
                if (assetbundlename != null && assetbundlename.Length > 0)
                {
                    if (!string.IsNullOrEmpty(assetbundlename[0]))
                    {
                        if (!result.Contains(assetbundlename[0]))
                        {
                            result.Add(assetbundlename[0]);
                        }
                    }
                }
            }

            return result.Count > 0 ? result : null;
        }

        /// <summary>
        ///   释放常驻的AssetBundle
        /// </summary>
        public void UnloadAllAssetBundle(bool unload_all_loaded_objects)
        {
            UnloadAssetBundleCache(unload_all_loaded_objects);
            UnloadAssetBundlePermanent(unload_all_loaded_objects);
        }

        /// <summary>
        ///   释放缓存的AssetBundle
        /// </summary>
        public void UnloadAssetBundleCache(bool unload_all_loaded_objects)
        {
            var itr = assetbundle_cache_.Values.GetEnumerator();
            while (itr.MoveNext())
            {
                itr.Current.Unload(unload_all_loaded_objects);
            }
            itr.Dispose();
            assetbundle_cache_.Clear();
        }

        /// <summary>
        ///   释放缓存的AssetBundle
        /// </summary>
        public void UnloadAssetBundle(string assetbundlename, bool unload_all_loaded_objects)
        {
            AssetBundle ab = null;
            if (assetbundle_permanent_.TryGetValue(assetbundlename, out ab))
                assetbundle_permanent_.Remove(assetbundlename);
            else if (assetbundle_cache_.TryGetValue(assetbundlename, out ab))
                assetbundle_cache_.Remove(assetbundlename);
            else if (assetbundle_temporary_.TryGetValue(assetbundlename, out ab))
                assetbundle_temporary_.Remove(assetbundlename);

            ab.Unload(unload_all_loaded_objects);
        }

        /// <summary>
        ///   加载有依赖的AssetBundle
        /// </summary>
        AssetBundle LoadAssetBundleAndDependencies(string assetbundlename)
        {
            if (assetbundlename == null)
                return null;
            if (MainManifest == null)
                return null;

            string[] deps = MainManifest.GetAllDependencies(assetbundlename);
            for (int index = 0; index < deps.Length; index++)
            {
                //加载所有的依赖AssetBundle
                if (LoadAssetBundle(deps[index]) == null)
                {
                    Debug.LogWarning(assetbundlename + "'s Dependencie AssetBundle can't find. Name is " + deps[index] + "!");
                    return null;
                }

            }

            return LoadAssetBundle(assetbundlename);
        }

        /// <summary>
        ///   加载AssetBundle
        /// </summary>
        AssetBundle LoadAssetBundle(string assetbundlename)
        {
            if (assetbundlename == null)
                return null;
            if (MainManifest == null)
                return null;

            AssetBundle ab = null;
            if (assetbundle_permanent_.ContainsKey(assetbundlename))
            {
                ab = assetbundle_permanent_[assetbundlename];
            }
            else if (assetbundle_cache_.ContainsKey(assetbundlename))
            {
                ab = assetbundle_cache_[assetbundlename];
            }
            else if (assetbundle_temporary_.ContainsKey(assetbundlename))
            {
                ab = assetbundle_temporary_[assetbundlename];
            }
            else
            {
                string assetbundle_path = Common.PATH + "/" + assetbundlename;
                if (System.IO.File.Exists(assetbundle_path))
                {
                    ab = AssetBundle.LoadFromFile(assetbundle_path);

                    //根据AssetBundleDescribe分别存放AssetBundle
                    bool permanent = ResourcesManifest.IsPermanent(assetbundlename);
                    if (permanent)
                        assetbundle_permanent_.Add(assetbundlename, ab);
                    else
                        assetbundle_temporary_.Add(assetbundlename, ab);
                }
            }

            return ab;
        }

        /// <summary>
        ///   释放常驻的AssetBundle
        /// </summary>
        void UnloadAssetBundlePermanent(bool unload_all_loaded_objects)
        {
            var itr = assetbundle_permanent_.Values.GetEnumerator();
            while (itr.MoveNext())
            {
                itr.Current.Unload(unload_all_loaded_objects);
            }
            itr.Dispose();
            assetbundle_permanent_.Clear();
        }

        /// <summary>
        /// 释放临时的AssetBundle
        /// </summary>
        void UnloadAssetBundleTemporary(bool unload_all_loaded_objects)
        {
            var itr = assetbundle_temporary_.Values.GetEnumerator();
            while (itr.MoveNext())
            {
                itr.Current.Unload(unload_all_loaded_objects);
            }
            itr.Dispose();
            assetbundle_temporary_.Clear();
        }

        /// <summary>
        /// 保存到缓存中
        /// </summary>
        void SaveAssetBundleToCache()
        {
            var itr = assetbundle_temporary_.GetEnumerator();
            while (itr.MoveNext())
            {
                assetbundle_cache_.Add(itr.Current.Key, itr.Current.Value);
            }
            itr.Dispose();
            assetbundle_temporary_.Clear();
        }

        /// <summary>
        /// 处理临时AssetBundle
        /// </summary>
        void DisposeAssetBundleTemporary(bool unload_assetbundle, bool unload_all_loaded_objects)
        {
            if (unload_assetbundle)
                UnloadAssetBundleTemporary(unload_all_loaded_objects);
            else
                SaveAssetBundleToCache();
        }

        /// <summary>
        /// 
        /// </summary>
        void Error(emErrorCode ec, string message = null)
        {
            ErrorCode = ec;

            string ms = string.IsNullOrEmpty(message) ?
                ErrorCode.ToString() : ErrorCode.ToString() + " - " + message;
            Debug.LogError(ms);
        }

        /// <summary>
        ///   启动(仅内部启用)
        /// </summary>
        void Launch()
        {
            if (assetbundle_permanent_ == null)
                assetbundle_permanent_ = new Dictionary<string, AssetBundle>();
            if (assetbundle_cache_ == null)
                assetbundle_cache_ = new Dictionary<string, AssetBundle>();
            if (assetbundle_temporary_ == null)
                assetbundle_temporary_ = new Dictionary<string, AssetBundle>();
            IsReady = false;
            ErrorCode = emErrorCode.None;
            StopAllCoroutines();
            StartCoroutine(Preprocess());
        }

        /// <summary>
        /// 关闭
        /// </summary>
        void ShutDown()
        {
            UnloadAllAssetBundle(true);
        }

        /// <summary>
        ///   初始化
        /// </summary>
        IEnumerator Preprocess()
        {
            //创建资源根目录
            if (!Directory.Exists(Common.PATH))
                Directory.CreateDirectory(Common.PATH);

            //判断主资源文件是否存在，不存在则拷贝备份资源至资源根目录
            bool do_initial_copy = false;
            string resources_manifest_file = Common.GetFileFullName(Common.RESOURCES_MANIFEST_FILE_NAME);
            if (!File.Exists(resources_manifest_file))
            {
                do_initial_copy = true;
            }
            else
            {
                // 拷贝安装包初始化目录中的ResourcesManifest，并判断是否重新拷贝初始化目录下的所有文件
                string full_name = Common.GetFileFullName(Common.RESOURCES_MANIFEST_FILE_NAME);
                string initial_full_name = Common.GetInitialFileFullName(Common.RESOURCES_MANIFEST_FILE_NAME);
                string cache_full_name = Common.GetCacheFileFullName(Common.RESOURCES_MANIFEST_FILE_NAME);
                yield return Common.StartCopyFile(initial_full_name, cache_full_name);
                
                //判断安装包初始目录是否完整
                ResourcesManifest initial = Common.LoadResourcesManifestByPath(cache_full_name);
                if (initial == null)
                {
                    Error(emErrorCode.PreprocessError
                        , "Initial path don't contains "
                            + Common.RESOURCES_MANIFEST_FILE_NAME + "!");
                    yield break;
                } 

                ResourcesManifest current = Common.LoadResourcesManifestByPath(full_name);
                if(current == null)
                    do_initial_copy = true;
                else if (current.Data.Version < initial.Data.Version)
                    do_initial_copy = true;

                //删除缓存中的文件
                if (File.Exists(cache_full_name))
                    File.Delete(cache_full_name);
            }

            if (do_initial_copy)
            {
                yield return CopyAllInitialFiles();
            }

            PreprocessFinished();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        bool PreprocessFinished()
        {
            //MainManifest
            MainManifest = Common.LoadMainManifest();
            if (MainManifest == null)
            {
                Error(emErrorCode.LoadMainManifestFailed
                    , "Can't load MainManifest file!");
                return false;
            }

            // ResourcesManifest
            ResourcesManifest = Common.LoadResourcesManifest();
            if (ResourcesManifest == null)
            {
                Error(emErrorCode.LoadResourcesManiFestFailed
                    , "Can't load ResourcesInfo file!");
                return false;
            }

            // ResourcesPackages
            ResourcesPackages = Common.LoadResourcesPackages();

            //记录当前版本号
            Version = ResourcesManifest.Data.Version;
            //标记已准备好
            IsReady = ErrorCode == emErrorCode.None;

            return true;
        }

        /// <summary>
        ///   拷贝初始目录所有文件
        /// </summary>
        IEnumerator CopyAllInitialFiles()
        {
            //拷贝所有配置文件
            for (int i = 0; i < Common.MAIN_CONFIG_NAME_ARRAY.Length; ++i)
                yield return Common.StartCopyInitialFile(Common.MAIN_CONFIG_NAME_ARRAY[i]);

            //拷贝AssetBundle文件
            ResourcesManifest resources_manifest = Common.LoadResourcesManifest();
            if (resources_manifest == null)
            {
                Debug.LogWarning("Can't load ResourcesManifest file!");
                yield break;
            }
            var itr = resources_manifest.Data.AssetBundles.GetEnumerator();
            while (itr.MoveNext())
            {
                if (itr.Current.Value.IsNative)
                {
                    string assetbundlename = itr.Current.Value.AssetBundleName;
                    string dest = Common.GetFileFullName(assetbundlename);

                    //保证路径存在
                    string directory = Path.GetDirectoryName(dest);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    //拷贝数据
                    yield return Common.StartCopyInitialFile(assetbundlename);
                }
            }
            itr.Dispose();
        }

        IEnumerator _WaitUnloadAssetBundleCacheFor(AsyncOperation ao)
        {
            while (!ao.isDone)
                yield return 1;

            UnloadAssetBundleCache(false);
        }

        #region MonoBahaviour
        /// <summary>
        ///   
        /// </summary>
        void Awake()
        {
            Launch();
        }

        /// <summary>
        ///   
        /// </summary>
        void OnDestroy()
        {
            ShutDown();
        }
        #endregion
    }

}
