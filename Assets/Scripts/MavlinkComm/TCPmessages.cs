using System;
using System.Collections; 
using System.Collections.Generic; 
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Threading; 
using UnityEngine;  
using MavLink;
using TCPheader;
using GPS;
using IMUcalc;

namespace MSG {
    public class MavMsgs {
        #region public members
        public static Byte[] hbCRC = new Byte[] {MavLinkSerializer.Lookup[0].CrcExtra};
        public static Byte[] sensorCRC = new Byte[] {MavLinkSerializer.Lookup[107].CrcExtra};
        public static Byte[] gpsCRC = new Byte[] {MavLinkSerializer.Lookup[113].CrcExtra};
        public static Byte[] commandCRC = new Byte[] {MavLinkSerializer.Lookup[76].CrcExtra};
        public static Byte[] actuatorCRC = new Byte[] {MavLinkSerializer.Lookup[93].CrcExtra};
        public static Byte[] CRC;
        #endregion

        #region private members
        private static TCP TCP = new TCP();
        private static TCPserver TCPserver = new TCPserver();
        public static float[] controls;
        private static UInt64 number_of_actuator_msgs_received = 0;
    	private static Vector3 acceleration;
        private static Vector3 velocity;
        private static double abs_pressure;
        private static float temperature;
        private static Vector3 angularVelocity;
        private static Int32 lat;
        private static Int32 lon;
        private static Vector3 compass_data;
        #endregion


        public static void HandleMessages(byte[] incomingData) {
            Tuple<List<Byte[]>, List<uint>> msgs_tup = TCP.handle_multiple_msgs(incomingData, hbCRC, commandCRC, actuatorCRC);
            List<Byte[]> msgs_list = msgs_tup.Item1;
            List<uint> id_list = msgs_tup.Item2;
            int offset = 10;
            bool receive = true;
            
            // Message deserialization
            for (int i = 0; i < msgs_list.Count; i++) {

                uint id = id_list[i];
                switch(id) {
                // Heartbeat
                case 0:
                    if (msgs_list[i].Length > 0 && TCP.is_correct_checksum(msgs_list[i], hbCRC, receive)) {
                        Msg_heartbeat hbMsg = (Msg_heartbeat)MavLinkSerializer.Deserialize_HEARTBEAT(msgs_list[i], offset);
                        //TCP.log_heartbeat(hbMsg);
                    }
                    break;
                
                // Command long 
                case 76:
                    if(msgs_list[i].Length > 0 && TCP.is_correct_checksum(msgs_list[i], commandCRC, receive)) {
                        Msg_command_long cmdMsg = (Msg_command_long)MavLinkSerializer.Deserialize_COMMAND_LONG(msgs_list[i], offset - 3); // deserialization seems off, doens't work with expected value of 10
                        TCP.log_command_long(cmdMsg);

                        Thread.Sleep(5000); // without this 5 second delay, the initialization messages are sent too soon and PX4 misses them
                        TCPserver.InitialSend();
                    }
                    break;
                
                // HIL actuator controls
                case 93:
                    if(msgs_list[i].Length > 0 && TCP.is_correct_checksum(msgs_list[i], actuatorCRC, receive)) {
                        Msg_hil_actuator_controls ctrlMsg = (Msg_hil_actuator_controls)MavLinkSerializer.Deserialize_HIL_ACTUATOR_CONTROLS(msgs_list[i], offset);
                        //TCP.log_hil_actuator_controls(ctrlMsg);

                        controls = ctrlMsg.controls;
                        
                        if (number_of_actuator_msgs_received > 0) {
                            TCPserver.lockstep_initialized = true;
                        } else {
                            number_of_actuator_msgs_received++;
                        }
                        
                        TCPserver.actuator_received = true;
                        TCPserver.SendMavlinkMsgs();
                    }
                    break;

                default:
                    Debug.Log("Unknown message type");
                    Thread.Sleep(1000);
                    continue;
                }
            }
	    }

