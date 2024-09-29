using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using VRTK;
using System;

public class HandShankRecive : ButtonsName
{
    public HandleTag handleTag= HandleTag.Right;
    private void OnEnable()
    {
        switch (handleTag)
        {
            case HandleTag.Left:
                HandShankSend.LeftControl.AddRecive(this);
                break;
            case HandleTag.Right:
                HandShankSend.RightControl.AddRecive(this);
                break;
        }
    }
    private void OnDisable()
    {
        switch (handleTag)
        {
            case HandleTag.Left:
                HandShankSend.LeftControl.RemoveRecive(this);
                break;
            case HandleTag.Right:
                HandShankSend.RightControl.RemoveRecive(this);
                break;
        }
    }
    public void _receive(HandShankSend sender, string msg, float[] value)
    {
        if (!string.IsNullOrEmpty(msg))
            switch (msg)
            {
                case GripPressed: Func_GripPressed(sender); break;
                case GripReleased: Func_GripReleased(sender); break;
                case TouchpadPressed: Func_TouchpadPressed(sender); break;
                case TouchpadReleased: Func_TouchpadReleased(sender); break;
                case TouchpadTouchStart: Func_TouchpadTouchStart(sender); break;
                case TouchpadTouchEnd: Func_TouchpadTouchEnd(sender); break;
                case TriggerAxisChanged: Func_TriggerAxisChanged(sender, value[0]); break;
                case TouchpadAxisChanged: Func_TouchpadAxisChanged(sender, value[0], value[1]); break;
                case ButtonTwoPressed: Func_ButtonTwoPressed(sender); break;
                case ButtonTwoReleased: Func_ButtonTwoReleased(sender); break;

            }
    }

    protected virtual void Func_ButtonTwoReleased(HandShankSend sender) { }
    protected virtual void Func_ButtonTwoPressed(HandShankSend sender) { }
    protected virtual void Func_GripPressed(HandShankSend sender) { }
    protected virtual void Func_GripReleased(HandShankSend sender) { }
    protected virtual void Func_TouchpadPressed(HandShankSend sender) { }
    protected virtual void Func_TouchpadReleased(HandShankSend sender) { }
    protected virtual void Func_TouchpadTouchStart(HandShankSend sender) { }
    protected virtual void Func_TouchpadTouchEnd(HandShankSend sender) { }
    protected virtual void Func_TriggerAxisChanged(HandShankSend sender,float triggerAxis) { }
    protected virtual void Func_TouchpadAxisChanged(HandShankSend sender,float touchpadAxis_X, float touchpadAxis_Y) { }
    public enum HandleTag { Left, Right }
}
