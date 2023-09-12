using System;
using System.Collections; 
using System.Collections.Generic; 
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Threading; 
using UnityEngine;  
using MavLink;
using System.Linq;

namespace TCPheader {
    public class TCP {

        public static float get_noise(float magnitude) {
            System.Random rand = new System.Random();
            float randomInt = rand.Next(1,101);
            float noise = (randomInt - 50.0f) / 100.0f * magnitude;
            return noise;
	    }

        public bool is_known_msg(uint id) {
            if (id != 0 && id != 76 && id != 93) {
                return false;
            }
            else return true;
        }

        public int get_offset(Byte[] bytes) {
            int offset = new int();
            if (bytes[0] == 253) offset = 10;
            else if (bytes[0] == 254) offset = 6;
            else Debug.Log("Failed to get offset");
            return offset;
        }

        public UInt32 get_msg_id_v2(Byte[] bytes) {
            // Bytes at indeces 7,8 and 9 are the low, middle and high bytes of the ID respectively.
            UInt32 id = (uint)bytes[7] | (uint)bytes[8] << 8 | (uint)bytes[9] << 16;
            return id;
        }

        public Byte[] make_msg_id_v2(UInt32 id) {
            Byte[] id_bytes = BitConverter.GetBytes(id);
            Array.Resize(ref id_bytes, id_bytes.Length - 1);
            return id_bytes;
        }

        public Tuple<List<Byte[]>, List<uint>> handle_multiple_msgs(Byte[] bytes, Byte[] hbCRC, Byte[] commandCRC, Byte[] actuatorCRC) {
            List<Byte[]> msgs_list = new List<byte[]>();
            List<uint> id_list = new List<uint>();
            List<int> index_list = new List<int>();
            for (int i = 0; i < bytes.Length - 10; i++) {
                if (bytes[i] == 253) {
                    UInt32 msg_id = (uint)bytes[i + 7] | (uint)bytes[i + 8] << 8 | (uint)bytes[i + 9] << 16;
                    if (msg_id == 0 | msg_id == 93 | msg_id == 76) {
                        index_list.Add(i);
                        id_list.Add(msg_id);                       
                    }                    
                }
            }

            if (index_list.Count > 1) { // multiple messages in array
                for (int i = 0; i < index_list.Count - 1; i++) {
                    Byte[] CRCextra = new Byte[] {MavLinkSerializer.Lookup[(int)id_list[i]].CrcExtra};
                    if (i < (index_list.Count - 1) && is_correct_checksum(bytes[index_list[i] .. index_list[i + 1]], CRCextra, true)) {
                        msgs_list.Add(bytes[index_list[i] .. index_list[i + 1]]);
                    }
                    else if (is_correct_checksum(bytes[(index_list[index_list.Count - 1]) .. (bytes.Length)], CRCextra, true)) {
                        msgs_list.Add(bytes[(index_list[index_list.Count - 1]) .. (bytes.Length)]); // final message from array
                    }
                    else {
                        msgs_list.Add(bytes);
                    }
                }
            }
            else {
                msgs_list.Add(bytes); // single message in array
            }
            return Tuple.Create<List<Byte[]>, List<uint>>(msgs_list, id_list);
        }

        public Byte[] removeZeros(Byte[] bytes) {
            while (bytes.Length > 0 && bytes[bytes.Length - 1] == 0) {
				Array.Resize(ref bytes, bytes.Length - 1);
			}
            return bytes;
        }

