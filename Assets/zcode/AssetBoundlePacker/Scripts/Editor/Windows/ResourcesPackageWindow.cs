/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/20
 * Note  : 资源包编辑窗口
***************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Runtime.Serialization;

namespace zcode.AssetBundlePacker
{
    public class ResourcesPackageWindow : EditorWindow
    {
        /// <summary>
        ///   AssetBundle包数据
        /// </summary>
        public ResourcesPackages Packages;

        /// <summary>
        ///   最后操作的AssetBundle包
        /// </summary>
        private ResourcesPackagesData.Package lastest_pack_;

        /// <summary>
        ///   
        /// </summary>
        private string current_pack_name_ = "";

        /// <summary>
        ///   
        /// </summary>
        private Vector2 scroll_ = Vector2.zero;

        /// <summary>
        ///   载入数据
        /// </summary>
        private void LoadData()
        {
            Packages = new ResourcesPackages();
            Packages.Load(EditorCommon.RESOURCES_PACKAGE_FILE_PATH);
        }

        /// <summary>
        ///   保存数据
        /// </summary>
        private void SaveData()
        {
            if (Packages != null)
                Packages.Save(EditorCommon.RESOURCES_PACKAGE_FILE_PATH);
        }

        /// <summary>
        ///   增加了一个包数据
        /// </summary>
        public bool AddPack(ResourcesPackagesData.Package pack)
        {
            if (string.IsNullOrEmpty(pack.Name))
                return false;
            if (!Packages.Data.Packages.ContainsKey(pack.Name))
            {
                Packages.Data.Packages.Add(pack.Name, pack);
            }

            return true;
        }

        /// <summary>
        ///   删除一个包数据
        /// </summary>
        public void DeletePack(string name)
        {
            if (Packages.Data.Packages.ContainsKey(name))
            {
                Packages.Data.Packages.Remove(name);
            }
        }

        /// <summary>
        ///   更新包名
        /// </summary>
        public void UpdatePackName(string name, string new_name)
        {
            if (name == new_name)
                return;

            if (Packages.Data.Packages.ContainsKey(name))
            {
                ResourcesPackagesData.Package pack = Packages.Data.Packages[name];
                pack.Name = new_name;
                Packages.Data.Packages.Add(new_name, pack);
            }

            DeletePack(name);
        }

        /// <summary>
        ///   添加选中的资源数据至包中
        /// </summary>
        public void AddSelectionAsset(ResourcesPackagesData.Package pack)
        {
            if (pack == null)
                return;

            foreach (var id in Selection.instanceIDs)
            {
                string str = AssetDatabase.GetAssetPath(id);
                string full_name = EditorCommon.RelativeToAbsolutePath(str);
                if (System.IO.File.Exists(full_name))
                {
                    if (!EditorCommon.IsIgnoreFile(str))
                    {
                        if (!pack.AssetList.Contains(str))
                        {
                            str = str.ToLower();
                            pack.AssetList.Add(str);
                        }
                    }
                }
                else if (System.IO.Directory.Exists(str))
                {
                    System.IO.DirectoryInfo dic = new System.IO.DirectoryInfo(str);
                    foreach (var file in dic.GetFiles("*", System.IO.SearchOption.AllDirectories))
                    {
                        string local = EditorCommon.AbsoluteToRelativePath(file.FullName);
                        if (!string.IsNullOrEmpty(local) && !EditorCommon.IsIgnoreFile(local))
                        {
                            if (!pack.AssetList.Contains(local))
                            {
                                local = local.ToLower();
                                pack.AssetList.Add(local);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   从包中移除资源
        /// </summary>
        public void RemoveAsset(ResourcesPackagesData.Package pack, string asset)
        {
            if (pack == null)
                return;

            if (pack.AssetList.Contains(asset))
                pack.AssetList.Remove(asset);
        }

        /// <summary>
        ///   
        /// </summary>
        void OnEnable()
        {
            LoadData();
        }

        /// <summary>
        ///   
        /// </summary>
        void OnInspectorUpdate()
        {
            //Debug.Log("窗口面板的更新");
            //这里开启窗口的重绘，不然窗口信息不会刷新
            this.Repaint();
        }

        /// <summary>
        ///   
        /// </summary>
        void OnGUI()
        {
            GUI.color = Color.white;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Resources Package", GUILayout.Width(176f));
            current_pack_name_ = GUILayout.TextField(current_pack_name_);
            if (GUILayout.Button("新建", GUILayout.Width(40f)))
            {
                ResourcesPackagesData.Package pack = new ResourcesPackagesData.Package() { Name = current_pack_name_ };
                AddPack(pack);
            }
            if (lastest_pack_ != null)
            {
                if (lastest_pack_.Name != current_pack_name_)
                {
                    if (GUILayout.Button("更新", GUILayout.Width(40f)))
                    {
                        UpdatePackName(lastest_pack_.Name, current_pack_name_);
                    }
                }
                else
                {
                    if (GUILayout.Button("删除", GUILayout.Width(40f)))
                    {
                        DeletePack(lastest_pack_.Name);
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            scroll_ = GUILayout.BeginScrollView(scroll_);
            foreach (var pack in Packages.Data.Packages)
            {
                GUI.color = Color.white;

                bool state = EditorPrefs.GetBool(pack.Key, true);
                string head = pack.Key;
                if (lastest_pack_ != null && head == lastest_pack_.Name)
                {
                    head = "<color=green>" + head + "</color>";
                }
                bool show = GUILayoutHelper.DrawHeader(head, pack.Key, true, false);
                if (show != state)
                {
                    lastest_pack_ = pack.Value;
                    current_pack_name_ = pack.Key;
                }

                if (show)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10f);
                    GUILayout.BeginVertical();
                    List<string> temp = new List<string>(pack.Value.AssetList);
                    foreach (var asset in temp)
                    {
                        string path = EditorCommon.ProjectDirectory + asset;
                        bool exist = System.IO.File.Exists(path) || System.IO.Directory.Exists(path);
                        GUI.color = exist ? Color.white : Color.red;
                        GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                        GUI.backgroundColor = Color.white;
                        GUILayout.Label(asset);

                        if (!exist)
                            GUILayout.Label("?", GUILayout.Width(22f));
                        if (GUILayout.Button("X", GUILayout.Width(22f)))
                            RemoveAsset(pack.Value, asset);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();

                    GUI.color = Color.white;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10f);
                    if (GUILayout.Button("添加选中的资源", GUILayout.Width(160f)))
                    {
                        AddSelectionAsset(pack.Value);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            if (GUILayout.Button("保存文件"))
            {
                SaveData();
                BuildAssetBundle.CopyResourcesPackageFileToStreamingAssets();
            }
        }

        [MenuItem("AssetBundle/Windows/Resources Package Window")]
        public static void Open()
        {
            EditorWindow.GetWindow<ResourcesPackageWindow>(false, "Resources Package", true).Show();
        }    }
}