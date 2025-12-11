using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using WeiDaEdu;

public class TextToSpeech_ZH : MonoBehaviour
{
    [Header("音源")] public AudioSource _Audio;

    [Header("AI 发声器")] public Pronouncer _Pronouncer = Pronouncer.Duyaya;

    //全局变量
    public static TextToSpeech_ZH _World;

    //网页文字转语音
    private string _Url;


    private float voiceRange;

    public Action EndPlaying;

    private Coroutine _Coroutine;

    private void Start()
    {
        // if (_Audio)
        // {
        //     _Audio.volume = GameEntry.CustomData.voiceRange;
        // }
    }

    private void OnEnable()
    {
        _World = this;
        //_World.StartCoroutine(GetAudioClip("世界"));
    }

    /// <summary>
    /// 保存音量，后面转入其他场景也一样使用
    /// </summary>
    public void SaveVolume()
    {
        GameEntry.CustomData.voiceRange = _Audio.volume;
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    /// <param name="volume"></param>
    public void SetVolume(float volume)
    {
        if (_Audio)
        {
            _Audio.volume = volume;
        }
    }

    /// <summary>
    /// 语音输入 播放
    /// </summary>
    /// <param name="_Str"></param>
    public void PlayAudio(string _Str)
    {
        if (_Coroutine!= null)
        {
            StopCoroutine(_Coroutine);
        }
        _Coroutine =  StartCoroutine(GetAudioClip(_Str));
    }

    /// <summary>
    /// 语音输入 播放
    /// </summary>
    /// <param name="_Str"></param>
    public void PlayAudio(string _Str, Action endPlaying)
    {
        if (_Coroutine != null)
        {
            StopCoroutine(_Coroutine);
        }
        EndPlaying = endPlaying;
        _Coroutine = StartCoroutine(GetAudioClip(_Str));
    }

    /// <summary>
    /// 语音停止方法
    /// </summary>
    /// <returns></returns>
    public IEnumerator StopAudioClip()
    {
        _Audio.Stop();
        yield return 0;
    }

    //获取 Web网页音源信息并播放
    public IEnumerator GetAudioClip(string AudioText)
    {
        //_Url = "https://tsn.baidu.com/text2audio?tex=" + AudioText + 
        //  "+&lan=zh&cuid=7919875968150074&ctp=1&aue=6&tok=25.3141e5ae3aa109abb6fc9a8179131181.315360000.1886566986.282335-17539441";
        //https://tsn.baidu.com/text2audio?tex= Test&lan=zh&cuid=7919875968150074&ctp=1&aue=6&tok=25.3141e5ae3aa109abb6fc9a8179131181.315360000.1886566986.282335-17539441

        //过滤地址不可用的特殊字符
        AudioText = AudioText.Replace("：", " ");
        AudioText = AudioText.Replace(':', '比');
        AudioText = AudioText.Replace("98%", "百分之九十八");
        AudioText = AudioText.Replace("74%", "百分之七十四");
        Debug.Log(AudioText);
        _Url = "http://tsn.baidu.com/text2audio?tex=" + AudioText +
               "&tok=25.3141e5ae3aa109abb6fc9a8179131181.315360000.1886566986.282335-17539441" +
               "&cuid=a7a0e3326da873c6fb0609e6385a82b934c9cb11" +
               "&ctp=1" +
               "&lan=zh" +
               "&spd=5" +
               "&pit=5" +
               "&vol=10" +
               "&per=" + (((int)_Pronouncer).ToString()) +
               "&aue=6";


        using (UnityWebRequest _AudioWeb = UnityWebRequestMultimedia.GetAudioClip(_Url, AudioType.WAV))
        {
            yield return _AudioWeb.SendWebRequest();
            if (_AudioWeb.isNetworkError)
            {
                yield break;
            }

            AudioClip _Cli = DownloadHandlerAudioClip.GetContent(_AudioWeb);
            _Audio.clip = _Cli;
            _Audio.Play();

            Debug.Log($"播放音频成功,时长：{_Cli.length}");
            yield return new WaitForSeconds(_Cli.length);
            EndPlaying?.Invoke();
            yield return null;
            EndPlaying = null;
        }
    }

    /// <summary>
    /// 获取 Web网页音源信息并播放 附带延迟时间
    /// </summary>
    /// <param 播放文字="AudioText"></param>
    /// <param 延迟时间="_DelayedTimer"></param>
    /// <returns></returns>
    public IEnumerator GetAudioClip(string AudioText, float _DelayedTimer)
    {
        //_Url = "https://tsn.baidu.com/text2audio?tex= AudioText &lan=zh&cuid=7919875968150074&ctp=1&aue=6&tok=25.3141e5ae3aa109abb6fc9a8179131181.315360000.1886566986.282335-17539441
        //  "";
        yield return new WaitForSeconds(_DelayedTimer);


        _Url = "http://tsn.baidu.com/text2audio?tex=" + AudioText +
               "&tok=25.3141e5ae3aa109abb6fc9a8179131181.315360000.1886566986.282335-17539441" +
               "&cuid=a7a0e3326da873c6fb0609e6385a82b934c9cb11" +
               "&ctp=1" +
               "&lan=zh" +
               "&spd=5" +
               "&pit=5" +
               "&vol=10" +
               "&per=" + (((int)_Pronouncer).ToString()) +
               "&aue=6";


        using (UnityWebRequest _AudioWeb = UnityWebRequestMultimedia.GetAudioClip(_Url, AudioType.WAV))
        {
            yield return _AudioWeb.SendWebRequest();
            if (_AudioWeb.isNetworkError)
            {
                yield break;
            }

            AudioClip _Cli = DownloadHandlerAudioClip.GetContent(_AudioWeb);
            _Audio.clip = _Cli;


            _Audio.Play();
        }
    }


    /// <summary>
    /// AI 发音器
    /// </summary>
    public enum Pronouncer
    {
        //普通女声
        Female,

        //普通男生
        Male,

        //特殊男声
        Teshunan,

        //情感合成男生
        Duxiaoyao,

        //情感合成女生
        Duyaya
    }
}