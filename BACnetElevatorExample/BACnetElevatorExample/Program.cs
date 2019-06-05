using BACnetStackDLLServerCSharpExample;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace BACnetElevatorExample
{
    class Program
    {
        static void Main(string[] args)
        {
            BACnetServer bacnetServer = new BACnetServer();
            bacnetServer.Run();
        }

        unsafe class BACnetServer
        {
            // UDP 
            UdpClient udpServer;
            IPEndPoint RemoteIpEndPoint;

            // Settings 
            const UInt16 SETTING_BACNET_PORT = 47808;
            const UInt32 SETTING_DEVICE_INSTANCE = 389001;
            const string SETTING_DEVICE_NAME = "Elevator Example";



            // Description of system. 
            // 
            // *-------------------* *------------------------------*
            // |                   | |                              | 
            // | |======| |======| | |                     *------* |           |======| 
            // | |======| |======| | |                     |  E   | |           |======| 
            // | |======| |======| | |                     | 2003 | |           |======| 
            // | |======| |======| | | *------*  *------*  *------* | *------*  |======| 
            // | |= A ==| |= B ==| | | |  C   |  |  D   |  |  F   | | |  G   |  |= H ==| 
            // | | 1001 | | 1002 | | | | 2001 |  | 2002 |  | 2004 | | | 2005 |  | 1003 | 
            // | |======| |======| | | *------*  *------|  *------* | *------*  |======| 
            // |       1000        | |             2000             |     ^        ^
            // *-------------------* *------------------------------*     |        |
            //           ^                           ^         ^          |        \----- Single escalator without group
            //           |                           |         |          \-------------- Single lift without group
            //           |                           |         \------------------------- Double decker Lift
            //           |                           \----------------------------------- Mulitple of lifts in a group
            //           \--------------------------------------------------------------- Mulitple of escalator in a group
            // 

            // Two escalators 
            // ----------------------------------------------------------------------------------
            // ESCALATOR GROUP 
            const UInt32 SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE = 1000;
            const string SETTING_ELEVATOR_GROUP_OF_ESCALATOR_NAME = "ESCALATOR Group";
            const UInt32 SETTING_ELEVATOR_GROUP_OF_ESCALATOR_MACHINE_ROOM_ID = 1;
            const Byte SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID = 1;
            
            // ESCALATOR A 
            const UInt32 SETTING_ESCALATOR_A_INSTANCE = 1001;
            const string SETTING_ESCALATOR_A_NAME = "Moving sidewalk (A)";
            const Byte SETTING_ESCALATOR_A_INSTALLATION_ID = 1;

            // ESCALATOR B
            const UInt32 SETTING_ESCALATOR_B_INSTANCE = 1002;
            const string SETTING_ESCALATOR_B_NAME = "Moving sidewalk (B)";
            const Byte SETTING_ESCALATOR_B_INSTALLATION_ID = 1;


            // Two independent lifs. 
            // ----------------------------------------------------------------------------------
            // LIFT GROUP 
            const UInt32 SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE = 2000;
            const string SETTING_ELEVATOR_GROUP_OF_LIFT_NAME = "LIFT Group";
            const UInt32 SETTING_ELEVATOR_GROUP_OF_LIFT_MACHINE_ROOM_ID = 2;
            const Byte SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID = 2;

            // LIFT C
            const UInt32 SETTING_LIFT_C_INSTANCE = 2001;
            const string SETTING_LIFT_C_NAME = "People Lifter (C)";
            const Byte SETTING_LIFT_C_INSTALLATION_ID = 2;

            // LIFT D 
            const UInt32 SETTING_LIFT_D_INSTANCE = 2002;
            const string SETTING_LIFT_D_NAME = "People Lifter (D)";
            const Byte SETTING_LIFT_D_INSTALLATION_ID = 2;

            // A double decker lift.  
            // ----------------------------------------------------------------------------------
            // LIFT E
            const UInt32 SETTING_LIFT_E_INSTANCE = 2003;
            const string SETTING_LIFT_E_NAME = "Top of a double decker People Lifter (E)";
            const Byte SETTING_LIFT_E_INSTALLATION_ID = 2;

            // LIFT F
            const UInt32 SETTING_LIFT_F_INSTANCE = 2004;
            const string SETTING_LIFT_F_NAME = "Bottom of a double decker People Lifter (F)";
            const Byte SETTING_LIFT_F_INSTALLATION_ID = 2;

            // Lift and escalators without groups. 
            // ----------------------------------------------------------------------------------
            // Lift G 
            const UInt32 SETTING_LIFT_G_INSTANCE = 2005;
            const string SETTING_LIFT_G_NAME = "People Lifter (G)";
            const Byte SETTING_LIFT_G_INSTALLATION_ID = 3;
            const Byte SETTING_LIFT_G_GROUP_ID = 3;

            // ESCALATOR H
            const UInt32 SETTING_ESCALATOR_H_INSTANCE = 1003;
            const string SETTING_ESCALATOR_H_NAME = "Moving sidewalk (H)";
            const Byte SETTING_ESCALATOR_H_INSTALLATION_ID = 4;
            const Byte SETTING_ESCALATOR_H_GROUP_ID = 4;


            // Version 
            const string APPLICATION_VERSION = "0.0.1";

            public void Run()
            {
                Console.WriteLine("Starting BACnetElevatorExample version {0}", APPLICATION_VERSION);
                Console.WriteLine("FYI: BACnet Stack version: {0}.{1}.{2}.{3}",
                    CASBACnetStackAdapter.GetAPIMajorVersion(),
                    CASBACnetStackAdapter.GetAPIMinorVersion(),
                    CASBACnetStackAdapter.GetAPIPatchVersion(),
                    CASBACnetStackAdapter.GetAPIBuildVersion());

                // Send/Recv callbacks. 
                CASBACnetStackAdapter.RegisterCallbackSendMessage(SendMessage);
                CASBACnetStackAdapter.RegisterCallbackReceiveMessage(RecvMessage);
                CASBACnetStackAdapter.RegisterCallbackGetSystemTime(CallbackGetSystemTime);

                // Get Datatype Callbacks 
                CASBACnetStackAdapter.RegisterCallbackGetPropertyCharacterString(CallbackGetPropertyCharString);

                CASBACnetStackAdapter.RegisterCallbackGetPropertyReal(CallbackGetPropertyReal);
                CASBACnetStackAdapter.RegisterCallbackGetPropertyEnumerated(CallbackGetEnumerated);


                // Add the device. 
                CASBACnetStackAdapter.AddDevice(SETTING_DEVICE_INSTANCE);

                // Enable optional services 
                CASBACnetStackAdapter.SetServiceEnabled(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.SERVICE_READ_PROPERTY_MULTIPLE, true);
                CASBACnetStackAdapter.SetServiceEnabled(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.SERVICE_WRITE_PROPERTY, true);
                CASBACnetStackAdapter.SetServiceEnabled(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.SERVICE_WRITE_PROPERTY_MULTIPLE, true);

                // ESCALATOR Group 
                CASBACnetStackAdapter.AddElevatorGroupObject(SETTING_DEVICE_INSTANCE, SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE, SETTING_ELEVATOR_GROUP_OF_ESCALATOR_MACHINE_ROOM_ID, SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID, false, false);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, SETTING_ESCALATOR_A_INSTANCE, SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE, SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID, SETTING_ESCALATOR_A_INSTALLATION_ID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, SETTING_ESCALATOR_B_INSTANCE, SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE, SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID, SETTING_ESCALATOR_B_INSTALLATION_ID);

                // LIFT GROUP
                CASBACnetStackAdapter.AddElevatorGroupObject(SETTING_DEVICE_INSTANCE, SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, SETTING_ELEVATOR_GROUP_OF_LIFT_MACHINE_ROOM_ID, SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, true, true);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, SETTING_LIFT_C_INSTANCE, SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, SETTING_LIFT_C_INSTALLATION_ID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, SETTING_LIFT_D_INSTANCE, SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, SETTING_LIFT_D_INSTALLATION_ID);

                // Double deck lift.                 
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, SETTING_LIFT_E_INSTANCE, SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, SETTING_LIFT_E_INSTALLATION_ID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, SETTING_LIFT_F_INSTANCE, SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, SETTING_LIFT_F_INSTALLATION_ID);
                CASBACnetStackAdapter.SetLiftHigherLowerDeck(SETTING_DEVICE_INSTANCE, SETTING_LIFT_E_INSTANCE, 4194303, SETTING_LIFT_F_INSTANCE);
                CASBACnetStackAdapter.SetLiftHigherLowerDeck(SETTING_DEVICE_INSTANCE, SETTING_LIFT_F_INSTANCE, SETTING_LIFT_E_INSTANCE, 4194303);

                // Indepenent Lift and Escalator 
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, SETTING_LIFT_G_INSTANCE, 4194303, SETTING_LIFT_G_GROUP_ID, SETTING_LIFT_G_INSTALLATION_ID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, SETTING_ESCALATOR_H_INSTANCE, 4194303, SETTING_ESCALATOR_H_GROUP_ID, SETTING_ESCALATOR_H_INSTALLATION_ID);


                // All done with the BACnet setup. 
                Console.WriteLine("FYI: CAS BACnet Stack Setup, successfuly");

                // Open the BACnet port to recive messages. 
                this.udpServer = new UdpClient(SETTING_BACNET_PORT);
                this.RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                // Main loop.
                for (; ; )
                {
                    CASBACnetStackAdapter.Loop();
                }
            }

            public ulong CallbackGetSystemTime()
            {
                // https://stackoverflow.com/questions/9453101/how-do-i-get-epoch-time-in-c
                return (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            }
            public UInt16 SendMessage(System.Byte* message, UInt16 messageLength, System.Byte* connectionString, System.Byte connectionStringLength, System.Byte networkType, Boolean broadcast)
            {
                if (connectionStringLength < 6 || messageLength <= 0)
                {
                    return 0;
                }
                // Extract the connection string into a IP address and port. 
                IPAddress ipAddress = new IPAddress(new byte[] { connectionString[0], connectionString[1], connectionString[2], connectionString[3] });
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, (connectionString[4] + connectionString[5] * 256));

                // Debug 
                Console.WriteLine("FYI: Sending {0} bytes to {1}", messageLength, ipEndPoint.ToString());

                // Copy from the unsafe pointer to a Byte array. 
                byte[] sendBytes = new byte[messageLength];
                Marshal.Copy((IntPtr)message, sendBytes, 0, messageLength);

                try
                {
                    this.udpServer.Send(sendBytes, sendBytes.Length, ipEndPoint);
                    return (UInt16)sendBytes.Length;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                return 0;
            }
            public UInt16 RecvMessage(System.Byte* message, UInt16 maxMessageLength, System.Byte* receivedConnectionString, System.Byte maxConnectionStringLength, System.Byte* receivedConnectionStringLength, System.Byte* networkType)
            {
                try
                {
                    if (this.udpServer.Available > 0)
                    {
                        // Data buffer for incoming data.  
                        byte[] receiveBytes = this.udpServer.Receive(ref this.RemoteIpEndPoint);
                        byte[] ipAddress = RemoteIpEndPoint.Address.GetAddressBytes();
                        byte[] port = BitConverter.GetBytes(UInt16.Parse(RemoteIpEndPoint.Port.ToString()));

                        // Copy from the unsafe pointer to a Byte array. 
                        Marshal.Copy(receiveBytes, 0, (IntPtr)message, receiveBytes.Length);

                        // Copy the Connection string 
                        Marshal.Copy(ipAddress, 0, (IntPtr)receivedConnectionString, 4);
                        Marshal.Copy(port, 0, (IntPtr)receivedConnectionString + 4, 2);
                        *receivedConnectionStringLength = 6;

                        // Debug 
                        Console.WriteLine("FYI: Recving {0} bytes from {1}", receiveBytes.Length, RemoteIpEndPoint.ToString());

                        // Return length. 
                        return (ushort)receiveBytes.Length;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                return 0;
            }

            public bool CallbackGetPropertyCharString(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, System.Char* value, UInt32* valueElementCount, UInt32 maxElementCount, System.Byte encodingType, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetPropertyCharString. objectType={0}, objectInstance={1}, propertyIdentifier={2}", objectType, objectInstance, propertyIdentifier);
                
                if (deviceInstance != SETTING_DEVICE_INSTANCE)
                {
                    return false; // Not for this device. 
                }

                if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OBJECT_NAME)
                {
                    if(objectType == CASBACnetStackAdapter.OBJECT_TYPE_DEVICE && objectInstance == SETTING_DEVICE_INSTANCE)
                    {
                        byte[] nameAsBuffer = ASCIIEncoding.ASCII.GetBytes(SETTING_DEVICE_NAME);
                        *valueElementCount = maxElementCount; 
                        if (nameAsBuffer.Length < *valueElementCount) {
                            *valueElementCount = Convert.ToUInt32( nameAsBuffer.Length ) ; 
                        }
                        Marshal.Copy(nameAsBuffer, 0, (IntPtr)value, Convert.ToInt32( *valueElementCount));
                        return true;
                    }                     
                }

                Console.WriteLine("FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false; // Could not handle this request. 
            }

            public bool CallbackGetPropertyReal(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, float* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetPropertyReal. objectType={0}, objectInstance={1}, propertyIdentifier={2}", objectType, objectInstance, propertyIdentifier);
                Console.WriteLine("FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier );
                return false; 
            }
            public bool CallbackGetEnumerated(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, UInt32* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetEnumerated. objectType={0}, objectInstance={1}, propertyIdentifier={2}", objectType, objectInstance, propertyIdentifier);
                Console.WriteLine("FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false; 
            }
        }
    }
}
