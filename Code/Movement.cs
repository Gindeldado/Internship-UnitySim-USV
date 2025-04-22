using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Threading;
using UnityEngine.Profiling;

using GPS;
using TMPro;



public class Movement : MonoBehaviour
{
    // Start is called before the first frame update
    public float simulationStep =0.02f;
    public Rigidbody boat;
    public Transform waterPlane; 
    public Transform leftThruster,rightThruster, bowThruster;
    /// <summary> Reference to Connection script, which handles communication with px4</summary>
    public Connection Connection;
    /// <summary> Decides if simulation may take a step</summary>
    public static bool updateSimulation = false; 

    Quaternion localRotationQ;
    Vector3 localRotationE;
    Vector3 localAngularRotation;

    float u_n, v_n, r_n;
    public Disturbances Disturbance;
    public ulong simulationTime;
    VesselModel currentUSV;
    public GameObject lidar;
    public ROSTf2Publisher ros2Pub;
    
    void Start()
    {
        //Get first active rigidbody from children of VESSEL obj in hierarchy
        boat = GetComponentInChildren<Rigidbody>();
        //Water plane object, which will be used to 
        waterPlane = GameObject.Find("water").GetComponent<Transform>();

        //Default model uses boxcollider, the others use meshcollider
        var col = boat.GetComponentInChildren<Collider>();
        if(boat.GetComponentInChildren<BoxCollider>() == null)
            col = boat.GetComponentInChildren<MeshCollider>();
        //scenario edit mode uses this collider, but the boat should ignore it! 
        Physics.IgnoreCollision(col, GameObject.Find("water").GetComponent<Collider>()); 

        //Get first active vessel model in hierachy under VESSEL and use it as vessel
        currentUSV =  GetComponentInChildren<VesselModel>();
        leftThruster = currentUSV.thruster0;
        rightThruster = currentUSV.thruster1;
        bowThruster = currentUSV.thruster2;

        //Initialize disturbance object
        Disturbance = new Disturbances(); 
        Disturbance.SetBoat(boat,currentUSV.disLen);
        
        //Attach lidar to active vessel
        lidar.transform.parent = boat.transform;
        //Attach ROS2 publisher and set vessel(body) transform 
        ros2Pub = GetComponentInChildren<ROSTf2Publisher>();
        ros2Pub.body = boat.transform.gameObject;
    } 

    /// <summary>
    /// Applies force at thruster positon
    /// </summary>
    public void ThrusterControl()
    {
        boat.AddForceAtPosition(leftThruster.forward * Connection.controls[0] * currentUSV.thruster0Max, leftThruster.transform.position);
        boat.AddForceAtPosition(rightThruster.forward * Connection.controls[1] * currentUSV.thruster1Max, rightThruster.transform.position);
        boat.AddForceAtPosition(-bowThruster.right * Connection.controls[2] * currentUSV.thruster2Max, bowThruster.transform.position);         
    }

/// <summary>
/// Called every Unity fixed update.
/// </summary>
    private void FixedUpdate() { 
        //Updates simulation
        if(updateSimulation){
            //Takes care of grafity, it should only be  
            boat.useGravity = true;

            simulationTime = Connection.prev_simulation_time;
            Physics.Simulate(simulationStep); //step = 0.02 sec= 20ms = 20.000us
            ThrusterControl(); 

            //calculate dynamics and apply them
            CalculateDynamicsPara();
            currentUSV.Apply(u_n, r_n,v_n,localRotationE.z, localAngularRotation.z ,localRotationE.x ,localAngularRotation.x);

            Disturbance.Apply(localRotationE.y); 
            
            waterPlane.position = new Vector3(boat.position.x, waterPlane.position.y, boat.position.z); 
            
            IMU.UpdateSensor();  
            GPSClass.UpdateSensor();
            
            updateSimulation = false;
            Connection.sendMessage = true;
        }           
    }

/// <summary>
/// Calculate parameters used for dynamics
/// </summary>
    public void CalculateDynamicsPara(){
        //Rotation and angular rotation of boat
        localAngularRotation = boat.transform.InverseTransformDirection(boat.angularVelocity);

        localRotationQ = boat.transform.localRotation;
        localRotationE = localRotationQ.eulerAngles;
        localRotationE.x = MapToMinus180To180(localRotationE.x) * Mathf.Deg2Rad;
        localRotationE.y = MapToMinus180To180(localRotationE.y) * Mathf.Deg2Rad;
        localRotationE.z = MapToMinus180To180(localRotationE.z) * Mathf.Deg2Rad;

        //Directions of boat converted from world to loacl space
        u_n = boat.transform.InverseTransformDirection(boat.velocity).z;            //forward motion, surge, z-axis
        r_n = localAngularRotation.y;                                               //roational motion, yaw, rotation around y-axis
        v_n = boat.transform.InverseTransformDirection(boat.velocity).x;            //lateral motion, sway, x-axis

        //Not to give it a very smal decimal
        if (Mathf.Abs(u_n) < 0.0001f)
        {
            u_n = 0;
        }
        if (Mathf.Abs(r_n) < 0.0001f)
        {
            r_n = 0;
        }
        if (Mathf.Abs(v_n) < 0.0001f)
        {
            v_n = 0;  
        }
    }

/// <summary>
/// Map angle between -180 - 180
/// </summary>
/// <param name="angle">angle, 0-360</param>
/// <returns></returns>
    float MapToMinus180To180(float angle)
    {
        float mappedAngle = angle % 360f;
        if (mappedAngle >= 180f)
        {
            mappedAngle -= 360f;
        }
        return mappedAngle;
    }

}
