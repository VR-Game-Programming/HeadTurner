using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Head2Shoulder : MonoBehaviour
{
    // Using the CenterEyeAnchor under the CameraRig as the head anchor
    public Transform headAnchor;
    Vector3 yawAxis, viewingDirection, worldPitchAxis, worldYawAxis, prevYawAxis, prevViewingDirection;
    float alpha = 0.2f;
    public int averagingFrames = 2000;
    // Actuate the yawing angle according to the head rotation
    public DOFCommunication dOFCommunication;
    public ArduinoCommunication arduinoCommunication;
    float error;
    float targetAngle = 0.5f;
    private void Start()
    {
        //Find the DOFCommunication script in the scene
        dOFCommunication = FindObjectOfType<DOFCommunication>();
        arduinoCommunication = FindObjectOfType<ArduinoCommunication>();
        if (headAnchor == null || dOFCommunication == null || arduinoCommunication == null)
        {
            Debug.LogError("Head anchor or Communication is not set");
            // Quit
            Application.Quit();
        }
        // Set the yaw axis as the y-axis of the head anchor, 
        // Set the viewing direction as the forward direction of the head anchor
        // averaging for averagingFrames frames to reduce noise
    }
    private void Update()
    {
        // the problem is that the headAnchor is not updated in the Start stage
        // so the yawAxis and viewingDirection are not correct
        // so we need to update them in the Update stage, but only averaging once
        if (averagingFrames > 0)
        {
            yawAxis += headAnchor.up;
            viewingDirection += headAnchor.forward;
            worldPitchAxis += headAnchor.right;
            averagingFrames -= 1;
        }
        else if (averagingFrames == 0)
        {
            yawAxis.Normalize();
            prevYawAxis = yawAxis;
            worldYawAxis = yawAxis;
            viewingDirection.Normalize();
            prevViewingDirection = viewingDirection;
            worldPitchAxis.Normalize();
            averagingFrames -= 1;
        }
        else
        {
            yawAxis = alpha * headAnchor.up + (1 - alpha) * prevYawAxis;
            viewingDirection = alpha * headAnchor.forward + (1 - alpha) * prevViewingDirection;
            Vector3 neutralViewingDirection = Vector3.Cross(worldPitchAxis, yawAxis);
            // The error is the angle between the current viewing direction and the neutral viewing direction, relative in the yaw axis
            error = Vector3.SignedAngle(viewingDirection, neutralViewingDirection, yawAxis) + (targetAngle - 0.5f) * 23;
            // The yawing range is (0,0) and (1,1), the neutral position is (0.5,0.5)
            // The error is normalized to the range (-0.5,0.5), then shifted to the range (0,1)
            // targetAngle is mapped from the error of (-90,90) to (0,1)
            // Debug.Log(); // -180 ~ +180
            targetAngle = (error / 90 + 0.5f);
            // Clamp the angle between 0 an 1
            targetAngle = Mathf.Clamp(targetAngle, 0, 1);
            // Now the problem is that we don't want the angle to change too fast

            // Set the yaw angle of the motors
            dOFCommunication.SetMotorPos(targetAngle, targetAngle);

            // pitch
            float pitchAngle = Vector3.SignedAngle(yawAxis, worldYawAxis, worldPitchAxis);
            PitchCommand(pitchAngle);
            // visualize the yaw axis (green) and neutral direction (blue) during the game
            Debug.DrawRay(headAnchor.position, yawAxis * 500, Color.green);
            Debug.DrawRay(headAnchor.position, viewingDirection * 500, Color.blue);
            Debug.DrawRay(headAnchor.position, neutralViewingDirection * 500, Color.black);
            Debug.DrawRay(headAnchor.position, worldPitchAxis * 500, Color.red);
            prevYawAxis = yawAxis;
            prevViewingDirection = viewingDirection;
            Debug.Log("error: " + error.ToString() + "pitchAngle: " + pitchAngle.ToString());
        }
    }
    public void PitchCommand(float pitch)
    {
        // Set the pitch angle of the motors
        // p(command) = 125 * (11 - 7.5f/(tan(pitch - tan^-1(7.5/11))), v1
        pitch = Mathf.Clamp(pitch, -45, 45);
        int targetMotor = (int)(1250 * (1 - Mathf.Tan((pitch + 45) * Mathf.Deg2Rad)));
        Debug.Log("targetMotor: " + targetMotor.ToString());
        arduinoCommunication.targetMotor = targetMotor;
    }
    public void SetYawAngle(float angle)
    {
        // Set the yaw angle of the motors
        dOFCommunication.SetMotorPos(angle, angle);
    }
}
