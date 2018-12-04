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
using System.Text;

namespace zcode.AssetBundlePacker
{
    /// <summary>
    ///   资源管理器
    /// </summary>
    public class AssetBundleManager : MonoSingleton<AssetBundleManager>
    {
#if UNITY_EDITOR
        /// <summary>
        /// 当前平台是否支持AssetBundle
        /// </summary>
        public static readonly bool IsPlatformSupport = true;
#elif UNITY_STANDALONE_WIN
        /// <summary>
        /// 当前平台是否支持AssetBundle
        /// </summary>
        public static readonly bool IsPlatformSupport = true; 
#elif UNITY_IPHONE
        /// <summary>
        /// 当前平台是否支持AssetBundle
        /// </summary>
        public static readonly bool IsPlatformSupport = true; 
#elif UNITY_ANDROID
        /// <summary>
        /// 当前平台是否支持AssetBundle
        /// </summary>
        public static readonly bool IsPlatformSupport = true; 
#endif

        /// <summary>
        ///   最新的资源版本(已弃用)
        /// </summary>
        [System.Obsolete("Use AssetBundleManager.strVersion")]
        public int Version;

        /// <summary>
        ///   最新的资源版本
        /// </summary>
        public string strVersion;

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
        public ResourcesManifest ResManifest { get; private set; }

        /// <summary>
        ///   资源包数据
        /// </summary>
        public ResourcesPackages ResPackages { get; private set; }

        /// <summary>
        ///   常驻的AssetBundle
        /// </summary>
        private Dictionary<string, AssetBundle> assetbundle_permanent_;

        /// <summary>
        ///   缓存的AssetBundle
        /// </summary>
        private Dictionary<string, Cache> assetbundle_cache_;

        /// <summary>
        /// 缓存的资源对照表
        /// Key: 资源名称
        /// Value: 资源依赖缓存（包含引用的AssetBundle名称与及被引用次数）
        /// </summary>
        private Dictionary<string, AssetDependCache> asset_dependency_cache_;

        /// <summary>
        /// 正在异步载入的AssetBundle
        /// </summary>
        private HashSet<string> assetbundle_async_loading_;

        /// <summary>
        /// 
        /// </summary>
        protected AssetBundleManager()
        { }

        /// <summary>
        /// 重启
        /// </summary>
        public bool Relaunch()
        {
            //必须处于启动状态或者异常才可以重启
            if (!IsReady)
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
            if (IsReady)
                return true;

            return false;
        }

