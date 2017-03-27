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
            }

            public Element Root;
        }
        public AssetBuild Assets = new AssetBuild();   

        /// <summary>
        ///   Scene's build data
        /// </summary>
        public class SceneBuild
        {
            public class Element
            {
                public string ScenePath;
                public bool IsBuild;
            }
            public List<Element> Scenes = new List<Element>();
        }
        public SceneBuild Scenes = new SceneBuild();
    }

    /// <summary>
    /// 
    /// </summary>
    public class AssetBundleBuild
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public static readonly string FILE_FULL_NAME = Application.dataPath + "/AssetBundleBuild.rule";

        /// <summary>
        ///   数据
        /// </summary>
        public AssetBundleBuildData Data;

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
            bool result = SimpleJsonReader.ReadFromFile<AssetBundleBuildData>(ref Data, file_name);
            if (result)
            {
                MatchAssetRuleElement(EditorCommon.ASSET_START_PATH, Data.Assets.Root);
                MatchSceneRuleData(ref Data.Scenes);
            }
            else
            {
                GenerateDefaultData();
            }
            
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
        /// 生成默认数据
        /// </summary>
        public void GenerateDefaultData()
        {
            Data.Assets.Root = GenerateAssetBundleRuleData(EditorCommon.ASSET_START_PATH);
            Data.Scenes = GenerateSceneRuleData();
        }

        /// <summary>
        /// 遍历指定目录以及子目录，生成默认数据
        /// </summary>
        static AssetBundleBuildData.AssetBuild.Element GenerateAssetBundleRuleData(string path
                                        , emAssetBundleNameRule rule = emAssetBundleNameRule.None)
        {
            try
            {
                AssetBundleBuildData.AssetBuild.Element result = null;
                if (Directory.Exists(path))
                {
                    if (!EditorCommon.IsIgnoreFolder(path))
                    {
                        DirectoryInfo dir_info = new DirectoryInfo(path);

                        //生成自身信息
                        result = new AssetBundleBuildData.AssetBuild.Element(dir_info.Name);
                        result.Rule = (int)rule;
                        result.IsFolder = true;

                        //遍历所有文件夹
                        foreach (DirectoryInfo d in dir_info.GetDirectories())
                        {
                            string str = d.ToString();
                            AssetBundleBuildData.AssetBuild.Element child = GenerateAssetBundleRuleData(str);
                            if (child != null)
                                result.Add(child);
                        }

                        //遍历所有子文件
                        foreach (FileInfo f in dir_info.GetFiles()) //查找文件  
                        {
                            string str = f.ToString();
                            AssetBundleBuildData.AssetBuild.Element child = GenerateAssetBundleRuleData(str);
                            if (child != null)
                                result.Add(child);
                        }
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

        /// <summary>
        ///   调整数据（匹配现有的文件&文件夹结构，删除无用的数据)
        /// </summary>
        static void MatchAssetRuleElement(string path, AssetBundleBuildData.AssetBuild.Element element)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    uint bit_0 = 0x1;   // 存在数据中
                    uint bit_1 = 0x2;   // 存在文件或文件夹
                    Dictionary<string, uint> folder_dic = new Dictionary<string, uint>();
                    Dictionary<string, uint> file_dic = new Dictionary<string, uint>();

                    if (element.Children != null && element.Children.Count > 0)
                    {
                        foreach (var elem in element.Children)
                        {
                            if (elem.IsFolder)
                            {
                                if (!folder_dic.ContainsKey(elem.Name))
                                    folder_dic.Add(elem.Name, bit_0);
                                else
                                    folder_dic[elem.Name] |= bit_0;
                            }
                            else
                            {
                                if (!file_dic.ContainsKey(elem.Name))
                                    file_dic.Add(elem.Name, bit_0);
                                else
                                    file_dic[elem.Name] |= bit_0;
                            }
                        }
                    }

                    DirectoryInfo dir_info = new DirectoryInfo(path);
                    foreach (DirectoryInfo d in dir_info.GetDirectories())
                    {
                        if (EditorCommon.IsIgnoreFolder(d.Name))
                            continue;

                        if (!folder_dic.ContainsKey(d.Name))
                            folder_dic.Add(d.Name, bit_1);
                        else
                            folder_dic[d.Name] |= bit_1;
                    }
                    foreach (FileInfo f in dir_info.GetFiles())
                    {
                        if (EditorCommon.IsIgnoreFile(f.Name))
                            continue;
                        if (!file_dic.ContainsKey(f.Name))
                            file_dic.Add(f.Name, bit_1);
                        else
                            file_dic[f.Name] |= bit_1;
                    }

                    //删除不存在的文件夹或文件
                    if (element.Children != null && element.Children.Count > 0)
                    {
                        element.Children.RemoveAll((elem) =>
                        {
                            if (elem.IsFolder)
                            {
                                return folder_dic[elem.Name] == bit_0;
                            }
                            else
                            {
                                return file_dic[elem.Name] == bit_0;
                            }
                        });

                        //更新子文件夹数据
                        for (int i = 0; i < element.Children.Count; ++i)
                        {
                            if (element.Children[i].IsFolder)
                            {
                                string full_name = path + "/" + element.Children[i].Name;
                                MatchAssetRuleElement(full_name, element.Children[i]);
                            }
                        }
                    }

                    //增加文件夹
                    foreach (var pair in folder_dic)
                    {
                        if (pair.Value == bit_1)
                        {
                            string full_name = path + "/" + pair.Key;
                            element.Add(GenerateAssetBundleRuleData(full_name, (emAssetBundleNameRule)element.Rule));
                        }
                    }

                    //增加文件
                    foreach (var pair in file_dic)
                    {
                        if (pair.Value == bit_1)
                        {
                            string full_name = path + "/" + pair.Key;
                            element.Add(GenerateAssetBundleRuleData(full_name, (emAssetBundleNameRule)element.Rule));
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(ex.Message);
            }
        }

        /// <summary>
        ///   生成场景默认数据
        /// </summary>
        static AssetBundleBuildData.SceneBuild GenerateSceneRuleData()
        {
            try
            {
                AssetBundleBuildData.SceneBuild scenes = new AssetBundleBuildData.SceneBuild();
                DirectoryInfo assets = new DirectoryInfo(EditorCommon.SCENE_START_PATH);
                if (assets.Exists)
                {
                    foreach (var f in assets.GetFiles("*.unity", SearchOption.AllDirectories))
                    {
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
        static void MatchSceneRuleData(ref AssetBundleBuildData.SceneBuild old)
        {
            AssetBundleBuildData.SceneBuild rules = new AssetBundleBuildData.SceneBuild();
            DirectoryInfo assets = new DirectoryInfo(EditorCommon.SCENE_START_PATH);
            if (assets.Exists)
            {
                foreach (var f in assets.GetFiles("*.unity", SearchOption.AllDirectories))
                {
                    var scene = old.Scenes.Find((elem) =>
                    {
                        return elem.ScenePath == f.FullName;
                    });

                    bool is_build = scene != null && scene.IsBuild;
                    rules.Scenes.Add(new AssetBundleBuildData.SceneBuild.Element() { ScenePath = f.FullName, IsBuild = is_build });
                }

                old = rules;
            }
        }
    }

}

