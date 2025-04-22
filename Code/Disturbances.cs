using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;



public class Disturbances{

    //CUSTOM_FORCE
    public float forceMagnitude;
    public float forceAngle; //(with respect to)wrt north, deg
    public float forcePeriod; //for sinus wave, mili sec
    public float reductionFactor;
    public float forceOffsetLength; 

    //CUSTOM_MOMENT
    public float momentX;
    public float momentY;
    public float momentZ;

    //WIND
    public float sideWindForce;
    public float sideWindAngle; //wrt north, deg
    public float frontWindReductionFactor; 
    public float windOffsetLength;

    //CURREMT 
    public float sideCurrentForce;
    public float sideCurrentAngle; //wrt north, deg
    public float frontCurrentReductionFactor; 
    public float currentOffsetLength;

    Rigidbody boat;
    float vesselLen;

    float vesselYaw;

/// <summary>
/// Reset all fields 
/// </summary>
    public void OnReset(){
        forceMagnitude = 0;
        forceAngle = 0;
        forcePeriod = 0;
        reductionFactor = 0;

        momentX = 0;
        momentY = 0;
        momentZ = 0;

        sideWindForce = 0;
        sideWindAngle = 0;
        frontWindReductionFactor = 0;
        windOffsetLength = 0;

        sideCurrentForce = 0;
        sideCurrentAngle = 0;
        frontCurrentReductionFactor = 0;
        currentOffsetLength = 0;
        Apply(0);
    }

/// <summary>
/// Set the rigidbody and legnt of the boat, which are being used for applying disturbance forces.
/// </summary>
/// <param name="_boat">Rigidbody of vessel</param>
/// <param name="len">length of vessel</param>
    public void SetBoat(Rigidbody _boat, float len){
        boat = _boat;
        vesselLen = len;
    }

/// <summary>
/// Reduce force with respect to angle between heading and force angle.
/// </summary>
/// <param name="angle">angle wrt north</param>
/// <param name="frf">front reduction factor</param>
/// <returns></returns>
    float DetermineForceFactor(float angle, float frf){
        if(frf == 0)
            return 1;
            // determine alignment: 1 at 0 deg, 0 at 90 deg, 1 at 180 deg etc
        var alignment = Mathf.Abs(Mathf.Abs(WrapPiPi(angle - vesselYaw)) - Mathf.PI/2) * 2 / Mathf.PI;
        return 1 - alignment * frf / 100;
    }

/// <summary>
/// Wrap value between pi and 2pi
/// </summary>
/// <param name="val"></param>
/// <returns>wrapped value between pi-2pi </returns>
    float WrapPiPi(float val){
        if(val > Mathf.PI)
            val = val -2*Mathf.PI;
        else if(val < -1 * Math.PI)
            val = val + 2*Mathf.PI;
        return val;
    }

/// <summary>
/// Base function which calculates the disturbance force
/// </summary>
/// <param name="angle">angle wrt north, deg</param>
/// <param name="mag">magintude of force, N</param>
/// <param name="frf">front reduction factor, %</param>
/// <param name="offset">force impact offset wrt length, %</param>
/// <param name="periodMs">How long one period takes, ms</param>
/// <param name="sinWave">Turn on/off adding force as a sin wave</param>
    void CalculateExternalForce(bool sinWave, float periodMs, float offset, float mag, float frf, float ang) 
    {
        // Calculate boat length offset, offset can be between 100 and -100
        float boatLength = vesselLen/2;  
        float offsetLen = boatLength * offset / 100f;

        // Calculate force based on frorce strength and direction 
        Vector3 forceDirection = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
        Vector3 offsetPosition = boat.transform.position + boat.transform.forward * offsetLen;

        // Apply force in waves
        if(sinWave)
        {
            //How fast the force comes and goes.
            var sinFreqDisturbance = 2 * Mathf.PI * 1000 / periodMs;
            mag = mag * Mathf.Sin(sinFreqDisturbance * Time.realtimeSinceStartup);
        }

        //simpel force reduction implementation...
        // if(frf > 0){
        //     float reduceForce = mag / 100 * frf;
        //     mag = mag - reduceForce;
        // }
        mag = mag * DetermineForceFactor(ang, frf);
        Vector3 force = mag * forceDirection.normalized;
        boat.AddForceAtPosition(force, offsetPosition);
    }

/// <summary>
/// Custom force disturbance
/// </summary>
/// <param name="angle">angle wrt north, deg</param>
/// <param name="mag">magintude of force, N</param>
/// <param name="frf">front reduction factor, %</param>
/// <param name="offset">force impact offset wrt length, %</param>
/// <param name="periodMs">How long one period takes, ms</param>
/// <param name="sinWave">Turn on/off adding force as a sin wave</param>
    public void CustomForceVec(float angle, float mag, float frf, float offset, float periodMs, bool sinWave){
        CalculateExternalForce(sinWave, periodMs, offset, mag, frf, angle);
    }

/// <summary>
/// Custom moment disturbance
/// </summary>
/// <param name="momentX">moment around x-axis, Nm</param>
/// <param name="momentY">moment around y-axis, Nm</param>
/// <param name="momentZ">moment around z-axis, Nm</param>
    public void CustomMomentVec(float _momentX, float _momentY, float _momentZ){        
        boat.AddRelativeTorque(_momentX,_momentY,_momentZ);
    }

/// <summary>
/// Wind disturbance
/// </summary>
/// <param name="angle">angle wrt north, deg</param>
/// <param name="mag">magintude of force, N</param>
/// <param name="frf">front reduction factor, %</param>
/// <param name="offset">force impact offset wrt length, %</param>
    public void CustomWindVec(float angle, float mag, float frf, float offset){
        CalculateExternalForce(false, 0, offset, mag, frf, angle);
    }

/// <summary>
/// Current disturbance
/// </summary>
/// <param name="angle">angle wrt north, deg</param>
/// <param name="mag">magintude of force, N</param>
/// <param name="frf">front reduction factor, %</param>
/// <param name="offset">force impact offset wrt length, %</param>
    public void CustomCurrentVec(float angle, float mag, float frf, float offset){
        CalculateExternalForce(false, 0, offset, mag, frf, angle);
    }

/// <summary> 
/// Apply the disturbance to the boat
/// </summary>
/// <param name="yaw">Yaw of vessel between -180 <> 180, rad</param>
    public void Apply(float yaw)
    {
        vesselYaw = yaw;
        bool sinWave = false;
        if(forcePeriod > 0)
            sinWave = true;
        
        CustomForceVec(forceAngle, forceMagnitude, reductionFactor, forceOffsetLength,forcePeriod,sinWave);
        CustomWindVec(sideWindAngle, sideWindForce, frontWindReductionFactor, windOffsetLength);
        CustomCurrentVec(sideCurrentAngle, sideCurrentForce, frontCurrentReductionFactor, currentOffsetLength);
        CustomMomentVec(momentX, momentY, momentZ);  
    }
    
}
