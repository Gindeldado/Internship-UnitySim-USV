# Internship: Creation of UnitySim - USV simulation in Unity  
#### Note: This repository contains only the code and documentation and has been simplified. It is NOT runnable in its current form. 
1. This repository can be opened as Unity project for developing purpose.<br>
2. The simulation can be started by running the `px4_unity.sh` start-up file in the px4-firmware repository.
 
## How to start the Unity Simulation (as "normal" linux user)

1. Clone the Democon px4-firmware repository
2. Inside the repo Switch to a stable ubuntu 20 branch(Ask!)
3. Run `git submodule update --init --recursive` if you haven't already. <br>
This wil update all submodules including getting the UnitySim build folder from this repository.
4. Run `px4_unity.sh`  
5. Read this [this](/USER_MANUAL.md) 

## Prerequisites for Unity TCP Endpoint ROS2 package(For working ros2 lidar) 
1. Install the companion repository on your system
2. Make sure you have ros2 humble installed and sourced inside `~./bashrc`
3. Make sure you have colcon installed

## How to use the Unity Simulation (as linux Unity Developer)

### Installing the Unity Editor
1. Install Unity hub for linux by following this offical Unity guide: https://docs.unity3d.com/hub/manual/InstallHub.html
2. Add the unity_project/ from the root of this respository as project in Unity hub. 
2. Install the editor version 2021.3.14f1 via the hub installs > install editor, should work with later versions but this project was tested with 2021.3.14f1.

Now you can start editing the scripts inside /unity_project/Assets/scripts and see the canges inside the Unity project editor screen. Click the play Icon to run the simulator in the editor.  
To connect it to px4 you can comment out every line except the following lines from the startup script `px4_unity.sh`:  
```
#!/bin/bash
# PX4_MODE="native"   #run realtime in real world
PX4_MODE="sim"      #run realtime in physics world
# PX4_MODE="debug"    #run standalone, no physics
export PX4_MODE

/bin/sh -c cd /build/px4_sitl_default/tmp && Tools/sitl_run.sh $PWD/build/px4_sitl_default/bin/px4 none unity usv none $PWD $PWD/build/px4_sitl_default
```

Try out some Unity beginner tutorials to get the hang of the editor. 

## Change Log
### Unity build inside unity_build/ <br>IS NOT UP-TO-DATE <br>with code inside unity/project/Assets/Scripts
<br>

-Added ros2 lidar toggle button in settings menu.<br>
-Added ros2 lidar states info in top-right corner.<br>

-Dus ros2 companion package works now with Unity ros2 node   <br>
-Added the other ros2 topics /odemetry, /clock, /origin_coordinate<br>
-Changed/modified topics /tf, /pointcloud<br>

-Added Lidar sensor<br>
-Added ROS2(Topics: /tf, /pointcloud)<br>

-Added headless mode in px4_unity.sh start up script(use ./px4_unity.sh -h)
<br>

-RTF values is wrong but added real time and simulation time<br>
-Fixed a problem where multiple warning popup window keeps getting created when you are in the wrong perspective when you want to create a obstacle.<br>

-Added better camera controls<br>
-Added information windows<br>
-Improved disturbance UI  <br>
-Fixed disturbance reset first then update bug<br>
-Changed popup window code <br>
-Removed changing fixedDeltaTime and maximumDeltaTime with numpad keys<br>

## Some folders explained
- `/unity_build` - Contains the Unity build which starts when using the start up script.  
- `/rosws_UnityEndpoint/src`- The location of Unity TCP ROS2 Endpoint.
- `/unity_projects` - Contains all folders used for editing Unity project.
- `/unity_projects/Assets/RosMessages` - Contains DUS custom ros messageas c# script 
-`unity_project/Assests/Script/ROS2/Nav2Slam`- Comes from Unity ros tutorial: Nav2Slam.
## Features
* Camera controls  
* Disturbances
* Placing shapes
* Pausing simulator
* Vessel dynamics for all vessels
* Choosing boat at startup
* Lidar sensor
* ROS2 Intergration

## Bugs
- When starting unity simulation with startup script in headless mode, and with LiDAR activated, ekf fault messages are getting dropped. Also warning messages are inside CLI from ROS2 unity side.

## [Extra Info]
- To start simulation in headless mode type in px4_firmware/ `./px4_unity.sh -h`
- Visit [USER MANUAL](\USER_MANUAL.md)
