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
	private static T instance_ = null;
	public static T Instance
	{
		get
		{
			if (instance_ == null)
			{
				instance_ = GameObject.FindObjectOfType(typeof(T)) as T;
				if (instance_ == null)
				{
					instance_ = new GameObject("SingletonOf" + typeof(T).ToString(), typeof(T)).GetComponent<T>();
					DontDestroyOnLoad(instance_);
				}
			}
			return instance_;
		}
	}

	//确保在程序退出时销毁实例。
	protected void OnApplicationQuit()
	{
		instance_ = null;
	}
}