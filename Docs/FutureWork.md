# Future work
- The graphical user interface could use a beauty update, during my internship the focus was not on this so the focus was only on getting it realised quickly. Also, the code should be improved to be more flexible. 

- The Unity ros2 module(ROS TCP Endpoint server) does not work optimally in Headless mode, I did not have enough time to figure this out.  
The CLI wil be flooded with warning messages when u use the command `./px4_unity -h`

- The latest boot V5750 has not yet been added in the simulation, I didn't have enough time for this 
either.  
You can find info about this vessel inside the px4 directory Tools/coppeliasim/scenes/USV/boat_V5750.lua.  
How boats are implemented should also be improved to make it more flexible and easy to integrate. 

- The Rate Limiter function inside the CoppeliaSim simulator needs to be implemented inside UnitySim.  
this function will make sure, the truster wil not immediately go from 0 to 100.

- It would be nice to actually simulate the water and then have control over certain parameters of the waves, to allow more realistic waves to act on the boat.

- No consideration is given to the position of the thrusters, as they are actually only allowed to generate thrust when they are underwater, currently this does not happen.

- A feature that allows users to save their scenarios they create and also load them, the loading should be able to be done in the command line when the user uses the startup script.

- A function allowing users to create and load a disturbance profile (during startup), a disturbance profile is like a file containing the external force values of the fields that are then automatically executed, several like profiles can be executed one after another with a certain delay, or even randomness.

- Detect when mavlink messages are out of sync and update the MAVLink C# generated code. This is important, because the mavlink messages are updated constatantly by the DUS software team.  

- Inertia matrix and correct center of mass position of the Vessels from CoppeliaSim are not implemented in the Vessels in UnitySim this still neeeds to be done.