        public static byte[] Combine(byte[] first, byte[] second) {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public Byte[] fill_offset_v2(Byte[] bytes, int sequence, Byte[] id) {
            bytes[0] = 253; // magic
            bytes[1] = (Byte)(bytes.Length - 10); // payload length
            bytes[2] = 0;
            bytes[3] = 0;
            bytes[4] = (Byte)sequence;
            bytes[5] = 0; // sysid
            bytes[6] = 0; // compid
            Array.Copy(id, 0, bytes, 7, id.Length); // Bytes at indeces 7 - 9 are the message id
            return bytes;
        }

        public Byte[] calculateChecksum(Byte[] bytes, Byte[] CRCextra, bool receive) {
            if (receive) {
                Array.Resize(ref bytes, bytes.Length - 2); // Remove checksum from end of msg  
            }
            bytes = bytes.Skip(1).ToArray(); // Remove magic from beginning of msg
            bytes = TCP.Combine(bytes, CRCextra); // Add CRC
            UInt16 msgCheck = Mavlink_Crc.Calculate(bytes, 0, (ushort)bytes.Length);
            Byte[] Checksumbytes = BitConverter.GetBytes(msgCheck);
            return Checksumbytes;
        }

        public bool is_correct_checksum(Byte[] msg, Byte[] CRCextra, bool receive) {
            if (msg.Length > 2) {
                Byte[] Checksum = calculateChecksum(msg, CRCextra, receive);

                if (msg[msg.Length - 2] == Checksum[0] && msg[msg.Length - 1] == Checksum[1]) {
                return true;
                }
                else {
                    return false;
                }
            }
            else {
                return false;
            }

        }

       	public static byte[] StringToByteArray(string hex) {
            return Enumerable.Range(0, hex.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                    .ToArray();
	    }

        public void log_heartbeat(Msg_heartbeat hbMsg) {
            Debug.Log("Heartbeat message:");
            Debug.Log("Type: " + hbMsg.type);
            Debug.Log("Autopilot: " + hbMsg.autopilot);
            Debug.Log("Base_mode: " + hbMsg.base_mode);
            Debug.Log("Custom_mode: " + hbMsg.custom_mode);
            Debug.Log("System_status: " + hbMsg.system_status);
            Debug.Log("Mavlink_version: " + hbMsg.mavlink_version);
        }

        public void log_command_long(Msg_command_long cmdMsg) {
            Debug.Log("Command long message:");
            Debug.Log("Target system: " + cmdMsg.target_system); 
            Debug.Log("Target component: " + cmdMsg.target_component);
            Debug.Log("Command: " + cmdMsg.command);
            Debug.Log("Confirmation: " + cmdMsg.confirmation);
            Debug.Log("Params: " + cmdMsg.param1 
                            + ", " + cmdMsg.param2 
                            + ", " + cmdMsg.param3 
                            + ", " + cmdMsg.param4 
                            + ", " + cmdMsg.param5 
                            + ", " + cmdMsg.param6 
                            + ", " + cmdMsg.param7);
        }

        public void log_hil_actuator_controls(Msg_hil_actuator_controls ctrlMsg) {
            Debug.Log("HIL actuator controls message:");
            Debug.Log("Timestamp: " + ctrlMsg.time_usec);
            Debug.Log("Controls: " + String.Join(", ", new List<float>(ctrlMsg.controls).ConvertAll(i => i.ToString()).ToArray()));
            Debug.Log("Mode: " + ctrlMsg.mode);
            Debug.Log("Flags: " + ctrlMsg.flags);
        }

        public void log_hil_gps(Msg_hil_gps gpsTest) {
            Debug.Log("GPS message test:");
            Debug.Log("time_usec: " + gpsTest.time_usec);
            Debug.Log("fix_type: " + gpsTest.fix_type);
            Debug.Log("lat: " + gpsTest.lat);
            Debug.Log("lon: " + gpsTest.lon);
            Debug.Log("alt: " + gpsTest.alt);
            Debug.Log("eph: " + gpsTest.eph);
            Debug.Log("epv: " + gpsTest.epv);
            Debug.Log("vel: " + gpsTest.vel);
            Debug.Log("vn: " + gpsTest.vn);
            Debug.Log("ve: " + gpsTest.ve);
            Debug.Log("vd: " + gpsTest.vd);
            Debug.Log("cog: " + gpsTest.cog);
            Debug.Log("satellites visible: " + gpsTest.satellites_visible);
        }

        public void log_hil_sensor(Msg_hil_sensor sensorTest) {
            Debug.Log("Sensor message test:");
            Debug.Log("time_usec:" + sensorTest.time_usec);
            Debug.Log("xacc:" + sensorTest.xacc);
            Debug.Log("yacc:" + sensorTest.yacc);
            Debug.Log("zacc:" + sensorTest.zacc);
            Debug.Log("xgyro:" + sensorTest.xgyro);
            Debug.Log("ygyro:" + sensorTest.ygyro);
            Debug.Log("zgyro:" + sensorTest.zgyro);
            Debug.Log("xmag:" + sensorTest.xmag);
            Debug.Log("ymag:" + sensorTest.ymag);
            Debug.Log("zmag:" + sensorTest.zmag);
            Debug.Log("abs_pressure:" + sensorTest.abs_pressure);
            Debug.Log("diff_pressure:" + sensorTest.diff_pressure);
            Debug.Log("pressure_alt:" + sensorTest.pressure_alt);
            Debug.Log("temperature:" + sensorTest.temperature);
            Debug.Log("fields_updated:" + sensorTest.fields_updated);
        }
    }
}