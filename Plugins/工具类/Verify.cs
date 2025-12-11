using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.Networking;

using Debug = UnityEngine.Debug;

namespace WeiDaEdu
{
    public class Verify : MonoBehaviour
    {

        public void VerifyNet()
        {
            //_SaveJson = LoadFromJson<SecretKeyData>(PLAYER_DATA_FILE_NAME);
            GameEntry.Event.Fire(this, VerifyEvent.Create(false));
            StartCoroutine(GetPostData(ExecuteCommand("wmic csproduct get UUID")));

        }

        #region 加密与授权验证

        [DllImport("winInet.dll")]
        //网络链接判断
        private static extern bool InternetGetConnectedState(ref int dwFlag, int dwReserved);

        [Header("访问网址")]
        public string _UUIDUrl = GameEntry.CustomData.VerifyURL;

        //发送数据
        private PostJson_Maddie _PostData = new PostJson_Maddie();

        //数据文件名称
        private const string PLAYER_DATA_FILE_NAME = "PlayerDataAuthorization.sav";

        [Header("软件序列号")]
        public string _SerialNumber = GameEntry.CustomData.SerialNumber;

        //解析数据
        [HideInInspector]
        public SecretKeyData _SaveJson = new SecretKeyData();

        //离线
        private const string OFF_LINE = "OffLine.sav";

        //本地化离线存储数据
        private OffLineData _OffLineData = new OffLineData();

