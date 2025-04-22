using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Unity.Robotics.ROSTCPConnector;


public class UIManager : MonoBehaviour
{
    public TMP_Text infoTxt0;
    public TMP_Text infoTxt1;
    public TMP_Text txt_info1;
    public TMP_Text txt_info2;
    public TMP_Text txt_info3;

    public GameObject popUpMessagePrefab;
    public string communicationTime;
    public ulong realTime;          //microseconds
    public ulong simulationTime;    //microseconds
    public ulong prevSimulationTime;    //microseconds
    public float dt;

    public Connection connectionScript;
    public CameraControls CameraScript;
    public bool simulationPaused;
    public string mode = "simulation";

    #region Panels - 
    public GameObject panelTools;
    public GameObject panelBrushs;
    public GameObject panelMENU;
    public GameObject panelCamera;
    public GameObject panelDisturbance;

    public GameObject settingsButtons;
    #endregion

    public Slider slider;

    public bool ros2LidarEnabledGUI = false;

    //To prevent regeistering a clik when it was on UI
    public bool clicked;
    //holds data for which action we use the mouse
    public enum State
    {
        DEFAULT,        //waiting state
        SIMULATION,     //in simulation mode
        SCENARIO,       //in scenario mode
        PLACING_POINTS, //for making points
        DELETE,         //for DELETEING objects
        CAMERA_SETTINGS,//in camera settings panel
        DISTURBANCE,
    }
    //Hold for which state the mouse is being used 
    public State currentState;
/// <summary>
/// State of simulation
/// </summary>
    public enum ConnectionState
    {
        NOT_CONNECTED,
        LOST_CONNECTION,
        CONNECTED,
        INITIALISE_LOCKSTEP,
        LOCKSTEP,
        PAUSED,
    }
    public ConnectionState connectionState;
    public bool timerOn = true;
    private bool isConnected;
    public Movement movement;
    public List<DisturbanceField> disturbanceList;

/// <summary>
/// A struct used for showing help messages
/// </summary>
    public struct HelpMessage
    {
        public string title;
        public string message;
        public bool seen;
    }
    HelpMessage scenarioHelp = new HelpMessage();
    HelpMessage disturbancesHelp = new HelpMessage(); 
    HelpMessage cameraHelp = new HelpMessage();
    HelpMessage startInfoHelp = new HelpMessage();
    HelpMessage controlsInfoHelp = new HelpMessage();

    public bool publishRosMsg = false; 
    ROSConnection rosConnector;

    public GameObject[] Vessels;
    
    void Awake()
    {
        CameraScript = GameObject.Find("CameraScript").GetComponent<CameraControls>();
        rosConnector = ROSConnection.GetOrCreateInstance();

        slider.value = CameraScript.speed;

        CreateHelpMessages();
        ReturnToSimulation();

        PopUpMsg(controlsInfoHelp.title, controlsInfoHelp.message);
        PopUpMsg(startInfoHelp.title, startInfoHelp.message);

        //Get argument from start command
        var arg = System.Environment.GetCommandLineArgs();
        string s = "";
        bool lidarOff = false;
        for (int i = 0; i < arg.Length; i++)
        {
            s += arg[i] + ", ";
            if(arg[i].StartsWith("-nolidar"))
            {
                Debug.Log("[UnitySim] Lidar is offline");
                lidarOff = true;
            }
            else if(arg[i].StartsWith("-Default"))
            {
                Debug.Log("[UnitySim] Vessel Model: Default");
                TurnOffAllVessels();
                Vessels[0].SetActive(true);
            }
            else if(arg[i].StartsWith("-Lorentz")){
                Debug.Log("[UnitySim] Vessel Model: Lorentz");
                TurnOffAllVessels();
                Vessels[1].SetActive(true);
            }
            else if(arg[i].StartsWith("-V2500")){
                Debug.Log("[UnitySim] Vessel Model: V2500");
                TurnOffAllVessels();
                Vessels[2].SetActive(true);
            }
                       
        }
        
        //turning lidar on 
        if(!lidarOff)
            BUTTONros2();          
    }

    void TurnOffAllVessels(){
        foreach (var vessel in Vessels)
        {
            vessel.SetActive(false);
        }
    }

