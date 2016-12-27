/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2015/12/17 
 * Note  : 平台相关
***************************************************************/
using UnityEngine;
using System.Collections;

namespace zcode
{
    /// <summary>
    ///   平台相关
    /// </summary>
    public static class Platform
    {
#if UNITY_EDITOR 
        public static string STREAMING_ASSETS_PATH = Application.streamingAssetsPath;
        public static string PERSISTENT_DATA_PATH = Application.dataPath + "/PersistentAssets";
#elif UNITY_STANDALONE_WIN
        public static string STREAMING_ASSETS_PATH = Application.streamingAssetsPath;
        public static string PERSISTENT_DATA_PATH = Application.dataPath + "/PersistentAssets";
#elif UNITY_IPHONE
        public static string STREAMING_ASSETS_PATH = Application.streamingAssetsPath;
        public static string PERSISTENT_DATA_PATH = Application.persistentDataPath;
#elif UNITY_ANDROID
        public static string STREAMING_ASSETS_PATH = Application.streamingAssetsPath;
        public static string PERSISTENT_DATA_PATH = Application.persistentDataPath;
#endif
    }
}
