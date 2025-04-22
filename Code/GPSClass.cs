using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace GPS {
    public class GPSClass : MonoBehaviour
    {
        public static double lat;
        public static double lon;
        public static UInt32 latInt;
        public static UInt32 lonInt;
        public static double originLat = 519969778e-7f;//51.99698483106159;
        public static double originLon = 43770921e-7f;//4.376641848733543;
        static Rigidbody body;
        public static Vector3 orientation;
        public static Vector3 compass_data;
        public static Vector3 position;
        public static Vector3 worldVelocity;

        void Start() {
            lat = 519969778e-7f;
            lon = 43770921e-7f;
            body = GetComponentInChildren<Rigidbody>();
            UpdateSensor();
        }

        public static void UpdateSensor()
        {
            // Compass
            orientation = body.transform.eulerAngles;

            position = body.position;
            worldVelocity = body.velocity;
            // float yaw = compass_euler_angles[2] + 0.5f * M_PI;
            float yaw = -orientation.y * 6.2831853f / 360;
            float length = 0.22f;

            compass_data[0] = (float)(length * Math.Cos(yaw) + get_noise(0.001f));
            compass_data[1] = (float)(length * Math.Sin(yaw) + get_noise(0.001f));
            compass_data[2] = (float)(-0.38f + get_noise(0.001f));
            
            // GPS
            float x = position.z;
            float y = -position.x;

            double lat0 = lat * Math.PI / 180.0;
            double lon0 = lon * Math.PI / 180.0; 
            double cos_lat0 = Math.Cos(lat0); 
            double sin_lat0 = Math.Sin(lat0);
            double r_earth = 6371000.0;
            double x_rad = x / r_earth;
            double y_rad = -y / r_earth;
            double c = Math.Sqrt(x_rad * x_rad + y_rad * y_rad);
            double sin_c = Math.Sin(c);
            double cos_c = Math.Cos(c);

            double lat_rad = 0.0;
            double lon_rad = 0.0;
            if(c != 0.0) {
                lat_rad = Math.Asin(cos_c * sin_lat0 + (x_rad * sin_c * cos_lat0) / c);
                lon_rad = (lon0 + Math.Atan2(y_rad * sin_c, c * cos_lat0 * cos_c - x_rad * sin_lat0 * sin_c));
            } else {
                lat_rad = lat0;
                lon_rad = lon0;
            } 

            latInt = (uint)(lat_rad*180/Math.PI * 10e6);
            lonInt = (uint)(lon_rad*180/Math.PI * 10e6);
        }

        public static float get_noise(float magnitude) {
            System.Random rand = new System.Random();
            float randomInt = rand.Next(1,101);
            float noise = (randomInt - 50.0f) / 100.0f * magnitude;
            return noise;
	    }
    }
}