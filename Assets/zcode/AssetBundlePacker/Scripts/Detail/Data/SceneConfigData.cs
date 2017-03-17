/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/04/15
 * Note  : 场景配置数据
***************************************************************/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace zcode.AssetBundlePacker
{
    /// <summary>
    /// 场景加载配置
    /// </summary>
    public class SceneConfigData
    {
        /// <summary>
        ///   Vector3
        /// </summary>
        public class Vector3
        {
            public float x;
            public float y;
            public float z;

            public static implicit operator UnityEngine.Vector3(Vector3 v)
            {
                return new UnityEngine.Vector3(v.x, v.y, v.z);
            }
            public static implicit operator Vector3(UnityEngine.Vector3 v)
            {
                return new Vector3() { x = v.x, y = v.y, z = v.z };
            }
        }

        /// <summary>
        ///   Quaternion
        /// </summary>
        public class Quaternion
        {
            public float x;
            public float y;
            public float z;
            public float w;

            public static implicit operator UnityEngine.Quaternion(Quaternion q)
            {
                return new UnityEngine.Quaternion(q.x, q.y, q.z, q.w);
            }
            public static implicit operator Quaternion(UnityEngine.Quaternion q)
            {
                return new Quaternion() { x = q.x, y = q.y, z = q.z, w = q.w };
            }
        }

        /// <summary>
        ///   场景对象
        /// </summary>
        public class SceneObject
        {
            public string AssetName;
            public Vector3 Position;
            public Vector3 Scale;
            public Quaternion Rotation;
            public string ParentName;
        }

        public string LevelName;                                            // 场景Level名称（用于Application.LoadLevel()加载）
        public List<SceneObject> SceneObjects = new List<SceneObject>();    // 场景对象信息
    }

    public class SceneConfig
    {
        /// <summary>
        ///   扩展名
        /// </summary>
        public const string EXTENSION_NAME = ".txt";

        /// <summary>
        ///   场景数据
        /// </summary>
        public SceneConfigData Data;

        /// <summary>
        ///   
        /// </summary>
        public SceneConfig()
        {
            Data = new SceneConfigData();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Load(string file_name)
        {
            if (SimpleJsonReader.ReadFromFile<SceneConfigData>(ref Data, file_name))
                return true;
            
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool LoadFromString(string str)
        {
            if (SimpleJsonReader.ReadFromString<SceneConfigData>(ref Data, str))
                return true;
            
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Save(string file_name)
        {
            return SimpleJsonWriter.WriteToFile(Data, file_name);
        }
       
        /// <summary>
        ///   获得场景配置文件名
        /// </summary>
        public static string GetSceneConfigPath(string scene_path)
        {
            return scene_path + "Config" + EXTENSION_NAME;
        }
    }

}