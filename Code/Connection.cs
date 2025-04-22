using System.Collections.Generic;
using UnityEngine;
using System.Threading; 
using System.Net.Sockets; 
using System.IO;
using System.Net;
using System;

using GPS;

public class Connection : MonoBehaviour
{
    public TcpListener tcpListener;
    public TcpClient connectedClient;

	///<summary> Separate thread for reading mavlink messages. </summary>
    public Thread connectionThread; 

	///<summary> Object which creates mavlink packets, with help of c# genereted library </summary>
	MAVLink.MavlinkParse mavlinkSendObj = new MAVLink.MavlinkParse(); 
	
#region STORING_SENSOR_DATA
    private Vector3 acceleration;
    private Vector3 angularVelocity;
    private Vector3 compass_data;
	public float yawRaw;
	private Int32 lat;
	private Int32 lon;
#endregion

	///<summary> Simulation Timestap</summary>
	public UInt64 timeStep = (UInt64) 20000;	//microseconds 

	///<summary> Simulation Time in microseconds</summary>
    static public UInt64 prev_simulation_time = 0;
    
	public bool actuator_received = false;
	public float[] controls;

	public Movement MovementScript;
	public UIManager UIManagerScript;

	public bool sendMessage = true; 
	public bool lockstep_enabled = false;

	bool heartbeatReceived = false;

	public System.Diagnostics.Stopwatch stopWatch;

	/// <summary>
	/// Start is called before the first frame update
	/// </summary>
    void Start()
    {
		controls = new float[16]; 

        Physics.autoSimulation = false; 

		//start connection thread 
		connectionThread = new Thread(CommunicationHandler);
      	connectionThread.Start(); 

    }

	/// <summary>
	/// Draws text on screen
	/// </summary>
	private void OnGUI() { 
		GUI.color = Color.black;
		//Only draw when we have the stopwatch.
		if(stopWatch != null)
		{
			TimeSpan ts = stopWatch.Elapsed;
				string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
				ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
			GUI.Label(new Rect(Screen.width /2, 35, 250, 25), "Real Time passed: " + elapsedTime);
			float simTime = (float)(prev_simulation_time - timeStep )/ 1000000;
			GUI.Label(new Rect(Screen.width /2, 50, 250, 25), "Simulation Time(s): " + simTime.ToString());
		}
	}
	/// <summary>
    /// This function will be called just before closing Unity. 
	/// </summary>
    private void OnApplicationQuit()
    {
        Debug.Log("[UnitySim] UnitySim is closing.");
		Debug.Log("[UnitySim] Closing connection with client..."); 
		if(connectedClient != null)
			if(connectedClient.Connected)
				connectedClient.Close();		
		Debug.Log("[UnitySim] Closed conenction thread...");

    }

	/// <summary>
	/// Handles communication between unity and px4, on background thread
	/// </summary>
	void CommunicationHandler(){
		//Setup tcp listener
        tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"),4560);
        tcpListener.Start();
        Byte[] buffer = new byte[1024];

        try
        {
			Debug.Log("[UnitySim] waiting for connection...");
            //Wait til client is connected stream
            connectedClient = tcpListener.AcceptTcpClient();
            NetworkStream stream = connectedClient.GetStream();
			Debug.Log("[UnitySim] client is connected");
			Debug.Log("[UnitySim] waiting for data..");
			//blocking while loop which checks if data is in stream 
			int len;
			while ((len = stream.Read(buffer,0,buffer.Length)) > 0)
			{
				//store stream data inside byte array
				var incomingData = new byte[len];
				Array.Copy(buffer, 0, incomingData, 0, len); 
				ReadMessages(incomingData);
				
				// Debug.Log("Received  data..");

				CheckIfSimulationIsPaused();
				
				//Lockstep communication_START
				if(lockstep_enabled) 
				{
					//Received actuator, simulation should update first!
					Movement.updateSimulation = true;
					//wait til simulation update has been updated...
					while (!sendMessage)
					{
						Thread.Sleep(1); 
					}
					//send new sensor data
					SendMessage(MAVLink.MAVLINK_MSG_ID.HIL_GPS);
					SendMessage(MAVLink.MAVLINK_MSG_ID.HIL_SENSOR);
					sendMessage = false;
					prev_simulation_time += timeStep;
				}	
				//Freewheeling period(Initializing Lockstep)
				else if(heartbeatReceived && !lockstep_enabled){
					UIManagerScript.connectionState = UIManager.ConnectionState.INITIALISE_LOCKSTEP;
					Debug.Log("[UnitySim] Initializing lockstep");
					InitialiseLockstep(stream);
				}
				//_END

			}
			Debug.Log("[UnitySim] Nothing in stream...");
			lockstep_enabled = false;
			UIManagerScript.connectionState = UIManager.ConnectionState.LOST_CONNECTION;
			stream.Close();
        }
        catch (System.Exception ex)
		{
			Debug.Log("[UnitySim] Lost connection to control computer, restart simulation!");
			connectedClient.Close();
			tcpListener.Stop();	
			UIManagerScript.connectionState = UIManager.ConnectionState.LOST_CONNECTION;
			Debug.LogWarning("[UnitySim] Exception " + ex.ToString()); 
		}
	}
	
