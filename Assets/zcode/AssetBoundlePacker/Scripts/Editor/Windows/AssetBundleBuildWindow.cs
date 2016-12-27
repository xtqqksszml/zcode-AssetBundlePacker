/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/20
 * Note  : AssetBundle打包配置窗口
***************************************************************/
using UnityEngine;
using UnityEditor;
using System.Collections;
using SimpleJson;
using System.IO;
using System;
using System.Collections.Generic;

namespace zcode.AssetBundlePacker
{
    public class AssetBundleBuildWindow : EditorWindow
    {
        /// <summary>
        /// 
        /// </summary>
        class AssetNodeGroup : GUILayoutMultiSelectGroup.NodeGroup
        {
            /// <summary>
            /// 
            /// </summary>
            public AssetNode Root;

            /// <summary>
            /// 
            /// </summary>
            public override GUILayoutMultiSelectGroup.OperateResult Draw()
            {
                GUILayoutMultiSelectGroup.OperateResult result = null;
                if (Root != null)
                    result = Root.Draw();

                return result;
            }

            /// <summary>
            /// 
            /// </summary>
            public override List<GUILayoutMultiSelectGroup.Node> GetRange(int begin, int end)
            {
                List<GUILayoutMultiSelectGroup.Node> temp = new List<GUILayoutMultiSelectGroup.Node>();
                Root.GetRange(ref temp, begin, end);
                return temp.Count > 0 ? temp : null;
            }
        }

        /// <summary>
        /// Asset节点
        /// </summary>
        class AssetNode : GUILayoutMultiSelectGroup.Node
        {
            /// <summary>
            /// 数据
            /// </summary>
            public AssetBundleBuildData.AssetBuild.Element Element;

            /// <summary>
            /// 是否展开
            /// </summary>
            public bool Expand;

            /// <summary>
            ///   粒度
            /// </summary>
            public int Granularity;

            /// <summary>
            /// 父对象
            /// </summary>
            public AssetNode Parent;

            /// <summary>
            /// 自身的子对象
            /// </summary>
            public List<AssetNode> Children = new List<AssetNode>();

            /// <summary>
            /// 
            /// </summary>
            public int Build(AssetBundleBuildData.AssetBuild.Element elem, int index)
            {
                if (elem == null)
                    return index;

                Element = elem;
                Index = index++;

                Children.Clear();
                if (elem.Children != null)
                {
                    for (int i = 0; i < elem.Children.Count; ++i)
                    {
                        AssetNode ctrl = new AssetNode();
                        index = ctrl.Build(elem.Children[i], index);
                        ctrl.Parent = this;
                        Children.Add(ctrl);
                    }
                }

                return index;
            }

            /// <summary>
            ///   刷新控件粒度信息
            /// </summary>
            public void RefreshGranularity(string parent_path, Dictionary<string, int> table)
            {
                string my_res_path = (parent_path + "/" + Element.Name).ToLower();

                if (Element.IsFolder)
                {
                    if (Children.Count > 0)
                    {
                        for (int i = 0; i < Children.Count; ++i)
                        {
                            Children[i].RefreshGranularity(my_res_path, table);
                        }
                    }
                }
                else
                {
                    if (table.ContainsKey(my_res_path))
                        Granularity = table[my_res_path];
                    else
                        Granularity = 0;
                }
            }

