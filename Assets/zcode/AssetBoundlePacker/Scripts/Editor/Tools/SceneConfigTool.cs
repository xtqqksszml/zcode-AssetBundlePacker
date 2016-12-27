/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/05/05
 * Note  : 场景配置工具
***************************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace zcode.AssetBundlePacker
{

    public class SceneConfigTool : MonoBehaviour
    {
        /// <summary>
        ///   需序列化且加载场景时才重新加载的GameObject的Tag
        /// </summary>
        public const string SERIALIZE_SCENE_OBJECT_TAG = "FixedSceneObject";

        /// <summary>
        ///   生成所有场景配置文件
        /// </summary>
        public static bool GenerateAllSceneConfig(AssetBundleBuildData.SceneBuild scene_rules)
        {
            RecordDefaultOpenScene();

            bool cancel = false;
            float total = (float)scene_rules.Scenes.Count;
            float current = 0;

            for (int i = 0; i < scene_rules.Scenes.Count; ++i)
            {
                var scene = scene_rules.Scenes[i].ScenePath;
                if (scene_rules.Scenes[i].IsBuild)
                {
                    CopySceneToBackup(scene);
                    GenerateSceneConfig(scene);
                    AssetBundleNameTool.SetAssetBundleName(SceneConfig.GetSceneConfigPath(scene));
                    AssetBundleNameTool.SetAssetBundleName(scene);
                }
                else
                {
                    DeleteSceneConfig(scene);
                    AssetBundleNameTool.ClearAssetBundleName(scene);
                }


                current += 1.0f;
                float progress = current / total;
                if (EditorUtility.DisplayCancelableProgressBar("正在生成场景配置数据", "Change " + scene, progress))
                {
                    cancel = true;
                    break;
                }
            }

            EditorUtility.ClearProgressBar();

            return !cancel;
        }

        /// <summary>
        ///   恢复所有场景
        /// </summary>
        public static void RestoreAllScene(AssetBundleBuildData.SceneBuild scene_rules)
        {
            for (int i = 0; i < scene_rules.Scenes.Count; ++i)
            {
                if (scene_rules.Scenes[i].IsBuild)
                    RestoreSceneFromBackup(scene_rules.Scenes[i].ScenePath);
            }

            ReturnDefaultOpenScene();
        }

        /// <summary>
        ///   配置场景信息
        /// </summary>
        public static void GenerateSceneConfig(string scene_path)
        {
            var scene = EditorSceneManager.OpenScene(scene_path);
            SaveAll();
            RemoveAll();
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        ///   删除配置文件
        /// </summary>
        public static void DeleteSceneConfig(string scene_path)
        {
            var file_name = SceneConfig.GetSceneConfigPath(scene_path);
            if (File.Exists(file_name))
                File.Delete(file_name);
        }

        /// <summary>
        ///   默认打开场景
        /// </summary>
        static string default_open_scene_;

        /// <summary>
        ///   记录默认打开场景
        /// </summary>
        static void RecordDefaultOpenScene()
        {
            Scene sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            default_open_scene_ = sc.path ?? null;
        }

        /// <summary>
        ///   恢复默认打开场景
        /// </summary>
        static void ReturnDefaultOpenScene()
        {
            if (string.IsNullOrEmpty(default_open_scene_))
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            }
            else
            {
                EditorSceneManager.OpenScene(default_open_scene_);
            }
        }

        static void SaveAll()
        {
            UnityEngine.SceneManagement.Scene sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!sc.IsValid())
                return;

            SceneConfig data = new SceneConfig();
            data.Data.LevelName = sc.name;

            GameObject[] array = GameObject.FindGameObjectsWithTag(SERIALIZE_SCENE_OBJECT_TAG);
            if (array == null)
                return;

            for (int i = 0; i < array.Length; ++i)
            {
                UnityEngine.Object parentObject = PrefabUtility.GetPrefabParent(array[i]);
                string path = AssetDatabase.GetAssetPath(parentObject);
                if (string.IsNullOrEmpty(path))
                    continue;

                var transform = array[i].transform;
                var parent = transform.parent;

                //写入数据
                var obj = new SceneConfigData.SceneObject();
                obj.AssetName = path;
                obj.Position = transform.position;
                obj.Scale = transform.lossyScale;
                obj.Rotation = transform.rotation;
                obj.ParentName = parent != null ?
                                    Common.CalcTransformHierarchyPath(parent) : "";
                data.Data.SceneObjects.Add(obj);
            }

            data.Save(SceneConfig.GetSceneConfigPath(sc.path));
            AssetDatabase.Refresh();
        }

        static void RemoveAll()
        {
            UnityEngine.SceneManagement.Scene sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(!sc.IsValid())
                return;
            
            GameObject[] array = GameObject.FindGameObjectsWithTag(SERIALIZE_SCENE_OBJECT_TAG);
            if (array == null)
                return;

            for (int i = 0; i < array.Length; ++i)
            {
                GameObject.DestroyImmediate(array[i]);
            }
        }

        /// <summary>
        ///   临时存放场景目录
        /// </summary>
        static readonly string TEMP_PATH = Application.temporaryCachePath;

        /// <summary>
        ///   备份场景
        /// </summary>
        static void CopySceneToBackup(string scene_path)
        {
            if (File.Exists(scene_path))
            {
                var dest = TEMP_PATH + "/" + Path.GetFileName(scene_path);
                File.Copy(scene_path, dest, true);
            }
        }

        /// <summary>
        ///   从备份中恢复场景
        /// </summary>
        static void RestoreSceneFromBackup(string scene_path)
        {
            var src = TEMP_PATH + "/" + Path.GetFileName(scene_path);
            if (File.Exists(src))
            {
                File.Copy(src, scene_path, true);
                File.Delete(src);
            }
        }
    }
}