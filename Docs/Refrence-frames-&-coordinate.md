# Refrence frames and Coordinate systems

Date of writing: 25/01/2024

### Unity coordinate system
Unity uses a left handed, y-up coordinate system:
- x = right
- y = up
- z = forward

In this project this coordinate system is always used as base coordinate system, unless it states otherwise. 
### Some reference frames
- ENU - (x)East, (y)North, (z)Down
- NED- (x)North, (y)East, (z)Down
- FLU- (x)Forward, (y)Left, (z)Up (local/body/)


### PX4 coordinate system
To get the correct translation result in DUMS  
the data is sent as followed to px4:
- x = Unity z-as7
- y = Unity x-as
- z = Unity y-as

#### Unity sending data to px4
The Server.cs sends the IMU and GPS sensor data to px4 in the following reference frame:
- IMU
    - (acc)acceleration, In local reference frame
    - (gyro)angular velocity, In local reference frame
    - (mag)compass data= -
- GPS
    - (vn/ve)velocity, In world reference frame
    - (lat)latitude= -  
    - (lon)longitude= -

### ROS2 Odometry message
ROS2OdometryPublisher.cs  
The odometry message contains position, rotation, velocity and angular velocity data from 
the IMU and GPS sensor on the vessel.  
All these variables are transformed to the ENU reference frame

### LiDar sensor data to ROS2
Lidar.cs  
The position of a point detected by the lidar sensor is transformed to FLU reference frame

### Coppelia Sim coordinate system
It appears that in the simExtPX4.cpp file within the DUS PX4 repository,<br> 
specifically in the section handling communication with PX4 within CoppeliaSim,<br> 
there is an addition of +90 to the yaw variable when creating the HIL GPS message sent to 
PX4.<br>
yaw holds the heading of the boat with 0/360 being north.
```
void create_hil_gps_msg(mavlink_message_t* msg, uint64_t time, double* gps_data){
...
...
    float yaw = gps_euler_angles[2] * 180.0f / M_PI + 90.0f;
..
}
```
