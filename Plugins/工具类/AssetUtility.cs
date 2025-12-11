using GameFramework;
using System.Diagnostics;

namespace WeiDaEdu
{
    public partial class AssetUtility
    {
        /// <summary>
        /// 获取配置表路径
        /// </summary>
        /// <param name="assetName">配置表名称</param>
        /// <param name="fromBytes">是否使用二进制文件</param>
        /// <returns></returns>
        public static string GetConfigAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format("Assets/GameMain/Configs/{0}.{1}", assetName, fromBytes ? "bytes" : "txt");
        }
        /// <summary>
        /// 获取数据表路径
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="fromBytes"></param>
        /// <returns></returns>
        public static string GetDataTableAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format("Assets/GameMain/DataTables/{0}.{1}", assetName, fromBytes ? "bytes" : "txt");
        }

        /// <summary>
        /// 获取字典路径 
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="fromBytes"></param>
        /// <returns></returns>
        public static string GetDictionaryAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Localization/Dictionaries/{0}.xml", assetName);
        }

        /// <summary>
        /// 获取场景路径
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetSceneAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Scenes/{0}.unity", assetName);
        }

        /// <summary>
        /// 获取UI预制体路径
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetUIFormAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/UI/UIForms/{0}.prefab", assetName);
        }

        /// <summary>
        /// 获取背景音乐路径
        /// </summary>
        /// <param name="asssetName"></param>
        /// <returns></returns>
        public static string GetMusicAsset(string asssetName)
        {
            return Utility.Text.Format("Assets/GameMain/Music/{0}.mp3", asssetName);
        }
        /// <summary>
        /// 获取背景音乐路径
        /// </summary>
        /// <param name="asssetName"></param>
        /// <returns></returns>
        public static string GetZSMusicAsset(string asssetName)
        {
            return Utility.Text.Format("Assets/GameMain/Music/ZSSound/{0}.mp3", asssetName);
        }
        /// <summary>
        /// 获取UI声音路径
        /// </summary>
        /// <param name="asssetName"></param>
        /// <returns></returns>
        public static string GetUISoundAsset(string asssetName)
        {
            return Utility.Text.Format("Assets/GameMain/UI/UISounds/{0}.wav", asssetName);
        }

        /// <summary>
        /// 获取项目图纸路径
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetDrawingAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Textures/UI/Drawing/{0}.png", assetName);
        }

        public static string GetRenderAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Textures/RenderTexture/{0}.renderTexture", assetName);
        }


        public static string GetDragToolsAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Textures/UI/Tools/{0}.png", assetName);
        }

        public static string GetHistoryTextureAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Textures/ZH/History/{0}.png", assetName);
        }

        public static string GetDesignInterpretationAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Textures/设计解读/Furniture/{0}.png", assetName);
        }

        public static string GetHistoryTimelineAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Animation/ZH_PC/HistoryTimeline/{0}.playable", assetName);
        }
        
        public static string GetHistoryFurnitureOperate(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Prefabs/JiaJuShi/{0}.prefab", assetName);
        }

        /// <summary>
        /// 获取资源库道具图片
        /// </summary>
        /// <param name="asssetName"></param>
        /// <returns></returns>
        public static string GetToolItemTextureAsset(string asssetName)
        {
            return Utility.Text.Format("Assets/GameMain/Textures/XuNiSheJi/ItemImage/{0}.png", asssetName);
        }
        /// <summary>
        /// 获取资源库道具模型
        /// </summary>
        /// <param name="asssetName"></param>
        /// <returns></returns>
        public static string GetToolItemAsset(string toolType, string asssetName)
        {
            return Utility.Text.Format("Assets/GameMain/Model/XuNiSheJi/" + toolType + "/Entities/{0}.prefab", asssetName);
        }
        /// <summary>
        /// 获取材质球
        /// </summary>
        /// <param name="asssetName"></param>
        /// <returns></returns>
        public static string GetModelMatAsset(string asssetName)
        {
            return Utility.Text.Format("Assets/GameMain/Model/XuNiSheJi/Sphere.prefab", asssetName);
        }
        /// <summary>
        /// 获取材质球资源
        /// </summary>
        public static string GetMaterialsAsset(string asssetName)
        {
            return Utility.Text.Format("Assets/GameMain/Material/材质库/{0}.mat", asssetName);
        }
        /// <summary>
        /// 获取材质球图片
        /// </summary>
        /// <param name="asssetName"></param>
        /// <returns></returns>
        public static string GetMatTextureAsset(string asssetName)
        {
            return Utility.Text.Format("Assets/GameMain/Textures/XuNiSheJi/MaterialImage/{0}.png", asssetName);
        }

        public static string GetVideoAsset(string videoName)
        {
            return Utility.Text.Format("Assets/GameMain/Video/{0}.mp4", videoName);
        }

    }

}
