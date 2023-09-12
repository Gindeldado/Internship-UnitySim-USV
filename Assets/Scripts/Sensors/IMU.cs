using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace IMUcalc {
    public class IMU : MonoBehaviour
    {
        public static Vector3 acceleration;
        Vector3 lastLocalVelocity;
        Vector3 lastRotation;
        public static Vector3 angVel;
        public static Vector3 localVelocity;
        public static double abs_pressure;
        public static float temperature;

        private float prevTime;
        private Stopwatch stopWatch;
        //private float prevTime;
        //private float timeDiff;
        //Vector3 lastAngVel;

        void Start() {
            lastLocalVelocity = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity);
        }

        
        void FixedUpdate() {
            localVelocity = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity);
            acceleration = (localVelocity - lastLocalVelocity) / Time.fixedDeltaTime;
            lastLocalVelocity = localVelocity;

            //angVel = (GetComponent<Rigidbody>().transform.eulerAngles - lastRotation) / Time.fixedDeltaTime;
            //lastRotation = GetComponent<Rigidbody>().transform.eulerAngles;

            angVel = transform.InverseTransformDirection(GetComponent<Rigidbody>().angularVelocity);// / Time.fixedDeltaTime;
            //angVel = GetComponent<Rigidbody>().angularVelocity;
            //lastAngVel = GetComponent<Rigidbody>().angularVelocity;

            //Debug.Log(acceleration + " " + angVel);
        }
    
        
        /*
        public Tuple<Vector3, Vector3> CalculateIMU() {
            //float timeDiff = stopWatch.ElapsedMilliseconds - prevTime;
            float timeDiff = 0.004f;

            localVelocity = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity);
            acceleration = (localVelocity - lastLocalVelocity) / timeDiff;
            lastLocalVelocity = localVelocity;

            //angVel = (GetComponent<Rigidbody>().transform.eulerAngles - lastRotation) / Time.fixedDeltaTime;
            //lastRotation = GetComponent<Rigidbody>().transform.eulerAngles;

            angVel = transform.InverseTransformDirection(GetComponent<Rigidbody>().angularVelocity);
            //lastAngVel = GetComponent<Rigidbody>().angularVelocity;

            //Debug.Log(acceleration + " " + angVel);
            //prevTime = stopWatch.ElapsedMilliseconds;

            Tuple<Vector3, Vector3> output = new Tuple<Vector3, Vector3>(acceleration, angVel);

            return output;
        }
        */
        
    }
}