            /// <summary>
            /// 选中指定ID区间展开的节点
            /// </summary>
            public bool GetRange(ref List<GUILayoutMultiSelectGroup.Node> list, int begin, int end)
            {
                //判断自身是否需要选中
                if (Index >= begin && Index <= end)
                    list.Add(this);

                //结束选中
                if (Index == end)
                    return true;

                //子节点
                if (Expand && Children != null)
                {
                    foreach (var c in Children)
                    {
                        if (c.GetRange(ref list, begin, end))
                            return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// 渲染
            /// </summary>
            public override GUILayoutMultiSelectGroup.OperateResult Draw()
            {
                return DrawAssetNode(this, 10);
            }

            static GUILayoutMultiSelectGroup.OperateResult DrawAssetNode(AssetNode node, int space)
            {
                if (node.Element == null)
                    return null;

                GUILayoutMultiSelectGroup.OperateResult result = null;
                if (node.Element.IsFolder)
                    result = DrawAssetNodeFolder(node, space);
                else
                    result = DrawAssetNodeFile(node, space);

                //绘制子节点
                if (node.Expand)
                {
                    if (node.Children != null)
                    {
                        foreach (var c in node.Children)
                        {
                            if (result == null)
                                result = DrawAssetNode(c, space + ASSET_NODE_LAYER_SPACE);
                        }
                    }
                }

                return result;
            }

            /// <summary>
            /// 渲染文件夹
            /// </summary>
            static GUILayoutMultiSelectGroup.OperateResult DrawAssetNodeFolder(AssetNode tree, int space)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(space);
                //设置箭头
                string title = (tree.Expand ? "\u25BC" : "\u25BA") + (char)0x200a;
                bool toggleTxt = GUILayout.Toggle(true, title, "PreToolbar2", GUILayout.Width(10f));
                if (!toggleTxt)
                    tree.Expand = !tree.Expand;
                //绘制标题
                var result = DrawAssetNodContent(tree);
                GUILayout.EndHorizontal();

                return result;
            }

            /// <summary>
            /// 渲染文件
            /// </summary>
            static GUILayoutMultiSelectGroup.OperateResult DrawAssetNodeFile(AssetNode tree, int space)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(space);
                var result = DrawAssetNodContent(tree);
                GUILayout.EndHorizontal();

                return result;
            }


            /// <summary>
            /// 绘制标题
            /// </summary>
            static GUILayoutMultiSelectGroup.OperateResult DrawAssetNodContent(AssetNode tree)
            {
                EditorGUILayout.BeginHorizontal();
                string style = tree.IsSelect ? "PreToolbar" : "PreToolbar2";
                bool toggle = GUILayout.Button(tree.Element.Name, style, GUILayout.Width(200f));
                GUILayout.Label(((emAssetBundleNameRule)tree.Element.Rule).ToString(), style, GUILayout.Width(100f));
                if (tree.Granularity > 0)
                    GUILayout.Label(tree.Granularity.ToString(), style, GUILayout.Width(50f));
                EditorGUILayout.EndHorizontal();

                if (toggle)
                {
                    return new GUILayoutMultiSelectGroup.OperateResult()
                    {
                        SelectNode = tree,
                        Status = null,
                    };
                }

                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public const int ASSET_NODE_LAYER_SPACE = 10;

        /// <summary>
        ///  
        /// </summary>
        private AssetBundleBuild asset_bundle_build_;

        /// <summary>
        ///   打包方式
        /// </summary>
        enum emBuildType
        {
            StandaloneWindows,
            Android,
            IOS,
        }
        private emBuildType build_type_;

        /// <summary>
        /// 
        /// </summary>
        private GUILayoutMultiSelectGroup gui_multi_select_;

        /// <summary>
        ///   
        /// </summary>
        private string selection_scene_ = null;

        /// <summary>
        /// 
        /// </summary>
        private Vector2 scene_scroll_ = Vector2.zero;

        /// <summary>
        /// 
        /// </summary>
        void LoadData()
        {
            asset_bundle_build_ = new AssetBundleBuild();
            asset_bundle_build_.Load(AssetBundleBuild.FILE_FULL_NAME);
            Build();
        }

        /// <summary>
        ///  保存数据到文件中
        /// </summary>
        void SaveData()
        {
            if (asset_bundle_build_ != null)
                asset_bundle_build_.Save(AssetBundleBuild.FILE_FULL_NAME);
        }

        /// <summary>
        ///  
        /// </summary>
        void Build()
        {
            AssetNodeGroup group = new AssetNodeGroup();
            group.Root = new AssetNode();
            group.Root.Build(asset_bundle_build_.Data.Assets.Root, 0);

            gui_multi_select_ = new GUILayoutMultiSelectGroup(group);
        }

        /// <summary>
        /// 修改打包规则
        /// </summary>
        void ModifyRuleForSelectTreeNodes(emAssetBundleNameRule rule)
        {
            if(gui_multi_select_ != null)
            {
                for (int i = 0; i < gui_multi_select_.SelectNodes.Count; i++)
                {
                    var asset_node = gui_multi_select_.SelectNodes[i] as AssetNode;
                    asset_node.Element.Rule = (int)rule;
                }
            }
        }

        /// <summary>
        ///   加载粒度信息
        /// </summary>
        void LoadAssetBundleGranularityInfo()
        {
            Dictionary<string, int> granularity_table = new Dictionary<string, int>();

            //载入ResourcesManifest文件
            ResourcesManifest resoureces_manifest = new ResourcesManifest();
            resoureces_manifest.Load(EditorCommon.RESOURCES_MANIFEST_FILE_PATH);
            if (resoureces_manifest.Data.AssetBundles.Count == 0)
                return;

            //载入AssetBunbleManifest
            string full_name = EditorCommon.MAIN_MANIFEST_FILE_PATH;
            AssetBundleManifest manifest = Common.LoadMainManifestByPath(full_name);
            if (manifest == null)
                return;

            //遍历AssetBundle
            foreach (var ab_name in manifest.GetAllAssetBundles())
            {
                AssetBundle ab = EditorCommon.LoadAssetBundleFromName(ab_name);
                if (ab != null)
                {
                    //获得所有的AssetBundle依赖
                    List<string> de_abs = new List<string>(manifest.GetAllDependencies(ab_name));
                    //获得所有依赖的AssetBundle的Asset
                    List<string> de_assets = new List<string>();
                    foreach (var ab_name1 in de_abs)
                    {
                        AssetBundle ab1 = EditorCommon.LoadAssetBundleFromName(ab_name1);
                        if (ab1 != null)
                        {
                            de_assets.AddRange(ab1.GetAllAssetNames());
                            ab1.Unload(false);
                        }
                    }

                    //获得所有的Asset
                    List<string> result = new List<string>();
                    List<string> assets = new List<string>(ab.GetAllAssetNames());
                    SearchValidAsset(assets, de_assets, ref result);

                    foreach (var name in result)
                    {
                        if (granularity_table.ContainsKey(name))
                            granularity_table[name] = granularity_table[name] + 1;
                        else
                            granularity_table.Add(name, 1);
                    }

                    ab.Unload(false);
                }
            }

            //刷新UI数据
            AssetNodeGroup group = gui_multi_select_.Group as AssetNodeGroup;
            group.Root.RefreshGranularity("assets", granularity_table);
        }

        /// <summary>
        /// 打包AssetBundle
        /// </summary>
        void BuildingAssetBundle()
        {
            bool running = true;
            SaveData();
            running = AssetBundleNameTool.RunningAssetBundleNameTool(asset_bundle_build_);
            if (running)
                running = SceneConfigTool.GenerateAllSceneConfig(asset_bundle_build_.Data.Scenes);
            if (running)
                BuildAssetBundle.BuildAllAssetBundlesToTarget(GetBuildTargetType());
            if (running)
                LoadAssetBundleGranularityInfo();

            SceneConfigTool.RestoreAllScene(asset_bundle_build_.Data.Scenes);
        }

        /// <summary>
        /// 打包目标平台
        /// </summary>
        BuildTarget GetBuildTargetType()
        {
            if (build_type_ == emBuildType.StandaloneWindows)
                return BuildTarget.StandaloneWindows;
            else if (build_type_ == emBuildType.Android)
                return BuildTarget.Android;
            else if (build_type_ == emBuildType.IOS)
                return BuildTarget.iOS;

            return BuildTarget.StandaloneWindows;
        }

        /// <summary>
        ///   
        /// </summary>
        static void SearchValidAsset(List<string> list, List<string> invalid, ref List<string> output)
        {
            list.RemoveAll((child) =>
            {
                return invalid.Contains(child);
            });
            output.AddRange(list);

            foreach (var name in list)
            {
                List<string> assets = new List<string>();
                string[] array = AssetDatabase.GetDependencies(name, false);
                foreach (var asset in array)
                    assets.Add(asset.ToLower());

                SearchValidAsset(assets, invalid, ref output);
            }
        }

        #region Draw
        /// <summary>
        /// 
        /// </summary>
        void DrawGeneral()
        {
            GUILayout.BeginVertical(GUI.skin.FindStyle("flow background"), GUILayout.MaxHeight(80f));
            GUILayout.BeginHorizontal();
            build_type_ = (emBuildType)EditorGUILayout.EnumPopup("打包方式", build_type_, GUILayout.MinWidth(200f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            bool is_save_data = GUILayout.Button("仅保存规则文件（" + AssetBundleBuild.FILE_FULL_NAME + ")");
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            bool is_running_ab_name_tool = GUILayout.Button("仅生成资源AssetBundleName");
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            bool is_build = GUILayout.Button("开始打包");
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            if (is_save_data)
                SaveData();
            if (is_running_ab_name_tool)
                AssetBundleNameTool.RunningAssetBundleNameTool(asset_bundle_build_);
            if (is_build)
                BuildingAssetBundle();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawAssets()
        {
            GUILayout.Space(3f);
            GUILayout.BeginHorizontal(GUI.skin.FindStyle("flow background"));
            GUILayout.BeginVertical(GUILayout.Width(this.position.size.x - 200f));
            gui_multi_select_.Draw(false, true);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(200f));
            emAssetBundleNameRule rule;
            bool is_modify_rule = DrawSelectAssetNodeInfo(out rule);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (is_modify_rule) 
                ModifyRuleForSelectTreeNodes(rule);
        }

        /// <summary>
        ///   
        /// </summary>
        void DrawScenes()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("场景", "OL Title");
            GUILayout.Label("是否打包", "OL Title", GUILayout.Width(96f));
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.FindStyle("flow background"));
            GUILayout.BeginHorizontal(GUI.skin.FindStyle("flow background"));
            scene_scroll_ = GUILayout.BeginScrollView(scene_scroll_);
            for (int i = 0; i < asset_bundle_build_.Data.Scenes.Scenes.Count; ++i)
            {
                var scene = asset_bundle_build_.Data.Scenes.Scenes[i];
                GUI.color = Color.white;
                GUILayout.Space(-1f);
                bool highlight = selection_scene_ == scene.ScenePath;
                GUI.backgroundColor = highlight ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button(scene.ScenePath, "OL TextField", GUILayout.Height(20f)))
                    selection_scene_ = scene.ScenePath;
                scene.IsBuild = GUILayout.Toggle(scene.IsBuild, "", GUILayout.Width(48f));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 选中的树形节点信息
        /// </summary>
        bool DrawSelectAssetNodeInfo(out emAssetBundleNameRule rule)
        {
            rule = emAssetBundleNameRule.None;

            if (gui_multi_select_ == null || gui_multi_select_.SelectNodes.Count == 0)
                return false;

            var asset_node = gui_multi_select_.SelectNodes[0] as AssetNode;
            rule = (emAssetBundleNameRule)asset_node.Element.Rule;

            //打包规则
            GUILayout.BeginHorizontal();
            GUILayout.Label("打包规则", GUILayout.MaxWidth(50f), GUILayout.MaxHeight(16f));
            emAssetBundleNameRule select_rule = (emAssetBundleNameRule)EditorGUILayout.EnumPopup("", rule, GUILayout.MaxWidth(150f), GUILayout.MaxHeight(16f));
            GUILayout.EndHorizontal();

            //资源粒度
            if (gui_multi_select_.SelectNodes.Count == 1)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("资源粒度", GUILayout.MaxWidth(50f), GUILayout.MaxHeight(16f));
                GUILayout.Label(asset_node.Granularity.ToString(), "AS TextArea", GUILayout.MaxWidth(150f), GUILayout.MaxHeight(16f));
                GUILayout.EndHorizontal();
            }

            if (select_rule != rule)
            {
                rule = select_rule;
                return true;
            }

            return false;
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        void Awake()
        {
        }

        /// <summary>
        ///   
        /// </summary>
        void OnEnable()
        {
            LoadData();
            LoadAssetBundleGranularityInfo();
        }

        /// <summary>
        /// 
        /// </summary>
        void Update()
        {
        }

        /// <summary>
        ///   
        /// </summary>
        void OnGUI()
        {
            if (GUILayoutHelper.DrawHeader("常规", "1", true, false))
            {
                DrawGeneral();
            }
            if (GUILayoutHelper.DrawHeader("场景(" + EditorCommon.SCENE_START_PATH + ")", "2", true, false))
            {
                DrawScenes();
            }
            if (GUILayoutHelper.DrawHeader("资源(" + EditorCommon.ASSET_START_PATH + ")", "3", true, false))
            {
                DrawAssets();
            }
        }

        [MenuItem("AssetBundle/Windows/AssetBundle Build Window")]
        public static void Open()
        {
            AssetBundleBuildWindow.GetWindow<AssetBundleBuildWindow>("AssetBundle Build");
        }
    }
}