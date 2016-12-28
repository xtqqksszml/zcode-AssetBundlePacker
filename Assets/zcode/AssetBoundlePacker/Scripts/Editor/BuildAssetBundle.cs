/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/11/30
 * Note  : 打包AssetBundle
***************************************************************/
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace zcode.AssetBundlePacker
{
    /// <summary>
    /// 
    /// </summary>
    public static class BuildAssetBundle
    {
        /// <summary>
        ///   打包AssetBundle
        /// </summary>
        public static void BuildAllAssetBundlesToTarget(BuildTarget target
            , BuildAssetBundleOptions options)
        {
            string manifest_file = EditorCommon.PATH + "/" + Common.MAIN_MANIFEST_FILE_NAME;
            AssetBundleManifest old_manifest = Common.LoadMainManifestByPath(manifest_file);

            if (!Directory.Exists(EditorCommon.PATH))
                Directory.CreateDirectory(EditorCommon.PATH);
            BuildPipeline.BuildAssetBundles(EditorCommon.PATH, options, target);
            AssetDatabase.Refresh();

            AssetBundleManifest new_manifest = Common.LoadMainManifestByPath(manifest_file);
            ComparisonAssetBundleManifest(old_manifest, new_manifest);
            ExportResourcesManifestFile(new_manifest);
            ResourcesManifest resoureces_manifest = Common.LoadResourcesManifestByPath(EditorCommon.RESOURCES_MANIFEST_FILE_PATH);
            CompressAssetBundles(resoureces_manifest, ref resoureces_manifest);
            resoureces_manifest.Save(EditorCommon.RESOURCES_MANIFEST_FILE_PATH);
            CopyNativeAssetBundleToStreamingAssets(resoureces_manifest);
        }

        /// <summary>
        /// 压缩AssetBundle
        /// </summary>
        public static bool CompressAssetBundles(ResourcesManifest old_resources_manifest
            , ref ResourcesManifest resources_manifest)
        {
            if (resources_manifest == null)
                return false;

            // 通过记录新旧版本中压缩标记
            // 判定资源是否需要压缩、删除压缩包
            Dictionary<string, int> dic = new Dictionary<string, int>();
            int old_version_bit = 0x1;                      // 旧版本中压缩
            int new_version_bit = 0x2;                      // 新版本中压缩
            var itr = old_resources_manifest.Data.AssetBundles.GetEnumerator();
            while (itr.MoveNext())
            {
                if (itr.Current.Value.IsCompress)
                {
                    string name = itr.Current.Value.AssetBundleName;
                    if (!dic.ContainsKey(name))
                        dic.Add(name, old_version_bit);
                    else
                        dic[name] |= old_version_bit;
                }
            }
            itr = resources_manifest.Data.AssetBundles.GetEnumerator();
            while (itr.MoveNext())
            {
                if (itr.Current.Value.IsCompress)
                {
                    string name = itr.Current.Value.AssetBundleName;
                    if (!dic.ContainsKey(name))
                        dic.Add(name, new_version_bit);
                    else
                        dic[name] |= new_version_bit;
                }
            }

            float current = 0f;
            float total = resources_manifest.Data.AssetBundles.Count;
            var itr1 = dic.GetEnumerator();
            while (itr1.MoveNext())
            {
                string name = itr1.Current.Key;
                int mask = itr1.Current.Value;

                //过滤主AssetBundle文件
                if (name == Common.MAIN_MANIFEST_FILE_NAME)
                    continue;

                string action;
                string file_name = EditorCommon.PATH + "/" + name;
                if((mask & old_version_bit) > 0 
                    && (mask & new_version_bit) == 0 )
                {
                    // 旧版本中存在，新版本不存在
                    // 删除压缩包
                    string compress_file = Compress.GetCompressFileName(file_name);
                    File.Delete(compress_file);
                    File.Delete(compress_file + Common.NATIVE_MANIFEST_EXTENSION);

                    //重写ResourcesManifest数据
                    var ab = resources_manifest.Data.AssetBundles[name];
                    ab.CompressSize = 0;

                    action = "Delete Compress";
                }
                else if((mask & new_version_bit) > 0 )
                {
                    //新版本中存在，压缩文件
                    Compress.CompressFile(file_name);

                    //重写ResourcesManifest数据
                    var ab = resources_manifest.Data.AssetBundles[name];
                    ab.CompressSize = zcode.FileHelper.GetFileSize(Compress.GetCompressFileName(file_name));

                    action = "Compress";
                }
                else
                {
                    action = "Ignore";
                }

                //更新进度条
                if (ShowProgressBar("", action + " " + name, current / total))
                {
                    EditorUtility.ClearProgressBar();
                    return false;
                }
            }

            EditorUtility.ClearProgressBar();
            return true;
        }

        /// <summary>
        /// 拷贝资源包文件
        /// </summary>
        public static bool CopyResourcesPackageFileToStreamingAssets()
        {
            string file = Common.RESOURCES_PACKAGE_FILE_NAME;
            string src_file_name = EditorCommon.PATH + "/" + file;
            string dest_file_name = Common.INITIAL_PATH + "/" + file;
            bool result = zcode.FileHelper.CopyFile(src_file_name, dest_file_name, true);
            if(result)
                AssetDatabase.Refresh();

            return result;
        }

        /// <summary>
        /// 拷贝本地AssetBunle至StreamingAssets目录
        /// </summary>
        public static bool CopyNativeAssetBundleToStreamingAssets(ResourcesManifest resources_manifest)
        {
            if (resources_manifest == null)
                return false;

            //清空本地资源目录
            if (ShowProgressBar("", "清空本地资源目录", 0f))
            {
                EditorUtility.ClearProgressBar();
                return false;
            }
            if (!Directory.Exists(Common.INITIAL_PATH))
                Directory.CreateDirectory(Common.INITIAL_PATH);
            else
                zcode.FileHelper.DeleteAllChild(Common.INITIAL_PATH
                    , FileAttributes.Hidden | FileAttributes.System);

            //拷贝所有配置文件
            for (int i = 0; i < Common.MAIN_CONFIG_NAME_ARRAY.Length; ++i)
            {
                string file = Common.MAIN_CONFIG_NAME_ARRAY[i];
                string src_file_name = EditorCommon.PATH + "/" + file;
                string dest_file_name = Common.INITIAL_PATH + "/" + file;
                float progress = (float)(i + 1) / (float)Common.MAIN_CONFIG_NAME_ARRAY.Length;
                if (ShowProgressBar("", "Copy " + file, progress))
                {
                    EditorUtility.ClearProgressBar();
                    return false;
                }
                zcode.FileHelper.CopyFile(src_file_name, dest_file_name, true);
            }
            
            //拷贝AssetBundle文件
            float current = 0f;
            float total = resources_manifest.Data.AssetBundles.Count;
            foreach (var desc in resources_manifest.Data.AssetBundles.Values)
            {
                current += 1f;

                //过滤主AssetBundle文件
                if (desc.AssetBundleName == Common.MAIN_MANIFEST_FILE_NAME)
                    continue;

                if (desc.IsNative)
                {
                    zcode.FileHelper.CopyFile(EditorCommon.PATH + "/" + desc.AssetBundleName
                            , Common.INITIAL_PATH + "/" + desc.AssetBundleName, true);
                }

                //更新进度条
                if (ShowProgressBar("", "Copy " + desc.AssetBundleName, current / total))
                {
                    EditorUtility.ClearProgressBar();
                    return false;
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            return true;
        }

        /// <summary>
        /// 根据AssetBundle导出ResourcesManifest文件
        /// </summary>
        public static void ExportResourcesManifestFile(AssetBundleManifest manifest)
        {
            ResourcesManifest info = new ResourcesManifest();

            //读取所有AssetBundle
            string root_dir = EditorCommon.PATH + "/";
            List<string> scenes = new List<string>();
            if (manifest != null)
            {
                //读取主AssetBundle
                ResourcesManifestData.AssetBundle desc = new ResourcesManifestData.AssetBundle();
                desc.AssetBundleName = Common.MAIN_MANIFEST_FILE_NAME;
                desc.Size = zcode.FileHelper.GetFileSize(root_dir + Common.MAIN_MANIFEST_FILE_NAME);
                info.Data.AssetBundles.Add(Common.MAIN_MANIFEST_FILE_NAME, desc);

                //读取其它AssetBundle
                foreach (var name in manifest.GetAllAssetBundles())
                {
                    desc = new ResourcesManifestData.AssetBundle();
                    desc.AssetBundleName = name;
                    desc.Size = zcode.FileHelper.GetFileSize(root_dir + name);
                    AssetBundle ab = AssetBundle.LoadFromFile(root_dir + name);
                    foreach (var asset in ab.GetAllAssetNames())
                    {
                        desc.Assets.Add(asset);
                    }
                    foreach (var scene in ab.GetAllScenePaths())
                    {
                        desc.Scenes.Add(scene);
                        scenes.Add(scene);
                    }
                    ab.Unload(false);

                    info.Data.AssetBundles.Add(name, desc);
                }
            }

            //读取所有Scene信息
            for (int i = 0; i < scenes.Count; ++i)
            {
                ResourcesManifestData.Scene scene_desc = new ResourcesManifestData.Scene();
                scene_desc.SceneLevelName = Path.GetFileNameWithoutExtension(scenes[i]);
                scene_desc.ScenePath = scenes[i];
                scene_desc.SceneConfigPath = SceneConfig.GetSceneConfigPath(scenes[i]);
                info.Data.Scenes.Add(scene_desc.SceneLevelName, scene_desc);
            }

            //读取旧的ResourcesInfo，同步其它额外的数据
            ResourcesManifest old_info = new ResourcesManifest();
            old_info.Load(EditorCommon.RESOURCES_MANIFEST_FILE_PATH);
            if (old_info.Data != null && old_info.Data.AssetBundles.Count > 0)
            {
                foreach (var desc in old_info.Data.AssetBundles.Values)
                {
                    if (info.Data.AssetBundles.ContainsKey(desc.AssetBundleName))
                    {
                        info.Data.AssetBundles[desc.AssetBundleName].IsNative = desc.IsNative;
                        info.Data.AssetBundles[desc.AssetBundleName].IsPermanent = desc.IsPermanent;
                    }
                }
            }

            //保存ResourcesInfo
            info.Save(EditorCommon.RESOURCES_MANIFEST_FILE_PATH);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 比对AssetBundleManifest, 删除冗余的AssetBundle
        /// </summary>
        static void ComparisonAssetBundleManifest(AssetBundleManifest old_manifest, AssetBundleManifest new_manifest)
        {
            if (old_manifest == null || new_manifest == null)
                return;
            //删除冗余
            string root_dir = EditorCommon.PATH + "/";
            string[] new_abs = new_manifest.GetAllAssetBundles();
            HashSet<string> new_ab_table = new HashSet<string>(new_abs);
            string[] old_abs = old_manifest.GetAllAssetBundles();
            for (int i = 0; i < old_abs.Length; ++i)
            {
                if (!new_ab_table.Contains(old_abs[i]))
                {
                    //删除AssetBundle与压缩包
                    File.Delete(root_dir + old_abs[i]);
                    File.Delete(root_dir + old_abs[i] + Common.NATIVE_MANIFEST_EXTENSION);
                    File.Delete(root_dir + old_abs[i] + Compress.EXTENSION);
                    File.Delete(root_dir + old_abs[i] + Compress.EXTENSION + Common.NATIVE_MANIFEST_EXTENSION);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static bool ShowProgressBar(string title, string operating, float progress)
        {
            return EditorUtility.DisplayCancelableProgressBar(title, operating, progress);
        }
    }
}