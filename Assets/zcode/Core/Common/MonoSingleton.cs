/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2015/09/24
 * Note  : 继承MonoBehaviour的单例
***************************************************************/
using UnityEngine;
using System.Collections;

/// <summary>
/// 
/// </summary>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    /// <summary>
    ///   单例实例
    /// </summary>
	private static T instance_ = null;
	public static T Instance
	{
		get
		{
			if (instance_ == null)
			{
                instance_ = Object.FindObjectOfType(typeof(T)) as T;
				if (instance_ == null)
                {
                    if(Application.isPlaying)
                    {
                        instance_ = new GameObject("SingletonOf" + typeof(T).ToString(), typeof(T)).GetComponent<T>();
                        DontDestroyOnLoad(instance_);
                    }
                }
			}
			return instance_;
		}
	}

    /// <summary>
    ///  创建单例实例
    /// </summary>
    public static T CreateSingleton()
    {
        return Instance;
    }

	/// <summary>
    ///   确保在程序退出时销毁实例。
	/// </summary>
	protected virtual void OnApplicationQuit()
	{
		instance_ = null;
	}
}