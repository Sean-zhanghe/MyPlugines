using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

/// <summary>
/// 事件统筹转发，必须在左右手柄上
/// </summary>
public class HandShankSend : ButtonsName
{
    static HandShankSend leftControl;
    static HandShankSend rightControl;
    public static HandShankSend LeftControl { get => leftControl; }
    public static HandShankSend RightControl { get => rightControl; }

    [SerializeField]
     List<HandShankRecive> TargetScripts=new List<HandShankRecive>();
    VRTK_ControllerEvents thisHandShankControl;
    public VRTK_ControllerEvents ThisHandShankControl { get => thisHandShankControl;}

    private void Awake()
    {
        thisHandShankControl = GetComponent<VRTK_ControllerEvents>();
        if (thisHandShankControl != null)
        {
            if (transform.name == "LeftController")
            {
                leftControl = this;
            }
            if (transform.name == "RightController")
            {
                rightControl = this;
            }
        }

    }
    private void Start()
    {
        #region controller trigger events
        thisHandShankControl.TriggerTouchStart += (object sender, ControllerInteractionEventArgs e) =>
        {
            isTriggerTouch = true;
        };
        thisHandShankControl.TriggerTouchEnd += (object sender, ControllerInteractionEventArgs e) =>
        {
            isTriggerTouch = false;
        };
        #endregion controller trigger events

        #region controller grip events
        thisHandShankControl.GripPressed += (object sender, ControllerInteractionEventArgs e) =>
        {
            Send(GripPressed);
        };
        thisHandShankControl.GripReleased += (object sender, ControllerInteractionEventArgs e) =>
        {
            Send(GripReleased);
        };
        #endregion controller grip events

        #region controller touchpad events
        thisHandShankControl.TouchpadPressed += (object sender, ControllerInteractionEventArgs e) =>
        {
            Send(TouchpadPressed);
        };
        thisHandShankControl.TouchpadReleased += (object sender, ControllerInteractionEventArgs e) =>
        {
            Send(TouchpadReleased);
        };
        thisHandShankControl.TouchpadTouchStart += (object sender, ControllerInteractionEventArgs e) =>
        {
            isTouchpadTouch = true;
            Send(TouchpadTouchStart);
        };
        thisHandShankControl.TouchpadTouchEnd += (object sender, ControllerInteractionEventArgs e) =>
        {
            isTouchpadTouch = false;
            Send(TouchpadTouchEnd);
        };
        thisHandShankControl.ButtonTwoPressed+= (object sender, ControllerInteractionEventArgs e) =>
        {
            Send(ButtonTwoPressed);
        };
        thisHandShankControl.ButtonTwoReleased += (object sender, ControllerInteractionEventArgs e) =>
        {
            Send(ButtonTwoReleased);
        };
        #endregion controller touchpad events
    }
    private void FixedUpdate()
    {
        TriggerButtonClick();
        TouchpadTouchClick();
    }
    //扳机
    bool isTriggerTouch = false;
    void TriggerButtonClick()
    {
        if (isTriggerTouch)
        {
            Send(TriggerAxisChanged, thisHandShankControl.GetTriggerAxis());
        }
    }
    bool isTouchpadTouch = false;
    //触摸板键
    void TouchpadTouchClick()
    {
        if (isTouchpadTouch)
        {
            Vector2 axis = thisHandShankControl.GetTouchpadAxis();
            Send(TouchpadAxisChanged, axis.x,axis.y);
        }
    }

    private void Send(string a,params float[] v)
    {
        Debug.LogError(transform.name+"========="+ a);
        if (TargetScripts != null)
        {
            for (int i = 0; i < TargetScripts.Count; i++)
            {
                TargetScripts[i]._receive(this,a, v);

            }
        }
    }

    public void AddRecive(HandShankRecive addTarget) {
        if (!TargetScripts.Contains(addTarget))
        TargetScripts.Add(addTarget);
    }
    public void RemoveRecive(HandShankRecive removeTarget) {
        if (TargetScripts.Contains(removeTarget))
        TargetScripts.Remove(removeTarget);
    }
}