	/// <summary>
	/// Keeps sending sensor data until Unity receives a message, <br/>
	/// which (hopefully) is a actuator control message.
	/// </summary>
	/// <param name="stream">The data stream between px4 and unity</param>
	private void InitialiseLockstep(NetworkStream stream){
        do
        {
            //start sending msg til we receive data(actuator_controls) 
            SendMessage(MAVLink.MAVLINK_MSG_ID.HIL_GPS);
            SendMessage(MAVLink.MAVLINK_MSG_ID.HIL_SENSOR);
            prev_simulation_time += timeStep;
            Thread.Sleep(5);    			//Sleep is needed otherwise it wil loop to quick
        } while (!stream.DataAvailable);	//When data(actuator) is received, lockstep is ready
        lockstep_enabled = true;
		Debug.Log("[UnitySim] starting lockstep comminucation in 2sec...");
		Thread.Sleep(2000);
		UIManagerScript.connectionState = UIManager.ConnectionState.LOCKSTEP;
		//start simulation timer
		var sw =  new System.Diagnostics.Stopwatch(); //continues in Movement.cs
		stopWatch = sw;
		stopWatch.Start();
    }
 
	/// <summary>
	/// Reads all messages from the stream in sequence.
	/// </summary>
	/// <param name="data">Contains byte array of stream data</param>
	private void ReadMessages(Byte[] data){
			int p = 0; 		            //pointer to keep track of last read byte in array
			int msg_len;	            //length of msg object
			Byte[] streamData = data;   

			//Makes sure every message in stream is handled
			do
			{
				//Initialise mavlink objects for holding and deserializing message
				MAVLink.MAVLinkMessage mavlinkMsg = new MAVLink.MAVLinkMessage();
				MAVLink.MavlinkParse mavlinkParse = new MAVLink.MavlinkParse();

				//Creating a memorystream to store data,
				//serving as the input for mavlink deserialization of a packet.
				using (MemoryStream ms = new MemoryStream(streamData))
				{
					//Read packet
					mavlinkMsg = mavlinkParse.ReadPacket(ms); 
					HandleMessage(mavlinkMsg);
				}
				//Update msg length and move pointer.
				msg_len = mavlinkMsg.Length;
				p += msg_len;	

				//Copy elements from streamData to itself, starting from index p 
				//with a length equal to the size of 
				//the array minus the position of the last read byte.
				Array.Copy(streamData, p, streamData, 0, streamData.Length-p);
			} while (p < data.Length);

			//NOTE - The messages will be read in reverse order of their sending sequence.
		}

 	/// <summary>
	/// Function called from connection thread, which checks if simulation is paused 
	/// </summary>
	void CheckIfSimulationIsPaused(){
		if(!UIManagerScript.simulationPaused)
			return;
		stopWatch.Stop();
		while(UIManagerScript.simulationPaused){
			//- Should Freeze (time.timescale) physics time from function inside main unity thread!
			Thread.Sleep(1);
		}
		if(lockstep_enabled)
			stopWatch.Start();
	}

