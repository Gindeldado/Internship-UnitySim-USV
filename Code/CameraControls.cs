using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraControls : MonoBehaviour
{
    // Start is called before the first frame update
    private GameObject cam;
    public bool rotationLocked = true;

    public Vector3 startPositionCam1;
    public Vector3 startPositionCam2;
    public Quaternion startRotatonCam1;
    public Quaternion startRotatonCam2;

    public GameObject cam1;
    Camera birdCam;
    public GameObject cam2;

    public float speed = 2.5f; 

    public bool cameraLocked;
    public bool cameraLockedOnShip;

    public Vector3 offsetCam;
    public Vector3 defaultLockPos;
    public int currentCam= 0; 

    public Movement boatPosition;
    void Start()
    {
        startPositionCam1 = cam1.transform.position;
        startPositionCam2 = cam2.transform.position;

        startRotatonCam1 = cam1.transform.rotation;
        startRotatonCam2 = cam2.transform.rotation;
        cam = cam1;
        birdCam = cam1.GetComponent<Camera>();
        // var go = boatPosition.GetComponentInChildren<Transform>();
        // boatPosition = go.gameObject;
        defaultLockPos = new Vector3(-1, 1, -3);
    }

    // Update is called once per frame
    void Update()
    {
        //Using 3rd person cam, turning camera
        if(Input.GetKey(KeyCode.LeftControl) && !rotationLocked){
            if (Input.GetKey(KeyCode.UpArrow)) 
            {
                cam.transform.Rotate(-Time.deltaTime * (speed * 5), 0,0, Space.Self);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                cam.transform.Rotate(Time.deltaTime* (speed * 5),0,0, Space.Self);
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                cam.transform.Rotate(0, -Time.deltaTime* (speed * 5), 0, Space.World);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                cam.transform.Rotate(0, Time.deltaTime* (speed * 5), 0, Space.World);
            }
        }
        else
        {
            if(!rotationLocked)//Using 3rd person cam, moving camera
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    cam.transform.Translate(0, 0, Time.deltaTime * speed);
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    cam.transform.Translate(0, 0, -Time.deltaTime* speed);
                }   
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    cam.transform.Translate(-Time.deltaTime* speed, 0, 0);
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    cam.transform.Translate(Time.deltaTime* speed, 0, 0);
                }
                //Using 3rd person cam, zoom-in/out
                if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus) )
                {
                    cam.transform.Translate(0, Time.deltaTime* speed, 0, Space.World);
                }
                if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Equals))
                {
                    cam.transform.Translate(0, -Time.deltaTime* speed, 0, Space.World);
                }
            }
            else//Using bird(top-down/ortographic) cam, moving camera
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    cam.transform.Translate(0,  Time.deltaTime * speed,0);
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    cam.transform.Translate(0, -Time.deltaTime* speed,0);
                }   
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    cam.transform.Translate(-Time.deltaTime* speed, 0, 0);
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    cam.transform.Translate(Time.deltaTime* speed, 0, 0);
                }
                //Using bird(top-down/ortographic) zoom-in/out
                if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus) )
                {
                    // cam.transform.Translate(0, Time.deltaTime* speed, 0, Space.World);
                    birdCam.orthographicSize += Time.deltaTime* speed;
                }
                if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Equals))
                {
                    birdCam.orthographicSize += Time.deltaTime* -speed;
                }
            }
        }
        
        if(cameraLocked){
            if(currentCam == 1)
                cam2.transform.position = boatPosition.boat.position - offsetCam;
            if(currentCam == 0)
                cam1.transform.position = boatPosition.boat.position - offsetCam;
        }
        if(cameraLockedOnShip){
            cam2.transform.position = boatPosition.boat.position + offsetCam;
        }
        
    }

/// <summary>
/// Selects camera
/// </summary>
/// <param name="_cam">Camera component</param>
    public void SelectCamera(String _cam){
        if(_cam == "3rdPerson Camera"){
            cam = cam2;
            currentCam = 1;
            rotationLocked = false;
            cameraLocked = false;

            cam1.SetActive(false);
            cam2.transform.position = new Vector3(boatPosition.boat.position.x, boatPosition.boat.position.y+1, boatPosition.boat.position.z - 1.5f);
            cam2.transform.rotation = Quaternion.AngleAxis(10, Vector3.right);
            cam2.SetActive(true);
        }
        if(_cam == "Main Camera"){
            cam = cam1;
            currentCam = 0;
            rotationLocked = true;
            cameraLocked = false;
            
            cam1.transform.position = new Vector3(boatPosition.boat.position.x, cam1.transform.position.y, boatPosition.boat.position.z);
            cam1.SetActive(true);
            cam2.SetActive(false);
        }
    } 

/// <summary>
/// Lock camera on distance from vessel as offset
/// </summary>
    public void LockCamera(){
        
        if(!cameraLocked){
            cameraLockedOnShip = false;

            
            if(currentCam == 1) //lock 3rd person camera
                offsetCam =  boatPosition.boat.position - cam2.transform.position;  
            else                // lock bird cam
                offsetCam =  boatPosition.boat.position - cam1.transform.position;

            cameraLocked = true;
        }else{
            cameraLocked = false;
        }
    }

/// <summary>
/// Lock camera at standard position from ship.(Only in 3rd person mode)
/// </summary>
    public void LockOnShip(){
        if(!cameraLockedOnShip){
            cameraLocked = false;

            SelectCamera("3rdPerson Camera");
            cameraLockedOnShip = true;
            offsetCam = defaultLockPos; 
            cam2.transform.rotation = Quaternion.AngleAxis(10, Vector3.right);
        }
        else{
            cameraLockedOnShip = false;
        }
    }

/// <summary>
/// Reset camera positions and orientation.
/// </summary>
    public void ResetCameras(){
        if(cam == cam1)
        {
            cam.transform.position = startPositionCam1; //Maybe change startpos to boat position?
            cam.transform.rotation = startRotatonCam1;
        }
        if(cam == cam2)
        {
            cam.transform.position = startPositionCam2;
            cam.transform.rotation = startRotatonCam2;
        }
    }

/// <summary>
/// Reset only orientation of camera 
/// </summary>
    public void ResetRotation(){
        if(cam == cam2)
        {
            cam.transform.rotation = startRotatonCam2;
        }
    }
}
