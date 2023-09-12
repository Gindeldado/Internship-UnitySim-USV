#!/bin/sh
#PX4_MODE="native"   #run realtime in real world
#PX4_MODE="sim"     #run realtime in physics world
#PX4_MODE="debug"   #run standalone, no physics
#export PX4_MODE

FILE=build/px4_sitl_default/bin/px4
if [ ! -f "$FILE" ]; then
    make
fi


/bin/sh -c cd /build/px4_sitl_default/tmp && Tools/sitl_run.sh $PWD/build/px4_sitl_default/bin/px4 none none usv $PWD build/px4_sitl_default

