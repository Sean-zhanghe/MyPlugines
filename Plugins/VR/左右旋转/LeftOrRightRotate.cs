using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class LeftOrRightRotate : HandShankRecive
{
    Dictionary<HandShankSend, KV<Vector2,int>> touchSave = new Dictionary<HandShankSend, KV<Vector2, int>>();
    float[] AngleRange = { 45, 135 };

    private void Start()
    {
        PublicUsing_Show.Instance.shankSend_Left.AddRecive(this);
        PublicUsing_Show.Instance.shankSend_Right.AddRecive(this);
    }

    protected override void Func_TouchpadAxisChanged(HandShankSend sender, float touchpadAxis_X, float touchpadAxis_Y)
    {
        if (!touchSave.ContainsKey(sender))
        {
            touchSave.Add(sender, new KV<Vector2, int>() {KEY= Vector2.one * -2,VALUE=-1 });
        }
        KV<Vector2, int> a = touchSave[sender];
        a.KEY.x = touchpadAxis_X;
        a.KEY.y = touchpadAxis_Y;

        float angle = Vector2.Angle(Vector2.left, touchSave[sender].KEY);
        int Index = -1;
        if (angle <= AngleRange[0])
        {
            Index = 0;
        }
        if (AngleRange[0] < angle && angle <= AngleRange[1] && touchpadAxis_Y > 0)
        {
            Index = 1;
        }
        if (AngleRange[0] < angle && angle <= AngleRange[1] && touchpadAxis_Y < 0)
        {
            Index = 2;
        }
        if (AngleRange[1] < angle)
        {
            Index = 3;
        }
        if (Index == 0 || Index == 3)
        {
            sender.GetComponent<VRTK_Pointer>().enableTeleport = false;
        }
        else
        {
            sender.GetComponent<VRTK_Pointer>().enableTeleport = true;
        }
        if (Index != a.VALUE)
        {
            VrTool.Instance.HapticPulse(sender.ThisHandShankControl);
            a.VALUE = Index;
        }

        touchSave[sender] = a;
    }
    protected override void Func_TouchpadPressed(HandShankSend sender)
    {
        if (touchSave.ContainsKey(sender) && touchSave[sender].KEY != Vector2.one * -2)
        {
            float angle = Vector2.Angle(Vector2.left, touchSave[sender].KEY);
            if (angle < AngleRange[0])
            {
                //PublicUsing_Show.Instance.VRTK_SDKManager.DORotate(PublicUsing_Show.Instance.VRTK_SDKManager.eulerAngles-Vector3.up*30,0.75f);
                PublicUsing_Show.Instance.CameraRig.eulerAngles = PublicUsing_Show.Instance.CameraRig.eulerAngles - Vector3.up * 30;
                VrTool.Instance.Blink();
            }
            if (angle > AngleRange[1])
            {
                //PublicUsing_Show.Instance.VRTK_SDKManager.DORotate(PublicUsing_Show.Instance.VRTK_SDKManager.eulerAngles + Vector3.up * 30 , 0.75f);
                PublicUsing_Show.Instance.CameraRig.eulerAngles = PublicUsing_Show.Instance.CameraRig.eulerAngles + Vector3.up * 30;
                VrTool.Instance.Blink();
            }
        }
    }

    protected override void Func_TouchpadTouchEnd(HandShankSend sender)
    {
        if (touchSave.ContainsKey(sender))
        {
            KV<Vector2, int> a = touchSave[sender];
            a.KEY = Vector2.one * -2;
            touchSave[sender] = a;
        } 
    }
}
