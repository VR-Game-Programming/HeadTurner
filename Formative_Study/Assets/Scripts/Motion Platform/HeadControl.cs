using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadControl : MonoBehaviour
{
    // Using the CenterEyeAnchor under the CameraRig as the head anchor
    public Transform headAnchor;
    Vector3 yawAxis, neutralViewingDirection;
    public int averagingFrames = 2000;
    // Actuate the yawing angle according to the head rotation
    public DOFCommunication dOFCommunication;
    [Header("PID Control")]
    public float kp = 1 / 180f;
    float error;
    private void Start()
    {
        //Find the DOFCommunication script in the scene
        dOFCommunication = FindObjectOfType<DOFCommunication>();
        if (headAnchor == null || dOFCommunication == null)
        {
            Debug.LogError("Head anchor or DOFCommunication is not set");
            // Quit
            Application.Quit();
        }
        // Set the yaw axis as the y-axis of the head anchor, 
        // Set the viewing direction as the forward direction of the head anchor
        // averaging for averagingFrames frames to reduce noise
        for (int i = 0; i < averagingFrames; i++)
        {
        }
        yawAxis /= averagingFrames;
        neutralViewingDirection /= averagingFrames;
    }
    private void Update()
    {
        // the problem is that the headAnchor is not updated in the Start stage
        // so the yawAxis and neutralViewingDirection are not correct
        // so we need to update them in the Update stage, but only averaging once
        if (averagingFrames > 0)
        {
            yawAxis += headAnchor.up;
            neutralViewingDirection += headAnchor.forward;
            averagingFrames -= 1;
        }
        else if (averagingFrames == 0)
        {
            yawAxis.Normalize();
            neutralViewingDirection.Normalize();
            averagingFrames -= 1;
        }
        else
        {
            // The error is the angle between the current viewing direction and the neutral viewing direction, relative in the yaw axis
            error = Vector3.SignedAngle(headAnchor.forward, neutralViewingDirection, yawAxis);
            // The yawing range is (0,0) and (1,1), the neutral position is (0.5,0.5)
            // The error is normalized to the range (-0.5,0.5), then shifted to the range (0,1)
            Debug.Log(error);
            float targetAngle = (error / 180f + 0.5f);
            // Now the problem is that we don't want the angle to change too fast

            // Set the yaw angle of the motors
            dOFCommunication.SetMotorPos(targetAngle, targetAngle);
            // visualize the yaw axis (green) and neutral direction (blue) during the game
            Debug.DrawRay(headAnchor.position, yawAxis * 500, Color.green);
            Debug.DrawRay(headAnchor.position, neutralViewingDirection * 500, Color.blue);
        }
    }
    public void SetYawAngle(float angle)
    {
        // Set the yaw angle of the motors
        dOFCommunication.SetMotorPos(angle, angle);
    }
}
