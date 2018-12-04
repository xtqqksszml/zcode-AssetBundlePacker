/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/18
 * Note  : 资源描述数据
***************************************************************/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace zcode.AssetBundlePacker
{
    /// <summary>
    /// 资源清单
    /// </summary>
    public class ResourcesManifestData
    {
        /// <summary>
        ///   场景描述信息
        /// </summary>
        public class Scene
        {
            /// <summary>
            /// 场景名称
            /// </summary>
            public string SceneLevelName;
            /// <summary>
            /// 场景路径
            /// </summary>
            public string ScenePath;
            /// <summary>
            /// 场景配置文件路径
            /// </summary>
            public string SceneConfigPath;
        }

        /// <summary>
        ///   AssetBundle描述信息
        /// </summary>
        public class AssetBundle
        {
            /// <summary>
            /// AssetBundleName
            /// </summary>
            public string AssetBundleName;
            /// <summary>
            /// 资源列表
            /// </summary>
            public List<string> Assets = new List<string>();
            /// <summary>
            /// 场景列表
            /// </summary>
            public List<string> Scenes = new List<string>();
            /// <summary>
            /// AssetBundle大小
            /// </summary>
            public long Size;
            /// <summary>
            /// 压缩包大小
            /// </summary>
            public long CompressSize;
            /// <summary>
            /// 是否压缩
            /// </summary>
            public bool IsCompress = false;
            /// <summary>
            /// 是否打包到安装包中（是否作为原始资源）
            /// </summary>
            public bool IsNative = false;
            /// <summary>
            /// 是否常驻内存
            /// </summary>
            public bool IsPermanent = false;
            /// <summary>
            /// 是否启动时加载（此AssetBundle也是常驻内存）
            /// </summary>
            public bool IsStartupLoad = false;
        }

        // 版本号(弃用)
        [System.Obsolete("Use ResourcesManifestData.strVersion")]
        public int Version;
        // 版本号
        public string strVersion;
        // 是否打包所有AssetBundle至安装包
        public bool IsAllNative;
        // 是否所有AssetBundle都压缩
        public bool IsAllCompress;
        //Key：AssetBundleName Value: Describe
        public Dictionary<string, AssetBundle> AssetBundles = new Dictionary<string, AssetBundle>();
        //Key：SceneLevelName Value: SceneDescribe
        public Dictionary<string, Scene> Scenes = new Dictionary<string, Scene>();
    }

    public class ResourcesManifest
    {
        /// <summary>
        /// 
        /// </summary>
        public ResourcesManifestData Data;

        /// <summary>
        ///   资源查询表
        ///   Key： Asset
        ///   Value： AssetBundleName's list
        /// </summary>
        public Dictionary<string, List<string>> AssetTable;

        /// <summary>
        ///   场景查询表(场景强制打包为一个AssetBundle)
        ///   Key： SceneLevelName
        ///   Value： AssetBundleName
        /// </summary>
        public Dictionary<string, string> SceneTable;

        /// <summary>
        ///   
        /// </summary>
        public ResourcesManifest()
        {
            Data = new ResourcesManifestData();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Load(string file_name)
        {
            bool result = SimpleJsonReader.ReadFromFile<ResourcesManifestData>(ref Data, file_name);
            if (result)
                Build();
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Save(string file_name)
        {
            return SimpleJsonWriter.WriteToFile(Data, file_name);
        }

        /// <summary>
        ///   组建数据，建立资源查询表
        /// </summary>
        private void Build()
        {
            AssetTable = new Dictionary<string, List<string>>();
            SceneTable = new Dictionary<string, string>();
            if(Data.AssetBundles != null)
            {
                var itr = Data.AssetBundles.Values.GetEnumerator();
                while (itr.MoveNext())
                {
                    List<string> list = itr.Current.Assets;
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (!AssetTable.ContainsKey(list[i]))
                        {
                            AssetTable.Add(list[i], new List<string>());
                        }

                        AssetTable[list[i]].Add(itr.Current.AssetBundleName);
                    }

                    List<string> scenes = itr.Current.Scenes;
                    for (int i = 0; i < scenes.Count; ++i)
                    {
                        if (!SceneTable.ContainsKey(scenes[i]))
                            SceneTable.Add(scenes[i], itr.Current.AssetBundleName);
                    }
                }
                itr.Dispose();
            }
        }

        /// <summary>
        ///   找到一个AssetBundleDescribe
        /// </summary>
        public ResourcesManifestData.AssetBundle Find(string assetbundlename)
        {
            if (Data == null)
                return null;
            if (Data.AssetBundles == null)
                return null;
            if (Data.AssetBundles.Count == 0)
                return null;
            if (!Data.AssetBundles.ContainsKey(assetbundlename))
                return null;

            return Data.AssetBundles[assetbundlename];
        }

        /// <summary>
        ///   找到一个AssetBundleDescribe
        /// </summary>
        public ResourcesManifestData.Scene FindScene(string scene_name)
        {
            if (Data == null)
                return null;
            if (Data.Scenes == null)
                return null;
            if (Data.Scenes.Count == 0)
                return null;
            if (!Data.Scenes.ContainsKey(scene_name))
                return null;

            return Data.Scenes[scene_name];
        }

        /// <summary>
        ///   获得包含某个资源的所有AssetBundle
        /// </summary>
        public string[] GetAllAssetBundleName(string asset)
        {
            if (AssetTable == null)
                return null;
            if (!AssetTable.ContainsKey(asset))
                return null;
            return AssetTable[asset].ToArray();
        }

        /// <summary>
        ///   获得场景的AssetBundleName
        /// </summary>
        public string GetAssetBundleNameByScene(string scene_path)
        {
            if (SceneTable == null)
                return null;
            if (!SceneTable.ContainsKey(scene_path))
                return null;
            return SceneTable[scene_path];
        }

        /// <summary>
        ///   获得场景的AssetBundleName
        /// </summary>
        public string GetAssetBundleNameBySceneLevelName(string scene_name)
        {
            ResourcesManifestData.Scene desc = FindScene(scene_name);
            if (desc == null)
                return null;
            return GetAssetBundleNameByScene(desc.ScenePath);
        }

        /// <summary>
        ///   判断一个AssetBundle是否常驻内存资源（标记为常驻内存或者启动时加载）
        /// </summary>
        public bool IsPermanent(string assetbundlename)
        {
            if (Data.AssetBundles == null)
                return false;
            if (Data.AssetBundles.ContainsKey(assetbundlename))
            {
                return Data.AssetBundles[assetbundlename].IsPermanent ||
                    Data.AssetBundles[assetbundlename].IsStartupLoad;
            }

            return false;
        }

        /// <summary>
        ///   获得AssetBundle的大小
        /// </summary>
        public long GetAssetBundleSize(string assetbunlename)
        {
            ResourcesManifestData.AssetBundle desc = Find(assetbunlename);
            if (desc != null)
                return desc.Size;

            return 0;
        }

        /// <summary>
        ///   获得AssetBundle的大小
        /// </summary>
        public long GetAssetBundleCompressSize(string assetbunlename)
        {
            ResourcesManifestData.AssetBundle desc = Find(assetbunlename);
            if (desc != null)
                return desc.CompressSize;

            return 0;
        }
    }
}