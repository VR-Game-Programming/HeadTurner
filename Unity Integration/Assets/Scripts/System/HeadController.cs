using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadController : MonoBehaviour
{
    public OrientationUtility orientationUtility;
    // Actuate the yawing angle according to the head rotation
    public DOFCommunication dOFCommunication;
    public ArduinoCommunication arduinoCommunication;
    public bool enableActuation = true;
    float yawHeadRelativeToTrunk, pitchAngle;
    float targetPlatformMotor = 0.5f;
    int targetLinearMotor;
    private void Start()
    {
        // Find the OrientationUtility script in the scene
        orientationUtility = FindObjectOfType<OrientationUtility>();
        //Find the Communication script in the scene
        dOFCommunication = FindObjectOfType<DOFCommunication>();
        arduinoCommunication = FindObjectOfType<ArduinoCommunication>();
        if (dOFCommunication == null || arduinoCommunication == null || orientationUtility == null)
        {
            Debug.LogError("Sensing or Communication is not set");
            // Quit
            Application.Quit();
        }
        // Set the yaw axis as the y-axis of the head anchor, 
        // Set the viewing direction as the forward direction of the head anchor
        // averaging for averagingFrames frames to reduce noise
    }
    private void Update()
    {
        if (!enableActuation)
        {
            return;
        }
        if (orientationUtility.IsCalibrated)
        {
            // Get the Yaw angle from the OrientationUtility script
            yawHeadRelativeToTrunk = orientationUtility.YawAngle + (targetPlatformMotor - 0.5f) * 23;
            // Body Yawing
            // The yawing range is (0,0) and (1,1), the neutral position is (0.5,0.5)
            // The error is normalized to the range (-0.5,0.5), then shifted to the range (0,1)
            // targetAngle is mapped from the error of (-90,90) to (0,1)
            // Debug.Log(); // -180 ~ +180
            targetPlatformMotor = yawHeadRelativeToTrunk / 90 + 0.5f;
            targetPlatformMotor = Mathf.Clamp(targetPlatformMotor, 0, 1);
            dOFCommunication.SetMotorPos(targetPlatformMotor, targetPlatformMotor);

            // Head Pitching
            pitchAngle = Mathf.Clamp(orientationUtility.PitchAngle, -45, 45);
            targetLinearMotor = (int)(1250 * (1 - Mathf.Tan((pitchAngle + 45) * Mathf.Deg2Rad)));
            arduinoCommunication.TargetLinearMotor = targetLinearMotor;
            //Debug.Log("yawRel: " + yawHeadRelativeToTrunk.ToString() + "|pitchAngle: " + pitchAngle.ToString());
        }
    }
}