        /// <summary>
        ///   加载一个资源
        /// </summary>
        public T LoadAsset<T>(string asset, bool unload_assetbundle = true)
                where T : Object
        {
            try
            {
                if(!IsPlatformSupport)
                {
                    return null;
                }
                if (!IsReady || IsFailed)
                {
                    return null;
                }

                asset = asset.ToLower();

                // 加载AssetBundle
                string assetbundlename = null;
                string[] all_assetbundle = FindAllAssetBundleNameByAsset(asset);
                if (all_assetbundle != null)
                {
                    for (int i = 0; i < all_assetbundle.Length; ++i)
                    {
                        if (CanLoadAssetBundleAndDependencies(all_assetbundle[i]))
                        {
                            assetbundlename = all_assetbundle[i];
                            break;
                        }
                    }
                }
                if (assetbundlename == null)
                {
                    Debug.LogWarning("AssetBundle can't find. Asset name is (" + asset + ")!");
                    return null;
                }

                // 加载依赖
                string[] deps = LoadDependenciesAssetBundle(assetbundlename);
                
                // 加载AssetBundle
                var ab = LoadAssetBundle(assetbundlename);
                // 加载资源
                T result = ab.LoadAsset<T>(asset);

                // 卸载AssetBundle
                if (unload_assetbundle)
                {
                    DisposeAssetBundleCache(deps, false);
                    DisposeAssetBundleCache(assetbundlename, false);
                }
                else
                {
                    SaveAssetDependency(asset, assetbundlename);
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
        public AssetLoadRequest LoadAssetAsync(string asset, bool unload_assetbundle = true)
        {
            try
            {
                if (!IsPlatformSupport)
                {
                    return null;
                }
                if (!IsReady || IsFailed)
                {
                    return null;
                }

                /// 转小写
                string assetName = asset.ToLower();

                ///判断此asset是否拥有可加载的AssetBundle
                string assetbundlename = null;
                string[] all_asssetbundle = FindAllAssetBundleNameByAsset(assetName);
                if (all_asssetbundle != null)
                {
                    for (int i = 0; i < all_asssetbundle.Length; ++i)
                    {
                        if (CanLoadAssetBundleAndDependencies(all_asssetbundle[i]))
                        {
                            assetbundlename = all_asssetbundle[i];
                            break;
                        }
                    }
                }
                if (string.IsNullOrEmpty(assetbundlename))
                {
                    return null;
                }

                AssetAsyncLoader loader = new AssetAsyncLoader(assetbundlename, assetName, asset, unload_assetbundle);
                AssetLoadRequest req = new AssetLoadRequest(loader);
                StartCoroutine(StartLoadAssetAsync(loader));
                return req;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("AssetBundleManager.LoadAsset is falid!\n" + ex.Message);
            }

            return null;
        }

        /// <summary>
        /// 卸载一个资源
        /// </summary>
        public void UnloadAsset(string asset)
        {
            asset = asset.ToLower();
            AssetDependCache cache;
            if (asset_dependency_cache_.TryGetValue(asset, out cache))
            {
                //减少资源缓存引用
                if (cache.RefCount == 1)
                {
                    asset_dependency_cache_.Remove(asset);
                }
                else
                {
                    cache.RefCount -= 1;
                    asset_dependency_cache_[asset] = cache;
                }

                //处理AssetBundle
                DisposeAssetBundleCache(cache.RefAssetBundleName, true);
                string[] deps = MainManifest.GetAllDependencies(cache.RefAssetBundleName);
                DisposeAssetBundleCache(deps, true);
            }
        }

        /// <summary>
        /// 卸载一个AssetBundle
        /// </summary>
        public void UnloadAssetBundle(string assetbundleName, bool unload_all_dependencies_ab, bool unload_all_loaded_objects)
        {
            //卸载AssetBundle
            bool is_unload = false;
            Cache cache;
            if (assetbundle_cache_.TryGetValue(assetbundleName, out cache))
            {
                cache.RefAssetBundle.Unload(unload_all_loaded_objects);
                assetbundle_cache_.Remove(assetbundleName);
                is_unload = true;
            }
            else
            {
                AssetBundle ab = null;
                if (assetbundle_permanent_.TryGetValue(assetbundleName, out ab))
                {
                    ab.Unload(unload_all_loaded_objects);
                    assetbundle_permanent_.Remove(assetbundleName);
                    is_unload = true;
                }
            }
            //卸载依赖的AssetBundle
            if(is_unload && unload_all_dependencies_ab)
            {
                string[] deps = MainManifest.GetAllDependencies(assetbundleName);
                for (int i = 0; i < deps.Length; ++i)
                {
                    string dep_assetbundleName = deps[i];
                    if (assetbundle_cache_.TryGetValue(dep_assetbundleName, out cache))
                    {
                        cache.RefAssetBundle.Unload(unload_all_loaded_objects);
                        assetbundle_cache_.Remove(dep_assetbundleName);
                    }
                    else
                    {
                        AssetBundle ab = null;
                        if (assetbundle_permanent_.TryGetValue(dep_assetbundleName, out ab))
                        {
                            ab.Unload(unload_all_loaded_objects);
                            assetbundle_permanent_.Remove(dep_assetbundleName);
                        }
                    }
                }
            }
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
            return false;
        }

        /// <summary>
        ///   异步加载场景
        /// </summary>
        public AsyncOperation LoadSceneAsync(string scene_name
                                                , LoadSceneMode mode = LoadSceneMode.Single
                                                , bool unload_assetbundle = true)
        {
            try
            {
                if (!IsPlatformSupport)
                {
                    return null;
                }
                if (!IsReady || IsFailed)
                    return null;

                string assetbundlename = FindAssetBundleNameByScene(scene_name);
                if (!string.IsNullOrEmpty(assetbundlename))
                {
                    // 加载依赖
                    string[] deps = LoadDependenciesAssetBundle(assetbundlename);
                    // 加载AssetBundle
                    LoadAssetBundle(assetbundlename);
                    // 加载场景
                    AsyncOperation result = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene_name, mode);

                    // 卸载AssetBundle
                    if (unload_assetbundle)
                    {
                        DisposeAssetBundleCache(deps, false);
                        DisposeAssetBundleCache(assetbundlename, false);
                    }
                    else
                    {
                        SaveAssetDependency(scene_name, assetbundlename);
                    }
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
        /// 卸载一个场景资源
        /// </summary>
        public void UnloadScene(string scene_name)
        {
            scene_name = scene_name.ToLower();
            AssetDependCache cache;
            if (asset_dependency_cache_.TryGetValue(scene_name, out cache))
            {
                //减少资源缓存引用
                if (cache.RefCount == 1)
                {
                    asset_dependency_cache_.Remove(scene_name);
                }
                else
                {
                    cache.RefCount -= 1;
                    asset_dependency_cache_[scene_name] = cache;
                }


                DisposeAssetBundleCache(cache.RefAssetBundleName, true);
                string[] deps = MainManifest.GetAllDependencies(cache.RefAssetBundleName);
                DisposeAssetBundleCache(deps, true);
            }
        }

        /*
        /// <summary>
        ///   异步加载场景
        /// </summary>
        public SceneLoadRequest LoadSceneAsync(string scene_name
                                                , LoadSceneMode mode = LoadSceneMode.Single
                                                , bool unload_assetbundle = true
                                                , bool unload_all_loaded_objects = false)
        {
            try
            {
                if (!IsReady)
                {
                    return null;
                }

                string assetbundlename = FindAssetBundleNameByScene(scene_name);
                if (string.IsNullOrEmpty(assetbundlename))
                {
                    return null;
                }
                if (!CanLoadAssetBundleAndDependencies(assetbundlename))
                {
                    return null;
                }

                SceneAsyncLoader loader = new SceneAsyncLoader(assetbundlename
                    , scene_name, mode, unload_assetbundle);
                SceneLoadRequest req = new SceneLoadRequest(loader);
                StartCoroutine("StartLoadAssetAsync", loader);
                return req;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("AssetBundleManager.LoadAsset is falid!\n" + ex.Message);
            }

            return null;
        }
        */

        /// <summary>
        ///   判断一个AssetBundle是否存在缓存
        /// </summary>
        public bool IsExist(string assetbundlename)
        {
            if (string.IsNullOrEmpty(assetbundlename))
                return false;

            return File.Exists(Common.GetFileFullName(assetbundlename));
        }

        /// <summary>
        ///   判断一个资源是否存在于AssetBundle中
        /// </summary>
        public bool IsAssetExist(string asset)
        {
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
            if (ResManifest == null)
            {
                return null;
            }

            return ResManifest.GetAllAssetBundleName(asset);
        }

        /// <summary>
        ///   获得一个场景的包名
        /// </summary>
        public string FindAssetBundleNameByScene(string scene_name)
        {
            if (ResManifest == null)
            {
                return null;
            }

            return ResManifest.GetAssetBundleNameBySceneLevelName(scene_name);
        }

        /// <summary>
        ///   获得指定资源包的AssetBundle列表
        /// </summary>
        public List<string> FindAllAssetBundleFilesNameByPackage(string package_name)
        {
            if (ResPackages == null)
            {
                return null;
            }

            ResourcesPackagesData.Package pack = ResPackages.Find(package_name);
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
        ///   释放所有的AssetBundle
        /// </summary>
        [System.Obsolete("Use AssetBundleManager.UnloadAllAssetBundle")]
        public void UnloadAssetBundle(bool unload_all_loaded_objects)
        {
            UnloadAssetBundleCache(unload_all_loaded_objects);
            UnloadAssetBundlePermanent(unload_all_loaded_objects);
        }

        /// <summary>
        ///   释放所有的AssetBundle
        /// </summary>
        public void UnloadAllAssetBundle(bool unload_all_loaded_objects)
        {
            UnloadAssetBundleCache(unload_all_loaded_objects);
            UnloadAssetBundlePermanent(unload_all_loaded_objects);
        }

        /// <summary>
        ///   释放所有缓存的AssetBundle
        /// </summary>
        public void UnloadAssetBundleCache(bool unload_all_loaded_objects)
        {
            if(assetbundle_cache_ != null && assetbundle_cache_.Count > 0)
            {
                var itr = assetbundle_cache_.Values.GetEnumerator();
                while (itr.MoveNext())
                {
                    itr.Current.RefAssetBundle.Unload(unload_all_loaded_objects);
                }
                itr.Dispose();
                assetbundle_cache_.Clear();
            }
        }

        /// <summary>
        ///   加载所有依赖的AssetBundle
        /// </summary>
        string[] LoadDependenciesAssetBundle(string assetbundlename)
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
                    Debug.LogWarning(assetbundlename + "'s Dependencie AssetBundle can't find. Name is (" + deps[index] + ")!");
                    return null;
                }
            }

            return deps;
        }

        /// <summary>
        ///   加载AssetBundle
        /// </summary>
        AssetBundle LoadAssetBundle(string assetbundlename)
        {
            if (assetbundlename == null)
                return null;

            ///判断此AssetBundle是否正在被异步加载，则等待加载完成
            bool isLoading = assetbundle_async_loading_.Contains(assetbundlename);
            if (isLoading)
            {
                while (assetbundle_async_loading_.Contains(assetbundlename) == true)
                {
                }
            }

            AssetBundle ab = FindLoadedAssetBundle(assetbundlename);
            if (ab == null)
            {
                string assetbundle_path = GetAssetBundlePath(assetbundlename);
                if (System.IO.File.Exists(assetbundle_path))
                {
                    ab = AssetBundle.LoadFromFile(assetbundle_path);
                }
            }
            SaveAssetBundle(assetbundlename, ab);

            return ab;
        }

        /// <summary>
        ///   异步加载一个AssetBundle
        /// </summary>
        IEnumerator LoadAssetBundleAsync(string assetbundlename)
        {
            if (assetbundlename == null)
                yield break;

            ///判断此AssetBundle是否正在被异步加载，则等待加载完成
            bool isLoading = assetbundle_async_loading_.Contains(assetbundlename);
            if (isLoading)
            {
                while (assetbundle_async_loading_.Contains(assetbundlename) == true)
                {
                    yield return null;
                }
            }

            AssetBundle ab = FindLoadedAssetBundle(assetbundlename);
            if (ab == null)
            {
                ///没有此AssetBundle缓存，开始异步加载
                assetbundle_async_loading_.Add(assetbundlename);
                string path = GetAssetBundlePath(assetbundlename);
                var req = AssetBundle.LoadFromFileAsync(path);
                while (!req.isDone)
                {
                    yield return null;
                }
                ab = req.assetBundle;
                SaveAssetBundle(assetbundlename, ab);
                assetbundle_async_loading_.Remove(assetbundlename);
            }
            else
            {
                SaveAssetBundle(assetbundlename, ab);
            }

            yield break;
        }

        /// <summary>
        ///   释放所有常驻的AssetBundle
        /// </summary>
        void UnloadAssetBundlePermanent(bool unload_all_loaded_objects)
        {
            if(assetbundle_permanent_ != null && assetbundle_permanent_.Count > 0)
            {
                var itr = assetbundle_permanent_.Values.GetEnumerator();
                while (itr.MoveNext())
                {
                    itr.Current.Unload(unload_all_loaded_objects);
                }
                itr.Dispose();
                assetbundle_permanent_.Clear();
            }
        }

        /// <summary>
        /// 保存AssetBundle到加载队列
        /// </summary>
        void SaveAssetBundle(string assetbundlename, AssetBundle ab)
        {
            //根据AssetBundleDescribe分别存放AssetBundle
            bool permanent = ResManifest.IsPermanent(assetbundlename);
            if (permanent)
            {
                if (!assetbundle_permanent_.ContainsKey(assetbundlename))
                {
                    assetbundle_permanent_.Add(assetbundlename, ab);
                }
            }
            else
            {
                SaveAssetBundleToCache(assetbundlename, ab);
            }
        }

        /// <summary>
        /// 保存资源依赖，用于后续卸载资源
        /// </summary>
        void SaveAssetDependency(string asset, string assetbundle)
        {
            int refCount = 0;
            if (asset_dependency_cache_.ContainsKey(asset))
            {
                refCount = asset_dependency_cache_[asset].RefCount;
            }
            ++refCount;

            asset_dependency_cache_[asset] = new AssetDependCache()
            {
                RefCount = refCount,
                RefAssetBundleName = assetbundle,
            };
        }

        /// <summary>
        /// 保存到缓存中
        /// </summary>
        void SaveAssetBundleToCache(string assetbundleName, AssetBundle ab)
        {
            int refCount = 0;
            if (assetbundle_cache_.ContainsKey(assetbundleName))
            {
                refCount = assetbundle_cache_[assetbundleName].RefCount;
            }
            ++refCount;

            assetbundle_cache_[assetbundleName] = new Cache()
            {
                RefCount = refCount,
                RefAssetBundle = ab,
            };
        }

        /// <summary>
        /// 处理缓存的多个AssetBundle, 如果没有引用则卸载
        /// </summary>
        void DisposeAssetBundleCache(string[] assetbundlesName, bool unload_all_loaded_objects)
        {
            if(assetbundlesName != null && assetbundlesName.Length > 0)
            {
                for (int index = 0; index < assetbundlesName.Length; index++)
                {
                    DisposeAssetBundleCache(assetbundlesName[index], unload_all_loaded_objects);
                }
            }
        }

        /// <summary>
        /// 处理缓存的AssetBundle, 如果没有引用则卸载
        /// </summary>
        void DisposeAssetBundleCache(string assetbundleName, bool unload_all_loaded_objects)
        {
            Cache cache;
            if (assetbundle_cache_.TryGetValue(assetbundleName, out cache))
            {
                if (cache.RefCount == 1)
                {
                    cache.RefAssetBundle.Unload(unload_all_loaded_objects);
                    assetbundle_cache_.Remove(assetbundleName);
                }
                else
                {
                    cache.RefCount -= 1;
                    assetbundle_cache_[assetbundleName] = cache;
                }
            }
        }

        /// <summary>
        ///   查找是否有已载加的AssetBundle
        /// </summary>
        AssetBundle FindLoadedAssetBundle(string assetbundlename)
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
                ab = assetbundle_cache_[assetbundlename].RefAssetBundle;
            }

            return ab;
        }

        /// <summary>
        /// 获得AssetBundle的依赖
        /// </summary>
        public string[] GetAllDependencies(string assetbundlename)
        {
            if (assetbundlename == null)
                return null;
            if (MainManifest == null)
                return null;

            return MainManifest.GetAllDependencies(assetbundlename);
        }

        /// <summary>
        /// 获得AssetBundle的依赖
        /// </summary>
        public bool HasDependencies(string assetbundlename)
        {
            var deps = GetAllDependencies(assetbundlename);
            return deps != null && deps.Length > 0;
        }

        /// <summary>
        ///   判断本地是否包含所有依赖
        /// </summary>
        bool CanLoadAssetBundleAndDependencies(string assetbundlename)
        {
            if (assetbundlename == null)
                return false;
            if (MainManifest == null)
                return false;

            string assetbundle_path = GetAssetBundlePath(assetbundlename);
            if (!System.IO.File.Exists(assetbundle_path))
            {
                return false;
            }

            string[] deps = MainManifest.GetAllDependencies(assetbundlename);
            for (int index = 0; index < deps.Length; index++)
            {
                AssetBundle ab = FindLoadedAssetBundle(deps[index]);
                if (ab == null)
                {
                    string path = GetAssetBundlePath(deps[index]);
                    if (!System.IO.File.Exists(path))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///   异步加载一个资源
        /// </summary>
        IEnumerator StartLoadAssetAsync(AssetAsyncLoader loader)
        {
            yield return loader.StartLoadAssetAsync(this);
        }

        /// <summary>
        /// 获得AssetBundle的路径
        /// </summary>
        static string GetAssetBundlePath(string assetbundlename)
        {
            return Common.PATH + "/" + assetbundlename;
        }

        /// <summary>
        /// 版本号比较
        /// </summary>
        public static int CompareVersion(string ver1, string ver2)
        {
            if (string.IsNullOrEmpty(ver1))
            {
                return -1;
            }
            if (string.IsNullOrEmpty(ver2))
            {
                return 1;
            }

            string[] arrVer1 = ver1.Split('.');
            if (arrVer1.Length == 0)
            {
                return -1;
            }
            string[] arrVer2 = ver2.Split('.');
            if (arrVer2.Length == 0)
            {
                return 1;
            }

            int length = System.Math.Min(arrVer1.Length, arrVer2.Length);
            for (int i = 0; i < length; ++i)
            {
                int intVer1 = System.Convert.ToInt32(arrVer1[i]);
                int intVer2 = System.Convert.ToInt32(arrVer2[i]);
                if (intVer1 < intVer2)
                {
                    return -1;
                }
                else if (intVer1 > intVer2)
                {
                    return 1;
                }
            }

            if (arrVer1.Length < arrVer2.Length)
            {
                return -1;
            }
            if (arrVer1.Length > arrVer2.Length)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Error(emErrorCode ec, string message = null)
        {
            ErrorCode = ec;

            StringBuilder sb = new StringBuilder("[AssetBundleManager] - ");
            sb.Append(ErrorCode.ToString());
            if (!string.IsNullOrEmpty(message)) { sb.Append("\n"); sb.Append(message); }
            Debug.LogError(sb.ToString());
        }

        /// <summary>
        ///   启动(仅内部启用)
        /// </summary>
        void Launch()
        {
            if (assetbundle_permanent_ == null)
            { assetbundle_permanent_ = new Dictionary<string, AssetBundle>(); }
            if (assetbundle_cache_ == null)
            { assetbundle_cache_ = new Dictionary<string, Cache>(); }
            if (asset_dependency_cache_ == null)
            { asset_dependency_cache_ = new Dictionary<string, AssetDependCache>(); }
            if (assetbundle_async_loading_ == null)
            { assetbundle_async_loading_ = new HashSet<string>(); }
            strVersion = "";
            IsReady = false;
            ErrorCode = emErrorCode.None;

            if(IsPlatformSupport)
            {
                StopAllCoroutines();
                StartCoroutine(Preprocess());
            }
            else
            {
                IsReady = true;
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        void ShutDown()
        {
            StopAllCoroutines();
            UnloadAllAssetBundle(true);
        }

        #region Loader
        /// <summary>
        /// 资源异步加载器
        /// </summary>
        internal class AssetAsyncLoader
        {
            /// <summary>
            /// AssetBundle名称
            /// </summary>
            public string AssetBundleName { get; private set; }

            /// <summary>
            /// 资源名称(全部小写)
            /// </summary>
            public string AssetName { get; private set; }

            /// <summary>
            /// 资源名称（原始名称）
            /// </summary>
            public string OrignalAssetName { get; private set; }

            /// <summary>
            /// 加载完成卸载AssetBundle
            /// </summary>
            public bool UnloadAssetBundle { get; private set; }

            /// <summary>
            /// 是否加载完成?
            /// </summary>
            public bool IsDone { get; private set; }

            /// <summary>
            /// 加载进度.
            /// </summary>
            public float Progress { get; private set; }

            /// <summary>
            /// 已加载的资源
            /// </summary>
            public Object Asset { get; private set; }

            /// <summary>
            /// 已加载的子资源
            /// </summary>
            public Object[] AllAssets { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            public AssetAsyncLoader(string assetbundlename, string asset_name, string orignal_asset_name, bool unload_assetbundle)
            {
                AssetBundleName = assetbundlename;
                AssetName = asset_name;
                OrignalAssetName = orignal_asset_name;
                UnloadAssetBundle = unload_assetbundle;
                IsDone = false;
                Progress = 0f;
                Asset = null;
                AllAssets = null;
            }

            /// <summary>
            ///   加载一个资源
            /// </summary>
            public IEnumerator StartLoadAssetAsync(AssetBundleManager mgr)
            {
                if (string.IsNullOrEmpty(AssetBundleName))
                {
                    IsDone = true;
                    yield break;
                }

                ///标记未完成
                IsDone = false;
                Progress = 0f;

                /// 加载依赖的assetbundle
                float current = 0;
                float count = 2;
                string[] deps = mgr.GetAllDependencies(AssetBundleName);
                if (deps != null)
                {
                    count += deps.Length;
                    
                    for (int index = 0; index < deps.Length; index++)
                    {
                        yield return mgr.LoadAssetBundleAsync(deps[index]);
                        Progress = ++current / count;
                    }
                }
                /// 加载assetbundle
                {
                    yield return mgr.LoadAssetBundleAsync(AssetBundleName);
                    Progress = ++current / count;
                }
                /// 加载资源
                {
                    var ab = mgr.FindLoadedAssetBundle(AssetBundleName);
                    if (ab == null)
                    {
                        IsDone = true;

                        yield break;
                    }
                    var req = ab.LoadAssetAsync(AssetName);
                    while (!req.isDone)
                    {
                        yield return req;
                    }
                    Asset = req.asset;
                    AllAssets = req.allAssets;
                    Progress = ++current / count;
                }
                if (UnloadAssetBundle)
                {
                    mgr.DisposeAssetBundleCache(deps, false);
                    mgr.DisposeAssetBundleCache(AssetBundleName, false);
                }
                else
                {
                    mgr.SaveAssetDependency(AssetName, AssetBundleName);
                }

                IsDone = true;
                Progress = 1;
                yield break;
            }
        }

        /// <summary>
        /// 场景异步加载器
        /// </summary>
        internal class SceneAsyncLoader
        {
            /// <summary>
            /// AssetBundle名称
            /// </summary>
            public string AssetBundleName { get; private set; }

            /// <summary>
            /// 场景名称
            /// </summary>
            public string SceneName { get; private set; }

            /// <summary>
            /// 加载场景模式
            /// </summary>
            public LoadSceneMode LoadMode { get; private set; }

            /// <summary>
            /// 加载完成卸载AssetBundle
            /// </summary>
            public bool UnloadAssetBundle { get; private set; }

            /// <summary>
            /// 是否加载完成?
            /// </summary>
            public bool IsDone { get; private set; }

            /// <summary>
            /// 加载进度.
            /// </summary>
            public float Progress { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            public SceneAsyncLoader(string assetbundlename, string scene_name
                , LoadSceneMode mode
                , bool unload_assetbundle)
            {
                AssetBundleName = assetbundlename;
                SceneName = scene_name;
                LoadMode = mode;
                UnloadAssetBundle = unload_assetbundle;
                IsDone = false;
                Progress = 0f;
            }

            /// <summary>
            /// 加载一个场景
            /// </summary>
            public IEnumerator StartLoadSceneAsync(AssetBundleManager mgr)
            {
                if (string.IsNullOrEmpty(AssetBundleName))
                {
                    IsDone = true;
                    yield break;
                }

                ///标记未完成
                IsDone = false;
                Progress = 0f;

                ///进度计算
                float current = 0;
                float count = 2;
                string[] deps = mgr.GetAllDependencies(AssetBundleName);
                if (deps != null)
                {
                    count += deps.Length;

                    /// 加载依赖的assetbundle
                    for (int index = 0; index < deps.Length; index++)
                    {
                        yield return mgr.LoadAssetBundleAsync(deps[index]);
                        Progress = ++current / count;
                    }
                }

                /// 加载assetbundle
                {
                    yield return mgr.LoadAssetBundleAsync(AssetBundleName);
                    Progress = ++current / count;
                }

                // 加载场景
                {
                    AsyncOperation async = SceneManager.LoadSceneAsync(SceneName, LoadMode);
                    while (!async.isDone)
                    {
                        yield return async;
                    }
                    Progress = ++current / count;
                }

                if (UnloadAssetBundle)
                {
                    mgr.DisposeAssetBundleCache(deps, false);
                    mgr.DisposeAssetBundleCache(AssetBundleName, false);
                }
                else
                {
                    mgr.SaveAssetDependency(SceneName, AssetBundleName);
                }

                IsDone = true;
                Progress = 1;

                yield break;
            }
        }
        #endregion

        #region Cache
        /// <summary>
        /// AssetBundle引用缓存结构
        /// </summary>
        struct Cache
        {
            public int RefCount;
            public AssetBundle RefAssetBundle;
        }

        /// <summary>
        /// 资源依赖缓存
        /// </summary>
        struct AssetDependCache
        {
            public int RefCount;
            public string RefAssetBundleName;
        }

        #endregion

        #region Preprocess
        /// <summary>
        /// 状态
        /// </summary>
        public enum emPreprocessState
        {
            None,               // 无
            Install,            // 安装包资源初始化
            Update,             // 最新版本资源拷贝
            Load,               // 游戏初始资源加载
            Dispose,            // 后备工作
            Completed,          // 完成
            Failed,             // 失败
        }

        /// <summary>
        /// 预加载信息
        /// </summary>
        public class PreprocessInformation
        {
            /// <summary>
            ///   当前状态
            /// </summary>
            public emPreprocessState State { get; private set; }

            /// <summary>
            ///   当前状态的进度
            /// </summary>
            public float Progress { get; private set; }

            /// <summary>
            ///   当前状态的的总量值
            /// </summary>
            public float Total { get; private set; }

            /// <summary>
            ///   当前状态的进度
            /// </summary>
            public float CurrentStateProgressPercent { get { return Total != 0 ? Progress / Total : 0f; } }

            /// <summary>
            /// 是否需要拷贝所有配置文件
            /// </summary>
            public bool NeedCopyAllConfig;

            /// <summary>
            /// 
            /// </summary>
            public PreprocessInformation()
            {
                this.State = emPreprocessState.None;
                this.Progress = 0f;
                this.Total = 1f;
            }

            /// <summary>
            /// 更新
            /// </summary>
            public void UpdateState(emPreprocessState state)
            {
                this.State = state;
                this.Progress = 0f;
                this.Total = 1f;
            }

            /// <summary>
            /// 更新
            /// </summary>
            public void UpdateProgress(float value, float total)
            {
                this.Progress = value;
                this.Total = total;
            }
        }

        /// <summary>
        /// 当前状态进度
        /// </summary>
        public PreprocessInformation preprocessInformation { get; private set; }

        /// <summary>
        ///   初始化
        /// </summary>
        IEnumerator Preprocess()
        {
            preprocessInformation = new PreprocessInformation();

            //判断主资源文件是否存在，不存在则拷贝备份资源至资源根目录
            string ab_manifest_file = Common.GetFileFullName(Common.MAIN_MANIFEST_FILE_NAME);
            string resources_manifest_file = Common.GetFileFullName(Common.RESOURCES_MANIFEST_FILE_NAME);
            if (!File.Exists(ab_manifest_file) || !File.Exists(resources_manifest_file))
            {
                // 初始化安装包资源
                preprocessInformation.UpdateState(emPreprocessState.Install);
                yield return PreProcessInstallNativeAssets();
            }
            else
            {
                // 更新本地资源
                preprocessInformation.UpdateState(emPreprocessState.Update);
                yield return PreprocessUpdateNativeAssets();
            }

            // 更新配置文件
            yield return PreprocessUpdateAllConfig();

            // 加载资源
            preprocessInformation.UpdateState(emPreprocessState.Load);
            yield return PreprocessLoad();


            // 结束前处理工作
            preprocessInformation.UpdateState(emPreprocessState.Dispose);
            yield return PreprocessDispose();

            //结束处理
            preprocessInformation.UpdateState(IsFailed ? emPreprocessState.Failed : emPreprocessState.Completed);
            yield return PreprocessFinished();

            preprocessInformation = null;
        }

        /// <summary>
        ///   初始化 - 安装包资源初始化
        /// </summary>
        IEnumerator PreProcessInstallNativeAssets()
        {
            if (ErrorCode != emErrorCode.None)
                yield break;

            // 清理资源目录
            if (Directory.Exists(Common.PATH))
            {
                Directory.Delete(Common.PATH, true);
            }

            List<string> copyFileList = new List<string>(512);

            // 拷贝安装包中的ResourcesManifest至缓存目录
            yield return StartCopyInitialFileToCache(Common.RESOURCES_MANIFEST_FILE_NAME);
            if (IsFailed)
            {
                yield break;
            }
            //加载缓存目录中的ResourcesManifest
            string res_manifest_cache_name = Common.GetCacheFileFullName(Common.RESOURCES_MANIFEST_FILE_NAME);
            ResourcesManifest newResManifest = Common.LoadResourcesManifestByPath(res_manifest_cache_name);
            if (newResManifest != null)
            {
                // 获取所有需要拷贝的文件名
                var itr = newResManifest.Data.AssetBundles.GetEnumerator();
                while (itr.MoveNext())
                {
                    if (itr.Current.Value.IsNative)
                    {
                        copyFileList.Add(itr.Current.Value.AssetBundleName);
                    }
                }
                itr.Dispose();
            }

            // 所有文件加入拷贝列表
            yield return StartBatchCopyInitialFileToNative(copyFileList, (AssetBundleBatchCopy c) =>
            {
                preprocessInformation.UpdateProgress(c.progress, c.total);
            });
            if (IsFailed)
            {
                yield break;
            }

            // 标记所有配置文件需拷贝
            preprocessInformation.NeedCopyAllConfig = true;
        }

        /// <summary>
        /// 初始化 - 最新版本资源拷贝
        /// </summary>
        IEnumerator PreprocessUpdateNativeAssets()
        {
            if (ErrorCode != emErrorCode.None)
            {
                yield break;
            }

            preprocessInformation.UpdateProgress(0f, 1f);

            ResourcesManifest res_manifest = Common.LoadResourcesManifest();
            if (res_manifest == null)
            {
                Error(emErrorCode.LoadResourcesManifestFailed
                    , "Can't load ResourcesManifest file!");
                yield break;
            }

            // 拷贝安装包中的ResourcesManifest至缓存目录
            yield return StartCopyInitialFileToCache(Common.RESOURCES_MANIFEST_FILE_NAME);
            if (IsFailed)
            {
                yield break;
            }

            //加载缓存目录中的ResourcesManifest
            string res_manifest_cache_name = Common.GetCacheFileFullName(Common.RESOURCES_MANIFEST_FILE_NAME);
            ResourcesManifest new_res_manifest = Common.LoadResourcesManifestByPath(res_manifest_cache_name);
            if (new_res_manifest == null)
            {
                Error(emErrorCode.LoadResourcesManifestFailed
                    , "Can't load ResourcesManifest cache file!");
                yield break;
            }

            List<string> copyFileList = new List<string>();
            List<string> delete_files = new List<string>();
            if (CompareVersion(res_manifest.Data.strVersion, new_res_manifest.Data.strVersion) < 0)
            {// 安装包的资源有更新
                AssetBundleManifest main_manifest = Common.LoadMainManifest();

                // 拷贝安装包中的AssetBundle.manifest
                yield return StartCopyInitialFileToCache(Common.MAIN_MANIFEST_FILE_NAME);
                if (IsFailed)
                {
                    yield break;
                }
                // 加载缓存目录中的AssetBundle.manifest
                string main_manifest_cache_name = Common.GetCacheFileFullName(Common.MAIN_MANIFEST_FILE_NAME);
                AssetBundleManifest new_main_manifest = Common.LoadMainManifestByPath(main_manifest_cache_name);
                if (new_main_manifest == null)
                {
                    Error(emErrorCode.LoadMainManifestFailed
                        , "Can't load MainManifest cache file!");
                    yield break;
                }

                // 计算最新的差异数据（拷贝文件列表，删除文件列表）
                ComparisonUtils.CompareAndCalcDifferenceFiles(ref copyFileList, ref delete_files
                    , main_manifest, new_main_manifest, res_manifest, new_res_manifest
                    , ComparisonUtils.emCompareMode.OnlyInitial);

                // 标记所有配置文件需拷贝
                preprocessInformation.NeedCopyAllConfig = true;
            }
            else if (CompareVersion(res_manifest.Data.strVersion, new_res_manifest.Data.strVersion) == 0)
            {// 安装包的资源无更新
                // 比较本地资源信息，计算需修复的数据（拷贝文件列表）
                ComparisonUtils.CompareAndCalcRecoverFiles(ref copyFileList, res_manifest);
            }

            int progress = 0;
            int count = delete_files.Count + copyFileList.Count;
            // 删除无用文件
            if (delete_files != null && delete_files.Count > 0)
            {
                for (int i = 0; i < delete_files.Count; ++i)
                {
                    string full_name = Common.GetFileFullName(delete_files[i]);
                    if (File.Exists(full_name))
                    {
                        File.Delete(full_name);
                    }
                    yield return null;
                    preprocessInformation.UpdateProgress(++progress, count);
                }
            }

            // 拷贝文件
            if (copyFileList != null && copyFileList.Count > 0)
            {
                yield return StartBatchCopyInitialFileToNative(copyFileList, (AssetBundleBatchCopy c) =>
                {
                    preprocessInformation.UpdateProgress(c.progress, c.total);
                });
                if (IsFailed)
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// 初始化 - 更新所有配置文件
        /// </summary>
        IEnumerator PreprocessUpdateAllConfig()
        {
            if (ErrorCode != emErrorCode.None)
            {
                yield break;
            }

            if(preprocessInformation.NeedCopyAllConfig)
            {
                // 拷贝配置文件（部分配置文件为非必要性文件所以需单独下载判断）
                for (int i = 0; i < Common.CONFIG_NAME_ARRAY.Length; ++i)
                {
                    StreamingAssetsCopy copy = new StreamingAssetsCopy();
                    yield return copy.Copy(Common.GetInitialFileFullName(Common.CONFIG_NAME_ARRAY[i]),
                        Common.GetFileFullName(Common.CONFIG_NAME_ARRAY[i]));
                    if (copy.resultCode != emIOOperateCode.Succeed && Common.CONFIG_REQUIRE_CONDITION_ARRAY[i])
                    {
                        var message = Common.CONFIG_NAME_ARRAY[i];
                        if (!string.IsNullOrEmpty(copy.error))
                        {
                            message += ", " + copy.error;
                        }
                        ErrorWriteFile(copy.resultCode, message);
                        break;
                    }
                }

                // 拷贝失败则需要把本地配置文件删除
                // （由于部分配置文件拷贝失败，会导致本地的配置文件不匹配会引起版本信息错误， 统一全部删除则下次进入游戏会重新拷贝全部数据）
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
        }

        /// <summary>
        /// 初始化 - 加载资源
        /// </summary>
        IEnumerator PreprocessLoad()
        {
            if (ErrorCode != emErrorCode.None)
                yield break;

            // 加载其它必要的配置文件
            //MainManifest
            MainManifest = Common.LoadMainManifest();
            if (MainManifest == null)
            {
                Error(emErrorCode.LoadMainManifestFailed
                    , "Can't load MainManifest file!");
                yield break;
            }
            //ResourcesManifest
            ResManifest = Common.LoadResourcesManifest();
            if (ResManifest == null)
            {
                Error(emErrorCode.LoadResourcesManifestFailed
                    , "Can't load ResourcesManifest file!");
                yield break;
            }
            // ResourcesPackages
            ResPackages = Common.LoadResourcesPackages();

            //加载常驻资源
            if (ResManifest != null
                && ResManifest.Data != null
                && ResManifest.Data.AssetBundles != null)
            {
                var asset_bundles = ResManifest.Data.AssetBundles;
                List<string> permanentAssetBundles = new List<string>(asset_bundles.Count);
                var itr = asset_bundles.GetEnumerator();
                while (itr.MoveNext())
                {
                    //预加载的初始化AssetBundle必须无任何依赖
                    if (itr.Current.Value.IsStartupLoad && !HasDependencies(itr.Current.Key))
                    {
                        permanentAssetBundles.Add(itr.Current.Key);
                    }
                }
                itr.Dispose();

                // 加载资源
                int total = permanentAssetBundles.Count;
                for (int i = 0; i < total; ++i)
                {
                    yield return LoadAssetBundleAsync(itr.Current.Key);
                    preprocessInformation.UpdateProgress(i, total);
                }

                preprocessInformation.UpdateProgress(1f, 1f);
            }
        }
        /// <summary>
        /// 初始化 - 结束前后备工作
        /// </summary>
        IEnumerator PreprocessDispose()
        {
            if (IsFailed)
            {
                UnloadAllAssetBundle(true);
                MainManifest = null;
                ResManifest = null;
                ResPackages = null;
            }

            //删除缓存目录
            if (Directory.Exists(Common.CACHE_PATH))
                Directory.Delete(Common.CACHE_PATH, true);

            yield return null;
        }
        /// <summary>
        /// 初始化 - 完成
        /// </summary>
        IEnumerator PreprocessFinished()
        {
            //记录当前版本号
            strVersion = IsFailed ? "" : ResManifest.Data.strVersion;
            //标记已准备好
            IsReady = true;
            yield return null;
        }

        /// <summary>
        /// 
        /// </summary>
        IEnumerator StartCopyInitialFileToCache(string local_name)
        {
            StreamingAssetsCopy copy = new StreamingAssetsCopy();
            yield return copy.Copy(Common.GetInitialFileFullName(local_name),
                Common.GetCacheFileFullName(local_name));
            if (copy.resultCode != emIOOperateCode.Succeed)
            {
                var message = local_name;
                if (!string.IsNullOrEmpty(copy.error))
                {
                    message += ", " + copy.error;
                }
                ErrorWriteFile(copy.resultCode, message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        IEnumerator StartCopyInitialFileToNative(string local_name)
        {
            StreamingAssetsCopy copy = new StreamingAssetsCopy();
            yield return copy.Copy(Common.GetInitialFileFullName(local_name),
                Common.GetFileFullName(local_name));
            if (copy.resultCode != emIOOperateCode.Succeed)
            {
                var message = local_name;
                if (!string.IsNullOrEmpty(copy.error))
                {
                    message += ", " + copy.error;
                }
                ErrorWriteFile(copy.resultCode, message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        IEnumerator StartBatchCopyInitialFileToNative(List<string> files
            , System.Action<AssetBundleBatchCopy> callback)
        {
            AssetBundleBatchCopy batchCopy = AssetBundleBatchCopy.Create();
            yield return batchCopy.StartBatchCopy(files, callback);
            if (batchCopy.resultCode != emIOOperateCode.Succeed)
            {
                ErrorWriteFile(batchCopy.resultCode, null);
            }
            AssetBundleBatchCopy.Destroy(batchCopy);
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
        #endregion

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
