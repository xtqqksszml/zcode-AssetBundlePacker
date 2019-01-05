/***************************************************************
* Author: Zhang Minglin
* Note  : 资源异步加载请求
***************************************************************/
using UnityEngine;

namespace zcode.AssetBundlePacker
{
    public sealed class AssetLoadRequest : YieldInstruction
    {
        AssetBundleManager.AssetAsyncLoader loader_;
        internal AssetLoadRequest(AssetBundleManager.AssetAsyncLoader loader)
        {
            loader_ = loader;
        }

        ~AssetLoadRequest()
        {

        }

        /// <summary>
        /// 是否加载完成? (Read Only)
        /// </summary>
        public bool IsDone
        {
            get { return loader_.IsDone; }
        }

        /// <summary>
        /// 加载进度. (Read Only)
        /// </summary>
        public float Progress
        {
            get { return loader_.Progress; }
        }

        /// <summary>
        /// 已加载的资源 (Read Only).
        /// </summary>
        public Object Asset
        {
            get { return loader_.Asset; }
        }

        /// <summary>
        /// 资源名(Read Only)
        /// </summary>
        public string AssetName
        {
            get { return loader_.AssetName; }
        }

        /// <summary>
        /// 资源原始名称
        /// </summary>
        public string OrignalAssetName
        {
            get { return loader_.OrignalAssetName; }
        }

        /// <summary>
        /// 已加载的子资源 (Read Only)
        /// </summary>
        public Object[] AllAssets
        {
            get { return loader_.AllAssets; }
        }

        /// <summary>
        /// AssetBundle名称
        /// </summary>
        public string AssetBundleName
        {
            get { return loader_.AssetBundleName; }
        }
    }
}