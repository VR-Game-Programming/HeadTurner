using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientationUtility : MonoBehaviour
{
    // Using the CenterEyeAnchor under the CameraRig as the head anchor, or use OptiTrack RigidBody
    public Transform headAnchor;
    // yaw: up, pitch: right, roll: forward/viewing direction
    // direction: up +, left +, CW +
    Vector3 pitchAxis , yawAxis, rollAxis, calibratedPitchAxis, calibratedYawAxis, calibratedRollAxis, prevPitchAxis, prevYawAxis, prevRollAxis;
    float alpha = 0.2f;
    public int averagingFrames = 100;
    bool isCalibrated = false;
    public bool IsCalibrated{
        get{
            return isCalibrated;
        }
    }
    float rollAngle, pitchAngle, yawAngle;
    public float RollAngle{
        get{
            if(IsCalibrated){
                return rollAngle;
            }
            else{
                Debug.LogError("Roll: OrientationUtility is not calibrated, return 0 anyway");
                return 0;
            }
        }
    }
    public float PitchAngle{
        get{
            if(IsCalibrated){
                return pitchAngle;
            }
            else{
                Debug.LogError("Pitch: OrientationUtility is not calibrated, return 0 anyway");
                return 0;
            }
        }
    }
    public float YawAngle{
        get{
            if(IsCalibrated){
                return yawAngle;
            }
            else{
                Debug.LogError("Yaw: OrientationUtility is not calibrated, return 0 anyway");
                return 0;
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        if (headAnchor == null)
        {
            Debug.LogError("HeadAnchor is not set");
            // Quit
            Application.Quit();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // the problem is that the headAnchor is not updated in the Start stage
        // so the yawAxis and viewingDirection are not correct
        // so we need to update them in the Update stage, but only averaging once
        if (averagingFrames > 0)
        {
            calibratedPitchAxis += headAnchor.right;
            calibratedYawAxis += headAnchor.up;
            calibratedRollAxis += headAnchor.forward;
            averagingFrames -= 1;
        }
        else if (averagingFrames == 0)
        {
            calibratedPitchAxis.Normalize();
            calibratedYawAxis.Normalize();
            calibratedRollAxis.Normalize();

            prevPitchAxis = calibratedPitchAxis;
            prevYawAxis = calibratedYawAxis;
            prevRollAxis = calibratedRollAxis;

            averagingFrames -= 1;
            isCalibrated = true;
        }
        else
        {
            // cascading calculation of the model angles representing the head orientation
            pitchAxis = headAnchor.right*alpha + prevPitchAxis*(1-alpha);
            yawAxis = headAnchor.up*alpha + prevYawAxis*(1-alpha);
            rollAxis = headAnchor.forward*alpha + prevRollAxis*(1-alpha);
            // pitch
            pitchAngle = Vector3.SignedAngle(yawAxis, calibratedYawAxis, calibratedPitchAxis);
            //pitchAngle = Vector3.SignedAngle(yawAxis, calibratedYawAxis, pitchAxis);
            // yaw
            Vector3 neutralRollAxis = Vector3.Cross(calibratedPitchAxis, yawAxis);
            yawAngle = Vector3.SignedAngle(rollAxis, neutralRollAxis, yawAxis);
            // roll
            Vector3 neutralYawAxis = Vector3.Cross(rollAxis, calibratedPitchAxis);
            rollAngle = Vector3.SignedAngle(yawAxis, neutralYawAxis, rollAxis);
            
            // visualize the yaw axis (green) and neutral direction (blue) during the game
            Debug.DrawRay(headAnchor.position, yawAxis * 500, Color.green);
            Debug.DrawRay(headAnchor.position, rollAxis * 500, Color.blue);
            Debug.DrawRay(headAnchor.position, neutralRollAxis * 500, Color.black);
            Debug.DrawRay(headAnchor.position, neutralYawAxis * 500, Color.black);
            Debug.DrawRay(headAnchor.position, calibratedPitchAxis * 500, Color.red);
            prevYawAxis = yawAxis;
            prevRollAxis = rollAxis;
            prevPitchAxis = pitchAxis;
        }
    }
}
