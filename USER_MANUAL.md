# UnitySim - User Manual
Welcome to the UnitySim User Manual!

This manual has been crafted to assist you in effectively using UnitySim simulator.  
Whether you are a new user seeking to get started or  
an experienced user looking to explore advanced features,  
this manual provides clear instructions and valuable insights to optimize your experience.

## Table of contents
#### [Camera controls and settings](#camera-controls-and-settings-1)
#### [Placing and removing obstacles](#placing-and-removing-obstacles-1)
#### [How do I use the Disturbance menu](#how-do-i-use-the-disturbance-menu-1)

#### [How do I choose a diffrent Vessel](#how-do-i-choose-a-diffrent-vessel-1)
#### [How do I turn of Lidar sensor(/ Unity TCP EndPoint server)](#how-do-i-turn-of-lidar-sensor-unity-tcp-endpoint-server-1)
#### [How do I start UnitySim in headless mode](#how-do-i-start-unitysim-in-headless-mode-1)  



## Camera controls and settings
### Controlling the camera
When you start UnitySim up you see the vessel in a **top-down** perspective.  
<br>
In this view mode you can move the camera up, down, left and right, by using the **arrow keys**.  
Zooming in is done by using one of the following keys  **+**, **KeypadPlus**, **=**.  
Zooming out is done by using one of the following keys  **-**, **KeypadMinus**,  **_**.

When you are inside **3rd person** perspective, the controls are as follows:  
<br>
In this view mode you can move the camera forward, backward, left and right by using the **arrow keys**.  
When Holding **Ctrl** and **arrow keys** you can turn the camera in direction of the arrow keys.  
Moving straight up is done by using one of the following keys  **+**, **KeypadPlus**, **=**.  
Moving straight down is done by using one of the following keys  **-**, **KeypadMinus**,  **_**.

### Camera settings buttons
_If you select the gear icon in the top-left corner, you can access the main menu and then navigate to `Camera`._ 
* _Top-down_ - Changes camera to top down view.
* _Third-person_ - Changes camera to 3rd person view.
* _Camera speed_(slider) - Changes camera travel speel.  

<br>

* _Lock cam in position_ - Toggle locks camera in current distance from vessel, maintaing it as offset.
* _Lock cam op ship_ - Toggle locks camera in default distance from vessel, maintaing it as offset.

<br>

* _Reset_ - Returns camera to start orientation and position.
* _Reset only rotation_ - Returns camera to start orientation.  
[Back to table of contents](#table-of-contents)



## Placing and removing obstacles
If you select the gear icon in the top-left corner, you can access the main menu and then navigate to `Scenario Edit Mode`.  
__The simulation is now paused, dont forget to unpause it when you are done,  
by clicking on `Simulation Mode` or inside main menu the `Pause/Run` button.__
### Placing Obstacles  
Navigate to `Make` > `Land shape` <br>
Now you can use the `Left Mouse button` to create red points on the map 
for making a polygon shape.  
You can delete your last placed point with `Right Mouse Button`.  
<br>
To create the plygon shape you marked with with points, you need to press the `Enter` key.

### Removing Obstacles 
Navigate to `Delete` <br>
Now you can click on a obstacle to remove it. 
[Back to table of contents](#table-of-contents)



## How do I use the Disturbance menu
If you select the gear icon in the top-left corner, you can access the main menu and then navigate to `Disturbances`. 
<br>
__Force, Current and Wind are all diffrent force vectors applied to the vessel in the same way, Moment is torque applied on the vessel.__

### Input Fields
`* magnitude` = Force in Newton _(take into account the weight of the vessel)_.  
`* angle wrt north` = The angle of Force vector on the boat with respect to(wrt) north in degrees, __with north facing upwards__.  
`* reduction factor` = Reduce Force with respect to angle between heading and force angle in percentage.  
`* offset wrt length` = Offset Force vector position on vessel from center of mass with respect to(wrt) the length of the vessel in percentage.  
<br>

`Force period` = Apply sinus on Force vector by setting the period in miliseconds.  
<br>
`Moment X on body` = Size of torque along the vessel x-axis in NewtonMeter, creating a ptich movement.  
`Moment Y on body` = Size of torque along the vessel y-axis in NewtonMeter, creating a rotating movement.  
`Moment Z on body` = Size of torque along the vessel z-axis in NewtonMeter, creating a roll movement.  
<br>

### Apply and Reset Disturbance
* When you have finished filling in the fields, you can press `Update` to Apply the Disturbances.
* Press `Reset` to clear all values in the field and stop the Disturbances.

[Back to table of contents](#table-of-contents)



## How do I choose a diffrent Vessel
Use `-m` argument when starting UnitySim in terminal with the name of the vessel model after it.  
e.g. `./px4_unity -m V2500` or `./px4_unity -m Lorentz` 
<br>

#### Inside Dev Mode(Unity Project)
When you want to use a diffrent vessel to test, Disable all vessels inside VESSEL object in Hierarchy except for the vessel you want to test.     
[Back to table of contents](#table-of-contents)



## How do I turn of Lidar sensor(/ Unity TCP EndPoint server)
Use `-n` argument when starting UnitySim in terminal.  
e.g. `./px4_unity -n`  
This will disable Lidar and the unity ros2 endpoint.  
[Back to table of contents](#table-of-contents)



## How do I start UnitySim in headless mode  
Use `-h` argument when starting UnitySim in terminal.  
e.g. `./px4_unity -h`  
This will start UnitySim in headless mode.  
<br>

__At this moment Lidar sensor does not work correctly in headless mode.  
For this reason use `./px4_unity -h -n`.__ 

[Back to table of contents](#table-of-contents) 