    void CreateHelpMessages(){
        //For general start info
        startInfoHelp.title = "Welcome to the Unity USV Simulation, UnitySim!";
        startInfoHelp.message =
        "Within this application, you can observe the USV from various viewpoints, "+
        "easily adjustable through the camera settings menu.\n"+
        "Explore the scenario edit mode in settings to create obstacles.\n" +
        "Utilize the disturbance menu to apply three force and one moment disturbance.\n" +
        "Enable ros2 Lidar for obstacle avoidance.";

        //For controls info
        controlsInfoHelp.title = "How to control the camera";
        controlsInfoHelp.message = 
        "You are now in top-down view. Here you can navigate using the arrow keys to move the camera.\n"+
        "Zoom in and out with the plus and minus keys.\n\n"+
        "In 3rd person view use the arrow keys to move around, then press the left control key to rotate.";

        //For scenario mode 
        scenarioHelp.title = "How to create and delete obstacles";
        scenarioHelp.message = "To create a obstacle click [Make] > [Land shape].\n" + 
        "Now you can use left mouse button on water to create points for your polygon shape.\n"+
        "You can undo a placed point with your right mouse button, " +
        "when your done press enter to place the polygon obstacle.\n" + 
        "To delete a obstacle click [Make] > [Delete].\n" +
        "Now you can click on the obstacles you have placed to delete them ";

        //for disturbances 
        disturbancesHelp.title = "How to apply disturbances";
        disturbancesHelp.message = "Fill numbers into the input fields.\n" +
        "When you are done the disturbances wil be applied when you press [Update].\n\n" +
        "North direction is to the right.\n" +
        "Moment Z on body is rotating around forward direction(roll).\n" +
        "Moment x on body is rotating around right direction(pitch).\n" +
        "Moment y on body is rotating around up direction(yaw).\n\n" +
        "Current force and Wind force are the same as Force,\n"+
        "but they give you the option to create multiple forces from diffrent direction, magnitude etc.\n"+
        "[Reset] - Remove all disturbances from vessel.\n\n";

        //for camera
        cameraHelp.title = "Camera settings information";
        cameraHelp.message =
        "[Top-down] - Change to a bird view perspective.\n" +
        "[Third person] - Change to a third person perspective.\n\n" +
        "[Lock cam on ship] - To make the camera follow the ship with the default offset click.\n" +
        "[Lock cam on ship] - To make the camera follow the ship with the distant the camera has now from the ship as offset.\n\n" +
        "[Reset] - Reset position and rotation of the camera\n"+
        "[Reset rotation] - Reset rotation of 3rd person perspective camera";
    } 

    /// <summary>
    /// Update loop function, Displays connection state and checks mouse state
    /// </summary>
    void Update()
    {
        switch (connectionState)
        {
            case ConnectionState.NOT_CONNECTED:
                infoTxt0.text = "Px4 is not conected - Connecting...";
                break;
            case ConnectionState.CONNECTED:
                infoTxt0.text = "Px4 is conected";
                break;
            case ConnectionState.INITIALISE_LOCKSTEP:
                infoTxt0.text = "Px4 is conected - Initialising lockstep...";
                break;
            case ConnectionState.LOCKSTEP:
                infoTxt0.text = "Px4 is conected - Lockstep Initialised";
                isConnected = true;
                break;
            case ConnectionState.LOST_CONNECTION:
                infoTxt0.text = "Px4 is not conected - Lost connection\nrestart UnitySim!";
                isConnected = false;
                break;
            case ConnectionState.PAUSED:
                infoTxt0.text = "Simulation is paused";
                break; 
            default:
                infoTxt0.text = "Px4 is not conected";
                break;
        }   

        switch (currentState)
        {
            case State.SCENARIO:
                //show tools
                panelMENU.SetActive(true);
                panelTools.SetActive(true);
                break;
            case State.SIMULATION:
                //close all menus and go to main menu 
                //reset all procceses in menus
                currentState = State.DEFAULT;
                break;
            default:
            break;
        }         
        DisplayRosState();
    }

/// <summary>
/// Create a pop up messages
/// </summary>
/// <param name="title">Header of the msg, which will be displayed at the top</param>
/// <param name="content">The text that wil be inside the box</param>
/// <returns></returns>
    public void PopUpMsg(string title, string content){
        GameObject msg = Instantiate(popUpMessagePrefab,this.gameObject.transform); 
        TMP_Text[] txtObjs = msg.GetComponentsInChildren<TMP_Text>();

        txtObjs[1].text = title;
        txtObjs[2].text = content;
    }

