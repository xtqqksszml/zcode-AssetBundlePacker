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
        internal SceneLoadRequest(AssetBundleManager.SceneAsyncLoader loader)
        {
            loader_ = loader;
        }

        ~SceneLoadRequest()
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
        /// 场景名称
        /// </summary>
        public string SceneName
        {
            get { return loader_.SceneName; }
        }
    }
}
