/**
 * Windows BACnet Elevator Example
 * ----------------------------------------------------------------------------
 * In this CAS BACnet Stack example, we create a BACnet IP server with Elevator groups, 
 * Lifs, Escalator objects using C#. This project was designed as an example for someone 
 * that wants to implment Elevator groups, Lifs, Escalator objects in a BACnet IP server using C#.
 *
 * More information https://github.com/chipkin/Windows-BACnetElevatorExample
 * 
 * Created by: Steven Smethurst 
 * Created on: June 7, 2019 
 * Last updated: July 4, 2019 
 */

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
            const string APPLICATION_VERSION = "0.0.2";

            public void Run()
            {
                // Prints the version of the application and the CAS BACnet stack. 
                Console.WriteLine("Starting BACnetElevatorExample version {0}.{1}", APPLICATION_VERSION, CIBuildVersion.CIBUILDNUMBER);
                Console.WriteLine("https://github.com/chipkin/Windows-BACnetElevatorExample");
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

                // Set Datatype Callbacks 
                CASBACnetStackAdapter.RegisterCallbackSetPropertyUnsignedInteger(CallbackSetUnsignedInteger);

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
                CASBACnetStackAdapter.SetPropertyWritable(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_C_INSTANCE, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_MAKINGCARCALL, true);
                CASBACnetStackAdapter.SetPropertyWritable(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_D_INSTANCE, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_MAKINGCARCALL, true);

                // Double deck lift.                 
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_E_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, ExampleDatabase.SETTING_LIFT_E_INSTALLATION_ID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_F_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, ExampleDatabase.SETTING_LIFT_F_INSTALLATION_ID);
                CASBACnetStackAdapter.SetLiftHigherLowerDeck(ExampleDatabase.SETTING_DEVICE_INSTANCE, ExampleDatabase.SETTING_LIFT_E_INSTANCE, 4194303, ExampleDatabase.SETTING_LIFT_F_INSTANCE);
                CASBACnetStackAdapter.SetLiftHigherLowerDeck(ExampleDatabase.SETTING_DEVICE_INSTANCE, ExampleDatabase.SETTING_LIFT_F_INSTANCE, ExampleDatabase.SETTING_LIFT_E_INSTANCE, 4194303);

                // Indepenent Lift with two doors.
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_G_INSTANCE, 4194303, ExampleDatabase.SETTING_LIFT_G_GROUP_ID, ExampleDatabase.SETTING_LIFT_G_INSTALLATION_ID);
                CASBACnetStackAdapter.SetPropertyWritable(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.SETTING_LIFT_G_INSTANCE, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_MAKINGCARCALL, true);

                // Indepenent and Escalator
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, ExampleDatabase.SETTING_ESCALATOR_H_INSTANCE, 4194303, ExampleDatabase.SETTING_ESCALATOR_H_GROUP_ID, ExampleDatabase.SETTING_ESCALATOR_H_INSTALLATION_ID);

                // Enabled optional properties. 
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FLOORTEXT, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(ExampleDatabase.SETTING_DEVICE_INSTANCE, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_MAKINGCARCALL, true);

                // All done with the BACnet setup. 
                Console.WriteLine("FYI: CAS BACnet Stack Setup, successfuly");

                // Database setup 
                database.Setup();

                // Open the BACnet port to recive messages. 
                this.udpServer = new UdpClient(SETTING_BACNET_PORT);
                this.RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                // Main loop.
                Console.WriteLine("FYI: Starting main loop");
                for (; ; )
                {
                    CASBACnetStackAdapter.Loop();
                    database.Loop(); 
                }
            }

            public ulong CallbackGetSystemTime()
            {
                // Get the system time as Linux EPOCH
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

                // Object_Name
                // This property, of type CharacterString, shall represent a name for the object that is unique within the BACnet device that
                // maintains it. The minimum length of the string shall be one character.The set of characters used in the Object_Name shall be
                // restricted to printable characters.
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
                // 12.59.9 Floor_Text
                // This property, of type BACnetARRAY of CharacterString, represents the descriptions or names for the floors. The universal
                // floor number serves as an index into this array. The size of this array shall match the highest universal floor number served
                // by this lift.
                else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FLOORTEXT && useArrayIndex)
                {
                    if (propertyArrayIndex <= ExampleDatabase.FLOOR_NAMES.Length)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.FLOOR_NAMES[propertyArrayIndex - 1]);
                        return true;
                    }
                }
                // 12.59.10 Car_Door_Text
                // This property, of type BACnetARRAY of CharacterString, represents the descriptions or names for the doors of the lift car.
                // Each array element represents the description or name for the door of the car assigned to this array element.
                else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORTEXT && useArrayIndex)
                {
                    if (objectInstance == ExampleDatabase.SETTING_LIFT_G_INSTANCE )
                    {
                        // Lift "G" has two doors
                        if (propertyArrayIndex <= ExampleDatabase.SETTING_LIFT_G_DOOR_TEXT.Length)
                        {
                            *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.SETTING_LIFT_G_DOOR_TEXT[propertyArrayIndex - 1]);
                            return true;
                        }                        
                    } else {
                        // All other lifts have 1 door. 
                        if (propertyArrayIndex <= ExampleDatabase.LIFT_CAR_DOOR_TEXT.Length)
                        {
                            *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, ExampleDatabase.LIFT_CAR_DOOR_TEXT[propertyArrayIndex - 1]);
                            return true;
                        }
                    }
                }

                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false; // Could not handle this request. 
            }

            public bool CallbackGetPropertyReal(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, float* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetPropertyReal. objectType={0}, objectInstance={1}, propertyIdentifier={2}", objectType, objectInstance, propertyIdentifier);

                // 12.59.27 Energy_Meter
                // This property, of type REAL, indicates the accumulated energy consumption by the lift. The units shall be kilowatt - hours.
                // When this value reaches 99999 kWh, it shall wrap to a value near zero; the particular value to which it wraps is a local
                // matter.
                if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER)
                {
                    if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR )
                    {
                        *value = ExampleDatabase.ESCALATOR_ENERGY_METER_VALUE;
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT )
                    {
                        *value = ExampleDatabase.LIFT_ENERGY_METER_VALUE;
                        return true;
                    }
                }

                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }
            public bool CallbackGetEnumerated(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, UInt32* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetEnumerated. objectType={0}, objectInstance={1}, propertyIdentifier={2}", objectType, objectInstance, propertyIdentifier);

                // 12.58.8 Group_Mode
                // This property, of type BACnetLiftGroupMode, shall convey the operating mode of the group of lifts. This is used to represent
                // some special traffic modes of control of the supervisory controller of a group of lifts. Supervisory controllers are not required
                // to support all modes.Under a special traffic mode, the car dispatching algorithm may be different.
                if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ELEVATOR_GROUP &&
                    objectInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_GROUPMODE)
                {
                    *value = ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_MODE;
                    return true;
                }

                // 12.59.17 Car_Door_Status
                // This property, of type BACnetARRAY of BACnetDoorStatus, indicates the status of the doors on the car. Each array element
                // indicates the status of the car door assigned to this array element.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORSTATUS)
                {
                    *value = ExampleDatabase.LIFT_CAR_DOOR_STATUS;
                    return true;
                }
                // 12.59.15 Car_Moving_Direction
                // This property, of type BACnetLiftCarDirection, represents whether or not this lift's car is moving, and if so, in which
                // direction.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARMOVINGDIRECTION)
                {
                    *value = ExampleDatabase.LIFT_CAR_MOVING_DIRECTION;
                    return true;
                }
                // 12.60.10 Operation_Direction
                // This property, of type BACnetEscalatorOperationDirection, represents the direction and speed in which this escalator is
                // presently moving.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OPERATIONDIRECTION)
                {
                    *value = ExampleDatabase.ESCALATOR_OPERATION_DIRECTION;
                    return true;
                }
                
                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }

            public bool CallbackGetUnsignedInteger(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, UInt32* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetUnsignedInteger. objectType={0}, objectInstance={1}, propertyIdentifier={2}, propertyArrayIndex={3}", objectType, objectInstance, propertyIdentifier, propertyArrayIndex);

                // 12.59.9 Floor_Text
                // This property, of type BACnetARRAY of CharacterString, represents the descriptions or names for the floors. The universal
                // floor number serves as an index into this array.The size of this array shall match the highest universal floor number served
                // by this lift.
                if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FLOORTEXT)
                {
                    *value = Convert.ToUInt32(ExampleDatabase.FLOOR_NAMES.Length);
                    return true;
                }
                // 12.59.10Car_Door_Text
                // This property, of type BACnetARRAY of CharacterString, represents the descriptions or names for the doors of the lift car.
                // Each array element represents the description or name for the door of the car assigned to this array element.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORTEXT)
                {
                    if (objectInstance == ExampleDatabase.SETTING_LIFT_G_INSTANCE)
                    {
                        // Lift "G" has two doors. 
                        *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_G_DOOR_TEXT.Length);
                    }
                    else
                    {
                        // All other doors have the same door count of 1 
                        *value = Convert.ToUInt32(ExampleDatabase.LIFT_CAR_DOOR_TEXT.Length);
                    }

                    return true;
                }

                // 12.59.17 Car_Door_Status
                // This property, of type BACnetARRAY of BACnetDoorStatus, indicates the status of the doors on the car. Each array element
                // indicates the status of the car door assigned to this array element.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORSTATUS)
                {
                    if (objectInstance == ExampleDatabase.SETTING_LIFT_G_INSTANCE)
                    {
                        // Lift "G" has two doors. 
                        *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_G_DOOR_TEXT.Length);
                    }
                    else
                    {
                        // All other doors have the same door count of 1 
                        *value = Convert.ToUInt32(ExampleDatabase.LIFT_CAR_DOOR_TEXT.Length);
                    }
                    return true;
                }
                // 12.59.14Car_Position
                // This property, of type Unsigned8, indicates the universal floor number of this lift's car position.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARPOSITION)
                {
                    *value = Convert.ToUInt32(ExampleDatabase.LIFT_CAR_POSITION);
                    return true;
                }
                // 12.59.12 Making_Car_Call
                // This property, of type BACnetARRAY of Unsigned8, indicates the last car calls written to this property.Writing to this
                // property is equivalent to a passenger requesting that the car stop at the designated floor. Each array element represents the
                // last car call written to this property for the door of the car assigned to this array element. If no car call has been written to an
                // array element, the array element shall indicate a value of zero.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_MAKINGCARCALL)
                {
                    // Array index 0 is used for getting the amount of elements in the array. This number corasponds to the 
                    // Number of doors. 
                    if (propertyArrayIndex == 0)
                    {
                        if (objectInstance == ExampleDatabase.SETTING_LIFT_C_INSTANCE)
                        {
                            *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_C_MAKING_CAR_CALL.Length);
                        }
                        else if (objectInstance == ExampleDatabase.SETTING_LIFT_D_INSTANCE)
                        {
                            *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_D_MAKING_CAR_CALL.Length);
                        }
                        else if (objectInstance == ExampleDatabase.SETTING_LIFT_E_INSTANCE)
                        {
                            *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_E_MAKING_CAR_CALL.Length);
                        }
                        else if (objectInstance == ExampleDatabase.SETTING_LIFT_F_INSTANCE)
                        {
                            *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_F_MAKING_CAR_CALL.Length);
                        }
                        else if (objectInstance == ExampleDatabase.SETTING_LIFT_G_INSTANCE)
                        {
                            // Lift "G" has two doors. 
                            *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_G_MAKING_CAR_CALL.Length);
                        }
                    }
                    else
                    {
                        if (objectInstance == ExampleDatabase.SETTING_LIFT_C_INSTANCE)
                        {
                            if (propertyArrayIndex <= ExampleDatabase.SETTING_LIFT_C_MAKING_CAR_CALL.Length)
                            {
                                *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_C_MAKING_CAR_CALL[propertyArrayIndex - 1]);
                            }
                        }
                        else if (objectInstance == ExampleDatabase.SETTING_LIFT_D_INSTANCE)
                        {
                            if (propertyArrayIndex <= ExampleDatabase.SETTING_LIFT_D_MAKING_CAR_CALL.Length)
                            {
                                *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_D_MAKING_CAR_CALL[propertyArrayIndex - 1]);
                            }
                        }
                        else if (objectInstance == ExampleDatabase.SETTING_LIFT_E_INSTANCE)
                        {
                            if (propertyArrayIndex <= ExampleDatabase.SETTING_LIFT_E_MAKING_CAR_CALL.Length)
                            {
                                *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_E_MAKING_CAR_CALL[propertyArrayIndex - 1]);
                            }
                        }
                        else if (objectInstance == ExampleDatabase.SETTING_LIFT_F_INSTANCE)
                        {
                            if (propertyArrayIndex <= ExampleDatabase.SETTING_LIFT_F_MAKING_CAR_CALL.Length)
                            {
                                *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_F_MAKING_CAR_CALL[propertyArrayIndex - 1]);
                            }
                        }
                        else if (objectInstance == ExampleDatabase.SETTING_LIFT_G_INSTANCE)
                        {
                            // Lift "G" has two doors. 
                            if (propertyArrayIndex <= ExampleDatabase.SETTING_LIFT_G_MAKING_CAR_CALL.Length)
                            {
                                *value = Convert.ToUInt32(ExampleDatabase.SETTING_LIFT_G_MAKING_CAR_CALL[propertyArrayIndex - 1]);
                            }
                        }
                    }
                    return true;
                }

                // 12.58.5 Machine_Room_ID
                // This property, of type BACnetObjectIdentifier, shall reference the Positive Integer Value Object whose Present_Value
                // property contains the identification number for the machine room that contains the group of lifts or escalators represented by
                // this object. if there is no such identification number, this property shall contain an object instance number of 4194303.
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
                // 12.58.6 Group_ID
                // This property, of type Unsigned8, shall represent the identification number for the group of lifts or escalators represented by
                // this object.This identification number shall be unique for the groups in this machine room, but might not be otherwise unique
                // in the building.
                else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_GROUPID)
                {
                    if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ELEVATOR_GROUP &&
                        objectInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE )
                    {
                        *value = ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID;
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ELEVATOR_GROUP &&
                        objectInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE)
                    {
                        *value = ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID;
                        return true;
                    }
                }


                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }

            public bool CallbackGetPropertyBool(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, bool* value, [In, MarshalAs(UnmanagedType.I1)] bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetPropertyBool. objectType={0}, objectInstance={1}, propertyIdentifier={2}, propertyArrayIndex={3}", objectType, objectInstance, propertyIdentifier, propertyArrayIndex);

                // 12.60.17 Passenger_Alarm
                // This property, of type BOOLEAN, indicates whether(TRUE) or not(FALSE) the passenger alarm has been activated, thus
                // stopping the escalator, and the alarm has not yet been cleared by a maintenance technician.
                if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PASSENGERALARM)
                {
                    if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR)
                    {
                        *value = ExampleDatabase.ESCALATOR_PASSENGER_ALARM;
                        return true;
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT)
                    {
                        *value = ExampleDatabase.LIFT_PASSENGER_ALARM;
                        return true;
                    }
                }

                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }

            public bool CallbackGetListOfEnumerations(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, Byte rangeOption, UInt32 rangeIndexOrSequenceNumber, [In, MarshalAs(UnmanagedType.I1)] bool rangeInPositiveDirection, UInt32* enumeration, bool* more)
            {
                Console.WriteLine("FYI: Request for CallbackGetListOfEnumerations. objectType={0}, objectInstance={1}, propertyIdentifier={2}, rangeIndexOrSequenceNumber={3}", objectType, objectInstance, propertyIdentifier, rangeIndexOrSequenceNumber);

                // 12.60.16 Fault_Signals
                // This property, of type BACnetLIST of BACnetEscalatorFault, represents a list of values that indicates fault conditions of the
                // escalator. The mechanism for determining the existence of a fault condition is a local matter.
                if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS)
                {
                    if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR && rangeOption == 0)
                    {
                        UInt32 count = 0;
                        foreach (UInt32 fault in database.ESCALATOR_FAULT_SINGALS)
                        {
                            if (count == rangeIndexOrSequenceNumber)
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
                    }
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT && rangeOption == 0)
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

            public bool CallbackSetUnsignedInteger(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, UInt32 value, bool useArrayIndex, UInt32 propertyArrayIndex, System.Byte priority, UInt32* errorCode)
            {
                Console.WriteLine("FYI: Request for CallbackSetUnsignedInteger. objectType={0}, objectInstance={1}, propertyIdentifier={2}, propertyArrayIndex={3}, value={4}, priority={5}", objectType, objectInstance, propertyIdentifier, propertyArrayIndex, value, priority);

                // 12.59.12 Making_Car_Call
                // This property, of type BACnetARRAY of Unsigned8, indicates the last car calls written to this property. Writing to this
                // property is equivalent to a passenger requesting that the car stop at the designated floor. Each array element represents the
                // last car call written to this property for the door of the car assigned to this array element. If no car call has been written to an
                // array element, the array element shall indicate a value of zero.
                if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_MAKINGCARCALL)
                {
                    if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT)
                    {
                        if(useArrayIndex)
                        {
                            if (objectInstance == ExampleDatabase.SETTING_LIFT_C_INSTANCE && (propertyArrayIndex > ExampleDatabase.SETTING_LIFT_C_MAKING_CAR_CALL.Length || propertyArrayIndex == 0 ) )
                            {
                                *errorCode = CASBACnetStackAdapter.ERROR_INVALID_ARRAY_INDEX;
                                return false; 
                            }
                            else if (objectInstance == ExampleDatabase.SETTING_LIFT_D_INSTANCE && 
                                     (propertyArrayIndex > ExampleDatabase.SETTING_LIFT_D_MAKING_CAR_CALL.Length || propertyArrayIndex == 0))
                            {
                                *errorCode = CASBACnetStackAdapter.ERROR_INVALID_ARRAY_INDEX;
                                return false;
                            }
                            else if (objectInstance == ExampleDatabase.SETTING_LIFT_E_INSTANCE && 
                                     (propertyArrayIndex > ExampleDatabase.SETTING_LIFT_E_MAKING_CAR_CALL.Length || propertyArrayIndex == 0))
                            {
                                *errorCode = CASBACnetStackAdapter.ERROR_INVALID_ARRAY_INDEX;
                                return false;
                            }
                            else if (objectInstance == ExampleDatabase.SETTING_LIFT_F_INSTANCE && 
                                     (propertyArrayIndex > ExampleDatabase.SETTING_LIFT_F_MAKING_CAR_CALL.Length || propertyArrayIndex == 0))
                            {
                                *errorCode = CASBACnetStackAdapter.ERROR_INVALID_ARRAY_INDEX;
                                return false;
                            }
                            else if (objectInstance == ExampleDatabase.SETTING_LIFT_G_INSTANCE && 
                                     (propertyArrayIndex > ExampleDatabase.SETTING_LIFT_G_MAKING_CAR_CALL.Length || propertyArrayIndex == 0))
                            {
                                // Lift "G" has two doors. 
                                *errorCode = CASBACnetStackAdapter.ERROR_INVALID_ARRAY_INDEX;
                                return false;
                            }

                            // Check to the value to ensure that it is within the limts of the floors. 
                            if(value > ExampleDatabase.FLOOR_NAMES.Length)
                            {
                                *errorCode = CASBACnetStackAdapter.ERROR_VALUE_OUT_OF_RANGE;
                                return false; 
                            }

                            // Set the value 
                            if (objectInstance == ExampleDatabase.SETTING_LIFT_C_INSTANCE )
                            {
                                ExampleDatabase.SETTING_LIFT_C_MAKING_CAR_CALL[propertyArrayIndex - 1] = Convert.ToByte(value);
                                return true;
                            }
                            else if (objectInstance == ExampleDatabase.SETTING_LIFT_D_INSTANCE)
                            {
                                ExampleDatabase.SETTING_LIFT_D_MAKING_CAR_CALL[propertyArrayIndex - 1] = Convert.ToByte(value);
                                return true;
                            }
                            else if (objectInstance == ExampleDatabase.SETTING_LIFT_E_INSTANCE)
                            {
                                ExampleDatabase.SETTING_LIFT_E_MAKING_CAR_CALL[propertyArrayIndex - 1] = Convert.ToByte(value);
                                return true;
                            }
                            else if (objectInstance == ExampleDatabase.SETTING_LIFT_F_INSTANCE)
                            {
                                ExampleDatabase.SETTING_LIFT_F_MAKING_CAR_CALL[propertyArrayIndex - 1] = Convert.ToByte(value);
                                return true;
                            }
                            else if (objectInstance == ExampleDatabase.SETTING_LIFT_G_INSTANCE)
                            {
                                // Lift "G" has two doors. 
                                ExampleDatabase.SETTING_LIFT_G_MAKING_CAR_CALL[propertyArrayIndex - 1] = Convert.ToByte(value);
                                return true;
                            }
                        }
                    }
                }

                Console.WriteLine("   FYI: Not implmented. ");
                return false;
            }
        }
    }
}
