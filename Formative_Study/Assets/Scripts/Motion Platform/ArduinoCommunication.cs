using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System;
using UnityEngine;
using System.Threading;

public class ArduinoCommunication : MonoBehaviour
{
    public string portName = "COM8";
    public int baudRate = 115200;
    const int motorRange = 2000;

    private SerialPort port;
    public int targetMotor = 0;
    int currentMotor = 0;
    float threshold = 15f;
    public float motorSpeed = 0.15f;
    Thread arduinoCommander;
    Thread arduinoReader;
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
    }
    private void Start()
    {
        arduinoCommander = new Thread(ArduinoCommand);
        arduinoCommander.Start();
        arduinoReader = new Thread(ArduinoRead);
        arduinoReader.Start();
    }
    protected void Command(char mode, int pos)
    {
        if (!port.IsOpen) return;
        port.WriteLine(mode + pos.ToString());
        Debug.Log(mode + pos.ToString());
    }

    // Update is called once per frame
    void ArduinoCommand()
    {
        while (true)
        {
            if (Math.Abs(currentMotor - targetMotor) > threshold)
            {
                currentMotor = (int)Mathf.Lerp(currentMotor, targetMotor, motorSpeed);
                currentMotor = Mathf.Clamp(currentMotor, -motorRange / 2, motorRange / 2);
                try
                {
                    Command('p', currentMotor);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed sending command. Log: \n{e}");
                }
            }
            else
            {
                Command('s', 0);
            }
            Thread.Sleep(100); // 100 ms
        }
    }
    void ArduinoRead()
    {
        while (true)
        {
            try
            {
                string message = port.ReadLine();
                Debug.Log(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed reading message. Log: \n{e}");
            }
        }
    }
    void OnDestroy()
    {
        arduinoCommander.Abort();
        arduinoReader.Abort();
        Command('p', 0);
        if (port.IsOpen)
        {
            port.Close();
        }
    }
}
