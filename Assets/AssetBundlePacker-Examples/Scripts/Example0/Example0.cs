/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/12/19
 * Note  : 例子 - 展示插件启动、使用等功能
***************************************************************/
using UnityEngine;
using System.Collections;
using System.IO;
using zcode.AssetBundlePacker;
using UnityEngine.SceneManagement;

public class Example0 : MonoBehaviour 
{
    /// <summary>
    /// 缓存目录
    /// </summary>
    public const string PATH = "Assets/AssetBundlePacker-Examples/Examples/Cache/Version_1/AssetBundle";
    
    /// <summary>
    /// 
    /// </summary>
    private int current_stage_ = 0;

    /// <summary>
    /// 
    /// </summary>
    private System.Action[] stage_funcs_;

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        stage_funcs_ = new System.Action[6];
        stage_funcs_[0] = OnGUI_PreparationWork;
        stage_funcs_[1] = OnGUI_Example;
        stage_funcs_[2] = OnGUI_LoadTextFile;
        stage_funcs_[3] = OnGUI_LoadTexture;
        stage_funcs_[4] = OnGUI_LoadModel;
        stage_funcs_[5] = OnGUI_LoadScene;
    }

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        //此层次下的所有对象禁止被删除
        DontDestroyOnLoad(transform.gameObject);

        var array = GameObject.FindObjectsOfType<Example0>();
        if (array.Length > 1)
        {
            GameObject.Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnGUI()
    {
        if (stage_funcs_[current_stage_] != null)
            stage_funcs_[current_stage_]();
    }

    #region PreparationWork
    const string NOTE =
        "1. 启动例子会从缓存目录中拷贝例子数据至StreamingAssets目录，拷贝执行前会先清空StreamingAssets目录，请注意先转移此目录下的数据。\n"
      + "2. 例子所有使用的AssetBundle原资源放置于\"Assets/AssetBundlePacker-Examples/Cache/Resources\"";
    /// <summary>
    /// 准备工作
    /// </summary>
    void OnGUI_PreparationWork()
    {
        if (GUI.Button(new Rect(0f, 0f, Screen.width, 40f), "从缓存目录中拷贝例子数据，并启动例子"))
        {
            StartExample();
        }

        GUI.color = Color.yellow;
        GUI.Label(new Rect(Screen.width / 2 - 100f, 60f, 100f, 20f), "注意");
        GUI.Label(new Rect(0f, 80f, Screen.width, Screen.height - 80f), NOTE);
    }

    /// <summary>
    /// 启动
    /// </summary>
    void StartExample()
    {
        if (Directory.Exists(zcode.AssetBundlePacker.Common.INITIAL_PATH))
            Directory.Delete(zcode.AssetBundlePacker.Common.INITIAL_PATH, true);
        //拷贝例子资源
        zcode.FileHelper.CopyDirectoryAllChildren(PATH, zcode.AssetBundlePacker.Common.INITIAL_PATH);
        //设定资源加载模式为仅加载AssetBundle资源
        ResourcesManager.LoadPattern = new AssetBundleLoadPattern();
        //设定场景加载模式为仅加载AssetBundle资源
        SceneResourcesManager.LoadPattern = new AssetBundleLoadPattern();
        
        //切换到示例GUI阶段
        current_stage_ = 1;
    }
    #endregion

    #region Example
    /// <summary>
    /// 
    /// </summary>
    void OnGUI_Example()
    {
        //AssetBundleManager全局单例实例化后会自动启动，此处等待其启动完毕
        if(!AssetBundleManager.Instance.WaitForLaunch())
        {
            GUI.Label(new Rect(0f, 0f, 200f, 20f), "AssetBundlePacker is launching！");
            return;
        }

       
        if(AssetBundleManager.Instance.IsReady)
        { 
            //启动成功
            GUI.Label(new Rect(0f, 0f, Screen.width, 20f), "AssetBundlePacker launch succeed, Version is" + AssetBundleManager.Instance.Version);

            bool load_text = GUI.Button(new Rect(0f, 30f, 300f, 30f), "例子 - 加载文本资源");
            bool load_tex = GUI.Button(new Rect(0f, 60f, 300f, 30f), "例子 - 加载纹理资源");
            bool load_model = GUI.Button(new Rect(0f, 90f, 300f, 30f), "例子 - 加载模型资源");
            bool load_scene = GUI.Button(new Rect(0f, 120f, 300f, 30f), "例子 - 加载场景资源");
            if (load_text)
                current_stage_ = 2;
            if (load_tex)
                current_stage_ = 3;
            if (load_model)
                current_stage_ = 4;
            if (load_scene)
                current_stage_ = 5;
        }
        else if(AssetBundleManager.Instance.IsFailed)
        {
            //启动失败
            GUI.color = Color.red;
            GUI.Label(new Rect(0f, 0f, 200f, 20f), "AssetBundlePacker launch occur error!");
        }
    }
    #endregion

    #region Load Text File
    const string TEXT_FILE = "Assets/Resources/Version_1/Text/Text.txt";
    string text_content_ = null;
    /// <summary>
    /// 
    /// </summary>
    void LoadTextFile()
    {
        TextAsset text_asset = ResourcesManager.Load<TextAsset>(TEXT_FILE);
        if (text_asset != null)
        {
            text_content_ = text_asset.text;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnGUI_LoadTextFile()
    {
        if (GUI.Button(new Rect(0f, 0f, Screen.width, 30f), "加载并显示文本资源(" + TEXT_FILE + ")"))
        {
            LoadTextFile();
        }
        if (GUI.Button(new Rect(0f, 30f, Screen.width, 30f), "返回"))
        {
            current_stage_ = 1;
            text_content_ = null;
        }

        if (!string.IsNullOrEmpty(text_content_))
        {
            GUI.Label(new Rect(0f, 100f, Screen.width, 60f), text_content_);
        }
    }
    #endregion

    #region Load Texture
    const string TEXTURE_FILE = "Assets/Resources/Version_1/Texture/Tex_1.png";
    Texture2D texture_ = null;
    /// <summary>
    /// 
    /// </summary>
    void LoadTexture()
    {
        texture_ = ResourcesManager.Load<Texture2D>(TEXTURE_FILE);
    }
    /// <summary>
    /// 
    /// </summary>
    void OnGUI_LoadTexture()
    {
        if (GUI.Button(new Rect(0f, 0f, Screen.width, 30f), "加载并显示纹理资源(" + TEXTURE_FILE + ")"))
        {
            LoadTexture();
        }
        if (GUI.Button(new Rect(0f, 30f, Screen.width, 30f), "返回"))
        {
            current_stage_ = 1;
            texture_ = null;
        }

        if (texture_ != null)
        {
            GUI.DrawTexture(new Rect(0f, 100f, Screen.width, 60f), texture_);
        }
    }
    #endregion

    #region Load Model
    const string MODEL_FILE = "Assets/Resources/Version_1/Models/Sphere/Sphere.Prefab";
    GameObject model_;
    /// <summary>
    /// 
    /// </summary>
    void LoadModel()
    {
        GameObject prefab = ResourcesManager.Load<GameObject>(MODEL_FILE);
        if(prefab != null)
        {
            model_ = GameObject.Instantiate(prefab);
            model_.transform.position = Vector3.zero;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    void OnGUI_LoadModel()
    {
        if (GUI.Button(new Rect(0f, 0f, Screen.width, 30f), "加载并显示模型资源(" + MODEL_FILE + ")"))
        {
            LoadModel();
        }
        if (GUI.Button(new Rect(0f, 30f, Screen.width, 30f), "返回"))
        {
            current_stage_ = 1;
            if(model_ != null)
                GameObject.Destroy(model_);
        }        
    }
    #endregion

    #region Load Scene
    const string SCENE_FILE = "SimpleScene";
    string original_scene;
    /// <summary>
    /// 
    /// </summary>
    void LoadScene()
    {
        original_scene = SceneManager.GetActiveScene().name;
        SceneResourcesManager.LoadSceneAsync(SCENE_FILE);
    }
    /// <summary>
    /// 
    /// </summary>
    void OnGUI_LoadScene()
    {
        if (GUI.Button(new Rect(0f, 0f, Screen.width, 30f), "加载并切换场景(" + SCENE_FILE + ")"))
        {
            LoadScene();
        }
        if (GUI.Button(new Rect(0f, 30f, Screen.width, 30f), "返回"))
        {
            current_stage_ = 1;
            SceneManager.LoadScene(original_scene);
        }
    }
    #endregion
}
