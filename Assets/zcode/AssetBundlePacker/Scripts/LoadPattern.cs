/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/12/09
 * Note  : 加载模式
***************************************************************/
using UnityEngine;
using System.Collections;

namespace zcode.AssetBundlePacker
{
    /// <summary>
    ///   加载方式
    /// </summary>
    public enum emLoadPattern
    {
        AssetBundle,        // 以AssetBundle的方式加载
        Original,           // 以Resoureces目录下的资源加载
        All,                // 先尝试以AssetBundle的方式加载， 失败再使用Resoureces目录下的资源加载
    }

    public interface ILoadPattern
    {
        /// <summary>
        /// 资源加载方式
        /// </summary>
        emLoadPattern ResourcesLoadPattern { get;}

        /// <summary>
        /// 场景加载方式
        /// </summary>
        emLoadPattern SceneLoadPattern { get;}
    }

    /// <summary>
    /// 默认的加载模式
    /// </summary>
    public class DefaultLoadPattern : ILoadPattern
    {

        /// <summary>
        /// 资源加载方式
        /// </summary>
        public emLoadPattern ResourcesLoadPattern
        {
            get
            {
#if UNITY_EDITOR
                return emLoadPattern.All;
#elif UNITY_STANDALONE_WIN
            return emLoadPattern.All;
#elif UNITY_IPHONE
            return emLoadPattern.All;
#elif UNITY_ANDROID
            return emLoadPattern.All;
#endif
            }
        }

        /// <summary>
        /// 场景加载方式
        /// </summary>
        public emLoadPattern SceneLoadPattern
        {
            get
            {
#if UNITY_EDITOR
                return emLoadPattern.All;
#elif UNITY_STANDALONE_WIN
            return emLoadPattern.All;
#elif UNITY_IPHONE
            return emLoadPattern.All;
#elif UNITY_ANDROID
            return emLoadPattern.All;
#endif
            }
        }
    }

    /// <summary>
    /// 仅加载AssetBundle
    /// </summary>
    public class AssetBundleLoadPattern : ILoadPattern
    {

        /// <summary>
        /// 资源加载方式
        /// </summary>
        public emLoadPattern ResourcesLoadPattern
        {
            get
            {
#if UNITY_EDITOR
                return emLoadPattern.AssetBundle;
#elif UNITY_STANDALONE_WIN
            return emLoadPattern.AssetBundle;
#elif UNITY_IPHONE
            return emLoadPattern.AssetBundle;
#elif UNITY_ANDROID
            return emLoadPattern.AssetBundle;
#endif
            }
        }

        /// <summary>
        /// 场景加载方式
        /// </summary>
        public emLoadPattern SceneLoadPattern
        {
            get
            {
#if UNITY_EDITOR
                return emLoadPattern.AssetBundle;
#elif UNITY_STANDALONE_WIN
            return emLoadPattern.AssetBundle;
#elif UNITY_IPHONE
            return emLoadPattern.AssetBundle;
#elif UNITY_ANDROID
            return emLoadPattern.AssetBundle;
#endif
            }
        }
    }

    /// <summary>
    /// 仅加载Resources目录下原始资源
    /// </summary>
    public class OriginalResourcesLoadPattern : ILoadPattern
    {

        /// <summary>
        /// 资源加载方式
        /// </summary>
        public emLoadPattern ResourcesLoadPattern
        {
            get
            {
#if UNITY_EDITOR
                return emLoadPattern.Original;
#elif UNITY_STANDALONE_WIN
            return emLoadPattern.Original;
#elif UNITY_IPHONE
            return emLoadPattern.Original;
#elif UNITY_ANDROID
            return emLoadPattern.Original;
#endif
            }
        }

        /// <summary>
        /// 场景加载方式
        /// </summary>
        public emLoadPattern SceneLoadPattern
        {
            get
            {
#if UNITY_EDITOR
                return emLoadPattern.AssetBundle;
#elif UNITY_STANDALONE_WIN
            return emLoadPattern.Original;
#elif UNITY_IPHONE
            return emLoadPattern.Original;
#elif UNITY_ANDROID
            return emLoadPattern.Original;
#endif
            }
        }
    }
}
