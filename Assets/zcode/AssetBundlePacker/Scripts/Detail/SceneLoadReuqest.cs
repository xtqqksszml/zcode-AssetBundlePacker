/***************************************************************
* Author: Zhang Minglin
* Note  : 场景异步加载请求
***************************************************************/
using UnityEngine;

namespace zcode.AssetBundlePacker
{
    public sealed class SceneLoadRequest : YieldInstruction
    {
        AssetBundleManager.SceneAsyncLoader loader_;
        AsyncOperation ao_;

        internal SceneLoadRequest(AssetBundleManager.SceneAsyncLoader loader)
        {
            loader_ = loader;
        }

        internal SceneLoadRequest(AsyncOperation ao)
        {
            ao_ = ao;
        }

        ~SceneLoadRequest()
        {

        }

        /// <summary>
        /// 是否加载完成? (Read Only)
        /// </summary>
        public bool IsDone
        {
            get { return loader_ != null ? loader_.IsDone : (ao_ != null ? ao_.isDone : false); }
        }

        /// <summary>
        /// 加载进度. (Read Only)
        /// </summary>
        public float Progress
        {
            get { return loader_ != null ? loader_.Progress : (ao_ != null ? ao_.progress : 0); }
        }
    }
}
