using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class YawContrller : DOFController
{
    public Slider yawAngle;
    private void Start() {
        yawAngle.value = yawAngle.maxValue / 2;
    }
    public override void OnSliderChange()
    {
        Debug.Log("Slider changed");
        // Yawing between (0,0) and (motorRange, motorRange)
        var angle = yawAngle.value;
        var left = (int)(angle * motorRange);
        var right = (int)(angle * motorRange);
        SetMotorAngles(left, right);
    }
}
