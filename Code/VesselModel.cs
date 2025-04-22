using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VesselModel : MonoBehaviour
{
    public float thruster0Max;
    public float thruster1Max;
    public float thruster2Max;

    public Transform thruster0;
    public Transform thruster1;
    public Transform thruster2;

    float[] param;

    float mass;

    //inertia tensor...?

    //distances in m
    
    /// <summary> Length, m </summary>
    public float disLen;  
    /// <summary> com to stern, m </summary>
    float disLR; 
    /// <summary> port to starboard, m </summary>
    float disB; 
    /// <summary> com to bow, m </summary>
    float disLF;
    /// <summary> hull height, m</summary>
    float disH;
    /// <summary> volume below water, m3</summary>
    public float volumeBelow;
    /// <summary> Maximum possible bouyancy force, N</summary>
    float maxBouy;
    
    //Transient parameters

    /// <summary> Heave demping </summary>
    float heaveD;
    /// <summary> Roll gain </summary>
    float rollP;
    /// <summary> Roll demping </summary>
    float rollD;
    /// <summary> Pitch demping </summary>
    float pitchD;
    /// <summary> pitch gain </summary>
    float pitchP;
    /// <summary> sway gain </summary>
    float swayP;

    /// <summary> water density, kg/l </summary>
    float rho_water;

    public Rigidbody vessel;

    /// <summary>
    /// Vessel models
    /// </summary>
    public enum CurrentVesselModel
    {
        DEFAULT,
        V2500,
        LORENTZ
    }
    /// <summary>
    /// Current vessel model
    /// </summary>
    public CurrentVesselModel model;

    private void Start() {
        //Set vessel model
        switch (model)
        {
            case CurrentVesselModel.DEFAULT:
            SetDefault();
            break;
            case CurrentVesselModel.LORENTZ:
            SetLorentz();
            break;
            case CurrentVesselModel.V2500:
            SetLorentz();
            break;
        }   
    }

/// <summary>
/// Apply dynamics based on vessel
/// </summary>
/// <param name="u_n">Forward motion</param>
/// <param name="r_n">Rotational motion</param>
/// <param name="v_n">lateral motion</param>
/// <param name="roll">rotation around z-axis</param>
/// <param name="angRoll">angular velocity around z-axis</param>
/// <param name="pitch">rotation around x-axis</param>
/// <param name="angPitch">angular velocity around x-axis</param>
    public void Apply(float u_n, float r_n, float v_n, float roll, float angRoll, float pitch, float angPitch){
        switch (model)
        {
            case CurrentVesselModel.DEFAULT:
            DefaultVessel(u_n, r_n, v_n, roll, angRoll, pitch, angPitch);
            break;

            case CurrentVesselModel.V2500:
            LorentzVessel(u_n, r_n, v_n, roll, angRoll, pitch, angPitch);
            break;

            case CurrentVesselModel.LORENTZ:    //Inside Lorentz vessel object in hierarachy orientation of thruster are diffrent
            LorentzVessel(u_n, r_n, v_n, roll, angRoll, pitch, angPitch);
            break;
        }
    }

/// <summary>
/// Set vessel values to default model 
/// </summary>
    public void SetDefault(){
        thruster0Max =304; thruster1Max = 304; thruster2Max = 100;
        disLen = 1.15f;
        disLR = 0.8f;
        disB = 0.5f;
        disLF = disLen - disLR;
        disH = 0.3f;

        rho_water = 997;
        volumeBelow = disLen * disB * disH* 3/5;
        maxBouy = rho_water * volumeBelow * 9.81f;

        mass = 53.55f;
        vessel.mass = mass;
        
        rollP = 170;
        rollD = 17;
        pitchD = 50;
        pitchP= 600;
        swayP = 200;
        heaveD = 400;

    }

/// <summary>
/// Set vessel values to Lorentz model 
/// </summary>
    public void SetLorentz(){
        thruster0Max =1618; thruster1Max = 1618; thruster2Max = 430;
        disLen = 2.907f;
        disLR = 2.30f;
        disB = 0.844f;
        disLF = disLen - disLR;
        disH = 0.7f;

        rho_water = 997;
        volumeBelow = disLen * disB * disH* 3/5;
        maxBouy = rho_water * volumeBelow * 9.81f; 

        mass = 458f;
        vessel.mass = mass;

        rollP = 6000;
        rollD = 1000;

        pitchP= 10000;
        pitchD = 4000;

        heaveD = 400;

        param = new float[]
        {
            0.0f, 0.0f, 0.0f, 77.37589f, -203.50376f, 120.93821f, 26.40504f, -8.14479f, 90.26094f,
            -23.28294f, -357.11513f, 1491.24603f, 1893.5497f, -5003.98744f, 4942.59604f, 254.63612f,
            -1434.03423f, 128.42916f, 813.79808f, -30.07178f, 825.70945f
        };

    }

/// <summary>
/// Simple buoyancy force
/// </summary>
/// <returns></returns>
    float SimpleBuoyancyForce()
    {
        float vesselPosY = vessel.position.y;

        float forceBuoyancy;
        float D_factor;

        if(vesselPosY > disH/2){
            forceBuoyancy = 0;
            D_factor = 0;
        }
        else if(vesselPosY < -disH / 2){
            forceBuoyancy = maxBouy;
            D_factor = -heaveD * vessel.velocity.y;
        }else{
            forceBuoyancy = maxBouy * (-vesselPosY/disH + 0.5f);
            D_factor = -heaveD * vessel.velocity.y;
        }
        
        float buoyancyForceTotal = forceBuoyancy + D_factor;
        return buoyancyForceTotal;

    }

/// <summary>
/// Calculate and apply dynamics for Default vessel
/// </summary>
/// <param name="u_n">Forward motion</param>
/// <param name="r_n">Rotational motion</param>
/// <param name="v_n">lateral motion</param>
/// <param name="roll">rotation around z-axis</param>
/// <param name="angRoll">angular velocity around z-axis</param>
/// <param name="pitch">rotation around x-axis</param>
/// <param name="angPitch">angular velocity around x-axis</param>
    public void DefaultVessel(float u_n, float r_n, float v_n, float roll, float angRoll, float pitch, float angPitch)
    {
        //sway force lateral (x-as)
        float swayForceNetto = -swayP*v_n;  

         //roll restoring moment (z-as)
        float rollMomentNetto = -rollP * roll - rollD * angRoll + swayForceNetto * 0.5f;

        //pitch restoring moment (x-as)
        float pitchMomentNetto = -pitchP * pitch - pitchD * angPitch;

        //yaw force rotational (y-as)
        float yawMomentNetto = 73.56f*u_n*r_n/2 -243.85f *r_n  + 31.728f * Mathf.Pow(r_n,3);

        //surge force forward (z-as) 
        float surgeForceNetto = -(31.247f*u_n - 53.043f*Mathf.Pow(u_n,2)+ 48.154f*Mathf.Pow(u_n,3) + 73.56f*Mathf.Pow(r_n,2)/2);  
        
        //heave force upwards "bouyancy" (y-as)
        float heaveForceNetto = SimpleBuoyancyForce();

        vessel.AddForce(0,heaveForceNetto,0); 

        vessel.AddRelativeForce(swayForceNetto,0, surgeForceNetto);
        
        vessel.AddRelativeTorque(0, 0, rollMomentNetto);  
        vessel.AddRelativeTorque(pitchMomentNetto, 0, 0);
        vessel.AddRelativeTorque(0, yawMomentNetto, 0);
    }

/// <summary>
/// Calculate and apply dynamics for Lorentz vessel
/// </summary>
/// <param name="u_n">Forward motion</param>
/// <param name="r_n">Rotational motion</param>
/// <param name="v_n">lateral motion</param>
/// <param name="roll">rotation around z-axis</param>
/// <param name="angRoll">angular velocity around z-axis</param>
/// <param name="pitch">rotation around x-axis</param>
/// <param name="angPitch">angular velocity around x-axis</param>
    public void LorentzVessel(float u_n, float r_n, float v_n, float roll, float angRoll, float pitch, float angPitch)
    {
        //sway force lateral (x-as)
        float swayForceNetto = -param[19]*u_n - param[20] * v_n;  

        //roll restoring moment (z-as)
        float rollMomentNetto = -rollP * roll - rollD * angRoll + swayForceNetto * 0.5f;

        //pitch restoring moment (x-as)
        float pitchMomentNetto = -pitchP * pitch - pitchD * angPitch;

        //yaw force rotational (y-as)
        float yawMomentNetto =  - param[12] * r_n - param[13] * Mathf.Abs(r_n) * r_n - param[14] * r_n * r_n * r_n - param[15] * u_n * r_n +
                 - param[16] * u_n * Mathf.Abs(r_n) * r_n - param[17] * u_n * u_n * r_n - param[18] * u_n * u_n * Mathf.Abs(r_n) * r_n;


        //surge force forward (z-as) 
        float surgeForceNetto = - param[3] * u_n - param[4] * u_n * u_n - param[5] * u_n * u_n * u_n - param[6] * u_n * u_n * u_n * u_n - param[7] * u_n * u_n * u_n * u_n * u_n + 
                  - param[8] * r_n * u_n - param[9] * Mathf.Abs(r_n) * r_n * u_n - param[10] * r_n * u_n * u_n - param[11] * Mathf.Abs(r_n) * r_n * u_n * u_n;
  
        
        //heave force upwards "bouyancy" (y-as)
        float heaveForceNetto = SimpleBuoyancyForce();

        vessel.AddForce(0,heaveForceNetto,0); 

        vessel.AddRelativeForce(swayForceNetto,0, surgeForceNetto);
        
        vessel.AddRelativeTorque(0, 0, rollMomentNetto);  
        vessel.AddRelativeTorque(pitchMomentNetto, 0, 0);
        vessel.AddRelativeTorque(0, yawMomentNetto, 0);
    }

}
