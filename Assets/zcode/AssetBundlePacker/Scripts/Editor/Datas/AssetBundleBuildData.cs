/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/03/18
 * Note  : AssetBundle打包规则配置数据
***************************************************************/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace zcode.AssetBundlePacker
{
    /// <summary>
    /// 
    /// </summary>
    public class AssetBundleBuildData
    {
        /// <summary>
        ///   Asset's build data
        /// </summary>
        public class AssetBuild
        {
            /// <summary>
            ///   资源结点
            /// </summary>
            public class Element
            {
                /// <summary>
                ///   名称
                /// </summary>
                public string Name;
                /// <summary>
                ///   是否文件夹
                /// </summary>
                public bool IsFolder;
                /// <summary>
                ///   规则
                /// </summary>
                public int Rule;
                /// <summary>
                /// 是否压缩
                /// </summary>
                public bool IsCompress;
                /// <summary>
                /// 是否打包到安装包中
                /// </summary>
                public bool IsNative;
                /// <summary>
                /// 是否常驻内存
                /// </summary>
                public bool IsPermanent;
                /// <summary>
                /// 启动时加载
                /// </summary>
                public bool IsStartupLoad;
                /// <summary>
                ///   子对象
                /// </summary>
                public List<Element> Children;

                /// <summary>
                /// 
                /// </summary>
                public Element()
                {
                }

                /// <summary>
                /// 
                /// </summary>
                public Element(string name)
                {
                    Name = name;
                }

                /// <summary>
                ///   增加一个子对象
                /// </summary>
                public void Add(Element child)
                {
                    if (Children == null)
                        Children = new List<Element>();

                    Children.Add(child);
                }

                /// <summary>
                ///   查找文件夹
                /// </summary>
                public Element FindFolderElement(string name)
                {
                    if (Children == null)
                        return null;
                    return Children.Find((elem) =>
                    {
                        return elem.Name == name && elem.IsFolder;
                    });
                }

                /// <summary>
                ///   查找文件
                /// </summary>
                public Element FindFileElement(string name)
                {
                    if (Children == null)
                        return null;
                    return Children.Find((elem) =>
                    {
                        return elem.Name == name && !elem.IsFolder;
                    });
                }

                /// <summary>
                ///   子数量
                /// </summary>
                public int Count()
                {
                    int count = 0;
                    if (Children != null)
                    {
                        count += Children.Count;
                        for (int i = 0; i < Children.Count; ++i)
                        {
                            count += Children[i].Count();
                        }
                    }

                    return count;
                }

                /// <summary>
                /// 拷贝
                /// </summary>
                public void CopyTo(Element elem)
                {
                    elem.Name = Name;
                    elem.IsFolder = IsFolder;
                    elem.Rule = Rule;
                    elem.IsCompress = IsCompress;
                    elem.IsNative = IsNative;
                    elem.IsPermanent = IsPermanent;
                    elem.Children = new List<Element>(Children);
                }

                /// <summary>
                ///   
                /// </summary>
                public override bool Equals(object obj)
                {
                    if (obj == null)
                    {
                        return false;
                    }
                    if (obj.GetType() != this.GetType())
                    {
                        return false;
                    }

                    Element other = obj as Element;
                    if (this.Name != other.Name)
                        return false;
                    if (this.IsFolder != other.IsFolder)
                        return false;
                    if (this.Rule != other.Rule)
                        return false;
                    if (this.Children == null && other.Children != null)
                        return false;
                    if (this.Children != null && other.Children == null)
                        return false;
                    if (this.Children != null && other.Children != null)
                    {
                        if (this.Children.Count != other.Children.Count)
                            return false;

                        int count = this.Children.Count;
                        for (int i = 0; i < count; ++i)
                        {
                            if (!this.Children[i].Equals(other.Children[i]))
                                return false;
                        }
                    }

                    return true;
                }

                /// <summary>
                /// 
                /// </summary>
                public override int GetHashCode()
                {
                    return Name.GetHashCode();
                }

                /// <summary>
                /// 排序
                /// 1.优先显示文件夹(以字符顺序排序)
                /// 2.其次显示文件(以字符顺序排序)
                /// </summary>
                public void SortChildren()
                {
                    if(Children != null && Children.Count > 1)
                    {
                        Children.Sort(_ComparisonElement);
                    }
                }

                int _ComparisonElement(Element x, Element y)
                {
                    if((x.IsFolder && y.IsFolder) || (!x.IsFolder && !y.IsFolder))
                    {
                        return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
                    }
                    else if(x.IsFolder)
                    {
                        return -1;
                    }
                    else if (y.IsFolder)
                    {
                        return 1;
                    }

                    return -1;
                }
            }

            public Element Root;
        }

        /// <summary>
        ///   Scene's build data
        /// </summary>
        public class SceneBuild
        {
            public class Element
            {
                /// <summary>
                /// 场景路径
                /// </summary>
                public string ScenePath;
                /// <summary>
                /// 是否打包
                /// </summary>
                public bool IsBuild;
                /// <summary>
                /// 是否压缩
                /// </summary>
                public bool IsCompress;
                /// <summary>
                /// 是否打包到安装包中
                /// </summary>
                public bool IsNative;
            }
            public List<Element> Scenes = new List<Element>();
        }

        /// <summary>
        /// 版本号
        /// </summary>
        public string strVersion;

        /// <summary>
        /// AssetBundle打包起始相对路径
        /// </summary>
        public string BuildStartLocalPath = Common.PROJECT_ASSET_ROOT_NAME;

        /// <summary>
        /// 是否打包所有AssetBundle至安装包
        /// </summary>
        public bool IsAllNative;

        /// <summary>
        /// 是否所有AssetBundle都压缩
        /// </summary>
        public bool IsAllCompress;

        /// <summary>
        /// 资源
        /// </summary>
        public AssetBuild Assets = new AssetBuild();

        /// <summary>
        /// 场景
        /// </summary>
        public SceneBuild Scenes = new SceneBuild();
    }

    /// <summary>
    /// 
    /// </summary>
    public class AssetBundleBuild
    {
        /// <summary>
        ///   数据
        /// </summary>
        public AssetBundleBuildData Data;

        /// <summary>
        /// AssetBundle打包起始全局路径
        /// </summary>
        public string BuildStartFullPath
        {
            get
            {
                if (Data == null)
                    return "";

                return EditorCommon.RelativeToAbsolutePath(Data.BuildStartLocalPath);
            }
        }


        /// <summary>
        ///   
        /// </summary>
        public AssetBundleBuild()
        {
            Data = new AssetBundleBuildData();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Load(string file_name)
        {
            return SimpleJsonReader.ReadFromFile<AssetBundleBuildData>(ref Data, file_name);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Save(string file_name)
        {
            return SimpleJsonWriter.WriteToFile(Data, file_name);
        }

        /// <summary>
        /// 更改打包资源起始路径
        /// </summary>
        public void ModifyAssetStartPath(string build_start_path, Action<string> progress_report)
        {
            if(!Directory.Exists(build_start_path))
            {
                return;
            }

            int startIndex = Common.PROJECT_ASSET_ROOT_NAME.Length;
            build_start_path = EditorCommon.AbsoluteToRelativePath(build_start_path);
            string new_native_path = build_start_path.Substring(startIndex);
            string old_native_path = Data.BuildStartLocalPath.Substring(startIndex);

            List<string> new_native_folders = new List<string>(new_native_path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
            List<string> old_native_folders = new List<string>(old_native_path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));

            AssetBundleBuildData.AssetBuild.Element root = null;
            CompareBuildData(ref root, Data.Assets.Root
                , new_native_folders, old_native_folders
                , EditorCommon.ASSET_START_PATH, progress_report);

            Data.Assets.Root = root;
            Data.BuildStartLocalPath = build_start_path;
        }

        /// <summary>
        /// 生成默认数据
        /// </summary>
        public void GenerateDefaultData(Action<string> progress_report)
        {
            Data.BuildStartLocalPath = Common.PROJECT_ASSET_ROOT_NAME;
            Data.Assets.Root = GenerateAssetBundleRuleData(EditorCommon.ASSET_START_PATH
                                , emAssetBundleNameRule.None, progress_report);
            Data.Scenes = GenerateSceneRuleData(progress_report);
        }

        /// <summary>
        /// 校正数据
        /// </summary>
        public void MatchData(Action<string> progress_report)
        {
            string path = EditorCommon.RelativeToAbsolutePath(Data.BuildStartLocalPath);
            MatchAssetRuleElement(path, Data.Assets.Root, progress_report);
            MatchSceneRuleData(ref Data.Scenes, progress_report);
        }

        /// <summary>
        /// 同步配置至ResourcesManifestData
        /// </summary>
        public void SyncConfigTo(ResourcesManifestData res)
        {
            if(res == null)
            {
                return;
            }

            res.strVersion = Data.strVersion;
            res.IsAllCompress = Data.IsAllCompress;
            res.IsAllNative = Data.IsAllNative;

            string path = Data.BuildStartLocalPath.ToLower();
            SyncAssetConfig(path, Data.Assets.Root, res, true);
            if(Data.Scenes.Scenes.Count > 0)
            {
                foreach(var element in Data.Scenes.Scenes)
                {
                    SyncSceneAssetConfig(element, res, true);
                }
            }
        }

        /// <summary>
        /// 从ResourcesManifestData同步配置
        /// </summary>
        public void SyncConfigFrom(ResourcesManifestData res)
        {
            if (res == null)
            {
                return;
            }

            Data.strVersion = res.strVersion;
            Data.IsAllCompress = res.IsAllCompress;
            Data.IsAllNative = res.IsAllNative;

            string path = Data.BuildStartLocalPath.ToLower();
            SyncAssetConfig(path, Data.Assets.Root, res, false);

            if (Data.Scenes.Scenes.Count > 0)
            {
                foreach (var element in Data.Scenes.Scenes)
                {
                    SyncSceneAssetConfig(element, res, false);
                }
            }
        }

        /// <summary>
        /// 遍历指定目录以及子目录，生成默认数据
        /// </summary>
        static AssetBundleBuildData.AssetBuild.Element GenerateAssetBundleRuleData(string path
                                        , emAssetBundleNameRule rule
                                        , Action<string> progress_report)
        {
            try
            {
                AssetBundleBuildData.AssetBuild.Element result = null;

                DirectoryInfo dir_info = new DirectoryInfo(path);
                if (dir_info.Exists)
                {
                    if (progress_report != null) { progress_report(path); }

                    if (!EditorCommon.IsIgnoreFolder(path))
                    {
                        //生成自身信息
                        result = new AssetBundleBuildData.AssetBuild.Element(dir_info.Name);
                        result.Rule = (int)rule;
                        result.IsFolder = true;

                        var dics = dir_info.GetDirectories();
                        var files = dir_info.GetFiles();
                        //遍历所有子文件
                        foreach (FileInfo f in files) //查找文件  
                        {
                            string str = f.ToString();
                            var child = GenerateAssetBundleRuleData(str, emAssetBundleNameRule.None, progress_report);
                            if (child != null)
                                result.Add(child);
                        }

                        //遍历所有文件夹
                        foreach (DirectoryInfo d in dics)
                        {
                            string str = d.ToString();
                            var child = GenerateAssetBundleRuleData(str, emAssetBundleNameRule.None, progress_report);
                            if (child != null)
                                result.Add(child);
                        }
                        result.SortChildren();
                    }
                }
                else if (File.Exists(path))
                {
                    if (!EditorCommon.IsIgnoreFile(path))
                    {
                        //生成自身信息
                        FileInfo info = new FileInfo(path);
                        result = new AssetBundleBuildData.AssetBuild.Element(info.Name);
                        result.Rule = (int)rule;
                        result.IsFolder = false;
                    }
                }

                return result;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }

            return null;
        }

        static Dictionary<string, uint> s_folder_dic_temp_;        ///< 辅助操作缓存
        static Dictionary<string, uint> s_file_dic_temp_;          ///< 辅助操作缓存

        /// <summary>
        ///   调整数据（匹配现有的文件&文件夹结构，删除无用的数据)
        /// </summary>
        static void MatchAssetRuleElement(string path, AssetBundleBuildData.AssetBuild.Element element
            , Action<string> progress_report)
        {
            try
            {
                DirectoryInfo dir_info = new DirectoryInfo(path);
                if (!dir_info.Exists)
                    return;

                if (progress_report != null) { progress_report(path); }

                uint bit_0 = 0x1;   // 存在数据中
                uint bit_1 = 0x2;   // 存在文件或文件夹

                if (s_folder_dic_temp_ == null)
                {
                    s_folder_dic_temp_ = new Dictionary<string, uint>(512);
                }
                else
                {
                    s_folder_dic_temp_.Clear();
                }
                if (s_file_dic_temp_ == null)
                {
                    s_file_dic_temp_ = new Dictionary<string, uint>(512);
                }
                else
                {
                    s_file_dic_temp_.Clear();
                }

                if (element.Children != null && element.Children.Count > 0)
                {
                    foreach (var elem in element.Children)
                    {
                        if (elem.IsFolder)
                        {
                            if (!s_folder_dic_temp_.ContainsKey(elem.Name))
                            {
                                s_folder_dic_temp_.Add(elem.Name, bit_0);
                            }
                        }
                        else
                        {
                            if (!s_file_dic_temp_.ContainsKey(elem.Name))
                            {
                                s_file_dic_temp_.Add(elem.Name, bit_0);
                            }
                        }
                    }
                }

                foreach (DirectoryInfo d in dir_info.GetDirectories())
                {
                    if (EditorCommon.IsIgnoreFolder(d.Name))
                        continue;

                    if (!s_folder_dic_temp_.ContainsKey(d.Name))
                    {
                        s_folder_dic_temp_.Add(d.Name, bit_1);
                    }
                    else
                    {
                        s_folder_dic_temp_.Remove(d.Name);
                    }
                }

                foreach (FileInfo f in dir_info.GetFiles())
                {
                    if (EditorCommon.IsIgnoreFile(f.Name))
                        continue;
                    if (!s_file_dic_temp_.ContainsKey(f.Name))
                    {
                        s_file_dic_temp_.Add(f.Name, bit_1);
                    }
                    else
                    {
                        s_file_dic_temp_.Remove(f.Name);
                    }
                }

                //删除不存在的数据
                if (element.Children != null && element.Children.Count > 0)
                {
                    element.Children.RemoveAll((elem) =>
                    {
                        if (elem.IsFolder)
                        {
                            return s_folder_dic_temp_.ContainsKey(elem.Name) && s_folder_dic_temp_[elem.Name] == bit_0;
                        }
                        else
                        {
                            return s_file_dic_temp_.ContainsKey(elem.Name) && s_file_dic_temp_[elem.Name] == bit_0;
                        }
                    });
                }

                //记录旧的子对象数量
                int oldChildrenCount = element.Children != null ? element.Children.Count : 0;

                //增加文件夹数据
                foreach (var pair in s_folder_dic_temp_)
                {
                    if (pair.Value == bit_1)
                    {
                        string full_name = path + "/" + pair.Key;
                        element.Add(GenerateAssetBundleRuleData(full_name
                                        , emAssetBundleNameRule.None
                                        , progress_report));
                    }
                }

                //增加文件数据
                foreach (var pair in s_file_dic_temp_)
                {
                    if (pair.Value == bit_1)
                    {
                        string full_name = path + "/" + pair.Key;
                        element.Add(GenerateAssetBundleRuleData(full_name
                                        , emAssetBundleNameRule.None
                                        , progress_report));
                    }
                }

                //更新子文件夹数据
                if (oldChildrenCount > 0)
                {
                    for (int i = 0; i < oldChildrenCount; ++i)
                    {
                        string full_name = path + "/" + element.Children[i].Name;
                        if (element.Children[i].IsFolder)
                        {
                            MatchAssetRuleElement(full_name, element.Children[i], progress_report);
                        }
                    }
                }

                //重新排序
                element.SortChildren();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(ex.Message);
            }
        }

        /// <summary>
        ///   生成场景默认数据
        /// </summary>
        static AssetBundleBuildData.SceneBuild GenerateSceneRuleData(Action<string> progress_report)
        {
            try
            {
                AssetBundleBuildData.SceneBuild scenes = new AssetBundleBuildData.SceneBuild();
                DirectoryInfo assets = new DirectoryInfo(EditorCommon.SCENE_START_PATH);
                if (assets.Exists)
                {
                    var files = assets.GetFiles("*.unity", SearchOption.AllDirectories);
                    foreach (var f in files)
                    {
                        if (progress_report != null) { progress_report(f.FullName); }

                        scenes.Scenes.Add(new AssetBundleBuildData.SceneBuild.Element() { ScenePath = f.FullName, IsBuild = false });
                    }
                }
                return scenes;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(ex.Message);
            }

            return new AssetBundleBuildData.SceneBuild();
        }

        /// <summary>
        ///   调整场景数据（匹配最新的场景目录,删除无用数据）
        /// </summary>
        static void MatchSceneRuleData(ref AssetBundleBuildData.SceneBuild old, Action<string> progress_report)
        {
            AssetBundleBuildData.SceneBuild rules = new AssetBundleBuildData.SceneBuild();
            DirectoryInfo assets = new DirectoryInfo(EditorCommon.SCENE_START_PATH);
            if (assets.Exists)
            {
                var files = assets.GetFiles("*.unity", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    if(progress_report != null) { progress_report(f.FullName); }
                    var scene = old.Scenes.Find((elem) =>
                    {
                        return elem.ScenePath == f.FullName;
                    });
                    if(scene == null)
                    {
                        scene = new AssetBundleBuildData.SceneBuild.Element() { ScenePath = f.FullName};
                    }
                    rules.Scenes.Add(scene);
                }

                old = rules;
            }
        }

        /// <summary>
        /// 同步资源配置
        /// </summary>
        static void SyncAssetConfig(string path, AssetBundleBuildData.AssetBuild.Element element, ResourcesManifestData data
            , bool is_write)
        {
            if(element.Rule == (int)emAssetBundleNameRule.Ignore)
            {
                return;
            }

            string key = EditorCommon.ConvertToAssetBundleName(path);
            ResourcesManifestData.AssetBundle ab_data;
            if (data.AssetBundles.TryGetValue(key, out ab_data))
            {
                if (is_write)
                {
                    ab_data.IsCompress = element.IsCompress;
                    ab_data.IsNative = element.IsNative;
                    ab_data.IsPermanent = element.IsPermanent;
                    ab_data.IsStartupLoad = element.IsStartupLoad;
                }
                else
                {
                    element.IsCompress = ab_data.IsCompress;
                    element.IsNative = ab_data.IsNative;
                    element.IsPermanent = ab_data.IsPermanent;
                    element.IsStartupLoad = ab_data.IsStartupLoad;
                }
            }

            if (element.Children != null && element.Children.Count > 0)
            {
                
                foreach (var child in element.Children)
                {
                    string child_path = path + "/" + child.Name.ToLower();
                    SyncAssetConfig(child_path, child, data, is_write);
                }
            }
        }

        /// <summary>
        /// 同步场景资源配置
        /// </summary>
        static void SyncSceneAssetConfig(AssetBundleBuildData.SceneBuild.Element element, ResourcesManifestData data
            , bool is_write)
        {
            string path = element.ScenePath.ToLower();
            string key = EditorCommon.ConvertToAssetBundleName(path);
            ResourcesManifestData.AssetBundle ab_data;
            if (data.AssetBundles.TryGetValue(key, out ab_data))
            {
                if (is_write)
                {
                    ab_data.IsCompress = element.IsCompress;
                    ab_data.IsNative = element.IsNative;
                    ab_data.IsPermanent = false;
                    ab_data.IsStartupLoad = false;
                }
                else
                {
                    element.IsCompress = ab_data.IsCompress;
                    element.IsNative = ab_data.IsNative;
                }
            }
        }

        /// <summary>
        /// 对比新旧打包资源起始路径，返回最新的配置数据
        /// </summary>
        static void CompareBuildData(ref AssetBundleBuildData.AssetBuild.Element result
            , AssetBundleBuildData.AssetBuild.Element old_root
            , List<string> new_native_folder_list
            , List<string> old_native_folder_list
            , string path
            , Action<string> progress_report)
        {
            if (new_native_folder_list.Count == 0 && old_native_folder_list.Count == 0)
            {
                result = old_root;
                return;
            }
            if (new_native_folder_list.Count == 0)
            {
                new_native_folder_list = null;
            }
            if (old_native_folder_list.Count == 0)
            {
                old_native_folder_list = null;
            }
            if (new_native_folder_list != null && old_native_folder_list != null)
            {
                if (new_native_folder_list[0] == old_native_folder_list[0])
                {
                    path = path + "/" + new_native_folder_list[0];
                    new_native_folder_list.RemoveAt(0);
                    old_native_folder_list.RemoveAt(0);
                    CompareBuildData(ref result, old_root
                        , new_native_folder_list, old_native_folder_list
                        , path, progress_report);
                }
                else
                {
                    foreach (var name in new_native_folder_list)
                    {
                        path = path + "/" + name;
                    }
                    result = GenerateAssetBundleRuleData(path
                                , emAssetBundleNameRule.None, progress_report);
                    return;
                }
            }

            //新路径是旧路径的父路径，保留旧的配置数据并生成其它的配置数据
            if (new_native_folder_list == null)
            {
                result = GenerateAssetBundleRuleData(path
                                , emAssetBundleNameRule.None, progress_report);

                var temp = result;
                for (int i = 0; i < old_native_folder_list.Count; ++i)
                {
                    if (temp == null)
                    {
                        break;
                    }
                    temp = temp.FindFolderElement(old_native_folder_list[i]);
                }
                if (temp != null)
                {
                    old_root.CopyTo(temp);
                }

                return;
            }

            //旧路径是新路径的父路径， 拷贝旧的配置数据中包含的新路径配置数据
            if (old_native_folder_list == null)
            {
                var temp = old_root;
                for (int i = 0; i < new_native_folder_list.Count; ++i)
                {
                    if (temp == null)
                    {
                        break;
                    }
                    temp = temp.FindFolderElement(new_native_folder_list[i]);
                }
                result = temp;

                if (result == null)
                {
                    foreach (var name in new_native_folder_list)
                    {
                        path = path + "/" + name;
                    }
                    result = GenerateAssetBundleRuleData(path
                                , emAssetBundleNameRule.None, progress_report);
                }
                return;
            }
        }
    }
}