	/// <summary>
	/// Handel incomming mavlink message
	/// </summary>
	/// <param name="msg">MAVLink message</param>
    private void HandleMessage(MAVLink.MAVLinkMessage msg){ 
		switch (msg.msgid)
		{
			case (uint)MAVLink.MAVLINK_MSG_ID.HEARTBEAT:
				if(msg.data is MAVLink.mavlink_heartbeat_t heartbeatMsg)
				{
					Debug.Log(String.Format("[UnitySim] GOT HEARTBEAT= custom mode: {0} type: {1} autopilot: {2} basemode: {3} sysytem status: {4} mavlink vers: {5}", 
					heartbeatMsg.custom_mode, heartbeatMsg.type, heartbeatMsg.autopilot, heartbeatMsg.base_mode, heartbeatMsg.system_status, heartbeatMsg.mavlink_version));

					//Start lockstep initialization
					heartbeatReceived = true;
				}
				break;

			case (uint)MAVLink.MAVLINK_MSG_ID.COMMAND_LONG:
				if(msg.data is MAVLink.mavlink_command_long_t commandlongMsg) 
				{
					Debug.Log(String.Format("[UnitySim] GOT COMMANDLONG= param1: {0} param2: {1} param3: {2} param4: {3} param5: {4} param6: {5} param7: {6} command: {7} targert_sys: {8} target_comp: {9} confirmation: {10}", 
					commandlongMsg.param1, commandlongMsg.param2, commandlongMsg.param3, commandlongMsg.param4, 
					commandlongMsg.param5, commandlongMsg.param6, commandlongMsg.param7, commandlongMsg.command,
					commandlongMsg.target_system, commandlongMsg.target_component, commandlongMsg.confirmation));

				}
				break;
 
			case (uint)MAVLink.MAVLINK_MSG_ID.HIL_ACTUATOR_CONTROLS:
				if(msg.data is MAVLink.mavlink_hil_actuator_controls_t actuatorControlsMsg){
					// string s = String.Join(", ", new List<float>(actuatorControlsMsg.controls).ConvertAll(i => i.ToString()).ToArray());
					// Debug.Log(String.Format("GOT ACTUATOR_CONTROLS= TIme_usec: {0} flags: {1} mode: {2} controls: [{3}]",
					// actuatorControlsMsg.time_usec , actuatorControlsMsg.flags , actuatorControlsMsg.mode, s)); 	

					controls = actuatorControlsMsg.controls;
					actuator_received = true;
				}
				break;
			default:
			 	Debug.LogError("[UnitySim] Unexpected MAVLink message type; type= " + msg.msgtypename);
				break;
		}
	}

