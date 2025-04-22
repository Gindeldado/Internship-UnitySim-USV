# How the GUI works

The GUI is controlled under the hood by UIManager.cs script component 
found inside the Hierarchy under `Canvas`.  

## How to add a Panel
In the Hierarchy within the `Canvas` > `Panel_MENU` object, there are all 
the menus/panels. 
This is where the new panels are placed. 
In `UIManager.cs`, create a public variable in which the panel object is 
stored, then add the variable to `ReturnToSimulation()` in the same way 
the other panels are added.  

## Small explanation of certain UIManafer.cs features
#### Mouse State  
In `UIManager.cs`, a variable `currentState` is kept which contains the 
current state of the mouse. This is used, for example, to ensure that 
placing obstacles only works in scenario edit mode.
#### Easy Custom Pop Up Messages
PopUpMsg(string:titel, string:message)  
This is a function which needs a title and message, which will be 
displayed in its own smal panel, the panel that then appears possesses a closing button.  
#### Return to simulation
The ReturnToSimulation() function in UIManager.cs causes all panels to be 
deactivated, this should include the new panels so that they are correctly deactivated.