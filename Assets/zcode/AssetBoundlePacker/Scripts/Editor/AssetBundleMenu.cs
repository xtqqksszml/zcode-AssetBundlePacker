/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/03/14
 * Note  : AssetBundle相关菜单项
***************************************************************/
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace zcode.AssetBundlePacker
{
    public class AssetBundleMenu : MonoBehaviour
    {
        protected AssetBundleMenu()
        { }

        #region Step 1
        [MenuItem("AssetBundle/Step-1 打包AssetBundle", false, 51)]
        static void BuildAssetBundle_Step1()
        {
            AssetBundleBuildWindow.Open();
        }
        #endregion

        #region Step 2
        [MenuItem("AssetBundle/Step-2 配置AssetBundle", false, 52)]
        static void OpenAssetBundleBrowse_Step2()
        {
            AssetBundleBrowseWindow.Open();
        }
        #endregion

        #region Step 3
        [MenuItem("AssetBundle/Step-3 配置AssetBundle资源包", false, 53)]
        static void OpenAssetBundlePack_Step3()
        {
            ResourcesPackageWindow.Open();
        }
        #endregion

        [MenuItem("AssetBundle/Tools/Set Selection AssetBundleName")]
        static void SetSelectionAssetBundleName()
        {
            AssetBundleNameTool.SetSelectionAssetBundleName();
        }

        [MenuItem("AssetBundle/Tools/Clear Selection AssetBundleName")]
        static void ClearSelectionAssetBundleName()
        {
            AssetBundleNameTool.ClearSelectionAssetBundleName();
        }
    }
}