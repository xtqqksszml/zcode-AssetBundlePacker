/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/03/14
 * Note  : 资源更新器下载缓存数据，用于断点续传
***************************************************************/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace zcode.AssetBundlePacker
{
    /// <summary>
    /// 下载缓存
    /// </summary>
    public class DownloadCacheData
    {
        /// <summary>
        ///   AssetBundle缓存
        /// </summary>
        public class AssetBundle
        {
            public string AssetBundleName;                      // AssetBundleName
            public string Hash;                                 // Hash值
        }

        public Dictionary<string, AssetBundle> AssetBundles = new Dictionary<string, AssetBundle>();
    }

    public class DownloadCache
    {
        /// <summary>
        /// 
        /// </summary>r
        public DownloadCacheData Data;

        /// <summary>
        ///   
        /// </summary>
        public DownloadCache()
        {
            Data = new DownloadCacheData();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Load(string file_name)
        {
            return SimpleJsonReader.ReadFromFile<DownloadCacheData>(ref Data, file_name);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Save(string file_name)
        {
            return SimpleJsonWriter.WriteToFile(Data, file_name);
        }

        /// <summary>
        ///   是否拥有数据
        /// </summary>
        public bool HasData()
        {
            return Data.AssetBundles.Count > 0;
        }

        /// <summary>
        ///   判断一个资源是否存在
        /// </summary>
        public bool IsExist(string assetbundle)
        {
            return Data.AssetBundles.ContainsKey(assetbundle);
        }

        /// <summary>
        ///   获得一个资源的哈希值
        /// </summary>
        public string GetHash(string assetbundle)
        {
            DownloadCacheData.AssetBundle elem;
            if (Data.AssetBundles.TryGetValue(assetbundle, out elem))
            {
                return elem.Hash;
            }

            return null;
        }
    }
}