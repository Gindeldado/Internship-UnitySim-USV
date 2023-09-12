# USV simulation in Unity
After cloning the repository, navigate to the root folder and run:
```
git submodule update --init --recursive
```
This will clone and initialize the submodules for MavLink and MainThreadDispatcher.


### Unity
Download and install Unity editor from https://unity.com/download, tested with version 2021.3.14f1, should work with latest.
Open Unity Hub and "Add project from disk", open the folder "unity_simulation" (the root folder of this repository).

Once the project has loaded, navigate to Assets/Scenes and drag the scene “SampleScene” into the hierarchy panel on the left hand side. The original scene can then be deleted from the same hierarchy panel.

Click on Window > Package Manager, then on the + button on the top left of the window. Select "add package from git URL" and enter `https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector` to install the ROS-TCP-Connector package.

Finally, click on Robotics > ROS Settings, and change the protocol from ROS1 to ROS2. The IP can remain unchanged, as we will be using a docker image.

To run the simulation, click the play button at the top, or ctrl + p.


### PX4
Move the file "px4_unity.sh" to the folder "px4-firmware"
Open a terminal in the px4-firmware folder and run `./px4_unity.sh`


### ROS2
In order to set up a ROS2 connection for use with the LiDAR, first of all, clone the Unity-ROS-Hub repository:
```
git clone https://github.com/Unity-Technologies/Unity-Robotics-Hub.git
```
Navigate to tutorials/ros_unity_integration and run the following commands (make sure you have Docker installed first):
```
docker build -t foxy -f ros2_docker/Dockerfile .
docker run -it --rm -p 10000:10000 foxy /bin/bash
```
This should build a docker image and start it. Next, run the following command to start the TCP connection:
```
ros2 run ros_tcp_endpoint default_server_endpoint --ros-args -p ROS_IP:=0.0.0.0
```
As we’re running in a docker container, 0.0.0.0 is a valid incoming address.

Unity should now be able to communicate over ROS2.


