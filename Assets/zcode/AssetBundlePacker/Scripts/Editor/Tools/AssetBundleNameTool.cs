/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/18
 * Note  : AssetBundle命名工具
***************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

namespace zcode.AssetBundlePacker
{
    public static class AssetBundleNameTool
    {
        /// <summary>
        ///   
        /// </summary>
        private static bool cancel_running_nametool_ = false;

        /// <summary>
        ///   载入规则数据，并设置相应的AssetBundleName
        /// </summary>
        public static bool RunningAssetBundleNameTool(AssetBundleBuild build)
        {
            try
            {
                cancel_running_nametool_ = false;
                float total = (float)build.Data.Assets.Root.Count();
                float current = 0;


                //从默认路径
                ChangeAssetBundleName(EditorCommon.ASSET_START_PATH
                    , build.BuildStartFullPath
                    , build.Data.Assets.Root
                    , emAssetBundleNameRule.None
                    , (name) =>
                    {
                        //进度条提示
                        current += 1.0f;
                        float progress = current / total;
                        if (EditorUtility.DisplayCancelableProgressBar("正在生成AssetBundleName", "Change " + name, progress))
                            cancel_running_nametool_ = true;
                    });
                EditorUtility.ClearProgressBar();
                return !cancel_running_nametool_;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            EditorUtility.ClearProgressBar();
            return false;
        }

        /// <summary>
        ///   
        /// </summary>
        static void ChangeAssetBundleName(string folder_full_name
                                          , string element_path
                                          , AssetBundleBuildData.AssetBuild.Element element
                                          , emAssetBundleNameRule inherit_rule
                                          , System.Action<string> change_report = null)
        {
            if (cancel_running_nametool_)
            {
                return;
            }
            if(string.IsNullOrEmpty(element_path))
            {
                return;
            }
            if (element == null)
            {
                return;
            }
            DirectoryInfo dir = new DirectoryInfo(folder_full_name);
            if (!dir.Exists)
            {
                return;
            }

            if(inherit_rule != emAssetBundleNameRule.Ignore)
            {
                inherit_rule = (emAssetBundleNameRule)element.Rule;
            }

            bool same_directory = folder_full_name == element_path;

            //遍历文件,并设置其AssetBundleName
            FileInfo[] all_files = dir.GetFiles();
            foreach (var f in all_files)
            {
                if (!EditorCommon.IsIgnoreFile(f.Name))
                {
                    if(!same_directory)
                    {
                        ClearAssetBundleName(f.FullName);
                    }
                    else
                    {
                        AssetBundleBuildData.AssetBuild.Element child = element.FindFileElement(f.Name);
                        emAssetBundleNameRule my_rule = child != null ? (emAssetBundleNameRule)child.Rule : emAssetBundleNameRule.None;

                        if (child != null && change_report != null) { change_report(f.FullName); }

                        if (my_rule == emAssetBundleNameRule.SingleFile && inherit_rule != emAssetBundleNameRule.Ignore)
                        {
                            SetAssetBundleName(f.FullName);
                        }
                        else
                        {
                            ClearAssetBundleName(f.FullName);
                        }
                    }
                }
            }

            //遍历文件夹
            DirectoryInfo[] all_dirs = dir.GetDirectories();
            foreach (DirectoryInfo d in all_dirs)
            {
                if (!EditorCommon.IsIgnoreFolder(d.Name))
                {
                    string child_element_path = null;
                    AssetBundleBuildData.AssetBuild.Element child = null;
                    if (!same_directory)
                    {
                        child_element_path = element_path;
                        child = element;
                        ClearAssetBundleName(d.FullName);
                    }
                    else
                    {
                        child_element_path = Common.CovertCommonPath(d.FullName);
                        child = element.FindFolderElement(d.Name);
                        emAssetBundleNameRule my_rule = child != null ? (emAssetBundleNameRule)child.Rule : emAssetBundleNameRule.None;

                        if (child != null && change_report != null) { change_report(d.FullName); }

                        if (my_rule == emAssetBundleNameRule.Folder && inherit_rule != emAssetBundleNameRule.Ignore)
                        {
                            SetAssetBundleName(d.FullName);
                        }
                        else
                        {
                            ClearAssetBundleName(d.FullName);
                        }
                    }

                    ChangeAssetBundleName(Common.CovertCommonPath(d.FullName)
                        , child_element_path, child, inherit_rule, change_report);
                }
            }
        }

        /// <summary>
        ///   设置AssetBundleName
        /// </summary>
        public static void SetAssetBundleName(string full_name)
        {
            full_name = EditorCommon.AbsoluteToRelativePath(full_name);
            AssetImporter importer = AssetImporter.GetAtPath(full_name);
            if (importer != null)
            {
                string str = EditorCommon.ConvertToAssetBundleName(full_name.ToLower());
                if(importer.assetBundleName != str)
                {
                    importer.SetAssetBundleNameAndVariant(str, "");
                }
            }
        }

        /// <summary>
        ///   设置AssetBundleName
        /// </summary>
        public static void ClearAssetBundleName(string full_name)
        {
            full_name = EditorCommon.AbsoluteToRelativePath(full_name);
            AssetImporter importer = AssetImporter.GetAtPath(full_name);
            if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
            {
                importer.assetBundleName = "";
            }
        }
    }
}