        // Construct MavLink message based on msg id
        public static Byte[] ConstructMessage(UInt16 id, int sequence) {
            // Send message
            Byte[] bytes = new Byte[280];
            int offset = 10;

            UInt16 msgid = id;
            switch(msgid) {
            // heartbeat
            case 0:  
                Msg_heartbeat heartbeatMsg = (Msg_heartbeat)MavMsgs.create_heartbeat_msg();
                MavLinkSerializer.Serialize_HEARTBEAT(heartbeatMsg, bytes, ref offset);
                CRC = hbCRC;
                bytes = CompleteMessage(bytes, sequence, id, CRC);
                return bytes;

            // hil_sensor
            case 107: 
                Msg_hil_sensor sensorMsg = (Msg_hil_sensor)MavMsgs.create_hil_sensor_msg();
                MavLinkSerializer.Serialize_HIL_SENSOR(sensorMsg, bytes, ref offset);
                CRC = sensorCRC;
                bytes = CompleteMessage(bytes, sequence, id, CRC);
                return bytes;

            // hil_gps
            case 113:
                Msg_hil_gps gpsMsg = (Msg_hil_gps)MavMsgs.create_hil_gps_msg();
                MavLinkSerializer.Serialize_HIL_GPS(gpsMsg, bytes, ref offset);
                CRC = gpsCRC;
                bytes = CompleteMessage(bytes, sequence, id, CRC);
                return bytes;

            default: return null;
            }
        }


        private static byte[] CompleteMessage(byte[] bytes, int sequence, uint id, Byte[] CRC){
            bool receive = false;

            bytes = TCP.removeZeros(bytes);
            TCP.fill_offset_v2(bytes, sequence, TCP.make_msg_id_v2(id));

            Byte[] Check = TCP.calculateChecksum(bytes, CRC, receive);
            bytes = TCP.Combine(bytes, Check);

            return bytes;
        }

        public static MavlinkMessage create_heartbeat_msg() {
            return new Msg_heartbeat {
                type = 11,
                autopilot = 12,
                base_mode = 65,
                custom_mode = 65536,
                system_status = 0,
                mavlink_version = 3
            };
        }

        public static MavlinkMessage create_hil_sensor_msg() {
            acceleration = IMU.acceleration;
            velocity = IMU.localVelocity;
            angularVelocity = IMU.angVel;
            compass_data = GPSsimple.compass_data;

            return new Msg_hil_sensor {
                time_usec = (ulong)(TCPserver.prev_simulation_time),
                abs_pressure = 1013.295f + TCP.get_noise(0.1f),
                diff_pressure = 0.0f,
                fields_updated = 7167,
                pressure_alt = 0.05881089f + TCP.get_noise(0.01f),
                temperature = 15.00039f,
                xacc = acceleration.z + TCP.get_noise(0.01f),
                yacc = acceleration.x + TCP.get_noise(0.01f),
                zacc = acceleration.y -9.81f + TCP.get_noise(0.01f),
                xmag = compass_data[0],
                ymag = compass_data[1],
                zmag = compass_data[2],
                xgyro = angularVelocity.z + TCP.get_noise(0.001f),
                ygyro = angularVelocity.x + TCP.get_noise(0.001f),
                zgyro = angularVelocity.y + TCP.get_noise(0.001f)
            };
        }

        public static MavlinkMessage create_hil_gps_msg() {
            lat = (Int32)(GPSsimple.latInt);
            lon = (Int32)(GPSsimple.lonInt);

            return new Msg_hil_gps {
                time_usec = (ulong)(TCPserver.prev_simulation_time),
                fix_type = 3,
                lat = lat + (int)TCP.get_noise(1.0f),
                lon = lon + (int)TCP.get_noise(1.0f),
                alt = -64 + (int)TCP.get_noise(10.0f),
                eph = 100,
                epv = 100,
                vel = 65535,
                vn = (short)(0 + (int)TCP.get_noise(1.0f)),
                ve = (short)(0 + (int)TCP.get_noise(1.0f)),
                vd = (short)(0 + (int)TCP.get_noise(1.0f)),
                cog = 65535,
                satellites_visible = 10
            };
        }
    }
}