        /// <summary>
        /// 网络连接 判断
        /// </summary>
        /// <returns></returns>
        private bool NetworkLinking()
        {
            int _DwFlag = new int();
            if (InternetGetConnectedState(ref _DwFlag, 0))
            {
                return true;

            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// UUID CMD 执行获取
        /// </summary>
        /// <param CMD 命令="_Command"></param>
        /// <returns></returns>
        private string ExecuteCommand(string _Command)
        {
            Process _Process = new Process();
            ProcessStartInfo _StartInfo = new ProcessStartInfo();
            _StartInfo.FileName = "cmd.exe";
            _StartInfo.Arguments = "/C " + _Command;
            _StartInfo.RedirectStandardOutput = true;
            _StartInfo.UseShellExecute = false;
            _StartInfo.CreateNoWindow = true;

            _Process.StartInfo = _StartInfo;
            _Process.Start();

            string _Output = _Process.StandardOutput.ReadToEnd();
            _Process.WaitForExit();

            // 提取数字部分
            string _UUID = ExtractUUID(_Output);

            // 去除多余的空格和换行符
            _UUID = _UUID.Trim();

            return _UUID;
        }

        /// <summary>
        /// 提取UUID数字部分
        /// </summary>
        /// <param 输入 ="_Input"></param>
        /// <returns></returns>
        private string ExtractUUID(string _Input)
        {
            string _UUIDPattern = @"\b[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}\b";
            MatchCollection _Matches = Regex.Matches(_Input, _UUIDPattern);

            if (_Matches.Count > 0)
            {
                return _Matches[0].Value;
            }

            return string.Empty;
        }


        /// <summary>
        /// 处理服务器响应消息
        /// </summary>
        /// <param 返回消息="_Message"></param>
        private void HandleResponseMessage(string _Message)
        {
            switch (_Message)
            {
                case "成功":
                    print("验证通过");
                    GameEntry.Event.Fire(this, VerifyEvent.Create(true));
                    //_MessageShowText.text += "验证通过" + "\n";
                    break;

                case "校验失败":
                    Debug.LogError("UUID 验证失败。");
                    //_MessageShowText.text += "UUID 验证失败。" + "\n";
                    Application.Quit();
                    break;

                case "已过期":
                    Debug.LogError("试用已过期。");
                    //_MessageShowText.text += "试用已过期" + "\n";
                    Application.Quit();
                    break;

                case "未授权":
                    Debug.LogError("当前软件未授权。");
                    //_MessageShowText.text += "当前软件未授权" + "\n";
                    Application.Quit();
                    break;

                default:
                    Application.Quit();
                    //_MessageShowText.text += "其他原因" + "\n";
                    break;
            }
        }




        /// <summary>
        /// 与当前日期比较
        /// </summary>
        /// <param 目标日期="_TargetDate"></param>
        public bool CompareToCurrentDate(string _TargetDate)
        {

            // 将字符串解析为日期对象
            DateTime _Date1;

            if (!DateTime.TryParse(_TargetDate, out _Date1))
            {
                Debug.LogError("无效的日期格式。请提供格式为：yyyy-MM-dd 的有效日期。");
                return false;
            }

            // 获取当前系统日期
            DateTime _CurrentDate = DateTime.Now;

            // 进行比较
            int _Comparison = DateTime.Compare(_Date1.Date, _CurrentDate.Date);

            // 根据比较结果输出消息
            if (_Comparison < 0)
            {
                Debug.Log($"{_Date1} 在当前系统日期之前，离线登录失败。");
                return false;
            }
            else if (_Comparison > 0)
            {
                Debug.Log($"{_Date1} 在当前系统日期之后，离线登录成功。");
                return true;
            }
            else
            {
                Debug.Log($"{_Date1} 与当前系统日期相同，离线登录成功。");
                return true;
            }
        }


        /// <summary>
        /// POST 方法请求
        /// </summary>
        /// <param 问题="_SendMessage"></param>
        /// <returns></returns>
        private IEnumerator GetPostData(string _SendMessage)
        {
            if (!NetworkLinking())
            {
                Debug.LogError("没有网络链接。");

                //当前系统时间获取
                string[] _SystemTime = System.DateTime.Now.ToString("d").Split('/');

                //加载离线文件
                _OffLineData = LoadFromJson<OffLineData>(OFF_LINE);

                //如果当前文件存在
                if (_OffLineData != null)
                {
                    //如果服务器 UUID 和 本地 UUID 相同 就进入
                    if (ExecuteCommand("wmic csproduct get UUID") == _OffLineData._SecretUUID)
                    {
                        //截止时间
                        string _Deadline = CheckOfflineData(_OffLineData, _SaveJson._SecretUUID, _SerialNumber);

                        //如果无匹配项
                        if (_Deadline == "No Match")
                        {
                            Debug.LogError("没有检测到相同的序列号。");

                            Application.Quit();
                            StopAllCoroutines();
                        }

                        //如果 是长期
                        if (_Deadline == "长期")
                        {
                            GameEntry.Event.Fire(this, VerifyEvent.Create(true));
                            Debug.LogError("无网络登录。");
                        }
                        //如果是 试用
                        //进行日期校验
                        else if (CompareToCurrentDate(_Deadline))
                        {
                            GameEntry.Event.Fire(this, VerifyEvent.Create(true));
                            Debug.LogError("无网络 试用登录。");
                        }
                        else
                        {
                            Application.Quit();
                            StopAllCoroutines();
                        }
                    }
                    else
                    {
                        Application.Quit();
                        StopAllCoroutines();
                    }
                }
                else
                {
                    Application.Quit();
                    StopAllCoroutines();
                }
                yield return null;
            }
            else
            {
                _UUIDUrl = _UUIDUrl.Replace(" ", "");
                using (UnityWebRequest _Request = new UnityWebRequest(_UUIDUrl, "POST"))
                {
                    _PostData.uuid = _SendMessage;

                    if (_SerialNumber == null || _SerialNumber.Replace(" ", "") == "")
                    {
                        Debug.LogError("序列号为空");

                        //Application.Quit();
                        StopAllCoroutines();

                        yield return null;
                    }
                    else
                    {
                        _PostData.serialNumber = _SerialNumber;
                    }


                    //数据转换
                    string _JsonText = JsonUtility.ToJson(_PostData);
                    print(_JsonText);

                    byte[] _Data = System.Text.Encoding.UTF8.GetBytes(_JsonText);

                    //数据上传 等待响应
                    _Request.uploadHandler = new UploadHandlerRaw(_Data);
                    _Request.downloadHandler = new DownloadHandlerBuffer();


                    //数据重定向
                    _Request.SetRequestHeader("Content-Type", "application/json");

                    //等待响应 开始与远程服务器通信
                    yield return _Request.SendWebRequest();
                    print("等待响应:" + _Request.responseCode);

                    //_MessageShowText.text += "等待响应:" + _Request.responseCode+"\n";

                    //数据返回
                    if (_Request.responseCode == 200)
                    {
                        //接收返回信息
                        string _Message = _Request.downloadHandler.text;

                        print(_Message);

                        //_MessageShowText.text += _Message + "\n";

                        //数据转换
                        AcceptJson_Maddie _Textback = JsonUtility.FromJson<AcceptJson_Maddie>(_Message);

                        //print(_Textback._Message);


                        //确保当前有消息传回
                        if (_Textback.message != null)
                        {
                            HandleResponseMessage(_Textback.message);
                        }
                    }
                    else
                    {
                        StopAllCoroutines();
                        Application.Quit();
                    }
                }
            }
        }



        /// <summary>
        /// 离线数据校验
        /// </summary>
        /// <param 离线数据="_OfflineData"></param>
        /// <param 当前 UUID="_UUID"></param>
        /// <param 当前 序列号="_SerialNumber"></param>
        /// <returns></returns>
        public string CheckOfflineData(OffLineData _OfflineData, string _UUID, string _SerialNumber)
        {
            //截止日期
            string _Deadline = "";

            // 验证 UUID 是否相同
            if (_OfflineData._SecretUUID != _UUID)
            {
                Debug.LogError("UUID 不相同");

                Application.Quit();
                StopAllCoroutines();
            }

            // 遍历 _OfflineData 中的序列号
            foreach (SerialNumber _OfflineSerialNumber in _OfflineData._SerialNumber)
            {
                // 如果找到匹配的序列号
                if (_OfflineSerialNumber._SerialNumberAlone == _SerialNumber)
                {
                    // 有匹配项
                    _Deadline = _OfflineSerialNumber._Deadline;
                    return _Deadline;
                }
            }

            return "No Match";
        }



        /// <summary>
        /// Json数据加载
        /// </summary>
        /// <typeparam 加载文件类型="T"></typeparam>
        /// <param 数据加载名字="_SaveFileName"></param>
        /// <returns></returns>
        public static T LoadFromJson<T>(string _SaveFileName)
        {
            //填写相对路径以及 文件名字
            var _Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _SaveFileName);

            try
            {
                //地址读取
                var _Json = File.ReadAllText(_Path);
                //数据加载
                var _Data = JsonUtility.FromJson<T>(_Json);


#if UNITY_EDITOR
                Debug.Log($"数据从{_Path}加载成功。");
#endif



                //数据返回
                return _Data;
            }
            catch (System.Exception _Exception)
            {

#if UNITY_EDITOR
                Debug.LogError($"数据从{_Path}加载失败。\n{_Exception}");
#endif

                return default;
            }
        }

        /// <summary>
        /// 数据写入
        /// </summary>
        /// <param 文件存储名字="_SaveFileName"></param>
        /// <param 存储数据="_Data"></param>
        public void SaveByJson(string _SaveFileName, object _Data)
        {
            //数据转换为Json 格式
            var _Json = JsonUtility.ToJson(_Data);
            //填写相对路径以及 文件名字
            var _Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _SaveFileName);


            //异常捕获
            try
            {
                //数据文件写入
                File.WriteAllText(_Path, _Json);

#if UNITY_EDITOR
                Debug.Log($"数据保存到{_Path}成功。");
#endif
            }
            catch (System.Exception _Exception)
            {

#if UNITY_EDITOR
                Debug.LogError($"数据保存到{_Path}失败。\n{_Exception}");
#endif

            }
        }

