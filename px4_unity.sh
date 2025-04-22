#!/bin/bash
# PX4_MODE="native"   #run realtime in real world
PX4_MODE="sim"      #run realtime in physics world
# PX4_MODE="debug"    #run standalone, no physics
export PX4_MODE
#-------------------------------------------------
UNITY_START_PX4=true    #run px4
HEADLESS_MODE=false
VESSEL="Default"        #Set vessel model[Default, V2500, Lorentz]
LIDAR=true              #Turn on/off ros2 lidar

# get starting directory
STARTD=$PWD

RED="\e[31m"
GREEN="\e[32m"
ENDCOLOR="\e[0m"

# Get Command line Arg
while getopts ":h :n m:" option; do
   case $option in
    h)  # start in headles mode
         HEADLESS_MODE=true;;
        # echo "Starting in Headless mode: $HEADLESS_MODE"
    n)  # Turn Lidar off
        LIDAR=false;;
    m)  #Set vessel model
        VESSEL=$OPTARG;;
    \?) # Invalid option
         echo -e "${RED}Error: Invalid option ${ENDCOLOR}"
         exit;;
   esac
done


# If px4 has not been build, build p4x first
FILE=build/px4_sitl_default/bin/px4
if [ ! -f "$FILE" ]; then
    make
fi

##Check/build ros2 Unity TCP-Endpoint package
# searching colcon build folder
echo -e "${GREEN}[UnitySim] Searching ros2ws_UnityEndpoint/build folder..."
cd Tools/unitysim/ros2ws_UnityEndpoint

# Check if build folder is present if not build colcon
if [ -e build ]; then
    echo -e "${GREEN}[UnitySim] Found colcon build!${ENDCOLOR}"
else
    echo -e "${RED}[UnitySim] Colcon build not found!${ENDCOLOR}"
    echo -e "${GREEN}[UnitySim] Creating colcon build${ENDCOLOR}"
    colcon build
    if [ $? -ne 0 ]; then
        # The command failed
        echo -e "${RED}[UnitySim] Building colcon failed...${ENDCOLOR}"
        exit 1
    fi
    echo -e "${GREEN}[UnitySim] Unity-TCP-Endpoint node build DONE!${ENDCOLOR}"
fi

cd ..

StartRos2Lidar(){
    ## Starting Unity-TCP-Endpoint node(ros2 lidar) in another terminal
    cd ../ros2ws_UnityEndpoint
    echo -e "${GREEN}[ros2unity] Starting ros2 unity tcp endpoint in new terminal${ENDCOLOR}"
    source install/local_setup.bash
    gnome-terminal -- ros2 run ros_tcp_endpoint default_server_endpoint --ros-args -p ROS_IP:=127.0.0.1 -p ROS_TCP_PORT:=10000

    cd ../unity_build/
}

##Starting Unity(normal or headless mode)
echo -e ${GREEN}[UnitySim] Starting simulation${ENDCOLOR}
# Go to Unity build
cd unity_build/
if [ $? -eq 0 ]; then
    echo -e "${GREEN}[UnitySim] Found build${ENDCOLOR}"
else
    echo -e "${RED}[UnitySim] Something went wrong :thinking: check this file!${ENDCOLOR}"
    exit
fi

if [[ $HEADLESS_MODE == true && $LIDAR == false ]]; then
    # Start headless mode WITHOUT lidar
    echo -e "${GREEN}[UnitySim] Starting Unity simulation in headless mode without Lidar${ENDCOLOR}"
    echo $VESSEL
    ./UnitySim.x86_64 -batchmode -nographics -$VESSEL -nolidar &
    cd $STARTD
elif [[ $HEADLESS_MODE == true && $LIDAR == true ]]; then
    # Start headless mode WITH lidar
    StartRos2Lidar
    echo -e "${GREEN}[UnitySim] Starting Unity simulation in headless mode${ENDCOLOR}"
    echo $VESSEL
    ./UnitySim.x86_64 -batchmode -nographics -$VESSEL &
    cd $STARTD
elif [[ $HEADLESS_MODE == false && $LIDAR == true ]]; then
    # Start unity WITH lidar
    StartRos2Lidar
    echo -e "${GREEN}[UnitySim] Starting Unity simulation${ENDCOLOR}"
    echo $VESSEL
    ./UnitySim.x86_64 -$VESSEL &
    cd $STARTD
else
    # Start unity WITHOUT lidar
    echo -e "${GREEN}[UnitySim] Starting Unity simulation without Lidar ${ENDCOLOR}"
    ./UnitySim.x86_64 -$VESSEL -nolidar &
    cd $STARTD
fi

echo "Vessel:       "$VESSEL
echo "Lidar on:     "$LIDAROFF  
echo "Graphic Mode  "$HEADLESS_MODE

__NV_PRIME_RENDER_OFFLOAD=1
__GLX_VENDOR_LIBRARY_NAME=nvidia
export __NV_PRIME_RENDER_OFFLOAD
export __GLX_VENDOR_LIBRARY_NAME


# Start px4
if [ $UNITY_START_PX4 == true ]; then
    echo -e "${GREEN}[UnitySim] Starting Px4... ${ENDCOLOR}"
    echo $PWD
    /bin/sh -c cd /build/px4_sitl_default/tmp && Tools/sitl_run.sh $PWD/build/px4_sitl_default/bin/px4 none unity usv none $PWD $PWD/build/px4_sitl_default
else
    echo -e "${RED}[UnitySim] Px4 has not been started${ENDCOLOR}"
fi
