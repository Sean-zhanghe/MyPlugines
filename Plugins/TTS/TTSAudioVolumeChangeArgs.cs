using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;


public class TTSAudioVolumeChangeArgs : GameEventArgs
{
    /// <summary>
    /// 网络连接成功事件编号。
    /// </summary>
    public static readonly int EventId = typeof(TTSAudioVolumeChangeArgs).GetHashCode();
    // Start is called before the first frame update

    public TTSAudioVolumeChangeArgs()
    {
        UserData = null;
    }
    /// <summary>
    /// 获取用户自定义数据。
    /// </summary>
    public object UserData
    {
        get;
        private set;
    }

    public override int Id
    {
        get
        {
            return EventId;

        }
    }

    public static GameEventArgs Create(VarDouble args)
    {
        TTSAudioVolumeChangeArgs ttsAudioVolumeChangeArgs = ReferencePool.Acquire<TTSAudioVolumeChangeArgs>();
        ttsAudioVolumeChangeArgs.UserData = (object)args;
        return ttsAudioVolumeChangeArgs;
    }

    public override void Clear()
    {
        UserData = null;
    }
}