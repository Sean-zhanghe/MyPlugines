using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GameFramework;
using GameFramework.Event;
using GameFramework.Sound;
using UnityEngine;
using UnityEngine.Networking;
using UnityGameFramework.Runtime;
using Utility = GameFramework.Utility;

namespace StarForce
{
    public class TTSComponent : GameFrameworkComponent
    {
        [Header("音源")]
        public AudioSource _Audio;

        private int m_speakerID;
        private string _Url;
        private AudioType m_AudioType;
        private float m_Pitch;

        private bool m_isStop = false;

        private void Start()    
        {
            GameEntry.Event.Subscribe(TTSAudioVolumeChangeArgs.EventId,ChangeVolume);
        }

        void OnDestory()
        {
            GameEntry.Event.Unsubscribe(TTSAudioVolumeChangeArgs.EventId,ChangeVolume);
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }
        public void TTSStart(string _content,int _ttsID, float _pitch = 1)
        {
            StopAllCoroutines();

            if (m_isStop)
            {
                return;
            }
            m_speakerID = _ttsID;
            m_Pitch = _pitch;
            SpeakerURL();
            StartCoroutine(GetAudioClip(UnityWebRequest.EscapeURL(_content)));
        }

        /// <summary>
        /// https://ai.baidu.com/ai-doc/SPEECH/Qk38y8lrl
        /// </summary>
        void SpeakerURL()
        {
            switch (m_speakerID)
            {
                case 0://百度:w[5-15]
                    _Url = "https://tsn.baidu.com/text2audio?tex={0}+&lan=zh&cuid=7919875968150074&ctp=1&aue=6&tok=25.3141e5ae3aa109abb6fc9a8179131181.315360000.1886566986.282335-17539441&spd=12&per=0&aue=3";
                    break;
                case 1://w
                    _Url = "https://tsn.baidu.com/text2audio?tex={0}+&lan=zh&cuid=7919875968150074&ctp=1&aue=6&tok=25.3141e5ae3aa109abb6fc9a8179131181.315360000.1886566986.282335-17539441&spd=12&per=4&aue=3";
                    break;
                case 2://m
                    _Url = "https://tsn.baidu.com/text2audio?tex={0}+&lan=zh&cuid=7919875968150074&ctp=1&aue=6&tok=25.3141e5ae3aa109abb6fc9a8179131181.315360000.1886566986.282335-17539441&spd=12&per=1&aue=3";
                    break;
                case 3://m
                    _Url = "https://tsn.baidu.com/text2audio?tex={0}+&lan=zh&cuid=7919875968150074&ctp=1&aue=6&tok=25.3141e5ae3aa109abb6fc9a8179131181.315360000.1886566986.282335-17539441&spd=12&per=3&aue=3";
                    break;
                case 4://yd：w[0.1-2]
                    _Url = "http://tts.youdao.com/fanyivoice?word={0}&le=zh&keyfrom=speaker-target&speed=0.9";
                    break;
                case 5://sg:w[0.7-1.3]
                    _Url = "https://fanyi.sogou.com/reventondc/synthesis?text={0}&speed=1&lang=zh-CHS&from=translateweb&speaker=3&speaking_rate=1.1";
                    break;
                case 6://w
                    _Url = "https://fanyi.sogou.com/reventondc/synthesis?text={0}&speed=1&lang=zh-CHS&from=translateweb&speaker=5&speaking_rate=1.1";
                    break;
                case 7://w
                    _Url = "https://fanyi.sogou.com/reventondc/synthesis?text={0}&speed=1&lang=zh-CHS&from=translateweb&speaker=6&speaking_rate=1.1";
                    break;
                case 8://m
                    _Url = "https://fanyi.sogou.com/reventondc/synthesis?text={0}&speed=1&lang=zh-CHS&from=translateweb&speaker=1&speaking_rate=1.1";
                    break;
                case 9://m
                    _Url = "https://fanyi.sogou.com/reventondc/synthesis?text={0}&speed=1&lang=zh-CHS&from=translateweb&speaker=4&speaking_rate=1.1";
                    break;
                case 10://m
                    _Url = "https://fanyi.sogou.com/reventondc/synthesis?text={0}&speed=1&lang=zh-CHS&from=translateweb&speaker=2&speaking_rate=1.1";
                    break;
                default:
                    break;
            }
        }

        //获取 Web网页音源信息并播放
        private IEnumerator GetAudioClip(string AudioText)
        {
            string url = Utility.Text.Format(_Url, AudioText);
            
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                DownloadHandlerAudioClip dHA = new DownloadHandlerAudioClip(string.Empty, AudioType.MPEG);
                dHA.streamAudio = true;
                www.downloadHandler = dHA;
                www.SendWebRequest();
                while (www.downloadProgress < 1)
                {
                    //Debug.Log(www.downloadProgress);
                    yield return new WaitForSeconds(.1f);
                }
                if (www.isNetworkError|| www.responseCode != 200)
                {
                    yield break;
                }

                try
                {
                    AudioClip _Cli = DownloadHandlerAudioClip.GetContent(www);
                    if (_Cli.LoadAudioData())
                    {
                        //Debug.Log("音频已成功加载");
                    }
                    else
                    {
                        Log.Error("音效加载失败");
                        yield break;
                    }

                    //将clip赋给A
                    _Audio.clip = _Cli;
                    _Audio.pitch = m_Pitch;
                    _Audio.Play();
                }
                catch (Exception e)
                {
                    Log.Debug(" TTS 语音播放错误"+ url +"  , "+ e.Message);
                    //throw;
                }
            }
        }

         public  void StopPlay()
        {
            Log.Debug("停止播放");
            StopAllCoroutines();
            _Audio.Stop();
        }

         public void Resume()
         {
             _Audio.Play();
         }


         public void Muted(bool isOn)
         {
             m_isStop = !isOn;
             _Audio.volume = isOn ? GameEntry.Setting.GetFloat(Constant.Setting.UISpeakVolume):0;
         }


         public void ChangeVolume(object sender,GameEventArgs args)
         {
             double volume= (VarDouble)((TTSAudioVolumeChangeArgs) args).UserData;
             _Audio.volume =(float)volume;
         }

    }
}

    