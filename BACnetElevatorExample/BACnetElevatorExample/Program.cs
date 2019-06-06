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

            // A Database to hold the current state of the 
            private ExampleDatabase database = new ExampleDatabase();


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
                CASBACnetStackAdapter.RegisterCallbackGetPropertyUnsignedInteger(CallbackGetUnsignedInteger);
                CASBACnetStackAdapter.RegisterCallbackGetPropertyBool(CallbackGetPropertyBool);
                CASBACnetStackAdapter.RegisterCallbackGetListOfEnumerations(CallbackGetListOfEnumerations);
                CASBACnetStackAdapter.RegisterCallbackGetListElevatorGroupLandingCall(CallbackGetListElevatorGroupLandingCall);

                // Add the device. 
                CASBACnetStackAdapter.AddDevice(ExampleDatabase.SETTING_DEVICE_INSTANCE);

                // Enable optional services 
                CASBACnetStackAdapter.SetServiceEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.SERVICE_READ_PROPERTY_MULTIPLE, true);
                CASBACnetStackAdapter.SetServiceEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.SERVICE_WRITE_PROPERTY, true);
                CASBACnetStackAdapter.SetServiceEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.SERVICE_WRITE_PROPERTY_MULTIPLE, true);

                // ESCALATOR Group 
                CASBACnetStackAdapter.AddElevatorGroupObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_MACHINE_ROOM_ID, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID, false, false);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, ExampleDatabase.SETTING_ESCALATOR_A_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID, ExampleDatabase.SETTING_ESCALATOR_A_INSTALLATION_ID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, ExampleDatabase.SETTING_ESCALATOR_B_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID, ExampleDatabase.SETTING_ESCALATOR_B_INSTALLATION_ID);

                // LIFT GROUP
                CASBACnetStackAdapter.AddElevatorGroupObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_MACHINE_ROOM_ID, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, true, true);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_C_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, ExampleDatabase.SETTING_LIFT_C_INSTALLATION_ID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_D_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, ExampleDatabase.SETTING_LIFT_D_INSTALLATION_ID);

                // Double deck lift.                 
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_E_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, ExampleDatabase.SETTING_LIFT_E_INSTALLATION_ID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_F_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, ExampleDatabase.SETTING_LIFT_F_INSTALLATION_ID);
                CASBACnetStackAdapter.SetLiftHigherLowerDeck(ExampleDatabase.SETTING_DEVICE_INSTANCE, ExampleDatabase.SETTING_LIFT_E_INSTANCE, 4194303, ExampleDatabase.SETTING_LIFT_F_INSTANCE);
                CASBACnetStackAdapter.SetLiftHigherLowerDeck(ExampleDatabase.SETTING_DEVICE_INSTANCE, ExampleDatabase.SETTING_LIFT_F_INSTANCE, ExampleDatabase.SETTING_LIFT_E_INSTANCE, 4194303);

                // Indepenent Lift and Escalator 
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_G_INSTANCE, 4194303, ExampleDatabase.SETTING_LIFT_G_GROUP_ID, ExampleDatabase.SETTING_LIFT_G_INSTALLATION_ID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, ExampleDatabase.SETTING_ESCALATOR_H_INSTANCE, 4194303, ExampleDatabase.SETTING_ESCALATOR_H_GROUP_ID, ExampleDatabase.SETTING_ESCALATOR_H_INSTALLATION_ID);

                // Enabled optional properties. 
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FLOORTEXT, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ELEVATOR_GROUP, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER, true);

                // All done with the BACnet setup. 
                Console.WriteLine("FYI: CAS BACnet Stack Setup, successfuly");

                // Database setup 
                database.Setup();

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

            private UInt32 UpdateStringAndReturnSize(System.Char* value, UInt32 maxElementCount, string stringAsVallue)
            {
                byte[] nameAsBuffer = ASCIIEncoding.ASCII.GetBytes(stringAsVallue);
                UInt32 valueElementCount = maxElementCount;
                if (nameAsBuffer.Length < valueElementCount)
                {
                    valueElementCount = Convert.ToUInt32(nameAsBuffer.Length);
                }
                Marshal.Copy(nameAsBuffer, 0, (IntPtr)value, Convert.ToInt32(valueElementCount));
                return valueElementCount; 
            }

            public bool CallbackGetPropertyCharString(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, System.Char* value, UInt32* valueElementCount, UInt32 maxElementCount, System.Byte encodingType, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetPropertyCharString. objectType={0}, objectInstance={1}, propertyIdentifier={2}, propertyArrayIndex={3}", objectType, objectInstance, propertyIdentifier, propertyArrayIndex);

                if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OBJECT_NAME)
                {
                    if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_DEVICE && objectInstance == ExampleDatabase.SETTING_DEVICE_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_DEVICE_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ELEVATOR_GROUP && objectInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR && objectInstance == ExampleDatabase.SETTING_ESCALATOR_A_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_ESCALATOR_A_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR && objectInstance == ExampleDatabase.SETTING_ESCALATOR_B_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_ESCALATOR_B_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ELEVATOR_GROUP && objectInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT && objectInstance == ExampleDatabase.SETTING_LIFT_C_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_LIFT_C_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT && objectInstance == ExampleDatabase.SETTING_LIFT_D_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_LIFT_D_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT && objectInstance == ExampleDatabase.SETTING_LIFT_E_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_LIFT_E_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT && objectInstance == ExampleDatabase.SETTING_LIFT_F_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_LIFT_F_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT && objectInstance == ExampleDatabase.SETTING_LIFT_G_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_LIFT_G_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR && objectInstance == ExampleDatabase.SETTING_ESCALATOR_H_INSTANCE)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_ESCALATOR_H_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_POSITIVE_INTEGER_VALUE && objectInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_MACHINE_ROOM_ID)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_MACHINE_ROOM_1_NAME);
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_POSITIVE_INTEGER_VALUE && objectInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_MACHINE_ROOM_ID)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_MACHINE_ROOM_2_NAME);
                        return true;
                    }
                }
                else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FLOORTEXT && useArrayIndex)
                {
                    if (propertyArrayIndex <= ExampleDatabase.FLOOR_NAMES.Length)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.FLOOR_NAMES[propertyArrayIndex - 1]);
                        return true;
                    }
                }
                else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORTEXT && useArrayIndex)
                {
                    if (propertyArrayIndex <= ExampleDatabase.LIFT_CAR_DOOR_TEXT.Length)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.LIFT_CAR_DOOR_TEXT[propertyArrayIndex - 1]);
                        return true;
                    }
                }

                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false; // Could not handle this request. 
            }

            public bool CallbackGetPropertyReal(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, float* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetPropertyReal. objectType={0}, objectInstance={1}, propertyIdentifier={2}", objectType, objectInstance, propertyIdentifier);

                if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ELEVATOR_GROUP &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER)
                {
                    *value = ExampleDatabase.ELEVATOR_GROUP_ENERGY_METER_VALUE;
                    return true;
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR &&
                  propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER)
                {
                    *value = ExampleDatabase.ESCALATOR_ENERGY_METER_VALUE;
                    return true;
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                  propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER)
                {
                    *value = ExampleDatabase.LIFT_ENERGY_METER_VALUE;
                    return true;
                }

                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier );
                return false; 
            }
            public bool CallbackGetEnumerated(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, UInt32* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetEnumerated. objectType={0}, objectInstance={1}, propertyIdentifier={2}", objectType, objectInstance, propertyIdentifier);

                if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ELEVATOR_GROUP &&
                    objectInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_GROUPMODE)
                {
                    *value = ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_MODE;
                    return true;
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORSTATUS)
                {
                    *value = ExampleDatabase.LIFT_CAR_DOOR_STATUS;
                    return true;
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OPERATIONDIRECTION)
                {
                    *value = ExampleDatabase.ESCALATOR_OPERATION_DIRECTION;
                    return true;
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARMOVINGDIRECTION)
                {
                    *value = ExampleDatabase.LIFT_CAR_MOVING_DIRECTION;
                    return true;
                }


                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }

            public bool CallbackGetUnsignedInteger(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, UInt32* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetUnsignedInteger. objectType={0}, objectInstance={1}, propertyIdentifier={2}, propertyArrayIndex={3}", objectType, objectInstance, propertyIdentifier, propertyArrayIndex);
                if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FLOORTEXT)
                {
                    *value = Convert.ToUInt32(ExampleDatabase.FLOOR_NAMES.Length );
                    return true; 
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORTEXT)
                {
                    *value = Convert.ToUInt32(ExampleDatabase.LIFT_CAR_DOOR_TEXT.Length);
                    return true;
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORSTATUS)
                {
                    *value = Convert.ToUInt32(ExampleDatabase.LIFT_CAR_DOOR_COUNT );
                    return true;
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARPOSITION)
                {
                    *value = Convert.ToUInt32(ExampleDatabase.LIFT_CAR_POSITION);
                    return true;
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_POSITIVE_INTEGER_VALUE &&
                   objectInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_MACHINE_ROOM_ID &&
                   propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE)
                {
                    *value = ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_MACHINE_ROOM_ID;
                    return true;
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_POSITIVE_INTEGER_VALUE &&
                   objectInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_MACHINE_ROOM_ID &&
                   propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE)
                {
                    *value = ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_MACHINE_ROOM_ID;
                    return true;
                }


                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }

            public bool CallbackGetPropertyBool(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, bool* value, [In, MarshalAs(UnmanagedType.I1)] bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetPropertyBool. objectType={0}, objectInstance={1}, propertyIdentifier={2}, propertyArrayIndex={3}", objectType, objectInstance, propertyIdentifier, propertyArrayIndex);

                if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PASSENGERALARM)
                {
                    *value = ExampleDatabase.ESCALATOR_PASSENGER_ALARM;
                    return true; 
                }
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                 propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PASSENGERALARM)
                {
                    *value = ExampleDatabase.LIFT_PASSENGER_ALARM;
                    return true;
                }

                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }

            public bool CallbackGetListOfEnumerations(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, Byte rangeOption, UInt32 rangeIndexOrSequenceNumber, [In, MarshalAs(UnmanagedType.I1)] bool rangeInPositiveDirection, UInt32* enumeration, bool* more)
            {
                Console.WriteLine("FYI: Request for CallbackGetListOfEnumerations. objectType={0}, objectInstance={1}, propertyIdentifier={2}, rangeIndexOrSequenceNumber={3}", objectType, objectInstance, propertyIdentifier, rangeIndexOrSequenceNumber);

                if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS &&
                    rangeOption == 0)
                {
                    UInt32 count = 0;
                    foreach (UInt32 fault in database.ESCALATOR_FAULT_SINGALS)
                    {
                        if( count == rangeIndexOrSequenceNumber)
                        {
                            *enumeration = fault;
                            *more = (count < database.ESCALATOR_FAULT_SINGALS.Count - 1);
                            Console.WriteLine("   FYI: Return *enumeration={0}, *more={1}", *enumeration, *more);
                            return true; 
                        }
                        count++; 
                    }

                    // Empty list. 
                    *more = false; 
                    return true; 
                } else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                           propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS &&
                           rangeOption == 0)
                {
                    UInt32 count = 0;
                    foreach (UInt32 fault in database.LIFT_FAULT_SINGALS)
                    {
                        if (count == rangeIndexOrSequenceNumber)
                        {
                            *enumeration = fault;
                            *more = (count < database.LIFT_FAULT_SINGALS.Count - 1);
                            Console.WriteLine("   FYI: Return *enumeration={0}, *more={1}", *enumeration, *more);
                            return true;
                        }
                        count++;
                    }

                    // Empty list. 
                    *more = false;
                    return true;
                }



                

                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false; 
            }

            public bool CallbackGetListElevatorGroupLandingCall(UInt32 deviceInstance, UInt32 elevatorGroupInstance, Byte rangeOption, UInt32 rangeIndexOrSequence, 
                Byte* floorNumber, Byte* commandChoice, UInt32* bacnetLiftCarDirection, Byte* destination, bool* useFloorText, System.Char* floorText, 
                UInt16 floorTextMaxLength, UInt16* floorTextLength, bool* more)
            {
                Console.WriteLine("FYI: Request for CallbackGetListElevatorGroupLandingCall. elevatorGroupInstance={0}, ", elevatorGroupInstance);

                // ToDo: 

                *more = false;
                return true;

                Console.WriteLine("   FYI: Not implmented. ");
                return false; 
            }
        }
    }
}
