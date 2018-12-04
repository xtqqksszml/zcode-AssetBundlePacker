/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/20
 * Note  : AssetBundle打包配置窗口
***************************************************************/
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

namespace zcode.AssetBundlePacker
{
    public class AssetBundleBuildWindow : EditorWindow
    {
        /// <summary>
        ///   打包方式
        /// </summary>
        enum emBuildType
        {
            StandaloneWindows,
            Android,
            IOS,
        }

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
            public override GUILayoutMultiSelectGroup.OperateResult Draw(float width)
            {
                GUILayoutMultiSelectGroup.OperateResult result = null;
                if (Root != null)
                    result = Root.Draw(width);

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
            /// 粒度详细信息
            /// </summary>
            public string GranularityDetails;

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
            public override GUILayoutMultiSelectGroup.OperateResult Draw(float width)
            {
                return DrawAssetNode(this, width, 10);
            }

            static GUILayoutMultiSelectGroup.OperateResult DrawAssetNode(AssetNode node, float width, int space)
            {
                if (node.Element == null)
                    return null;

                bool is_ignore = node.Element != null && node.Element.Rule == (int)emAssetBundleNameRule.Ignore;
                GUI.color = is_ignore ? Color.grey : Color.white;
                GUILayoutMultiSelectGroup.OperateResult result = null;
                if (node.Element.IsFolder)
                    result = DrawAssetNodeFolder(node, width, space);
                else
                    result = DrawAssetNodeFile(node, width, space);

                //绘制子节点
                if (!is_ignore && node.Expand)
                {
                    if (node.Children != null)
                    {
                        foreach (var c in node.Children)
                        {
                            if (result == null)
                                result = DrawAssetNode(c, width, space + ASSET_NODE_LAYER_SPACE);
                        }
                    }
                }

                GUI.color = Color.white;
                return result;
            }

            /// <summary>
            /// 渲染文件夹
            /// </summary>
            static GUILayoutMultiSelectGroup.OperateResult DrawAssetNodeFolder(AssetNode tree, float width, int space)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(space);
                //设置箭头
                string title = (tree.Expand ? "\u25BC" : "\u25BA") + (char)0x200a;
                bool toggleTxt = GUILayout.Toggle(true, title, "PreToolbar2", GUILayout.Width(10f));
                if (!toggleTxt)
                    tree.Expand = !tree.Expand;
                //绘制标题
                var result = DrawAssetNodeContent(tree, width - space);
                GUILayout.EndHorizontal();

                return result;
            }

            /// <summary>
            /// 渲染文件
            /// </summary>
            static GUILayoutMultiSelectGroup.OperateResult DrawAssetNodeFile(AssetNode tree, float width, int space)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(space);
                var result = DrawAssetNodeContent(tree, width - space);
                GUILayout.EndHorizontal();

                return result;
            }


