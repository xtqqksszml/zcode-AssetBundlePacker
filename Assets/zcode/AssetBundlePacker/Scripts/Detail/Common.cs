/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/18 15:21:08
 * Note  : AssetBundle相关公共定义
***************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace zcode.AssetBundlePacker
{
    public static class Common
    {
        /// <summary>
        ///   AssetBundle后缀名
        /// </summary>
        public const string EXTENSION = "_ab";

        /// <summary>
        /// 
        /// </summary>
        public const string NATIVE_MANIFEST_EXTENSION = ".manifest";

        /// <summary>
        /// 资源所在根文件夹名
        /// </summary>
        public const string ROOT_FOLDER_NAME = "AssetBundle";

        /// <summary>
        /// 项目资源根路径名称
        /// </summary>
        public const string PROJECT_ASSET_ROOT_NAME = "Assets";

        /// <summary>
        ///   常驻根路径
        ///   此路径主要存放所有游戏中所需使用的AssetBundle、其它配置文件
        /// </summary>
        public static readonly string PATH = Platform.PERSISTENT_DATA_PATH + "/" + ROOT_FOLDER_NAME;

        /// <summary>
        ///   初始路径
        ///   此路径为安装包中的AssetBundlePacker所携带的资源和配置文件路径
        /// </summary>
        public static readonly string INITIAL_PATH = Platform.STREAMING_ASSETS_PATH + "/" + ROOT_FOLDER_NAME;

        /// <summary>
        ///   缓存路径
        ///   此路径主要存放临时文件（下载，拷贝缓存等）
        /// </summary>
        public static readonly string CACHE_PATH = PATH + "/Cache";

        /// <summary>
        ///   更新器缓存路径
        ///   此路径主要存放临时文件（下载，拷贝缓存等）
        /// </summary>
        public static readonly string UPDATER_CACHE_PATH = PATH + "/UpdaterCache";

        /// <summary>
        ///   DownloadCache文件路径
        /// </summary>
        public static readonly string DOWNLOADCACHE_FILE_PATH = UPDATER_CACHE_PATH + "/DownloadCache.cfg";

        /// <summary>
        ///   主Manifest文件名称（必须存在）
        /// </summary>
        public const string MAIN_MANIFEST_FILE_NAME = "AssetBundle";

        /// <summary>
        ///   ResourcesManifest文件名称（必须存在）
        /// </summary>
        public const string RESOURCES_MANIFEST_FILE_NAME = "ResourcesManifest.cfg";

        /// <summary>
        ///   ResourcesPackage文件名称（非必须存在）
        /// </summary>
        public const string RESOURCES_PACKAGE_FILE_NAME = "ResourcesPackage.cfg";

        /// <summary>
        /// 配置文件名
        /// 此数组是插件所使用的所有的配置文件名
        /// </summary>
        public static readonly string[] CONFIG_NAME_ARRAY = 
        {
            MAIN_MANIFEST_FILE_NAME,
            RESOURCES_MANIFEST_FILE_NAME,
            RESOURCES_PACKAGE_FILE_NAME,
        };

        /// <summary>
        /// 配置存在检查（如果为true，必须存在）
        /// </summary>
        public static readonly bool[] CONFIG_REQUIRE_CONDITION_ARRAY =
        {
            true,
            true,
            false,
        };

        /// <summary>
        ///   路径字符串转换成通用的路径字符串("/")
        /// </summary>
        public static string CovertCommonPath(string path)
        {
            return path.Replace('\\', '/');
        }

        /// <summary>
        ///   获得资源全局路径
        /// </summary>
        public static string GetFileFullName(string file)
        {
            return PATH + "/" + file;
        }

        /// <summary>
        ///   获得资源原始全局路径
        /// </summary>
        public static string GetInitialFileFullName(string file)
        {
            return INITIAL_PATH + "/" + file;
        }

        /// <summary>
        ///   获得缓存路径
        /// </summary>
        public static string GetCacheFileFullName(string file)
        {
            return CACHE_PATH + "/" + file;
        }

        /// <summary>
        ///   获得缓存路径
        /// </summary>
        public static string GetUpdaterCacheFileFullName(string file)
        {
            return UPDATER_CACHE_PATH + "/" + file;
        }

        /// <summary>
        ///   载入Manifest
        /// </summary>
        public static AssetBundleManifest LoadMainManifest()
        {
            string file = Common.GetFileFullName(Common.MAIN_MANIFEST_FILE_NAME);
            return LoadMainManifestByPath(file);
        }

        /// <summary>
        ///   载入ResourcesMnifest
        /// </summary>
        public static ResourcesManifest LoadResourcesManifest()
        {
            string file = Common.GetFileFullName(Common.RESOURCES_MANIFEST_FILE_NAME);
            return LoadResourcesManifestByPath(file);
        }

        /// <summary>
        ///   载入ResourcesPack
        /// </summary>
        public static ResourcesPackages LoadResourcesPackages()
        {
            string file = Common.GetFileFullName(Common.RESOURCES_PACKAGE_FILE_NAME);
            return LoadResourcesPackagesByPath(file);
        }

        /// <summary>
        ///   载入Manifest
        /// </summary>
        public static AssetBundleManifest LoadMainManifestByPath(string full_name)
        {
            if (!System.IO.File.Exists(full_name))
            {
                return null;
            }

            AssetBundleManifest manifest = null;
            UnityEngine.AssetBundle mainfest_bundle = UnityEngine.AssetBundle.LoadFromFile(full_name);
            if (mainfest_bundle != null)
            {
                manifest = (AssetBundleManifest)mainfest_bundle.LoadAsset("AssetBundleManifest");
                mainfest_bundle.Unload(false);
            }

            return manifest;
        }

        /// <summary>
        ///   载入ResourcesManifest
        /// </summary>
        public static ResourcesManifest LoadResourcesManifestByPath(string full_name)
        {
            var result = new ResourcesManifest();
            result.Load(full_name);
            return result;
        }

        /// <summary>
        ///   载入ResourcesPack
        /// </summary>
        public static ResourcesPackages LoadResourcesPackagesByPath(string full_name)
        {
            var result = new ResourcesPackages();
            result.Load(full_name);
            return result;
        }

        /// <summary>
        ///   计算Transform的层次路径
        /// </summary>
        public static string CalcTransformHierarchyPath(Transform trans)
        {
            if (trans.parent == null)
                return trans.name;
            else
                return CalcTransformHierarchyPath(trans.parent) + "/" + trans.name;
        }

        /// <summary>
        /// 计算指定URL的下载URL
        /// </summary>
        public static string CalcAssetBundleDownloadURL(string url)
        {
            string new_url = url;
            if (new_url[new_url.Length - 1] != '/')
                new_url = new_url + '/';
            new_url = new_url + ROOT_FOLDER_NAME + "/";

            return new_url;
        }
    }
}