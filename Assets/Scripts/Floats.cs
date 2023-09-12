using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MSG;

namespace CustomPhysics {
public class Floats : MonoBehaviour
{
    #region private members
    private static Rigidbody boat;
    private GameObject water;
    private GameObject floatFR;
    private GameObject floatFL;
    private GameObject floatRR;
    private GameObject floatRL;
    private GameObject bowThruster;
    private GameObject thrusterL;
    private GameObject thrusterR;
    private Vector3 forceDirection;
    private float waterLevel;
    private float forceFR;
    private float forceFL;
    private float forceRR;
    private float forceRL;
    private float depthFR;
    private float depthFL;
    private float depthRR;
    private float depthRL;
    [SerializeField] private float thrust = 10f;
    private float boatVolume;
    private float waterDensity = 1000f;
    private float[] controls;
    private static Vector3 dragDirection;
    private static Vector3 dragForces;
    #endregion


    void Start()
    {
        boat = GetComponent<Rigidbody>();
        water = GameObject.Find("WaterSurface");
        floatFR = GameObject.Find("FloatFR");
        floatFL = GameObject.Find("FloatFL");
        floatRR = GameObject.Find("FloatRR");
        floatRL = GameObject.Find("FloatRL");
        bowThruster = GameObject.Find("BowThruster");
        thrusterL = GameObject.Find("ThrusterL");
        thrusterR = GameObject.Find("ThrusterR");

        waterLevel = water.transform.position.y;

        forceDirection = new Vector3 (0f, 1f, 0f);

        boatVolume = boat.transform.localScale[0] * boat.transform.localScale[1] * boat.transform.localScale[2];

        Physics.IgnoreCollision(transform.GetComponent<Collider>(), water.GetComponent<Collider>());
    }

    public static void SideDrag() {
        float drag = 0.5f;
        Vector3 localVel = boat.transform.InverseTransformDirection(boat.velocity);
        localVel.x *= 1.0f - drag;
    }

    void FixedUpdate()
    {
        if (TCPserver.lockstep_initialized) {
            controls = MavMsgs.controls;
            
            for (int i = 0; i < 3; i++) {
                if (-1.1f < controls[i] && controls[i] < 1.1f) {
                    controls[i] = MavMsgs.controls[i];
                }
                else {
                    controls[i] = 0.0f;
                }
            }
        }

        if (floatFR.transform.position.y < waterLevel)
        {
            depthFR = waterLevel - floatFR.transform.position.y;
            forceFR = depthFR * -Physics.gravity.y * boatVolume * waterDensity / 20; // / 4;
            boat.AddForceAtPosition(forceDirection * forceFR, floatFR.transform.position);
        }

        if (floatFL.transform.position.y < waterLevel)
        {
            depthFL = waterLevel - floatFL.transform.position.y;
            forceFL = depthFL * -Physics.gravity.y * boatVolume * waterDensity / 20; // / 4;
            boat.AddForceAtPosition(forceDirection * forceFL, floatFL.transform.position);
        }

        if (floatRR.transform.position.y < waterLevel)
        {
            depthRR = waterLevel - floatRR.transform.position.y;
            forceRR = depthRR * -Physics.gravity.y * boatVolume * waterDensity / 20; // / 4;
            boat.AddForceAtPosition(forceDirection * forceRR, floatRR.transform.position);

            // actuator msg controls
            if (TCPserver.lockstep_initialized) {
                boat.AddForceAtPosition(boat.transform.forward * controls[1] * thrust, thrusterR.transform.position);
            }

            
            if (Input.GetKey("left"))
            {
                boat.AddForceAtPosition(boat.transform.forward * thrust, floatRR.transform.position);
            }

            if (Input.GetKey("up") && !Input.GetKey("left") && !Input.GetKey("right"))
            {
                boat.AddForceAtPosition(boat.transform.forward * thrust, floatRR.transform.position);
            }

            if (Input.GetKey("down") && !Input.GetKey("left") && !Input.GetKey("right"))
            {
                boat.AddForceAtPosition(-boat.transform.forward * thrust, floatRR.transform.position);
            }
        }

        if (floatRL.transform.position.y < waterLevel)
        {
            depthRL = waterLevel - floatRL.transform.position.y;
            forceRL = depthRL * -Physics.gravity.y * boatVolume * waterDensity / 20; // / 4;
            boat.AddForceAtPosition(forceDirection * forceRL, floatRL.transform.position);

            // actuator msg controls
            if (TCPserver.lockstep_initialized) {
                boat.AddForceAtPosition(boat.transform.forward * controls[0] * thrust, thrusterL.transform.position);
            }

            
            if (Input.GetKey("right"))
            {
                boat.AddForceAtPosition(boat.transform.forward * thrust, floatRL.transform.position);
            }

            if (Input.GetKey("up") && !Input.GetKey("left") && !Input.GetKey("right"))
            {
                boat.AddForceAtPosition(boat.transform.forward * thrust, floatRL.transform.position);
            }

            if (Input.GetKey("down") && !Input.GetKey("left") && !Input.GetKey("right"))
            {
                boat.AddForceAtPosition(-boat.transform.forward * thrust, floatRL.transform.position);
            }
        }

        if (bowThruster.transform.position.y < waterLevel) {
            // actuator msg controls
            if (TCPserver.lockstep_initialized) {
                boat.AddForceAtPosition(-boat.transform.right * controls[2] * thrust, bowThruster.transform.position);
            }
        }
    }
}
}
