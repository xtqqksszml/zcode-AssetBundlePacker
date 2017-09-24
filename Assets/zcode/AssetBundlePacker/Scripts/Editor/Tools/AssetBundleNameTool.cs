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
                ChangeAssetBundleName(EditorCommon.ASSET_START_PATH, build.Data.Assets.Root, (name) =>
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
                                          , AssetBundleBuildData.AssetBuild.Element element
                                          , System.Action<string> change_callback = null)
        {
            if (cancel_running_nametool_)
                return;
            if (element == null)
                return;
            
            DirectoryInfo dir = new DirectoryInfo(folder_full_name);
            if (!dir.Exists)
                return;

            if ((emAssetBundleNameRule)element.Rule == emAssetBundleNameRule.Ignore)
                return;

            //遍历文件,并设置其AssetBundleName
            FileInfo[] all_files = dir.GetFiles();
            foreach (var f in all_files)
            {
                AssetBundleBuildData.AssetBuild.Element child = element.FindFileElement(f.Name);
                emAssetBundleNameRule my_rule = child != null ? (emAssetBundleNameRule)child.Rule : emAssetBundleNameRule.None;

                if (!EditorCommon.IsIgnoreFile(f.Name) && my_rule != emAssetBundleNameRule.Ignore)
                {
                    if (my_rule == emAssetBundleNameRule.SingleFile)
                        SetAssetBundleName(f.FullName);
                    else
                        ClearAssetBundleName(f.FullName);
                }

                if (child != null)
                {
                    if (change_callback != null)
                        change_callback(f.FullName);
                }
            }

            //遍历文件夹
            DirectoryInfo[] all_dirs = dir.GetDirectories();
            foreach (DirectoryInfo d in all_dirs)
            {
                if (!EditorCommon.IsIgnoreFolder(d.Name))
                {
                    AssetBundleBuildData.AssetBuild.Element child = element.FindFolderElement(d.Name);
                    emAssetBundleNameRule my_rule = child != null ? (emAssetBundleNameRule)child.Rule : emAssetBundleNameRule.None;


                    if (my_rule == emAssetBundleNameRule.Folder)
                        SetAssetBundleName(d.FullName);
                    else
                        ClearAssetBundleName(d.FullName);


                    if (child != null)
                    {
                        if (change_callback != null)
                            change_callback(d.FullName);
                    }

                    ChangeAssetBundleName(d.FullName, child, change_callback);
                }
            }

            //刷新
            AssetDatabase.Refresh();
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
                importer.SetAssetBundleNameAndVariant(str, "");
            }
        }

        /// <summary>
        ///   设置AssetBundleName
        /// </summary>
        public static void ClearAssetBundleName(string full_name)
        {
            full_name = EditorCommon.AbsoluteToRelativePath(full_name);
            AssetImporter importer = AssetImporter.GetAtPath(full_name);
            if (importer != null)
            {
                importer.assetBundleName = "";
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void SetSelectionAssetBundleName()
        {
            foreach (var id in Selection.instanceIDs)
            {
                string str = AssetDatabase.GetAssetPath(id);
                if (!EditorCommon.IsIgnoreFile(str))
                {
                    SetAssetBundleName(str);
                }
            }
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ClearSelectionAssetBundleName()
        {
            foreach (var id in Selection.instanceIDs)
            {
                string str = AssetDatabase.GetAssetPath(id);
                if (!EditorCommon.IsIgnoreFile(str))
                {
                    ClearAssetBundleName(str);
                }
            }
            AssetDatabase.Refresh();
        }
    }
}