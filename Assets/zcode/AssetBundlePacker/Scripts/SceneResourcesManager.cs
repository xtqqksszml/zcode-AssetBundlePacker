/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/05/05
 * Note  : 场景管理器
***************************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace zcode.AssetBundlePacker
{
    public class SceneResourcesManager
    {
        /// <summary>
        /// 资源加载方式，默认采用DefaultLoadPattern
        /// </summary>
        public static ILoadPattern LoadPattern = new DefaultLoadPattern();

        /// <summary>
        ///   异步加载场景
        /// </summary>
        public static bool LoadSceneAsync(string scene_name
                                            , System.Action<string> callback
                                            , LoadSceneMode mode = LoadSceneMode.Single)
        {
            AsyncOperation ao;
            return LoadSceneAsync(out ao, scene_name, callback, mode);
        }

        /// <summary>
        ///   异步加载场景
        /// </summary>
        public static AsyncOperation LoadSceneAsync(string scene_name
                                                    , LoadSceneMode mode = LoadSceneMode.Single)
        {
            AsyncOperation ao;
            LoadSceneAsync(out ao, scene_name, null, mode);
            return ao;
        }

        /// <summary>
        ///   异步加载场景
        /// </summary>
        public static bool LoadSceneAsync(out AsyncOperation ao
                                            , string scene_name
                                            , System.Action<string> callback = null
                                            , LoadSceneMode mode = LoadSceneMode.Single)
        {
            ao = null;

#if UNITY_EDITOR
            if (LoadPattern.SceneLoadPattern == emLoadPattern.EditorAsset
                || LoadPattern.SceneLoadPattern == emLoadPattern.All)
            {
                if (!Application.CanStreamedLevelBeLoaded(scene_name))
                    return false;

                ao = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene_name, mode);
                if (ao != null)
                {
                    CoroutineExecutor.Create(ao, () =>
                    {
                        if (callback != null) callback(scene_name);
                    });
                    return true;
                }
            }
#endif

            if (LoadPattern.SceneLoadPattern == emLoadPattern.AssetBundle
                || LoadPattern.SceneLoadPattern == emLoadPattern.All)
            {
                ao = AssetBundleManager.Instance.LoadSceneAsync(scene_name, mode);
                if (ao != null)
                {
                    CoroutineExecutor.Create(ao, () =>
                    {
                        GenerateSceneObject(scene_name);
                        if (callback != null) callback(scene_name);
                    });
                    return true;
                }
            }
            if (LoadPattern.SceneLoadPattern == emLoadPattern.Original
                || LoadPattern.SceneLoadPattern == emLoadPattern.All)
            {
                if (!Application.CanStreamedLevelBeLoaded(scene_name))
                    return false;

                ao = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene_name, mode);
                if (ao != null)
                {
                    CoroutineExecutor.Create(ao, () =>
                    {
                        if (callback != null) callback(scene_name);
                    });
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///   读取场景配置文件，生成场景对象
        /// </summary>
        static void GenerateSceneObject(string scene_name)
        {
            if (!AssetBundleManager.Instance.IsSceneExist(scene_name))
                return;
            var scene_desc = AssetBundleManager.Instance.ResourcesManifest.FindScene(scene_name);
            if (scene_desc == null)
                return;
            TextAsset text_asset = AssetBundleManager.Instance.LoadAsset<TextAsset>(scene_desc.SceneConfigPath);
            if (text_asset == null)
                return;

            SceneConfig config = new SceneConfig();
            config.LoadFromString(text_asset.text);
            for (int i = 0; i < config.Data.SceneObjects.Count; ++i)
            {
                var obj = config.Data.SceneObjects[i];
                var go = ResourcesManager.Load<GameObject>(obj.AssetName);
                var parent = GameObject.Find(obj.ParentName);
                var instance = GameObject.Instantiate<GameObject>(go);
                instance.transform.parent = parent != null ? parent.transform : null;
                instance.transform.position = obj.Position;
                instance.transform.localScale = obj.Scale;
                instance.transform.rotation = obj.Rotation;
            }
        }
    }
}