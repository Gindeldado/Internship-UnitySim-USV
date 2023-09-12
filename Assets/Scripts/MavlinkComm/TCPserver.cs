using System;
using System.Collections; 
using System.Collections.Generic; 
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Threading; 
using System.Linq;
using UnityEngine;
using UnityEditor;
using MavLink;
using TCPheader;
using CustomPhysics;

using MSG;

public class TCPserver : MonoBehaviour {  	
	#region private members 		
	// TCPListener to listen for incoming TCP connection requests. 		
	private static TcpListener tcpListener; 

	// Background thread for TcpServer workload.	
	private static Thread tcpListenerThread;  	
 	
	// Create handle to connected tcp client. 		
	private static TcpClient connectedTcpClient;  	
	private static TCP TCP;
	private int sequence = 0;
	private UInt64 timestep = 4000; // microseconds
	#endregion

	#region public members
	public static bool lockstep_initialized = false;
	public static UInt64 prev_simulation_time = 0;
	public bool actuator_received = true;
	#endregion

	
	void Start() { 		
		// Start TcpServer background thread
		tcpListenerThread = new Thread (new ThreadStart(ListenForIncomingRequests)); 		
		tcpListenerThread.IsBackground = true; 		
		tcpListenerThread.Start();
		TCP = new TCP();
	}  	 


	// Sends 10 sensor and GPS messages to initialize lockstep
	public void InitialSend() {
		while (sequence < 10) {
			SendMessage(113);
			Thread.Sleep(10);
			SendMessage(107);
			Thread.Sleep(10);
			prev_simulation_time += timestep;
			UnityMainThreadDispatcher.Instance().Enqueue(() => Physics.Simulate(0.004f));
		}
	}  	


	public void SendMavlinkMsgs() {
		if (actuator_received && lockstep_initialized) {
			SendMessage(113);
			Thread.Sleep(1);
			SendMessage(107);			

			UnityMainThreadDispatcher.Instance().Enqueue(() => Thread.Sleep(1));
			UnityMainThreadDispatcher.Instance().Enqueue(() => Physics.Simulate(0.004f)); 
			
			prev_simulation_time += timestep;
		}
		actuator_received = false;
	}


	// Runs in background TcpServerThread; Handles incoming TcpClient requests
	private void ListenForIncomingRequests() { 		
		try {
			// Create listener on localhost port 4560
			tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 4560); 			
			tcpListener.Start();              
			Debug.Log("Server is listening");
			Byte[] bytes = new Byte[1024];
			while (true) {
				using (connectedTcpClient = tcpListener.AcceptTcpClient()) {
					// Get a stream object for reading
					using (NetworkStream stream = connectedTcpClient.GetStream()) { 
						int length;
						// Read incoming stream into byte array
						while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
							
							var incomingData = new byte[length];
							Array.Copy(bytes, 0, incomingData, 0, length); 

							MavMsgs.HandleMessages(incomingData);
						}
					}
				} 				
			}
		}
		catch (SocketException socketException) { 			
			Debug.LogWarning("SocketException " + socketException.ToString()); 		
		}
	}  	


	// Send message to client using socket connection. 	
	private void SendMessage(UInt32 id) {
		if (connectedTcpClient == null) {  
			Debug.LogWarning("Sending message failed, no TCP connection.");
			return;         
		}
		try {
			// Get a stream object for writing. 			
			NetworkStream stream = connectedTcpClient.GetStream(); 			
			if (stream.CanWrite) { 
				Byte[] msg = new Byte[0];     
				msg = MavMsgs.ConstructMessage((ushort)id, sequence);
				stream.Write(msg, 0, msg.Length);
				sequence += 1;
			}       
			else Debug.Log("Cannot write to stream");
		} 		
		catch (SocketException socketException) {
			Debug.Log("Socket exception: " + socketException);
		}
	}
}
