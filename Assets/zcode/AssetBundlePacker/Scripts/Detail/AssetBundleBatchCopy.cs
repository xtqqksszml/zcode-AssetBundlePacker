/***************************************************************
* Author: Zhang Minglin
* Note  : AssetBundle批量拷贝
***************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace zcode.AssetBundlePacker
{
    public class AssetBundleBatchCopy : MonoBehaviour
    {
        /// <summary>
        /// 最大并行拷贝数量
        /// </summary>
        const int MAX_PARALLEL_COUNT = 30;

        /// <summary>
        /// 是否结束
        /// </summary>
        public bool isDone { get; private set; }

        /// <summary>
        /// 拷贝结果
        /// </summary>
        public emIOOperateCode resultCode { get; private set; }

        /// <summary>
        /// 需要拷贝的数量
        /// </summary>
        public int total { get; private set; }

        /// <summary>
        /// 当前拷贝的数量进度
        /// </summary>
        public int progress { get; private set; }

        /// <summary>
        /// 正在拷贝列表
        /// </summary>
        HashSet<string> copy_list_ = new HashSet<string>();

        /// <summary>
        ///   拷贝所有文件
        /// </summary>
        public IEnumerator StartBatchCopy(List<string> files, System.Action<AssetBundleBatchCopy> callback)
        {
            if (files == null || files.Count == 0)
            {
                yield break;
            }

            SetResult(false, emIOOperateCode.Succeed);
            progress = 0;
            total = files.Count;

            int current = 0;
            while (current < files.Count || copy_list_.Count > 0)
            {
                if(isDone)
                {
                    StopAllCoroutines();
                    break;
                }
                if (copy_list_.Count < MAX_PARALLEL_COUNT && current < files.Count)
                {
                    StartCoroutine(StartCopyInitialFile(files[current++], callback));
                }

                yield return null;
            }

            SetResult(true, resultCode);
        }

        /// <summary>
        ///   从安装目录拷贝文件到本地目录
        /// </summary>
        IEnumerator StartCopyInitialFile(string local_name, System.Action<AssetBundleBatchCopy> callback)
        {
            if (copy_list_.Contains(local_name))
            {
                yield break;
            }

            copy_list_.Add(local_name);
            StreamingAssetsCopy copy = new StreamingAssetsCopy();
            yield return copy.Copy(Common.GetInitialFileFullName(local_name),
                Common.GetFileFullName(local_name));
            copy_list_.Remove(local_name);
            ++progress;

            if (callback != null) { callback(this); }
            if(copy.resultCode != emIOOperateCode.Succeed)
            {
                SetResult(true, copy.resultCode);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void SetResult(bool isDone, emIOOperateCode result)
        {
            this.isDone = isDone;
            this.resultCode = result;
        }

        /// <summary>
        /// 创建
        /// </summary>
        public static AssetBundleBatchCopy Create()
        {
            GameObject go = new GameObject(typeof(AssetBundleBatchCopy).Name, typeof(AssetBundleBatchCopy));
            var copy = go.GetComponent<AssetBundleBatchCopy>();
            return copy;
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public static void Destroy(AssetBundleBatchCopy copy)
        {
            GameObject.Destroy(copy.gameObject);
        }
    }
}