    /// <summary>
    /// Toggle main menu button onclick handler.
    /// </summary>
    public void BUTTONMenuToggle()
    {
        if(panelMENU.activeSelf){
            ReturnToSimulation();
            currentState = State.SIMULATION;
        }else{
            panelMENU.SetActive(true);
            settingsButtons.SetActive(true);
        }
    }

/// <summary>
/// Reset al UI panels 
/// </summary>
    void ReturnToSimulation()
    {
        settingsButtons.SetActive(false);
        panelTools.SetActive(false);
        panelBrushs.SetActive(false);
        panelMENU.SetActive(false);
        panelCamera.SetActive(false);
        panelDisturbance.SetActive(false);
        clicked = true;
    }

/// <summary>
/// Return to home settings menu
/// </summary>
    void ReturnToHome(){
        ReturnToSimulation();
        settingsButtons.SetActive(true);
        panelMENU.SetActive(true);
        currentState = State.DEFAULT;
        clicked = true;
    }

/// <summary>
/// Button handler which sets the given string inside the onclick() event 
/// of the button in the unity editor inspector.
/// to a Enum type to set the state.
/// </summary>
/// <param name="state">Type of state as string</param>
    public void BUTTONSetState(String state)
    {
        ReturnToSimulation();

        switch (state)
        {   
            case "SIMULATION":
                currentState = State.SIMULATION;
                if(isConnected)
                    connectionState = ConnectionState.LOCKSTEP;
                else
                    connectionState = ConnectionState.LOST_CONNECTION;
                simulationPaused = false; 
                
                break;
            case "SCENARIO":
                if(!scenarioHelp.seen){
                    PopUpMsg(scenarioHelp.title, scenarioHelp.message);
                    scenarioHelp.seen = true;
                }
                currentState = State.SCENARIO;
                connectionState = ConnectionState.PAUSED;
                simulationPaused = true;
                break;
            default:
            break;
        }
    } 
/// <summary>
/// Display brush panel, called from onclick() event inside Unity inspector Button component 
/// </summary>
    public void BUTTONmake(){
        ReturnToSimulation();
        panelMENU.SetActive(true);
        panelTools.SetActive(true);
        panelBrushs.SetActive(true);
        clicked = true;
    }
/// <summary>
/// Remove land shape, called from onclick() event inside Unity inspector Button component 
/// </summary>
    public void BUTTONdelete(){
        ReturnToSimulation();
        panelMENU.SetActive(true);
        panelTools.SetActive(true);
        currentState = State.DELETE;
        clicked = true;
    }
/// <summary>
/// Pause or play the simulation, called from onclick() event inside Unity inspector Button component 
/// </summary>
    public void BUTTONtogglePause(){
        ReturnToHome();
        panelMENU.SetActive(true);

        if(simulationPaused == false){
            connectionState = ConnectionState.PAUSED;
            Time.timeScale = 0;
            simulationPaused = true;
        }
        else{
            if(isConnected){
                connectionState = ConnectionState.LOCKSTEP;
                Time.timeScale = 1;
                simulationPaused = false;
            }
            else{
                connectionState = ConnectionState.LOST_CONNECTION;
                Time.timeScale = 1;
                simulationPaused = false;
            }
        }
        clicked = true;
    }

/// <summary>
/// Set brush,  called from onclick() event inside Unity inspector Button component 
/// </summary>
/// <param name="brush">0 = make shape</param>
    public void BUTTONbrush(int brush){
        if(brush == 0){
            currentState = State.PLACING_POINTS;
        }
        clicked = true;
    }

/// <summary>
/// Open camera settings<br/>  Called from onclick() event inside Unity inspector Button component 
/// </summary>
    public void BUTTONCamera(){
        ReturnToSimulation();
        panelMENU.SetActive(true);
        panelCamera.SetActive(true);
        currentState = State.CAMERA_SETTINGS;
        clicked = true;
        if(!cameraHelp.seen)
        {
            PopUpMsg(cameraHelp.title, cameraHelp.message);
            cameraHelp.seen = true;
        }
    }

/// <summary>
/// Return back to main menu <br/>  Called from onclick() event inside Unity inspector Button component.
/// </summary>
    public void BUTTONbackToMenu(){
       ReturnToHome();
    }


/// <summary>
/// Change camera view <br/>  Called from onclick() event inside Unity inspector Button component 
/// </summary>
/// <param name="perspective">0 = top-down<br/> 1 = 3rd person</param>
    public void BUTTONcameraView(int perspective){
        switch (perspective)
        {
            case 0:
                //top down
                CameraScript.SelectCamera("Main Camera");
            break;
            case 1:
                //third preson
                CameraScript.SelectCamera("3rdPerson Camera");
            break; 
            default:
                //not valid
            break;
        }
        clicked = true;
    }

/// <summary>
/// Update camera speed to slider value <br/>  Called from onValueChanged() event inside Unity inspector Slider component 
/// </summary>
    public void SLIDERcameraSpeed()
    {
        CameraScript.speed = slider.value;
    }

/// <summary>
/// Reset camera rotation and position
/// </summary>
    public void BUTTONresetCamera()
    {
        CameraScript.ResetCameras();
        clicked = true;
    }

/// <summary>
/// Reset camera rotation
/// </summary>
    public void BUTTONresetRotationCamera()
    {
        CameraScript.ResetRotation();
        clicked = true;
    }

/// <summary>
/// Lock 3rdPers camera position 
/// </summary>
    public void BUTTONLock3rdPersCamera()
    {
        CameraScript.LockCamera();
        clicked = true;
    }

/// <summary>
/// Lock 3rdPers camera position ON SHIP
/// </summary>
    public void BUTTONLock3rdPersCameraOnShip()
    {
        CameraScript.LockOnShip();
        clicked = true;
    }

/// <summary>
/// Update disturbance values <br/>  Called from onclick() event inside Unity inspector Button component 
/// </summary>
    public void BUTTONupdateDisturbance()
    {
        for (int i = 0; i < disturbanceList.Count; i++)
        {
            if(disturbanceList[i].input.text == "" 
            || disturbanceList[i].input.text[0] == 'm' 
            ||  disturbanceList[i].input.text[0] == 'N'
            ||  disturbanceList[i].input.text[0] == 'd' 
            ||  disturbanceList[i].input.text[0] == '%'
            )
                continue;

            float stringTofloat = 0;  
            var isANumb =   float.TryParse(disturbanceList[i].input.text ,out stringTofloat);

            switch (disturbanceList[i].id)
            {
                case 0:
                    movement.Disturbance.forceMagnitude = stringTofloat;
                    break; 
                case 1:
                    movement.Disturbance.forceAngle = stringTofloat;
                    break; 
                case 2:
                    movement.Disturbance.forcePeriod = stringTofloat;
                    break; 
                case 3:
                    movement.Disturbance.reductionFactor = stringTofloat;
                    break; 
                case 4:
                    movement.Disturbance.forceOffsetLength = stringTofloat; 
                    break; 

                case 5:
                    movement.Disturbance.momentX = stringTofloat;
                    break; 
                case 6:
                    movement.Disturbance.momentY = stringTofloat;
                    break; 
                case 7:
                    movement.Disturbance.momentZ = stringTofloat;
                    break; 

                case 8:
                    movement.Disturbance.sideWindForce = stringTofloat;
                    break; 
                case 9:
                    movement.Disturbance.sideWindAngle = stringTofloat; 
                    break; 
                case 10:
                    movement.Disturbance.frontWindReductionFactor = stringTofloat;
                    break; 
                case 11:
                    movement.Disturbance.windOffsetLength = stringTofloat; 
                    break; 

                case 12:
                    movement.Disturbance.sideCurrentForce = stringTofloat;
                    break; 
                case 13:
                    movement.Disturbance.sideCurrentAngle = stringTofloat; 
                    break; 
                case 14:
                    movement.Disturbance.frontCurrentReductionFactor = stringTofloat;
                    break; 
                case 15:
                    movement.Disturbance.currentOffsetLength = stringTofloat; 
                    break; 
                default:
                break;
            }
        }
    }

/// <summary>
/// Reset disturbance values <br/>  Called from onclick() event inside Unity inspector Button component 
/// </summary>
    public void BUTTONresetDisturbance(){
        movement.Disturbance.OnReset();
        foreach (var item in disturbanceList)
        {
            item.input.text = item.defaultValue;
        }
    } 

/// <summary>
/// Open disturbance settings<br/>  Called from onclick() event inside Unity inspector Button component 
/// </summary>
    public void BUTTONdisturbance(){
        ReturnToSimulation();
        panelMENU.SetActive(true);
        panelDisturbance.SetActive(true);
        currentState = State.DISTURBANCE;
        clicked = true;

        if(!disturbancesHelp.seen)
        {
            PopUpMsg(disturbancesHelp.title, disturbancesHelp.message);
            disturbancesHelp.seen = true;
        }
    }

