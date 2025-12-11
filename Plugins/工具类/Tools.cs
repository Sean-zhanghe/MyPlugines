using System;
using System.IO;
using System.IO.Compression;
using System.Text;

using UnityEngine;

using UnityGameFramework.Runtime;

namespace WeiDaEdu
{
    public static class Tools
    {
        public static readonly string UserInfoPath = $"{Application.persistentDataPath}/UserInfo.dat";

        /// <summary>
        /// 压缩文本
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string Compress(string target)
        {
            try
            {
                string data = "";
                byte[] byteArry = Encoding.Default.GetBytes(target);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (GZipStream sw = new GZipStream(ms, CompressionMode.Compress))
                    {
                        sw.Write(byteArry, 0, byteArry.Length);

                    }
                    data = Convert.ToBase64String(ms.ToArray());
                }
                return data;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 解压文本
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string DeCompress(string target)
        {
            try
            {
                string data = "";
                byte[] bytes = Convert.FromBase64String(target);
                using (MemoryStream msReader = new MemoryStream())
                {
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
                        {
                            byte[] buffer = new byte[1024];
                            int readLen = 0;
                            while ((readLen = zip.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                msReader.Write(buffer, 0, readLen);
                            }
                        }
                    }
                    data = Encoding.Default.GetString(msReader.ToArray());
                }
                return data;
            }
            catch (System.Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 数据写入
        /// </summary>
        /// <param name="saveFileName"></param>
        /// <param name="data"></param>
        public static void SaveByJson(string saveFileName, object data)
        {
            //数据转换为Json 格式
            string json = LitJson.JsonMapper.ToJson(data);
            //填写相对路径以及 文件名字
            string path = Path.Combine(Application.persistentDataPath,saveFileName);

            try
            {
                File.WriteAllText(path, json);
#if UNITY_EDITOR
                //Log.Info("数据保存成功！地址：{0}",path);
#endif
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Log.Error("数据保存失败！地址：{0}，原因：{1}", path,e.ToString());
#endif

            }

        }

        /// <summary>
        /// 数据删除
        /// </summary>
        /// <param name="savedFile"></param>
        public static void DeleteSaveFile(string savedFile)
        {
            string filePath = Path.Combine(Application.persistentDataPath, savedFile);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            else
            {
                Log.Info("删除文件失败");
            }
           
        }

        public static string LoadFormToJson(string fileName)
        {
            string filePath=Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return json;
                }
                else
                {
                    Application.Quit();
                    return string.Empty;
                }
            }
            catch (Exception)
            {
                Application.Quit();
                return string.Empty;
            }

        }

    }
}
