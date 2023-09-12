using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.Core;

public class Lidar2 : MonoBehaviour {

    [SerializeField] private string topic = "LidarCloud";
    [SerializeField] private string frameId = "Stand";
    private ROSConnection m_Ros;
    private byte[] pointCloudBytes;
    [SerializeField] private float maxAngle = 22.5f;
    [SerializeField] private float minAngle = -22.5f;
    [SerializeField] private int numberOfLayers = 128;
    [SerializeField] private int numberOfIncrements = 360;
    [SerializeField] private float maxRange = 100.0f;
    float elapsed = 0f;
    [SerializeField] private float frequency = 10f;
    float vertIncrement;
    float azimutIncrAngle;
    [HideInInspector]
    public float[] distances;
    public float[] azimuts;
    private float[] pointCloudFloats;


    void Start () {
        distances = new float[numberOfLayers* numberOfIncrements];
        azimuts = new float[numberOfIncrements];
        vertIncrement = (float)(maxAngle - minAngle) / (float)(numberOfLayers - 1);
        azimutIncrAngle = (float)(360.0f / numberOfIncrements);
        
        pointCloudFloats = new float[numberOfLayers * numberOfIncrements * 3];
        pointCloudBytes = new byte[pointCloudFloats.Length * 4];
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Ros.RegisterPublisher<PointCloud2Msg>(topic);

        //InvokeRepeating("Scan", 5f, 0.1f);
    }



void Update() {
    elapsed += Time.deltaTime;
    if (elapsed >= 1/frequency) {
        elapsed = elapsed % 1/frequency;
        Scan();
    }  
}


void Scan() {
        Vector3 fwd = new Vector3(0, 0, 1);
        Vector3 dir;
        Vector3 dirWorld;
        RaycastHit hit;
        int indx = 0;
        float angle;

        //azimut angles
        for (int incr = 0; incr < numberOfIncrements; incr++)
        {
            for (int layer = 0; layer < numberOfLayers; layer++)
            {
                float layerAngle = minAngle + (float)layer * vertIncrement;
                //print("incr "+ incr +" layer "+layer+"\n");
                indx = layer + incr * numberOfLayers;
                angle = minAngle + (float)layer * vertIncrement;
                azimuts[incr] = incr * azimutIncrAngle;
                //dir = transform.rotation * Quaternion.Euler(-angle, azimuts[incr], 0) * fwd;
                dir = transform.InverseTransformDirection( Quaternion.Euler(-angle, azimuts[incr], 0) * fwd);

                dirWorld = Quaternion.Euler(-layerAngle, incr * azimutIncrAngle + azimutIncrAngle/2, 0) * Vector3.forward;
                // print("idx "+ indx +" angle " + angle + "  azimut " + azimut + " quats " + Quaternion.Euler(-angle, azimut, 0) + " dir " + dir+ " fwd " + fwd+"\n");

                //Debug.DrawRay(transform.position, dirWorld * 10.0f, Color.green);
                if (Physics.Raycast(transform.position, dirWorld, out hit, maxRange))
                {
                    distances[indx] = (float)hit.distance;
                }
                else
                {
                    //distances[indx] = 100.0f;
                    distances[indx] = float.NaN;
                }

                //Vector3 hitPoint = distances[indx] * maxRange * dir;
                Vector3 hitPoint = distances[indx] * dir;
                pointCloudFloats[indx * 3] = hitPoint.z;
                pointCloudFloats[indx * 3 + 1] = -hitPoint.x;
                pointCloudFloats[indx * 3 + 2] = hitPoint.y;
            }
        }

        Buffer.BlockCopy(pointCloudFloats, 0, pointCloudBytes, 0, pointCloudBytes.Length);
        SendScanMessage(pointCloudBytes);
    }


    //make our msg and publish it
    public void SendScanMessage(byte[] Data)
    {
        var timestamp = new TimeStamp(Clock.time);

        PointCloud2Msg msg = new PointCloud2Msg
        {
            header = new HeaderMsg
            {
                frame_id = frameId,
                stamp = new TimeMsg
                {
                    sec = timestamp.Seconds,
                    nanosec = timestamp.NanoSeconds,
                }
            },
            width = (uint)(numberOfLayers * numberOfIncrements),
            height = 1,
            is_bigendian = false, // false
            is_dense = false, // true
            fields = new PointFieldMsg[]
            { new PointFieldMsg
                {
                    name = "x",
                    //float32
                    datatype = 7,
                    count = 1,
                    offset = 0
                },
                new PointFieldMsg
                {
                    name = "y",
                    //float32
                    datatype = 7,
                    count = 1,
                    offset = 4
                },
                new PointFieldMsg
                {
                    name = "z",
                    //float32
                    datatype = 7,
                    count = 1,
                    offset = 8
                }
            },
            point_step = 12,
            row_step = 12 * (uint)(numberOfLayers * numberOfIncrements),
            data = Data
        };

        m_Ros.Publish(topic, msg);
    }
}