	/// <summary>
	/// Sending MAVLink message
	/// </summary>
	/// <param name="id">Message type id of MAVLink message</param>
	private void SendMessage(MAVLink.MAVLINK_MSG_ID id){
		if (connectedClient == null || !connectedClient.Connected) {  
			Debug.LogWarning("[UnitySim] Sending message failed, no TCP connection.");
			UIManagerScript.connectionState = UIManager.ConnectionState.LOST_CONNECTION;
			return;         
		}

		NetworkStream stream = connectedClient.GetStream();  
		if(!stream.CanWrite)
		{
			Debug.Log("[UnitySim] Can not write to stream!..");
			return;
		}	

		switch (id)
		{
			case MAVLink.MAVLINK_MSG_ID.HEARTBEAT:
				Debug.Log("[UnitySim] Sending heartbeat");
				try {
					//create a mavlink heartbeat message
					MAVLink.mavlink_heartbeat_t heartbeat = new MAVLink.mavlink_heartbeat_t(
						custom_mode: (byte)11,
						type: (byte)12,
						autopilot: (byte)65,
						base_mode: (byte) 128,
						system_status: (byte)0,
						mavlink_version: (byte)3
					);
					//Create a object which handles creating the packet
					//parse and serialise msg and store it as byte array packet
					byte[] mavlinkPacket = mavlinkSendObj.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.HEARTBEAT, heartbeat);
					//write packet on stream and send to client 
					stream.Write(mavlinkPacket, 0, mavlinkPacket.Length);

				}       
				catch (SocketException socketException) { 
					Debug.Log("[UnitySim] Socket exception: " + socketException);
				}
				break;

			case MAVLink.MAVLINK_MSG_ID.HIL_GPS:
				lat = (Int32)(GPSClass.latInt);
				lon = (Int32)(GPSClass.lonInt);

				//calculate gps yaw
				//get the yaw angle (rotation around the Y-axis) (0-360) degree
				float yawDeg = GPSClass.orientation.y; 
				if(yawDeg < 0.01f) 
				{
					yawDeg = 360f;
				}else if (yawDeg < 0.0f) {
					yawDeg += 360.0f;
				}
				yawRaw = yawDeg;//debug line

				try
				{
					MAVLink.mavlink_hil_gps_t hilgps = new MAVLink.mavlink_hil_gps_t(
						time_usec: (ulong) prev_simulation_time,
						lat: lat + (int)get_noise(1.0f),
						lon: lon + (int)get_noise(1.0f),
						alt: (int) 10 + (int)get_noise(10.0f),
						eph: (ushort)100,
						epv: (ushort)100,
						vel: (ushort)UInt16.MaxValue,
						vn: (short)((GPSClass.worldVelocity.z * 100)+ (int)get_noise(1.0f)), 
						ve: (short)((GPSClass.worldVelocity.x * 100)+ (int)get_noise(1.0f)),
						vd: (short)((0 * 100)+ (int)get_noise(1.0f)),
						cog: (ushort)UInt16.MaxValue,
						fix_type: (byte)3,
						satellites_visible: (byte)10,
						id: (byte)0,
						yaw: (ushort) (yawDeg * 100.0f)
					);

					byte[] mavlinkPacket = mavlinkSendObj.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.HIL_GPS, hilgps);
					stream.Write(mavlinkPacket, 0, mavlinkPacket.Length);
					//Debug.Log("Sending gps");
				}
				catch (SocketException socketException)
				{
					Debug.Log("[UnitySim] Socket exception: " + socketException);
				}
				break;

			case MAVLink.MAVLINK_MSG_ID.HIL_SENSOR:
				acceleration =  IMU.acceleration;
				angularVelocity = IMU.angVelocity; 
				compass_data = GPSClass.compass_data;
				 
				try
				{
					MAVLink.mavlink_hil_sensor_t hilsensor = new MAVLink.mavlink_hil_sensor_t(
						time_usec: (ulong)prev_simulation_time,
						xacc: acceleration.z + get_noise(0.01f),
						yacc: acceleration.x + get_noise(0.01f),
						zacc: acceleration.y + get_noise(0.01f),
						xgyro: angularVelocity.z + get_noise(0.001f),
						ygyro: angularVelocity.x  + get_noise(0.001f),
						zgyro: angularVelocity.y + get_noise(0.001f),
						xmag: compass_data[0],
						ymag: compass_data[1],
						zmag: compass_data[2],
						abs_pressure: 1013.295f + get_noise(0.1f),
						diff_pressure: 0.0f, 
						pressure_alt: 0.05881089f + get_noise(0.01f), 
						temperature: 15.00039f,
						fields_updated:(uint) 7167,
						id: (byte)0
					);
					byte[] mavlinkPacket = mavlinkSendObj.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.HIL_SENSOR, hilsensor);
					stream.Write(mavlinkPacket, 0, mavlinkPacket.Length);
					// Debug.Log(String.Format("SENDING HIL_SENSOR= Time_usec: {0}",prev_simulation_time)); 	 

				}
				catch (SocketException socketException)
				{
					Debug.Log("[UnitySim] Socket exception: " + socketException);
				}
				break;
			
			default:
				break; 
		}
	}
	/// <summary>
	/// Noise function
	/// </summary>
	/// <param name="magnitude">size of noise</param>
	/// <returns>noise value</returns>
    public float get_noise(float magnitude) {
		System.Random rand = new System.Random();
		float randomInt = rand.Next(1,101);
		float noise = (randomInt - 50.0f) / 100.0f * magnitude;
		return noise;
	}
}