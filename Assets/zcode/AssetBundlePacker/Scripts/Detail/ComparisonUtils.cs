using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace zcode.AssetBundlePacker
{
    public class ComparisonUtils
    {
        /// <summary>
        ///   比较本地数据，获得需要修复的资源文件列表
        /// </summary>
        public static void CompareAndCalcRecoverFiles(ref List<string> recover_files
                                    , ResourcesManifest resourcesmanifest)
        {
            if (resourcesmanifest == null)
            {
                return;
            }

            var itr = resourcesmanifest.Data.AssetBundles.GetEnumerator();
            while (itr.MoveNext())
            {
                if (itr.Current.Value.IsNative)
                {
                    string name = itr.Current.Value.AssetBundleName;
                    string full_name = Common.GetFileFullName(name);
                    if (!File.Exists(full_name))
                    {
                        recover_files.Add(name);
                    }
                }
            }
        }

        /// <summary>
        /// 比较方式
        /// </summary>
        public enum emCompareMode
        {
            OnlyInitial,        ///< 仅与安装包比较（用于重装更新）
            All,                ///< 全部比较（用于更新器更新）
        }

        /// <summary>
        ///   比较AssetBundle差异，获得增量更新列表与删除列表
        /// </summary>
        public static void CompareAndCalcDifferenceFiles(ref List<string> download_files
                                                , ref List<string> delete_files
                                                , AssetBundleManifest old_manifest
                                                , AssetBundleManifest new_manifest
                                                , ResourcesManifest old_resourcesmanifest
                                                , ResourcesManifest new_resourcesmanifest
                                                , emCompareMode compareMode)
        {
            if(download_files == null)
            {
                return;
            }
            download_files.Clear();

            if (delete_files == null)
            {
                return;
            }
            delete_files.Clear();

            if (old_manifest == null)
            {
                return;
            }
            if (new_manifest == null)
            {
                return;
            }
            if (new_resourcesmanifest == null)
            {
                return;
            }

            //采用位标记的方式判断资源
            //位标记： 0： 存在旧资源中 1： 存在新资源中 2：旧的本地资源 3：新的本地资源
            int old_version_bit = 0x1;                      // 存在旧版本中
            int new_version_bit = 0x2;                      // 存在新版本中
            int old_version_native_bit = 0x4;               // 旧的本地资源
            int new_version_native_bit = 0x8;               // 新的本地资源
            Dictionary<string, int> temp_dic = new Dictionary<string, int>();
            //标记旧资源
            string[] all_assetbundle = old_manifest.GetAllAssetBundles();
            for (int i = 0; i < all_assetbundle.Length; ++i)
            {
                string name = all_assetbundle[i];
                _SetDictionaryBit(ref temp_dic, name, old_version_bit);
            }
            //标记新资源
            string[] new_all_assetbundle = new_manifest.GetAllAssetBundles();
            for (int i = 0; i < new_all_assetbundle.Length; ++i)
            {
                string name = new_all_assetbundle[i];
                _SetDictionaryBit(ref temp_dic, name, new_version_bit);
            }

            //标记旧的本地资源
            if (old_resourcesmanifest.Data != null && old_resourcesmanifest.Data.AssetBundles != null)
            {
                var resource_manifest_itr = old_resourcesmanifest.Data.AssetBundles.GetEnumerator();
                while (resource_manifest_itr.MoveNext())
                {
                    if (resource_manifest_itr.Current.Value.IsNative)
                    {
                        string name = resource_manifest_itr.Current.Value.AssetBundleName;
                        string full_name = Common.GetFileFullName(Common.RESOURCES_MANIFEST_FILE_NAME);
                        if(File.Exists(full_name))
                        {
                            _SetDictionaryBit(ref temp_dic, name, old_version_native_bit);
                        }
                    }
                }
            }

            //标记新的本地资源
            if (new_resourcesmanifest.Data != null && new_resourcesmanifest.Data.AssetBundles != null)
            {
                var resource_manifest_itr = new_resourcesmanifest.Data.AssetBundles.GetEnumerator();
                while (resource_manifest_itr.MoveNext())
                {
                    if (resource_manifest_itr.Current.Value.IsNative)
                    {
                        string name = resource_manifest_itr.Current.Value.AssetBundleName;
                        _SetDictionaryBit(ref temp_dic, name, new_version_native_bit);
                    }
                }
            }

            //获得对应需操作的文件名， 优先级： both > add > delete
            int both_bit = old_version_bit | new_version_bit;        // 二个版本资源都存在
            List<string> add_files = new List<string>();
            List<string> both_files = new List<string>();
            var itr = temp_dic.GetEnumerator();
            while (itr.MoveNext())
            {
                string name = itr.Current.Key;
                int mask = itr.Current.Value;

                //add: 第2位未标记，且第3位被标记的
                if ((mask & new_version_native_bit) == new_version_native_bit
                    && (mask & old_version_native_bit) == 0)
                {
                    add_files.Add(name);
                }
                //both: 第0位与第1位都被标记的
                else if ((mask & both_bit) == both_bit)
                {
                    // 如果为emCompareMode.OnlyInitial比较方式，则需确定是否为本地资源
                    if (compareMode == emCompareMode.OnlyInitial)
                    {
                        if((mask & new_version_native_bit) == new_version_native_bit)
                        {
                            both_files.Add(name);
                        }
                    }
                    else
                    {
                        both_files.Add(name);
                    }
                }
                //delete: 第0位被标记的
                else if ((mask & old_version_bit) == old_version_bit)
                {
                    // 且必须本地存在
                    string full_name = Common.GetFileFullName(name);
                    if (File.Exists(full_name))
                    {
                        delete_files.Add(name);
                    }
                }
            }
            itr.Dispose();

            //记录需下载的文件
            {
                //加入新增的文件
                download_files.AddRange(add_files);
                //比较所有同时存在的文件，判断哪些需要更新
                for (int i = 0; i < both_files.Count; ++i)
                {
                    string name = both_files[i];
                    string full_name = Common.GetFileFullName(name);
                    if (File.Exists(full_name))
                    {
                        //判断哈希值是否相等
                        string old_hash = old_manifest.GetAssetBundleHash(name).ToString();
                        string new_hash = new_manifest.GetAssetBundleHash(name).ToString();
                        if (old_hash.CompareTo(new_hash) == 0)
                            continue;

                        download_files.Add(name);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void _SetDictionaryBit(ref Dictionary<string, int> dic, string name, int bit)
        {
            if (!dic.ContainsKey(name))
            {
                dic.Add(name, bit);
            }
            else
            {
                dic[name] |= bit;
            }
        }
    }
}