using System;
using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI;
public class DOFController : MonoBehaviour
{
	public string portName = "COM7";
	public int baudRate = 500000;

	private SerialPort port;

	private byte[] message = { 0x5B, 0x41, 0x01, 0xFF, 0x5D, 0x5B, 0x42, 0x01, 0xFF, 0x5D, 0x5B, 0x43, 0x01, 0xFF, 0x5D };

	public Slider leftMotor, rightMotor;
	protected const int motorRange = 1023;
	int[] middleMotor = { motorRange/2, motorRange/2 };

	// Use this for initialization
	void Awake()
	{
		port = new SerialPort(portName, baudRate);
		try
		{
			port.Open();
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed opening port. Log: \n{e}");
		}

		if (!port.IsOpen) return;

		leftMotor.value = middleMotor[0];
		rightMotor.value = middleMotor[1];
		SetMotorAngles(middleMotor[0], middleMotor[1]);
	}
	protected void SetMotorAngles(int left, int right)
	{
		if (!port.IsOpen) return;
		message[2] = (byte)((int)left / 256);
		message[3] = (byte)((int)left % 256);
		message[7] = (byte)((int)right / 256);
		message[8] = (byte)((int)right % 256);

		port.Write(message, 0, 15);
	}

	public virtual void OnSliderChange()
	{
		Debug.Log("Slider changed");
		var lv = leftMotor.value;
		var rv = rightMotor.value;
		SetMotorAngles((int)lv, (int)rv);
	}
}