    /// <summary>
    /// Toggle ros2<br/>  Called from onclick() event inside Unity inspector Button component 
    /// </summary>
    public void BUTTONros2(){
        ReturnToHome();
        clicked = true;

        if(!ros2LidarEnabledGUI)
        {
            rosConnector.Connect();
            ros2LidarEnabledGUI = true;
        }else{
            rosConnector.Disconnect();
            ros2LidarEnabledGUI = false;
        }
    }

/// <summary>
/// Display correct ros2 lidar state
/// </summary>
    void DisplayRosState()
    {
        if(!ros2LidarEnabledGUI)
        {
            infoTxt1.text = "Ros2 Lidar \t\t\t\t\tdisabled\nRos2 Unity TCP Endpoint node\t\tdisabled";
        }
        else if(ros2LidarEnabledGUI && rosConnector.HasConnectionError){
            infoTxt1.text = "Ros2 Lidar \t\t\t\t\tenabled\nRos2 Unity TCP Endpoint node\t\tdisabled";
        }
        else if(ros2LidarEnabledGUI && !rosConnector.HasConnectionError){
            infoTxt1.text = "Ros2 Lidar \t\t\t\t\tenabled\nRos2 Unity TCP Endpoint node\t\tenabled";
            publishRosMsg = true;
            return;
        }
        publishRosMsg = false;
    }

}
