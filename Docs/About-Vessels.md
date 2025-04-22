# How to add a Vessel
As it stands now, it's quite a process you have to go through to add a new vessel.
There is definitely room for improvement here.  

### Inside the code
Create Inside VesselModel.cs a new field Inside `CurrentVesselModel` enum with the new vessel name.
Create a function which will set the parameters of the vessel you can use `SetLorentz(..)` and `SetDefault(..)` as a reference. The values can be obtained from CoppeliaSim.  

Create The function which will be used for calculating the dynamics of the vessel, you can use 
`LorentzVessel(..)` and `DefaultVessel(..)` as a reference.  

And as last step add a case for the new Vessel inside the `Apply(..)` function which will be called from `Movement.cs`which calles the function for calculating the dynamics of the new vessel.  

### Inside the Unity Hierarchy and inspector

Create a duplicate of the `VESSEL > vessel_V2500` object to serve as the new vessel object. Adjust the positions of the `stern`, `bow`, `starboard`, `port`, and `hull` objects within the duplicated vessel.

Ensure accurate fitting of the vessel model by using the `com`(CenterOfMass) object as the origin for the other objects. Feel free to modify the position of the com object, but bear in mind that the distances of the other objects are relative to the `com` object. This approach will help maintain the proper dimensions of the vessel model.

Set `motor0`, `motor1` and `motor2` objects on the correct positions with respect to `com` and the other dimensions.  
[Adding more thrusters: dont forget to change movement code to apply force for more thrusters if there are more than three.]

Set the mass property of the Rigidbody component for the new vessel object. 
Set the Model property of the VesselModel component on the new vessel object, to the new vessel model.
The remaining values inside the VesselModel component will be automatically configured. 

Verify that the Vessel field inside the VesselModel component, corresponds to the Rigidbody of the new vessel. 
Additionally, confirm that the thruster transforms are accurately assigned, with index 0 representing the 
left thruster, index 1 for the right thruster, and index 2 designated for the bow thruster.  

<br>

__Dont forget to add Ignore Lidar layer in inspector of collider on the vessel__

### How to make new Vessel available at startup

* Drag new Vessel object in Hierarchy `Canvas` object, Vessels array field of UIManager componenet.
* Add inside Awake() methode of UIManager.cs in the for loop a option for the new Vessel.