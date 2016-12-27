# zcode-AssetBundlePacker
Unity的AssetBundle模块扩展插件，主要目的用于简化AssetBundle打包，提供AssetBundle管理，支持热更新、支持资源包下载等。

## 主要功能
* 便捷的打包环境，独立的打包编辑窗口
* 多种资源打包方式指定，可视化资源粒度显示，可方便查看资源被AssetBundle打包次数，便于优化。
* 支持场景打包，可支持场景对象动态加载，减小场景大小。
* AssetBundle压缩功能，支持外部AssetBundle的压缩功能，从而可以取消AssetBundle本身压缩提高AssetBundle加载效率，又可以通过外部压缩减小网络传输时AssetBundle的大小。
* 版本管理功能，支持AssetBundle热更新。
* 资源包功能，并提供PackageDownloader类用于游戏中下载资源包匹配的AssetBundle文件，实现块化资源利用。
* 提供资源加载器与场景加载器，可提供多种加载配置（AssetBundle、Resources、AssetBundleAndResources）。

## 第三方库
	Json:  https://github.com/xtqqksszml/simple-json
	7-Zip: http://7-zip.org/
	
## 目录结构
	>Assets
		|-AssetBundlePacker-Examples		- 例子（可删除）
		|-Plugins							- 引用的第三方插件
		|-ThridParty						- 引用的第三方库
		|-zcode								- 核心目录
			|-AssetBoundlePacker			- AssetBoundlePacker插件源码目录
			|-Core							- 公共类、函数等源代码
	
## 如何打包AssetBundle
	AssetBundle打包方法可通过Unity编辑器下打开"AssetBundle/Instructions"菜单项打开Instructions窗口，里面包含详细的打包说明与注意事项。
	
## 如何使用
	直接使用Unity打开，Assets/AssetBundlePacker-Examples目录下包含多个例子，展示了AssetBundlePacker的主要功能与用法。
	例子包含
		启动
		资源加载、场景加载
		更新器的用法
		包下载器的用法
		
## 版权声明
	插件使用 Apache License 2.0 协议.
