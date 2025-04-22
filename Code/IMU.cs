using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class IMU : MonoBehaviour
    {
        public static Vector3 acceleration;
        public static Vector3 lastLocalVelocity;
        public static Vector3 lastRotation;
        public static Vector3 angVelocity;
        public static Vector3 localVelocity;
        public static Vector3 worldVelocity;
        public static Vector3 lastWorldVelocity;

        public static Quaternion rotation;
        static Rigidbody boat;

        static float deltaTime;

        void Start() { 
            boat = GetComponentInChildren<Rigidbody>();
            deltaTime = this.GetComponent<Movement>().simulationStep;
            UpdateSensor();
        }
        public static void UpdateSensor()
        {
            lastWorldVelocity = worldVelocity;

            //Acceleration in world frame
            worldVelocity =  boat.velocity;     //velocity in world frame
            var worldAcceleration = (worldVelocity - lastWorldVelocity) / deltaTime;
            worldAcceleration.y -= 9.81f;       //Adding Gravity 

            //Acceleration in local frame
            acceleration = boat.transform.InverseTransformDirection(worldAcceleration);

            //Gyro in local frame
            angVelocity = boat.transform.InverseTransformDirection(boat.angularVelocity);
            rotation = boat.transform.localRotation;
        } 
        // private void OnGUI() {
        //     GUI.color = Color.black;
        //     GUI.Label(new Rect(Screen.width - 300, Screen.height - 30, 300, 25), "Acceleration: " + acceleration); 
        //     GUI.Label(new Rect(Screen.width - 300, Screen.height-60, 300, 25), "World Velocity: " + worldVelocity);
        //     GUI.Label(new Rect(Screen.width - 300, Screen.height-90, 300, 25), "Local Velocity: " + localVelocity);
        //     GUI.Label(new Rect(Screen.width - 300, Screen.height-120, 300, 25), "Last Local Velocity: " + lastLocalVelocity);
        //     GUI.Label(new Rect(Screen.width - 300, Screen.height-150, 300, 25), "Last Rotation: " + lastRotation);
        // }
}