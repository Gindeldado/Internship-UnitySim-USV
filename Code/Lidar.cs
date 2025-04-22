using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class Lidar : MonoBehaviour {

    [SerializeField] private string topic = "/pointcloud";
    [SerializeField] private string frameId = "lidar";
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
    int floatArraySize = 6;
    int byteArraySize = 0;
    byte[] byteDataArray;

    public bool registerd;

    UIManager guiRos;
    void Start () {
        distances = new float[numberOfLayers* numberOfIncrements];
        azimuts = new float[numberOfIncrements];
        vertIncrement = (float)(maxAngle - minAngle) / (float)(numberOfLayers - 1);
        azimutIncrAngle = (float)(360.0f / numberOfIncrements);
        
        pointCloudFloats = new float[numberOfLayers * numberOfIncrements * floatArraySize];

        byteArraySize = numberOfLayers * numberOfIncrements * 29;
        byteDataArray = new byte[byteArraySize];
        pointCloudBytes = new byte[byteArraySize];

        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Ros.RegisterPublisher<PointCloud2Msg>(topic);


        guiRos = GameObject.Find("Canvas").GetComponent<UIManager>();
        
    }

      public void RegisterPublisher(){
        m_Ros.RegisterPublisher<PointCloud2Msg>(topic);
        registerd = true;
    }



void Update() {
    elapsed += Time.deltaTime;
    
    if(guiRos.publishRosMsg){
        if (elapsed >= 1/frequency) {
            elapsed = elapsed % 1/frequency;
            Scan();
        }  
    }
}


void Scan() {
        Vector3 fwd = new Vector3(0, 0, 1);
        Vector3 dir;
        Vector3 dirWorld;
        RaycastHit hit;
        int indx = 0;
        int count = 0;
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

                if (Physics.Raycast(transform.position, dirWorld, out hit, maxRange, ~(1 << 6)))//send raycast, but ignore layer 4(ignore lidar)[water and vessel]
                {
                    distances[indx] = (float)hit.distance;
                    // Debug.DrawLine(transform.position, hit.point, Color.red);
                }
                else
                {
                    // Debug.DrawRay(transform.position, dirWorld * 10.0f, Color.green);
                    distances[indx] = float.NaN;
                }

                //Populate byte array with fields, which will be sent to ros2
                Vector3 hitPointUnity = distances[indx] * dir;
                //Set hitpoint to correct coordinatesystem
                Vector3<FLU> hitPoint = hitPointUnity.To<FLU>();

                byte[] hitPointXBytes = BitConverter.GetBytes(hitPoint.x);
                byteDataArray[count] = hitPointXBytes[0];
                byteDataArray[count + 1] = hitPointXBytes[1];
                byteDataArray[count + 2] = hitPointXBytes[2];
                byteDataArray[count + 3] = hitPointXBytes[3];

                byte[] hitPointYBytes = BitConverter.GetBytes(hitPoint.y);
                byteDataArray[count + 4] = hitPointYBytes[0];
                byteDataArray[count + 5] = hitPointYBytes[1];
                byteDataArray[count + 6] = hitPointYBytes[2];
                byteDataArray[count + 7] = hitPointYBytes[3];

                byte[] hitPointZBytes = BitConverter.GetBytes(hitPoint.z);
                byteDataArray[count + 8] = hitPointZBytes[0];
                byteDataArray[count + 9] = hitPointZBytes[1];
                byteDataArray[count + 10] = hitPointZBytes[2];
                byteDataArray[count + 11] = hitPointZBytes[3];

                float intensity = 200;
                byte[] intensityBytes = BitConverter.GetBytes(intensity);
                byteDataArray[count + 12] = intensityBytes[0];
                byteDataArray[count + 13] = intensityBytes[1];
                byteDataArray[count + 14] = intensityBytes[2];
                byteDataArray[count + 15] = intensityBytes[3];

                UInt32 t = (UInt32) Clock.GetSimulationTimeSec();
                byte[] tBytes = BitConverter.GetBytes(t);
                byteDataArray[count + 16] = tBytes[0];
                byteDataArray[count + 17] = tBytes[1];
                byteDataArray[count + 18] = tBytes[2];
                byteDataArray[count + 19] = tBytes[3];

                UInt16 reflectivity = 2000; 
                byte[] reflectivityBytes = BitConverter.GetBytes(reflectivity);
                byteDataArray[count + 20] = reflectivityBytes[0];
                byteDataArray[count + 21] = reflectivityBytes[1];

                byte ring = (byte) (incr%numberOfLayers); // layers dus y-as, !is eigenlijk uint16...
                byteDataArray[count + 22] = ring;

                UInt16 ambient = 50;
                byte[] ambientBytes = BitConverter.GetBytes(ambient);
                byteDataArray[count + 23] = ambientBytes[0];
                byteDataArray[count + 24] = ambientBytes[1];

                float rangeMeters = Mathf.Sqrt(hitPoint.x * hitPoint.x + hitPoint.y * hitPoint.y + hitPoint.z * hitPoint.z);
                UInt32 range = (UInt32)(rangeMeters * 1000);
                byte[] rangeBytes = BitConverter.GetBytes(range);
                byteDataArray[count + 25] = rangeBytes[0];
                byteDataArray[count + 26] = rangeBytes[1];
                byteDataArray[count + 27] = rangeBytes[2];
                byteDataArray[count + 28] = rangeBytes[3];

                count += 29;
            }   
        }

        SendScanMessage(byteDataArray);
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
                },
                new PointFieldMsg
                {
                    name = "intensity",
                    //float32
                    datatype = 7,
                    count = 1,
                    offset = 12
                },
                new PointFieldMsg
                {
                    name = "t",
                    //uint32
                    datatype = 6,
                    count = 1,
                    offset = 16
                },
                new PointFieldMsg
                {           
                    name = "reflectivity",
                    //uint16
                    datatype = 4,
                    count = 1,
                    offset = 20
                },
                new PointFieldMsg
                {
                    name = "ring",
                    //uint16
                    datatype = 4,
                    count = 1,
                    offset = 22
                },
                new PointFieldMsg
                {
                    name = "ambient",
                    //uint16
                    datatype = 4,
                    count = 1,
                    offset = 23 
                },
                new PointFieldMsg
                {
                    name = "range",
                    //uint32
                    datatype = 6,
                    count = 1,
                    offset = 25
                }
            },
            point_step = 29,
            row_step = 29 * (uint)(numberOfLayers * numberOfIncrements),
            data = Data
        };

        m_Ros.Publish(topic, msg);
    }
}