            /// <summary>
            /// 绘制节点内容
            /// </summary>
            static GUILayoutMultiSelectGroup.OperateResult DrawAssetNodeContent(AssetNode tree, float width)
            {
                EditorGUILayout.BeginHorizontal();
                string style = tree.IsSelect ? "PreToolbar" : "PreToolbar2";
                bool toggle = GUILayout.Button(tree.Element.Name, style, GUILayout.MaxWidth(width - 480f));
                GUILayout.Label(((emAssetBundleNameRule)tree.Element.Rule).ToString(), style, GUILayout.Width(80f));

                if (tree.Element.Rule == (int)emAssetBundleNameRule.SingleFile ||
                    tree.Element.Rule == (int)emAssetBundleNameRule.Folder)
                {
                    var config = AssetBundleBuildWindow.Instance.asset_bundle_build_.Data;
                    if (config.IsAllCompress) { tree.Element.IsCompress = true; }
                    if (config.IsAllNative) { tree.Element.IsNative = true; }

                    GUILayout.Label(tree.Element.IsCompress ? "√" : "✗", style, GUILayout.Width(80f));
                    GUILayout.Label(tree.Element.IsNative ? "√" : "✗", style, GUILayout.Width(80f));
                    GUILayout.Label(tree.Element.IsPermanent ? "√" : "✗", style, GUILayout.Width(80f));
                    GUILayout.Label(tree.Element.IsStartupLoad ? "√" : "✗", style, GUILayout.Width(80f));
                }
                else
                {
                    GUILayout.Label("", style, GUILayout.Width(320f));
                }

                GUILayout.Label(tree.Granularity > 0 ? tree.Granularity.ToString() : "", style, GUILayout.Width(80f));

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

            /// <summary>
            /// 绘制资源标题
            /// </summary>
            public static void DrawAssetTitle(float width)
            {
                EditorGUILayout.BeginHorizontal();
                string style = "OL Title";
                GUILayout.Label("  资源路径", style, GUILayout.Width(width - 500f));
                GUILayout.Label("打包规则", style, GUILayout.Width(80f));
                GUILayout.Label("压缩", style, GUILayout.Width(80f));
                GUILayout.Label("打包到安装包", style, GUILayout.Width(80f));
                GUILayout.Label("常驻内存", style, GUILayout.Width(80f));
                GUILayout.Label("启动时加载", style, GUILayout.Width(80f));
                GUILayout.Label("资源粒度", style, GUILayout.Width(80f));
                GUILayout.Label("", style, GUILayout.Width(20f));
                EditorGUILayout.EndHorizontal();
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
        /// 打包选项
        /// </summary>
        private BuildAssetBundleOptions build_option_ = BuildAssetBundleOptions.DeterministicAssetBundle;

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
        /// 资源打包起始路径
        /// </summary>
        string build_start_full_path_;

        /// <summary>
        /// 重新载入数据
        /// </summary>
        public void SyncConfigForm(ResourcesManifestData res)
        {
            asset_bundle_build_.SyncConfigFrom(res);
        }

        /// <summary>
        /// 
        /// </summary>
        void LoadData()
        {
            asset_bundle_build_ = new AssetBundleBuild();
            if (!asset_bundle_build_.Load(EditorCommon.ASSETBUNDLE_BUILD_RULE_FILE_PATH))
            {
                if (EditorUtility.DisplayDialog("初始化打包配置文件", "当前打包配置文件不存在, 需要生成默认配置文件！", "生成"))
                {
                    float total = 0;
                    if (Directory.Exists(EditorCommon.SCENE_START_PATH))
                    {
                        string[] files = Directory.GetFiles(EditorCommon.SCENE_START_PATH, "*.unity", SearchOption.AllDirectories);
                        total += files.Length;
                    }
                    if (Directory.Exists(asset_bundle_build_.BuildStartFullPath))
                    {
                        string[] dirs = Directory.GetDirectories(asset_bundle_build_.BuildStartFullPath, "*", SearchOption.AllDirectories);
                        total += dirs.Length;
                    }

                    float current = 0f;
                    asset_bundle_build_.GenerateDefaultData((name) =>
                    {
                        //进度条提示
                        current += 1.0f;
                        float progress = current / total;
                        EditorUtility.DisplayProgressBar("初始化打包配置文件", "正在初始化 " + name, progress);
                    });

                    EditorUtility.ClearProgressBar();
                    Build();
                }
            }
            else
            {
                if (EditorUtility.DisplayDialog("更新打包配置文件", "是否需要更新打包配置文件！（不执行更新可能会导致资源变更无法被正确识别）", "更新", "不更新"))
                {
                    float total = 0;
                    if (Directory.Exists(EditorCommon.SCENE_START_PATH))
                    {
                        string[] files = Directory.GetFiles(EditorCommon.SCENE_START_PATH, "*.unity", SearchOption.AllDirectories);
                        total += files.Length;
                    }
                    if (Directory.Exists(asset_bundle_build_.BuildStartFullPath))
                    {
                        string[] dirs = Directory.GetDirectories(asset_bundle_build_.BuildStartFullPath, "*", SearchOption.AllDirectories);
                        total += dirs.Length;
                    }

                    float current = 0f;
                    asset_bundle_build_.MatchData((name) =>
                    {
                        //进度条提示
                        current += 1.0f;
                        float progress = current / total;
                        EditorUtility.DisplayProgressBar("更新打包配置文件", "正在更新 " + name, progress);
                    });
                    EditorUtility.ClearProgressBar();
                }

                Build();
            }
        }

        /// <summary>
        /// 修改打包资源起始路径
        /// </summary>
        void ModifyAssetStartPath()
        {
            if (build_start_full_path_ == asset_bundle_build_.BuildStartFullPath)
            {
                return;
            }

            if (!Directory.Exists(build_start_full_path_) || !build_start_full_path_.Contains(EditorCommon.ASSET_START_PATH))
            {
                if (EditorUtility.DisplayDialog("更改资源打包起始路径"
                    , "无效的路径，路径必须是项目资源路径\n（" + EditorCommon.ASSET_START_PATH + "）下的子路径！"
                    , "好的"))
                {
                    build_start_full_path_ = asset_bundle_build_.BuildStartFullPath;
                }
                else
                {
                    build_start_full_path_ = asset_bundle_build_.BuildStartFullPath;
                }
                return;
            }

            if (EditorUtility.DisplayDialog("更改资源打包起始路径"
                , "更改资源打包起始路径并重新生成打包配置（兼容旧的配置）。\n此操作会删除所有打包的AssetBundle！\n\n是否确定更改?"
                , "确定"
                , "取消"))
            {
                //修改配置文件
                asset_bundle_build_.ModifyAssetStartPath(build_start_full_path_, (name) =>
                {
                    //进度条提示
                    EditorUtility.DisplayProgressBar("重新生成打包配置文件", "修改 " + name, 1f);
                });
                SaveData();

                Build();

                //删除所有AssetBundle
                if (!Directory.Exists(EditorCommon.BUILD_PATH))
                {
                    Directory.Delete(EditorCommon.BUILD_PATH);
                }

                EditorUtility.ClearProgressBar();
            }
            else
            {
                build_start_full_path_ = asset_bundle_build_.BuildStartFullPath;
            }
        }

        /// <summary>
        ///  保存数据到文件中
        /// </summary>
        void SaveData()
        {
            EditorUtility.DisplayProgressBar("保存", "正在保存规则文件", 0);
            if (asset_bundle_build_ != null)
            {
                asset_bundle_build_.Save(EditorCommon.ASSETBUNDLE_BUILD_RULE_FILE_PATH);
            }
            EditorUtility.ClearProgressBar();
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

            build_start_full_path_ = asset_bundle_build_.BuildStartFullPath;
        }

        /// <summary>
        /// 修改打包安装包配置
        /// </summary>
        void ModifyPackNativeConfig(bool set)
        {
            asset_bundle_build_.Data.IsAllNative = set;
        }

        /// <summary>
        /// 修改压缩配置
        /// </summary>
        void ModifyCompressConfig(bool set)
        {
            asset_bundle_build_.Data.IsAllCompress = set;
        }

        /// <summary>
        /// 刷新打包数据
        /// </summary>
        void UpdateAssetBundleBuildData()
        {

        }

        /// <summary>
        /// 修改打包规则
        /// </summary>
        void ModifyRuleForSelectTreeNodes(emAssetBundleNameRule rule
            , bool is_compress, bool is_native, bool is_permanent, bool is_startup_load)
        {
            if (gui_multi_select_ != null)
            {
                for (int i = 0; i < gui_multi_select_.SelectNodes.Count; i++)
                {
                    var asset_node = gui_multi_select_.SelectNodes[i] as AssetNode;
                    asset_node.Element.Rule = (int)rule;
                    asset_node.Element.IsCompress = is_compress;
                    asset_node.Element.IsNative = is_native;
                    asset_node.Element.IsPermanent = is_permanent;
                    asset_node.Element.IsStartupLoad = is_startup_load;
                }
            }
        }

        /// <summary>
        /// 开启粒度加载
        /// </summary>
        void LoadGranularityInfoAndDisplayProgress(bool show_details)
        {
            LoadGranularityInfo(show_details, (name, progress) =>
            {
                EditorUtility.DisplayProgressBar("分析粒度信息", name, progress);
            });
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        ///   加载粒度信息
        /// </summary>
        void LoadGranularityInfo(bool show_details, Action<string, float> progress_report)
        {
            Dictionary<string, string> granularity_details_table = null;
            if (show_details)
            {
                granularity_details_table = new Dictionary<string, string>(16384);
            }
            Dictionary<string, int> granularity_table = new Dictionary<string, int>(16384);

            //载入ResourcesManifest文件
            ResourcesManifest resoureces_manifest = new ResourcesManifest();
            resoureces_manifest.Load(EditorCommon.RESOURCES_MANIFEST_FILE_PATH);
            if (resoureces_manifest.Data.AssetBundles == null || resoureces_manifest.Data.AssetBundles.Count == 0)
                return;

            //载入AssetBunbleManifest
            string full_name = EditorCommon.MAIN_MANIFEST_FILE_PATH;
            AssetBundleManifest manifest = Common.LoadMainManifestByPath(full_name);
            if (manifest == null)
                return;

            var all_asset_bundle_names = manifest.GetAllAssetBundles();

            //遍历AssetBundle,并载入所有AssetBundle
            float count = all_asset_bundle_names.Length;
            float current = 0;
            Dictionary<string, AssetBundle> all_ab = new Dictionary<string, AssetBundle>(all_asset_bundle_names.Length);
            foreach (var ab_name in all_asset_bundle_names)
            {
                if (progress_report != null) { progress_report("正在加载 " + ab_name, ++current / count); }

                AssetBundle ab = EditorCommon.LoadAssetBundleFromName(ab_name);
                all_ab.Add(ab_name, ab);
            }

            //遍历所有ab包，计算粒度
            HashSet<string> result = new HashSet<string>();
            HashSet<string> dep_asset_names = new HashSet<string>();

            count = all_ab.Count;
            current = 0;
            foreach (var ab in all_ab)
            {
                string ab_name = ab.Key;
                AssetBundle assetbundle = ab.Value;

                if (progress_report != null) { progress_report("正在分析 " + ab_name, ++current / count); }

                //获得所有的AssetBundle依赖
                string[] dep_ab_names = manifest.GetAllDependencies(ab_name);
                //获得所有依赖的AssetBundle的Asset
                dep_asset_names.Clear();
                foreach (var dep_name in dep_ab_names)
                {
                    AssetBundle dep_ab = null;
                    if (all_ab.TryGetValue(dep_name, out dep_ab))
                    {
                        string[] asset_names = dep_ab.GetAllAssetNames();
                        for (int i = 0; i < asset_names.Length; ++i)
                        {
                            dep_asset_names.Add(asset_names[i]);
                        }
                    }
                }

                //搜寻所有有效的资源
                result.Clear();
                List<string> assets = new List<string>(assetbundle.GetAllAssetNames());
                SearchValidAsset(assets, dep_asset_names, ref result);

                //设置粒度值
                foreach (var name in result)
                {
                    if (granularity_table.ContainsKey(name))
                        granularity_table[name] = granularity_table[name] + 1;
                    else
                        granularity_table.Add(name, 1);

                    if(granularity_details_table != null)
                    {
                        if (!granularity_details_table.ContainsKey(name))
                        {
                            granularity_details_table.Add(name, "");
                        }
                        granularity_details_table[name] = granularity_details_table[name] + ab_name + "\n";
                    }
                }
                result.Clear();
            }

            //卸载所有AssetBundle
            foreach (var ab in all_ab)
            {
                ab.Value.Unload(true);
            }
            all_ab.Clear();

            //刷新UI数据
            string path = asset_bundle_build_.Data.BuildStartLocalPath.ToLower();
            AssetNodeGroup group = gui_multi_select_.Group as AssetNodeGroup;
            current = 0;
            count = asset_bundle_build_.Data.Assets.Root.Count();
            RefreshGranularity(granularity_table, granularity_details_table, path, group.Root, (name) =>
            {
                if (progress_report != null) { progress_report("正在刷新 " + name, ++current / count); }
            });

        }

        /// <summary>
        /// 刷新资源的粒度信息
        /// </summary>
        static void RefreshGranularity(Dictionary<string, int> table
            , Dictionary<string, string> details
            , string relative_path, AssetNode node
            , Action<string> progress_report)
        {
            if (progress_report != null) { progress_report(relative_path); }
            if (node.Element.IsFolder)
            {
                if (node.Children.Count > 0)
                {
                    for (int i = 0; i < node.Children.Count; ++i)
                    {
                        string child_path = relative_path + "/" + node.Children[i].Element.Name.ToLower();
                        RefreshGranularity(table, details, child_path, node.Children[i], progress_report);
                    }
                }
            }
            else
            {
                if (table.ContainsKey(relative_path))
                    node.Granularity = table[relative_path];
                else
                    node.Granularity = 0;

                if (details != null && details.ContainsKey(relative_path))
                    node.GranularityDetails = details[relative_path];
                else
                    node.GranularityDetails = null;
            }
        }

        /// <summary>
        /// 打包AssetBundle
        /// </summary>
        void BuildingAssetBundle(emBuildType build_type)
        {
            try
            {
                bool running = true;
                SaveData();
                running = AssetBundleNameTool.RunningAssetBundleNameTool(asset_bundle_build_);
                if (running)
                    running = SceneConfigTool.GenerateAllSceneConfig(asset_bundle_build_.Data.Scenes);
                if (running)
                    AssetDatabase.Refresh();
                if (running)
                    BuildAssetBundle.BuildAllAssetBundlesToTarget(asset_bundle_build_, GetBuildTargetType(build_type), build_option_);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            SceneConfigTool.RestoreAllScene(asset_bundle_build_.Data.Scenes);
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 打包目标平台
        /// </summary>
        BuildTarget GetBuildTargetType(emBuildType build_type)
        {
            if (build_type == emBuildType.StandaloneWindows)
                return BuildTarget.StandaloneWindows;
            else if (build_type == emBuildType.Android)
                return BuildTarget.Android;
            else if (build_type == emBuildType.IOS)
                return BuildTarget.iOS;

            return BuildTarget.StandaloneWindows;
        }

        /// <summary>
        ///   
        /// </summary>
        static void SearchValidAsset(List<string> list, HashSet<string> invalid, ref HashSet<string> output)
        {
            foreach (var name in list)
            {
                if (!output.Contains(name))
                {
                    output.Add(name);
                }

                List<string> assets = new List<string>();
                string[] array = AssetDatabase.GetDependencies(name, false);
                foreach (var asset in array)
                {
                    if (!EditorCommon.IsIgnoreFile(asset))
                    {
                        var asset_name = asset.ToLower();
                        if (!invalid.Contains(asset_name))
                        {
                            assets.Add(asset_name);
                        }
                    }
                }

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
            GUILayout.Label("打包资源起始路径", GUILayout.MaxWidth(200f));
            build_start_full_path_ = GUILayout.TextField(build_start_full_path_);
            bool is_modify_build_path = GUILayout.Button("更改", GUILayout.Width(40f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("资源版本号", GUILayout.MaxWidth(200f));
            asset_bundle_build_.Data.strVersion = EditorGUILayout.TextField(asset_bundle_build_.Data.strVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("输出位置", GUILayout.MaxWidth(200f));
            EditorGUILayout.LabelField(EditorCommon.BUILD_PATH);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("打包全部AssetBundle至安装包", GUILayout.MaxWidth(200f));
            bool is_all_native = EditorGUILayout.Toggle(asset_bundle_build_.Data.IsAllNative);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("压缩全部AssetBundle", GUILayout.MaxWidth(200f));
            bool is_all_compress = EditorGUILayout.Toggle(asset_bundle_build_.Data.IsAllCompress);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("打包选项", GUILayout.MaxWidth(200f));
            build_option_ = (BuildAssetBundleOptions)EditorGUILayout.EnumPopup(build_option_, GUILayout.MinWidth(200f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            bool is_show_granularity_details = GUILayout.Button("加载粒度数据，显示粒度引用次数与详细引用信息");
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            bool is_save_data = GUILayout.Button("仅保存规则文件（" + EditorCommon.ASSETBUNDLE_BUILD_RULE_FILE_PATH + ")");
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            bool is_running_ab_name_tool = GUILayout.Button("仅生成资源AssetBundleName");
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            bool is_build_win = GUILayout.Button("Windows平台版本 - 打包");
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            bool is_build_android = GUILayout.Button(" Android平台版本 - 打包");
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            bool is_build_ios = GUILayout.Button("      IOS平台版本 - 打包");
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            if(is_all_native != asset_bundle_build_.Data.IsAllNative)
            {
                ModifyPackNativeConfig(is_all_native);
            }
            if(is_all_compress != asset_bundle_build_.Data.IsAllCompress)
            {
                ModifyCompressConfig(is_all_compress);
            }
            if(is_show_granularity_details)
            {
                LoadGranularityInfoAndDisplayProgress(true);
            }
            if (is_modify_build_path)
            {
                ModifyAssetStartPath();
            }
            if (is_save_data)
            {
                SaveData();
            }
            if (is_running_ab_name_tool)
            {
                SaveData();
                AssetBundleNameTool.RunningAssetBundleNameTool(asset_bundle_build_);
                SceneConfigTool.GenerateAllSceneConfig(asset_bundle_build_.Data.Scenes);
                AssetDatabase.Refresh();
            }
            if (is_build_win)
            {
                BuildingAssetBundle(emBuildType.StandaloneWindows);
            }
            else if (is_build_android)
            {
                BuildingAssetBundle(emBuildType.Android);
            }
            else if (is_build_ios)
            {
                BuildingAssetBundle(emBuildType.IOS);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawAssets()
        {
            GUILayout.Space(3f);
            GUILayout.BeginHorizontal(GUI.skin.FindStyle("flow background"));
            float width = this.position.size.x - 200f;
            GUILayout.BeginVertical(GUILayout.Width(width));
            AssetNode.DrawAssetTitle(width);
            gui_multi_select_.Draw(width, false, true);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(200f));
            emAssetBundleNameRule rule;
            bool is_compress;
            bool is_native;
            bool is_permanent;
            bool is_startup_load;
            bool is_modify_rule = DrawSelectAssetNodeInfo(out rule, out is_compress
                , out is_native, out is_permanent, out is_startup_load);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (is_modify_rule)
                ModifyRuleForSelectTreeNodes(rule, is_compress, is_native, is_permanent, is_startup_load);
        }

        /// <summary>
        ///   
        /// </summary>
        void DrawScenes()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("场景", "OL Title");
            GUILayout.Label("是否打包", "OL Title", GUILayout.Width(80f));
            GUILayout.Label("压缩", "OL Title", GUILayout.Width(80f));
            GUILayout.Label("打包到安装包", "OL Title", GUILayout.Width(80f));
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

                scene.IsBuild = GUILayout.Toggle(scene.IsBuild, "", GUILayout.Width(80f));
                scene.IsCompress = GUILayoutHelper.Toggle(scene.IsCompress || asset_bundle_build_.Data.IsAllCompress, "", asset_bundle_build_.Data.IsAllCompress, GUILayout.Width(80f));
                scene.IsNative = GUILayoutHelper.Toggle(scene.IsNative || asset_bundle_build_.Data.IsAllNative, "", asset_bundle_build_.Data.IsAllNative, GUILayout.Width(60f));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 选中的树形节点信息
        /// </summary>
        bool DrawSelectAssetNodeInfo(out emAssetBundleNameRule rule
            , out bool is_compress, out bool is_native, out bool is_permanent, out bool is_startup_load)
        {
            rule = emAssetBundleNameRule.None;
            is_compress = false;
            is_native = false;
            is_permanent = false;
            is_startup_load = false;

            if (gui_multi_select_ == null || gui_multi_select_.SelectNodes.Count == 0)
                return false;

            var asset_node = gui_multi_select_.SelectNodes[0] as AssetNode;
            rule = (emAssetBundleNameRule)asset_node.Element.Rule;
            is_compress = asset_node.Element.IsCompress;
            is_native = asset_node.Element.IsNative;
            is_permanent = asset_node.Element.IsPermanent;
            is_startup_load = asset_node.Element.IsStartupLoad;

            GUILayout.BeginVertical();

            //打包规则
            GUILayout.BeginHorizontal();
            GUILayout.Label("打包规则", GUILayout.Width(50f), GUILayout.MaxHeight(16f));
            emAssetBundleNameRule select_rule = (emAssetBundleNameRule)EditorGUILayout.EnumPopup("", rule, GUILayout.MaxWidth(150f), GUILayout.MaxHeight(16f));
            GUILayout.EndHorizontal();

            var config = AssetBundleBuildWindow.Instance.asset_bundle_build_.Data;
            bool is_compress_op = is_compress;
            bool is_native_op = is_native;
            bool is_permanent_op = is_permanent;
            bool is_startup_load_op = is_startup_load;
            if (select_rule == emAssetBundleNameRule.SingleFile
                || select_rule == emAssetBundleNameRule.Folder)
            {
                is_compress_op = GUILayoutHelper.Toggle(is_compress_op, "压缩", config.IsAllCompress);
                is_native_op = GUILayoutHelper.Toggle(is_native_op, "打包到安装包", config.IsAllNative);
                is_permanent_op = GUILayout.Toggle(is_permanent_op, "常驻内存");
                is_startup_load_op = GUILayout.Toggle(is_startup_load_op, "启动时加载");
            }

            if (asset_node != null && asset_node.GranularityDetails != null)
            {
                GUILayout.Space(20f);
                GUILayout.Label("粒度详细引用信息", "OL Title");
                GUILayout.TextArea(asset_node.GranularityDetails);
            }

            GUILayout.EndVertical();

            if (select_rule != rule
                || is_compress != is_compress_op
                || is_native != is_native_op
                || is_permanent != is_permanent_op
                || is_startup_load != is_startup_load_op)
            {
                rule = select_rule;
                is_compress = is_compress_op;
                is_native = is_native_op;
                is_permanent = is_permanent_op;
                is_startup_load = is_startup_load_op;
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
            LoadData();
            
        }

        /// <summary>
        ///   
        /// </summary>
        void OnEnable()
        {
            if(asset_bundle_build_ == null || gui_multi_select_ == null)
            {
                LoadData();
            }

            Instance = this;
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
            if (GUILayoutHelper.DrawHeader("资源(" + asset_bundle_build_.BuildStartFullPath + ")", "3", true, false))
            {
                DrawAssets();
            }
            if (GUILayoutHelper.DrawHeader("场景(" + EditorCommon.SCENE_START_PATH + ")", "2", true, false))
            {
                DrawScenes();
            }
        }

        [MenuItem("AssetBundle/Windows/AssetBundle Build Window")]
        public static void Open()
        {
            var win = EditorWindow.GetWindow<AssetBundleBuildWindow>("AssetBundle Build");
            if (win != null)
            {
                win.Show();
            }
        }

        /// <summary>
        /// 界面单例
        /// </summary>
        public static AssetBundleBuildWindow Instance = null;
    }
}