        /// <summary>
        /// 验证 发送数据
        /// </summary>
        [System.Serializable]
        public class PostJson_Maddie
        {
            /// <summary>
            /// 本机 UUID
            /// </summary>
            public string uuid;

            /// <summary>
            /// 软件序列号
            /// </summary>
            public string serialNumber;
        }

        /// <summary>
        /// 验证 接收数据
        /// </summary>
        [System.Serializable]
        public class AcceptJson_Maddie
        {
            /// <summary>
            /// 相应类型
            /// </summary>
            public string code;
            /// <summary>
            /// 反馈消息
            /// </summary>
            public string message;
            /// <summary>
            /// 反馈数据
            /// </summary>
            public string data;
        }

        /// <summary>
        /// 密钥
        /// </summary>
        [System.Serializable]
        public class SecretKeyData
        {
            //基础加密
            public string _SecretUUID;

            //用户ID
            public string _SecretName;

            //用户名
            public string _SecretID;
        }


        /// <summary>
        /// 离线 验证密钥
        /// </summary>
        [System.Serializable]
        public class OffLineData
        {
            /// <summary>
            /// 本机 UUID
            /// </summary>
            public string _SecretUUID;

            /// <summary>
            /// 序列号
            /// </summary>
            public List<SerialNumber> _SerialNumber;

        }

        /// <summary>
        /// 离线 序列号
        /// </summary>
        [System.Serializable]
        public class SerialNumber
        {
            /// <summary>
            /// 序列号
            /// </summary>
            public string _SerialNumberAlone;

            /// <summary>
            /// 日期
            /// </summary>
            public string _Deadline;
        }


        #endregion

    }

}
