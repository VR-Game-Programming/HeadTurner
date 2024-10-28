using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;
using System.Threading;
using Unity.VisualScripting;

public class DistanceSensor : MonoBehaviour
{
    [Header("Sensor")]
    public int averagingFrames = 100;
    public OrientationUtility orientationUtility;
    const float stroke = 330f; // 1250 for slower motor
    int currentFrame = 0;
    float pitchAngle = 0, prevPitchAngle = 0;

    bool isCalibrated = false;
    public bool IsCalibrated
    {
        get
        {
            return isCalibrated;
        }
    }
    [Header("OptiTrack Sensor")]
    public Transform head;
    public Transform pillow;
    float dist, distAvg = 0;
    public float distThreshold = 3f;

    [Header("Controller")]
    public float angleThreshold = 5;
    public float timeDelay = 0.5f;
    float time = 0;
    int step = 7;
    public enum Status { Stay, PitchUp, PitchDown };
    public Status status = Status.Stay;
    [Header("Actuator")]
    public ArduinoCommunication arduinoCommunication;
    int targetLinearMotor;

    // Methods
    // Start is called before the first frame update
    void Start()
    {
        arduinoCommunication = FindObjectOfType<ArduinoCommunication>();

        if (orientationUtility == null)
        {
            orientationUtility = FindObjectOfType<OrientationUtility>();
        }
        if (arduinoCommunication == null || head == null || pillow == null)
        {
            Debug.LogError("Sensing or Communication is not set");
            // Quit
            Application.Quit();
        }
        currentFrame = 0;
    }
    void Update()
    {
        dist = Vector3.Distance(head.position, pillow.position);
        //Debug.Log("dist:" + dist);
        if (currentFrame < averagingFrames)
        {
            distAvg += dist;
            currentFrame++;
        }
        else if (currentFrame == averagingFrames)
        {
            distAvg /= averagingFrames;
            Debug.Log("distAvg:" + distAvg);
            currentFrame++;
            isCalibrated = true;
        }
        // Control
        else
        {
            pitchAngle = Mathf.Clamp(orientationUtility.PitchAngle, -45, 20);
            switch (status)
            {
                case Status.Stay:
                    if (dist > distAvg + distThreshold)
                    {
                        status = Status.PitchDown;
                    }
                    if (pitchAngle - prevPitchAngle > angleThreshold)
                    {
                        status = Status.PitchUp;
                    }
                    break;
                case Status.PitchUp:
                    targetLinearMotor = (int)(stroke * (1 - Mathf.Tan((pitchAngle + 45) * Mathf.Deg2Rad)));
                    arduinoCommunication.TargetLinearMotor = targetLinearMotor;
                    // Delay for actuator to move
                    time += Time.deltaTime;
                    if (time > timeDelay)
                    {
                        if (dist <= distAvg + distThreshold)
                        {
                            status = Status.Stay;
                        }
                        time = 0;
                    }
                    break;
                case Status.PitchDown:
                    arduinoCommunication.TargetLinearMotor += step;
                    if (dist <= distAvg)
                    {
                        status = Status.Stay;
                    }
                    break;
                default:
                    break;
            }
            prevPitchAngle = pitchAngle;
        }
    }
}
