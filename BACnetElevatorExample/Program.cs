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
 * Last updated: January 17, 2020
 */

using CASBACnetStack;
using System;
using System.Collections.Generic;
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
            const string APPLICATION_VERSION = "0.0.5";

            public void Run()
            {
                // Prints the version of the application and the CAS BACnet stack. 
                Console.WriteLine("Starting BACnetElevatorExample version {0}.{1}", APPLICATION_VERSION, CIBuildVersion.CIBUILDNUMBER);
                Console.WriteLine("https://github.com/chipkin/Windows-BACnetElevatorExample");

                Console.WriteLine("FYI: BACnet Stack Adapter version: {0}", CASBACnetStackAdapter.ADAPTER_VERSION); 
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
                CASBACnetStackAdapter.RegisterCallbackGetListElevatorGroupLandingCallStatus(CallbackGetListElevatorGroupLandingCallStatus);
                CASBACnetStackAdapter.RegisterCallbackGetSequenceLiftAssignedLandingCall(CallbackGetSequenceLiftAssignedLandingCall);
                CASBACnetStackAdapter.RegisterCallbackGetSequenceLiftRegisteredCarCall(CallbackGetSequenceLiftRegisteredCarCall);
                CASBACnetStackAdapter.RegisterCallbackGetSequenceLiftLandingDoorStatus(CallbackGetSequenceLiftLandingDoorStatus);

                // Set Datatype Callbacks 
                CASBACnetStackAdapter.RegisterCallbackSetPropertyUnsignedInteger(CallbackSetUnsignedInteger);
                CASBACnetStackAdapter.RegisterCallbackSetElevatorGroupLandingCallControl(CallbackSetElevatorGroupLandingCallControl);

                // Service Callbacks
                CASBACnetStackAdapter.RegisterCallbackAcknowledgeAlarm(CallbackAcknowledgeAlarm);

                // Add the device. 
                CASBACnetStackAdapter.AddDevice(database.device.instance);

                // Enable optional services 
                CASBACnetStackAdapter.SetServiceEnabled(database.device.instance, CASBACnetStackAdapter.SERVICES_SUPPORTED_I_AM, true);
                CASBACnetStackAdapter.SetServiceEnabled(database.device.instance, CASBACnetStackAdapter.SERVICES_SUPPORTED_READ_PROPERTY_MULTIPLE, true);
                CASBACnetStackAdapter.SetServiceEnabled(database.device.instance, CASBACnetStackAdapter.SERVICES_SUPPORTED_WRITE_PROPERTY, true);
                CASBACnetStackAdapter.SetServiceEnabled(database.device.instance, CASBACnetStackAdapter.SERVICES_SUPPORTED_WRITE_PROPERTY_MULTIPLE, true);
                CASBACnetStackAdapter.SetServiceEnabled(database.device.instance, CASBACnetStackAdapter.SERVICES_SUPPORTED_SUBSCRIBE_COV_PROPERTY, true);
                CASBACnetStackAdapter.SetServiceEnabled(database.device.instance, CASBACnetStackAdapter.SERVICES_SUPPORTED_ACKNOWLEDGE_ALARM, true);
                CASBACnetStackAdapter.SetServiceEnabled(database.device.instance, CASBACnetStackAdapter.SERVICES_SUPPORTED_GET_EVENT_INFORMATION, true);

                // ESCALATOR Group 
                CASBACnetStackAdapter.AddElevatorGroupObject(database.device.instance, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_MACHINE_ROOM_ID, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID, false, false);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, ExampleDatabase.SETTING_ESCALATOR_A_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID, ExampleDatabase.SETTING_ESCALATOR_A_INSTALLATION_ID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, ExampleDatabase.SETTING_ESCALATOR_B_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID, ExampleDatabase.SETTING_ESCALATOR_B_INSTALLATION_ID);

                // LIFT GROUP
                CASBACnetStackAdapter.AddElevatorGroupObject(database.device.instance, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_MACHINE_ROOM_ID, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, true, true);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_C_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, database.lifts[ExampleDatabase.LIFT_C_INSTANCE].installationID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_D_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, database.lifts[ExampleDatabase.LIFT_D_INSTANCE].installationID);

                // Double deck lift.                 
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_E_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, database.lifts[ExampleDatabase.LIFT_E_INSTANCE].installationID);
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_F_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID, database.lifts[ExampleDatabase.LIFT_F_INSTANCE].installationID);
                CASBACnetStackAdapter.SetLiftHigherLowerDeck(database.device.instance, ExampleDatabase.LIFT_E_INSTANCE, 4194303, ExampleDatabase.LIFT_F_INSTANCE);
                CASBACnetStackAdapter.SetLiftHigherLowerDeck(database.device.instance, ExampleDatabase.LIFT_F_INSTANCE, ExampleDatabase.LIFT_E_INSTANCE, 4194303);

                // Indepenent Lift with two doors.
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_G_INSTANCE, 4194303, database.lifts[ExampleDatabase.LIFT_G_INSTANCE].groupID, database.lifts[ExampleDatabase.LIFT_G_INSTANCE].installationID);
                
                // Indepenent and Escalator
                CASBACnetStackAdapter.AddLiftOrEscalatorObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, ExampleDatabase.SETTING_ESCALATOR_H_INSTANCE, 4194303, ExampleDatabase.SETTING_ESCALATOR_H_GROUP_ID, ExampleDatabase.SETTING_ESCALATOR_H_INSTALLATION_ID);

                // Enabled optional properties. 
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_ESCALATOR, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FLOORTEXT, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS, true);                
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ENERGYMETER, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_MAKINGCARCALL, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeWritable(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_MAKINGCARCALL, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_REGISTEREDCARCALL, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ASSIGNEDLANDINGCALLS, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeEnabled(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_LANDINGDOORSTATUS, true);

                // Make some property of Lift Object Subscribable
                CASBACnetStackAdapter.SetPropertyByObjectTypeSubscribable(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PASSENGERALARM, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeSubscribable(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARMOVINGDIRECTION, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeSubscribable(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARPOSITION, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeSubscribable(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORSTATUS, true);
                CASBACnetStackAdapter.SetPropertyByObjectTypeSubscribable(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS, true);

                // Make some property of group of lifts object Subscribable
                CASBACnetStackAdapter.SetPropertySubscribable(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_ELEVATOR_GROUP, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_GROUPMODE, true);

                // Add Notification Class
               CASBACnetStackAdapter.AddNotificationClassObject(database.device.instance, ExampleDatabase.SETTING_NOTIFICATION_CLASS_INSTANCE, ExampleDatabase.NOTIFICATION_CLASS_TOOFFNORMAL_PRIORITY, ExampleDatabase.NOTIFICATION_CLASS_TOFAULT_PRIORITY, ExampleDatabase.NOTIFICATION_CLASS_TONORMAL_PRIORITY, ExampleDatabase.NOTIFICATION_CLASS_TOOFFNORMAL_ACKREQUIRED, ExampleDatabase.NOTIFICATION_CLASS_TOFAULT_ACKREQUIRED, ExampleDatabase.NOTIFICATION_CLASS_TONORMAL_ACKREQUIRED);

                // Data buffer for recipient mac address (IP Address and Port).  
                byte[] recipientAddress = { 0xC0, 0xA8, 0x01, 0x54, 0xC0, 0xBA };
                fixed(byte* recipientAddressPtr = recipientAddress)
                    CASBACnetStackAdapter.AddRecipientToNotificationClass(database.device.instance, ExampleDatabase.SETTING_NOTIFICATION_CLASS_INSTANCE, 127, 0, 0, 0, 0, 23, 59, 59, 99, 1, false, true, true, true, false, 0, true, 0, recipientAddressPtr, 6);

                // Enable Alarming for all the lifts
                // The intrinsic alarms available for lifts and escalators are:  ChangeOfState for the PassengerAlarm property, FaultsListed for the FaultSignals property
                CASBACnetStackAdapter.EnableAlarmsAndEventsForObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_C_INSTANCE, ExampleDatabase.SETTING_NOTIFICATION_CLASS_INSTANCE, CASBACnetStackAdapter.NOTIFY_TYPE_ALARM, true, true, true, true);
                CASBACnetStackAdapter.SetIntrinsicChangeOfStateAlgorithmBool(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_C_INSTANCE, true, 0, false, 0, true);
                CASBACnetStackAdapter.SetFaultListedAlgorithm(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_C_INSTANCE, true);

                CASBACnetStackAdapter.EnableAlarmsAndEventsForObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_D_INSTANCE, ExampleDatabase.SETTING_NOTIFICATION_CLASS_INSTANCE, CASBACnetStackAdapter.NOTIFY_TYPE_ALARM, true, true, true, true);
                CASBACnetStackAdapter.SetIntrinsicChangeOfStateAlgorithmBool(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_D_INSTANCE, true, 0, false, 0, true);
                CASBACnetStackAdapter.SetFaultListedAlgorithm(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_D_INSTANCE, true);

                CASBACnetStackAdapter.EnableAlarmsAndEventsForObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_E_INSTANCE, ExampleDatabase.SETTING_NOTIFICATION_CLASS_INSTANCE, CASBACnetStackAdapter.NOTIFY_TYPE_ALARM, true, true, true, true);
                CASBACnetStackAdapter.SetIntrinsicChangeOfStateAlgorithmBool(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_E_INSTANCE, true, 0, false, 0, true);
                CASBACnetStackAdapter.SetFaultListedAlgorithm(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_E_INSTANCE, true);

                CASBACnetStackAdapter.EnableAlarmsAndEventsForObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_F_INSTANCE, ExampleDatabase.SETTING_NOTIFICATION_CLASS_INSTANCE, CASBACnetStackAdapter.NOTIFY_TYPE_ALARM, true, true, true, true);
                CASBACnetStackAdapter.SetIntrinsicChangeOfStateAlgorithmBool(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_F_INSTANCE, true, 0, false, 0, true);
                CASBACnetStackAdapter.SetFaultListedAlgorithm(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_F_INSTANCE, true);

                CASBACnetStackAdapter.EnableAlarmsAndEventsForObject(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_G_INSTANCE, ExampleDatabase.SETTING_NOTIFICATION_CLASS_INSTANCE, CASBACnetStackAdapter.NOTIFY_TYPE_ALARM, true, true, true, true);
                CASBACnetStackAdapter.SetIntrinsicChangeOfStateAlgorithmBool(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_G_INSTANCE, true, 0, false, 0, true);
                CASBACnetStackAdapter.SetFaultListedAlgorithm(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, ExampleDatabase.LIFT_G_INSTANCE, true);

                // All done with the BACnet setup. 
                Console.WriteLine("FYI: CAS BACnet Stack Setup, successfuly");

                // Open the BACnet port to recive messages. 
                this.udpServer = new UdpClient(SETTING_BACNET_PORT);
                this.RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                // Main loop.
                Console.WriteLine("FYI: Starting main loop");
                for (; ; )
                {
                    CASBACnetStackAdapter.Loop();
                    database.Loop();
                    UserInput();
                }
            }

            // The selected lift used for changing values. 
            Int32 selectedLift = 0;

            // Prints the status of the currently selected Lift. 
            private void PrintCurrentlySelectedLiftStatus(Int32 liftOffset )
            {
                List<UInt32> keyList = new List<UInt32>(database.lifts.Keys);
                Console.WriteLine("===============================================================================");
                Console.WriteLine("Selected lift ({0}/{1}):", liftOffset + 1, database.lifts.Count);

                Console.WriteLine("  Instance:               {0}", database.lifts[keyList[liftOffset]].instance);
                Console.WriteLine("  objectName:             {0}", database.lifts[keyList[liftOffset]].objectName);
                
                Console.Write("  Car Door Text ({0}):      ", database.lifts[keyList[liftOffset]].carDoorText.Length);
                foreach (string text in database.lifts[keyList[liftOffset]].carDoorText)
                {
                    Console.Write("{0}, ", text);
                }
                Console.WriteLine("");

                Console.Write("  Making Car Call:        ");
                int carCallCount = 0;
                foreach (Byte carCall in database.lifts[keyList[liftOffset]].makingCarCall)
                {
                    Console.Write("{0}: {1}, ", database.lifts[keyList[liftOffset]].carDoorText[carCallCount], (int)carCall);
                    carCallCount++;
                }
                Console.WriteLine("");

                Console.WriteLine("  Registered Car Calls:   ");
                UInt32 indexDoor = 0;
                foreach (List<Byte> call in database.lifts[keyList[liftOffset]].registeredCarCalls)
                {
                    Console.Write("    {0}: ", database.lifts[keyList[liftOffset]].carDoorText[indexDoor]);
                    foreach (byte carCall in call)
                    {
                        Console.Write("{0}, ", BACnetLiftObject.floorText[carCall]);
                    }
                    indexDoor++;
                    Console.Write("\n");
                }

                Console.WriteLine("  Assigned Landing Calls: ");
                indexDoor = 0;
                foreach (List<BACnetLandingCall> call in database.lifts[keyList[liftOffset]].assignedLandingCalls)
                {
                    Console.Write("    {0}: ", database.lifts[keyList[liftOffset]].carDoorText[indexDoor]);
                    foreach (BACnetLandingCall landingCall in call)
                    {
                        Console.Write("{0}={1}, ", BACnetLiftObject.floorText[landingCall.floorNumber], BACnetLandingCall.carDirectionText[landingCall.direction]);
                    }
                    indexDoor++;
                    Console.Write("\n");
                }

                Console.WriteLine("  Landing Door Status:    ");
                indexDoor = 0; 
                foreach (List<BACnetLandingDoor> door in database.lifts[keyList[liftOffset]].landingDoorStatus)
                {
                    Console.Write("    {0}: ", database.lifts[keyList[liftOffset]].carDoorText[indexDoor] );
                    foreach (BACnetLandingDoor landingDoor in door)
                    {
                        Console.Write("{0}={1}, ", BACnetLiftObject.floorText[landingDoor.floorNumber], BACnetLandingDoor.carDoorStatusText[landingDoor.carDoorStatus]);
                    }
                    indexDoor++;
                    Console.Write("\n");
                }

                Console.Write("  Fault Signals:          ");
                foreach (uint fault in database.lifts[keyList[liftOffset]].faultSignals)
                {
                    Console.Write(fault + ", ");
                }
                Console.WriteLine("");

                Console.Write("  Car Door Status:        ");
                for (int i = 0; i < database.lifts[keyList[liftOffset]].carDoorText.Length; i++)
                {
                    Console.Write("{0}: {2} ({1}), ", 
                        database.lifts[keyList[liftOffset]].carDoorText[i], 
                        database.lifts[keyList[liftOffset]].carDoorStatus[i],
                        BACnetLandingDoor.carDoorStatusText[database.lifts[keyList[liftOffset]].carDoorStatus[i]]);
                }
                Console.WriteLine("");

                Console.WriteLine("  Car Moving Direction:   {1} ({0})", database.lifts[keyList[liftOffset]].carMovingDirection, BACnetLandingCall.carDirectionText[database.lifts[keyList[liftOffset]].carMovingDirection]);
                Console.WriteLine("  Car Position:           {1} ({0})", database.lifts[keyList[liftOffset]].carPosition, BACnetLiftObject.floorText[database.lifts[keyList[selectedLift]].carPosition]);
                Console.WriteLine("  Passenger Alarm:        {0}", database.lifts[keyList[liftOffset]].passengerAlarm);
                Console.WriteLine("  Energy Meter:           {0}", database.lifts[keyList[liftOffset]].energyMeter);
                
                Console.WriteLine("");
                Console.WriteLine("  ** Left/Right arrow, to change the selected lift ** ");
                Console.WriteLine("");
            }

            private void PrintGlobalStatus()
            {
                // Print current status 
                Console.WriteLine("FYI: Current System Status:");

                // Print the floors                             
                Console.Write("  Floors({0}): ", BACnetLiftObject.floorText.Length);
                int floorNameCount = 0;
                foreach (string floorName in BACnetLiftObject.floorText)
                {
                    Console.Write("{1}({0}), ", floorNameCount, floorName);
                    floorNameCount++;
                }
                Console.WriteLine("");

                // Print the status of the lifts. 
                Console.WriteLine("  Lift Group object: ");
                Console.Write("    Landing Call Status: ");
                if (ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice != BACnetLandingCallStatus.BACnetLandingCallStatusCommand.direction)
                {
                    Console.WriteLine("FloorNumber: {0}, Direction: {1}, FloorText: {2}",
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorNumber,
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandVaue,
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorText);
                }
                else if (ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice != BACnetLandingCallStatus.BACnetLandingCallStatusCommand.destination)
                {
                    Console.WriteLine("FloorNumber: {0}, Destination: {1}, FloorText: {2}",
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorNumber,
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandVaue,
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorText);
                }
                else
                {
                    Console.WriteLine("N/A");
                }

                // Lift landing calls 
                Console.WriteLine("    Lift Landing Calls: ");
                int landingCallCount = 0;
                foreach (BACnetLandingCallStatus status in ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALLS)
                {
                    if (status.commandChoice != BACnetLandingCallStatus.BACnetLandingCallStatusCommand.direction)
                    {
                        Console.WriteLine("      [{0}]: FloorNumber: {1}, Direction: {2}, FloorText: {3}",
                            landingCallCount,
                            status.floorNumber,
                            status.commandVaue,
                            status.floorText);
                    }
                    else if (status.commandChoice != BACnetLandingCallStatus.BACnetLandingCallStatusCommand.destination)
                    {
                        Console.WriteLine("      [{0}]: FloorNumber: {1}, Destination: {2}, FloorText: {3}",
                            landingCallCount,
                            status.floorNumber,
                            status.commandVaue,
                            status.floorText);
                    }
                    else
                    {
                        Console.WriteLine("      [{0}]: N/A", landingCallCount);
                    }
                }
                Console.WriteLine("");
            }

            private void UserInput()
            {                
                if (Console.KeyAvailable)
                {
                    // Get a list of keys
                    List<UInt32> keyList = new List<UInt32>(database.lifts.Keys);

                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.LeftArrow:
                            {
                                // Change selected lift 
                                if (selectedLift > 0) {
                                    selectedLift--;
                                }
                                break; 
                            }
                        case ConsoleKey.RightArrow:
                            {
                                // Change selected lift 
                                if (selectedLift < database.lifts.Count -1 ) {
                                    selectedLift++;
                                }
                                break;
                            }
                        case ConsoleKey.F:
                            // Update the Fault Signals
                            Random random = new Random();
                            uint faultSignal = (uint)random.Next(0, 16);
                            if(database.lifts[keyList[selectedLift]].faultSignals.Contains(faultSignal))
                            {
                                Console.WriteLine("Removing {0} from Fault Signals", faultSignal);
                                database.lifts[keyList[selectedLift]].faultSignals.Remove(faultSignal);
                            }
                            else
                            {
                                Console.WriteLine("Adding {0} from Fault Signals", faultSignal);
                                database.lifts[keyList[selectedLift]].faultSignals.Add(faultSignal);
                            }
                            CASBACnetStackAdapter.ValueUpdated(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, database.lifts[keyList[selectedLift]].instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_FAULTSIGNALS);
                            break;


                        case ConsoleKey.M:
                            {
                                if (ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_MODE == 1)
                                {
                                    ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_MODE = 0; // unknown(0)
                                }
                                else
                                {
                                    ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_MODE = 1; // normal(1)
                                }

                                Console.WriteLine("FYI: Updating elevator group ({0}), group mode property to {1}", ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_MODE);
                                // Update the CAS BACnet Stack that this value has been updated. 
                                CASBACnetStackAdapter.ValueUpdated(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_ELEVATOR_GROUP, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_GROUPMODE);
                                break;
                            }
                        case ConsoleKey.P:
                            {
                                // Toggle passanger alarm 
                                database.lifts[keyList[selectedLift]].passengerAlarm = !database.lifts[keyList[selectedLift]].passengerAlarm ;
                                Console.WriteLine("FYI: Toggled passengerAlarm for lift ({0}), to {1}", database.lifts[keyList[selectedLift]].instance, database.lifts[keyList[selectedLift]].passengerAlarm);
                                CASBACnetStackAdapter.ValueUpdated(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, database.lifts[keyList[selectedLift]].instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PASSENGERALARM);
                                break;
                            }
                        case ConsoleKey.D:
                            {
                                // Toggle car moving direction
                                database.lifts[keyList[selectedLift]].carMovingDirection++;
                                if(database.lifts[keyList[selectedLift]].carMovingDirection >= BACnetLandingCall.carDirectionText.Length)
                                {
                                    // Loop 
                                    database.lifts[keyList[selectedLift]].carMovingDirection = 0; 
                                }
                                Console.WriteLine("FYI: Toggled Car Moving Direction for lift ({0}), to {1} ({2})", database.lifts[keyList[selectedLift]].instance, database.lifts[keyList[selectedLift]].carMovingDirection, BACnetLandingCall.carDirectionText[database.lifts[keyList[selectedLift]].carMovingDirection] );
                                CASBACnetStackAdapter.ValueUpdated(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, database.lifts[keyList[selectedLift]].instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARMOVINGDIRECTION);
                                break;
                            }
                        case ConsoleKey.C:
                            {
                                // Toggle car moving direction
                                database.lifts[keyList[selectedLift]].carPosition++;
                                if (database.lifts[keyList[selectedLift]].carPosition >= BACnetLiftObject.floorText.Length)
                                {
                                    // Loop 
                                    database.lifts[keyList[selectedLift]].carPosition = 0;
                                }
                                Console.WriteLine("FYI: Changed Car Position for lift ({0}), to {1} ({2})", database.lifts[keyList[selectedLift]].instance, database.lifts[keyList[selectedLift]].carPosition, BACnetLiftObject.floorText[database.lifts[keyList[selectedLift]].carPosition]);
                                CASBACnetStackAdapter.ValueUpdated(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, database.lifts[keyList[selectedLift]].instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARPOSITION);
                                break;
                            }
                        case ConsoleKey.S:
                            {
                                // Car Door Status
                                database.lifts[keyList[selectedLift]].carDoorStatus[0]++;
                                if (database.lifts[keyList[selectedLift]].carDoorStatus[0] >= BACnetLandingDoor.carDoorStatusText.Length)
                                {
                                    // Loop 
                                    database.lifts[keyList[selectedLift]].carDoorStatus[0] = 0;
                                }
                                Console.WriteLine("FYI: Changed Car Door Status for lift ({0}), to {1} ({2})", database.lifts[keyList[selectedLift]].instance, database.lifts[keyList[selectedLift]].carDoorStatus, BACnetLandingDoor.carDoorStatusText[database.lifts[keyList[selectedLift]].carDoorStatus[0]]);
                                CASBACnetStackAdapter.ValueUpdated(database.device.instance, CASBACnetStackAdapter.OBJECT_TYPE_LIFT, database.lifts[keyList[selectedLift]].instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORSTATUS);
                                break;
                            }
                        case ConsoleKey.G:
                            {
                                PrintGlobalStatus();
                                break; 
                            }
                        case ConsoleKey.Q:
                            {
                                System.Environment.Exit(1);
                                break;
                            }
                    }

                    PrintCurrentlySelectedLiftStatus(selectedLift);

                    // Print current status                             
                    Console.WriteLine("Actions:");
                    Console.WriteLine("  Selected Lift:");
                    Console.WriteLine("  * F - Update Fault Signals");
                    Console.WriteLine("  * P - Toggle Passanger Alarm");
                    Console.WriteLine("  * D - Toggle Car Moving Direction");
                    Console.WriteLine("  * C - Change Car Position");
                    Console.WriteLine("  * S - Change Car Door Status");
                    Console.WriteLine("  General:");
                    Console.WriteLine("  * Q - Quit");
                    Console.WriteLine("  * M - Update Group mode, for elevator group ({0})", ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE);
                    Console.WriteLine("  * G - Print global status");
                    
                    Console.WriteLine("");
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

            private static unsafe UInt32 UpdateStringAndReturnSize(System.Byte* value, UInt32 maxElementCount, string stringAsVallue)
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

            // Source: https://stackoverflow.com/a/19502491
            private static unsafe String MarshalUnsafeCStringToString(System.Byte* unsafeCString, int textLength)
            {
                if(textLength == 0)
                {
                    return "";
                }
                

                // now that we have the length of the string, let's get its size in bytes
                int lengthInBytes = ASCIIEncoding.ASCII.GetByteCount( (char *) unsafeCString, textLength);
                byte[] asByteArray = new byte[lengthInBytes];

                fixed (byte* ptrByteArray = asByteArray)
                {
                    ASCIIEncoding.ASCII.GetBytes((char*) unsafeCString, textLength, ptrByteArray, lengthInBytes);
                }

                // now get the string
                return ASCIIEncoding.ASCII.GetString(asByteArray);
            }

            public bool CallbackGetPropertyCharString(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, System.Byte* value, UInt32* valueElementCount, UInt32 maxElementCount, System.Byte encodingType, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetPropertyCharString. objectType={0}, objectInstance={1}, propertyIdentifier={2}, propertyArrayIndex={3}", objectType, objectInstance, propertyIdentifier, propertyArrayIndex);

                // Object_Name
                // This property, of type CharacterString, shall represent a name for the object that is unique within the BACnet device that
                // maintains it. The minimum length of the string shall be one character.The set of characters used in the Object_Name shall be
                // restricted to printable characters.
                if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OBJECT_NAME)
                {
                    if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_DEVICE && objectInstance == database.device.instance)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, database.device.objectName);
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
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT && database.lifts.ContainsKey(objectInstance))
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, database.lifts[objectInstance].objectName);
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
                    if (propertyArrayIndex <= BACnetLiftObject.floorText.Length)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, BACnetLiftObject.floorText[propertyArrayIndex - 1]);
                        return true;
                    }
                }
                // 12.59.10 Car_Door_Text
                // This property, of type BACnetARRAY of CharacterString, represents the descriptions or names for the doors of the lift car.
                // Each array element represents the description or name for the door of the car assigned to this array element.
                else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORTEXT && useArrayIndex && 
                    objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT && database.lifts.ContainsKey(objectInstance))
                {
                    // Lift "G" has two doors
                    // All other lifts have 1 door. 
                    if (propertyArrayIndex <= database.lifts[objectInstance].carDoorText.Length)
                    {
                        *valueElementCount = UpdateStringAndReturnSize(value, maxElementCount, database.lifts[objectInstance].carDoorText[propertyArrayIndex - 1]);
                        return true;
                    }
                }

                Console.WriteLine("   FYI: Not implemented. propertyIdentifier={0}", propertyIdentifier);
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
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                        database.lifts.ContainsKey(objectInstance) )
                    {   
                        *value = database.lifts[objectInstance].energyMeter;
                        return true;
                    }
                }

                Console.WriteLine("   FYI: Not implemented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }
            public bool CallbackGetEnumerated(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, UInt32* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetEnumerated. objectType={0}, objectInstance={1}, propertyIdentifier={2}", objectType, objectInstance, propertyIdentifier);

                // 12.58.8 Group_Mode
                // This property, of type BACnetLiftGroupMode, shall convey the operating mode of the group of lifts. This is used to represent
                // some special traffic modes of control of the supervisory controller of a group of lifts. Supervisory controllers are not required
                // to support all modes. Under a special traffic mode, the car dispatching algorithm may be different.
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
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORSTATUS &&
                    database.lifts.ContainsKey(objectInstance) && useArrayIndex)
                {
                    if(propertyArrayIndex == 0 )
                    {
                        *value = (UInt32) database.lifts[objectInstance].carDoorStatus.Length;
                    } else
                    {
                        *value = database.lifts[objectInstance].carDoorStatus[propertyArrayIndex - 1];
                    }
                    
                    return true;
                }
                // 12.59.15 Car_Moving_Direction
                // This property, of type BACnetLiftCarDirection, represents whether or not this lift's car is moving, and if so, in which
                // direction.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARMOVINGDIRECTION &&
                    database.lifts.ContainsKey(objectInstance))
                {
                    *value = database.lifts[objectInstance].carMovingDirection;
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
                
                Console.WriteLine("   FYI: Not implemented. propertyIdentifier={0}", propertyIdentifier);
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
                    *value = Convert.ToUInt32(BACnetLiftObject.floorText.Length);
                    return true;
                }
                // 12.59.10 Car_Door_Text
                // This property, of type BACnetARRAY of CharacterString, represents the descriptions or names for the doors of the lift car.
                // Each array element represents the description or name for the door of the car assigned to this array element.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORTEXT &&
                         database.lifts.ContainsKey(objectInstance) )
                {
                    *value = Convert.ToUInt32(database.lifts[objectInstance].carDoorText.Length);
                    return true;
                }

                // 12.59.17 Car_Door_Status
                // This property, of type BACnetARRAY of BACnetDoorStatus, indicates the status of the doors on the car. Each array element
                // indicates the status of the car door assigned to this array element.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARDOORSTATUS &&
                         database.lifts.ContainsKey(objectInstance) && useArrayIndex )
                {
                    // Lift "G" has two doors. 
                    // All other doors have the same door count of 1 
                    if(propertyArrayIndex == 0 )
                    {
                        *value = Convert.ToUInt32(database.lifts[objectInstance].carDoorStatus.Length);
                    } else
                    {
                        *value = Convert.ToUInt32(database.lifts[objectInstance].carDoorStatus[propertyArrayIndex-1]);
                    }
                    
                    return true;
                }

                // 12.59.11 Assigned_Landing_Calls
                // This property, of type BACnetARRAY of BACnetAssignedLandingCalls, shall represent the current landing calls and their
                // direction for the lift represented by this object.Each array element represents the list of assigned landing calls for the door of
                // the car assigned to this array element.
                else if(objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_ASSIGNEDLANDINGCALLS &&
                    database.lifts.ContainsKey(objectInstance) )
                {
                    // Lift "G" has two doors. 
                    // All other doors have the same door count of 1 
                    *value = Convert.ToUInt32(database.lifts[objectInstance].assignedLandingCalls.Length);
                    return true;
                }

                // 12.59.13 Registered_Car_Call
                // This property, of type BACnetARRAY of BACnetLiftCarCallList, represents the lists of currently registered car calls
                // (requests to stop at particular floors using a particular door) for this lift.Each array element represents the list of universal
                // floor numbers for which calls are registered for the door of the car assigned to this array element.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_REGISTEREDCARCALL &&
                    database.lifts.ContainsKey(objectInstance))
                {
                    // Lift "G" has two doors. 
                    // All other doors have the same door count of 1 
                    *value = Convert.ToUInt32(database.lifts[objectInstance].registeredCarCalls.Length);
                    return true;
                }

                // 12.59.33 Landing_Door_Status
                // This property, of type BACnetARRAY of BACnetLandingDoorStatus, represents the status of the landing doors on the floors
                // served by this lift.Each element of this array represents the list of landing doors for the door of the car assigned to this array
                // element.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                    propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_LANDINGDOORSTATUS &&
                    database.lifts.ContainsKey(objectInstance) )
                {
                    // Lift "G" has two doors. 
                    // All other doors have the same door count of 1 
                    *value = Convert.ToUInt32(database.lifts[objectInstance].landingDoorStatus.Length);

                    return true;
                }

                // 12.59.14 Car_Position
                // This property, of type Unsigned8, indicates the universal floor number of this lift's car position.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_CARPOSITION &&
                         database.lifts.ContainsKey(objectInstance))
                {
                    *value = Convert.ToUInt32(database.lifts[objectInstance].carPosition);
                    return true;
                }
                // 12.59.12 Making_Car_Call
                // This property, of type BACnetARRAY of Unsigned8, indicates the last car calls written to this property. Writing to this
                // property is equivalent to a passenger requesting that the car stop at the designated floor. Each array element represents the
                // last car call written to this property for the door of the car assigned to this array element. If no car call has been written to an
                // array element, the array element shall indicate a value of zero.
                else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                         propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_MAKINGCARCALL &&
                         database.lifts.ContainsKey(objectInstance))
                {
                    // Array index 0 is used for getting the amount of elements in the array. This number corasponds to the 
                    // Number of doors. 
                    if (propertyArrayIndex == 0)
                    {
                        *value = Convert.ToUInt32(database.lifts[objectInstance].makingCarCall.Length);
                        return true;
                    }
                    else
                    {
                        if (propertyArrayIndex <= database.lifts[objectInstance].makingCarCall.Length)
                        {
                            *value = Convert.ToUInt32(database.lifts[objectInstance].makingCarCall[propertyArrayIndex - 1]);
                            return true;
                        } 
                    }
                    return false;
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


                Console.WriteLine("   FYI: Not implemented. propertyIdentifier={0}", propertyIdentifier);
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
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT &&
                        database.lifts.ContainsKey(objectInstance) )
                    {
                        *value = database.lifts[objectInstance].passengerAlarm;
                        return true;
                    }
                }

                Console.WriteLine("   FYI: Not implemented. propertyIdentifier={0}", propertyIdentifier);
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
                    else if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT && rangeOption == 0 && database.lifts.ContainsKey(objectInstance) )
                    {
                        UInt32 count = 0;
                        foreach (UInt32 fault in database.lifts[objectInstance].faultSignals)
                        {
                            if (count == rangeIndexOrSequenceNumber)
                            {
                                *enumeration = fault;
                                *more = (count < database.lifts[objectInstance].faultSignals.Count - 1);
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
                Console.WriteLine("   FYI: Not implemented. propertyIdentifier={0}", propertyIdentifier);
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
                    if (objectType == CASBACnetStackAdapter.OBJECT_TYPE_LIFT && database.lifts.ContainsKey(objectInstance) )
                    {
                        if(useArrayIndex)
                        {
                            if ( (propertyArrayIndex > database.lifts[objectInstance].makingCarCall.Length || propertyArrayIndex == 0 ) )
                            {
                                *errorCode = CASBACnetStackAdapter.ERROR_INVALID_ARRAY_INDEX;
                                return false; 
                            }                            

                            // Check to the value to ensure that it is within the limts of the floors. 
                            if(value > BACnetLiftObject.floorText.Length)
                            {
                                *errorCode = CASBACnetStackAdapter.ERROR_VALUE_OUT_OF_RANGE;
                                return false; 
                            }

                            // Set the value 
                            if (objectInstance == database.lifts[objectInstance].instance )
                            {
                                database.lifts[objectInstance].makingCarCall[propertyArrayIndex - 1] = Convert.ToByte(value);
                                return true;
                            }
                        }
                    }
                }

                Console.WriteLine("   FYI: Not implemented. ");
                return false;
            }

            public bool CallbackGetListElevatorGroupLandingCallStatus(UInt32 deviceInstance, UInt32 elevatorGroupInstance, UInt32 propertyIdentifier, Byte rangeOption, 
                        UInt32 rangeIndexOrSequence, Byte* floorNumber, Byte* commandChoice, UInt32* bacnetLiftCarDirection, Byte* destination, bool* useFloorText, 
                        Byte* floorText, UInt16 floorTextMaxLength, UInt16* floorTextLength, bool* more)
            {
                Console.WriteLine("FYI: Request for callbackGetListElevatorGroupLandingCallStatus. elevatorGroupInstance={0}, propertyIdentifier={1}, rangeOption={2}, rangeIndexOrSequence={3}", elevatorGroupInstance, propertyIdentifier, rangeOption, rangeIndexOrSequence);

                if (elevatorGroupInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE)
                {
                    // 12.58.10 Landing_Call_Control
                    // This property, of type BACnetLandingCallStatus, may be present if the Elevator Group object represents a group of lifts. If it
                    // is present, it shall be writable. A write to this property is equivalent to a passenger pressing a call button at a landing,
                    // indicating either desired direction of travel or destination floor.
                    if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_LANDINGCALLCONTROL)
                    {
                        *floorNumber = ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorNumber;
                        if (ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice == BACnetLandingCallStatus.BACnetLandingCallStatusCommand.destination)
                        {
                            *commandChoice = Convert.ToByte(BACnetLandingCallStatus.BACnetLandingCallStatusCommand.destination);
                            *destination = Convert.ToByte(ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandVaue);
                        }
                        else if (ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice == BACnetLandingCallStatus.BACnetLandingCallStatusCommand.direction)
                        {
                            *commandChoice = Convert.ToByte(BACnetLandingCallStatus.BACnetLandingCallStatusCommand.direction);
                            *bacnetLiftCarDirection = Convert.ToByte(ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandVaue);
                        }

                        if(ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorText.Length > 0 )
                        {
                            *useFloorText = true;
                            *floorTextLength = Convert.ToUInt16(UpdateStringAndReturnSize(floorText, floorTextMaxLength, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorText));
                        }
                        else
                        {
                            *useFloorText = false;
                        }
                        *more = false;
                        return true;
                    }

                    // 12.58.9 Landing_Calls
                    // This property, of type BACnetLIST of BACnetLandingCallStatus, may be present if the Elevator Group object represents a
                    // group of lifts. Each element of this list shall represent a currently active call for the group of lifts.

                    else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_LANDINGCALLS)
                    {
                        if(rangeOption == CASBACnetStackAdapter.ELEVATOR_GROUP_LANDING_CALL_RANGE_BY_POSITION)
                        {
                            if(rangeIndexOrSequence > ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALLS.Count)
                            {
                                // Out of range. 
                                *more = false;
                                return false; 
                            } else if (ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALLS.Count == 0 )
                            {
                                // No items in the list. 
                                return false;
                            }

                            int count = 0; 
                            foreach(BACnetLandingCallStatus status in ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALLS)
                            {
                                if(rangeIndexOrSequence == count)
                                {
                                    // Found the one we are looking for. 
                                    *floorNumber = status.floorNumber;
                                    if (status.commandChoice == BACnetLandingCallStatus.BACnetLandingCallStatusCommand.destination)
                                    {
                                        *commandChoice = Convert.ToByte(BACnetLandingCallStatus.BACnetLandingCallStatusCommand.destination);
                                        *destination = Convert.ToByte(status.commandVaue);
                                    }
                                    else if (ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice == BACnetLandingCallStatus.BACnetLandingCallStatusCommand.direction)
                                    {
                                        *commandChoice = Convert.ToByte(BACnetLandingCallStatus.BACnetLandingCallStatusCommand.direction);
                                        *bacnetLiftCarDirection = Convert.ToByte(status.commandVaue);
                                    }

                                    if (status.floorText.Length > 0)
                                    {
                                        *useFloorText = true;
                                        *floorTextLength = Convert.ToUInt16(UpdateStringAndReturnSize(floorText, floorTextMaxLength, status.floorText));
                                    }
                                    else
                                    {
                                        *useFloorText = false;
                                    }

                                    // Check to see if there is more beyond this element. 
                                    *more = (ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALLS.Count > (count + 1) );
                                    return true; 
                                }
                                count++;
                            }
                        }
                    }
                }
               
                Console.WriteLine("   FYI: Not implemented. ");
                return false;
            }

            public bool CallbackSetElevatorGroupLandingCallControl(
                            UInt32 deviceInstance, UInt32 elevatorGroupInstance, Byte floorNumber, Byte commandChoice, UInt32 bacnetLiftCarDirection, 
                            Byte destination, bool useFloorText, Byte* floorText, UInt16 floorTextLength)
            {
                Console.WriteLine("FYI: Request for CallbackSetElevatorGroupLandingCallControl. elevatorGroupInstance={0}, floorNumber={1}, commandChoice={2}, bacnetLiftCarDirection={3}, destination={4}, useFloorText={5}", 
                    elevatorGroupInstance, floorNumber, commandChoice, bacnetLiftCarDirection, destination, useFloorText);

                if (elevatorGroupInstance == ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE)
                {
                    // 12.58.10 Landing_Call_Control
                    // This property, of type BACnetLandingCallStatus, may be present if the Elevator Group object represents a group of lifts. If it
                    // is present, it shall be writable. A write to this property is equivalent to a passenger pressing a call button at a landing,
                    // indicating either desired direction of travel or destination floor.
                    ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorNumber = floorNumber;
                    if (useFloorText)
                    {
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorText = MarshalUnsafeCStringToString(floorText, floorTextLength); 
                    }
                    else
                    {
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorText = "";
                    }

                    ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice = (BACnetLandingCallStatus.BACnetLandingCallStatusCommand)commandChoice;
                    if (ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice == BACnetLandingCallStatus.BACnetLandingCallStatusCommand.destination)
                    {
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice = BACnetLandingCallStatus.BACnetLandingCallStatusCommand.destination;
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandVaue = destination;

                        Console.WriteLine("   New Elevator group request. commandChoice=Destination, destination={0}, floorNumber={1}, floorText={2}", destination, floorNumber, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorText);
                    }
                    else
                    {
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice = BACnetLandingCallStatus.BACnetLandingCallStatusCommand.direction;
                        ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandVaue = bacnetLiftCarDirection;

                        Console.WriteLine("   New Elevator group request. commandChoice=Direction, direction={0}, floorNumber={1}, floorText={2}", bacnetLiftCarDirection, floorNumber, ExampleDatabase.SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorText);
                    }
                    return true; 
                }


                Console.WriteLine("   FYI: Not implemented. ");
                return false; 
            }

            public bool CallbackGetSequenceLiftRegisteredCarCall(UInt32 deviceInstance, UInt32 objectInstance, UInt32 arrayIndexForDoor, UInt32 offset, Byte* floorNumber, bool* more)
            {
                Console.WriteLine("FYI: Request for CallbackGetSequenceLiftRegisteredCarCall. deviceInstance={0}, objectInstance={1}, arrayIndexForDoor={2}, offset={3}",
                    deviceInstance, objectInstance, arrayIndexForDoor, offset);

                // 12.59.13 Registered_Car_Call
                // This property, of type BACnetARRAY of BACnetLiftCarCallList, represents the lists of currently registered car calls
                // (requests to stop at particular floors using a particular door) for this lift.Each array element represents the list of universal
                // floor numbers for which calls are registered for the door of the car assigned to this array element.

                if( ! database.lifts.ContainsKey(objectInstance) )
                {
                    return false; 
                }

                // Get the registered car call list based on the door array index
                List<Byte> registeredCarCalls;
                registeredCarCalls = database.lifts[objectInstance].registeredCarCalls[arrayIndexForDoor - 1];

                // Check for empty container
                if(registeredCarCalls.Count == 0)
                {
                    return false;
                }

                // Fill in the floor number and more values
                *floorNumber = registeredCarCalls[(int)offset];
                *more = true;
                // Check if there are any more values after this
                if(offset + 1 == registeredCarCalls.Count)
                {
                    // No more values, set more to false
                    *more = false;
                }

                return true;
            }
            
            public bool CallbackGetSequenceLiftAssignedLandingCall(UInt32 deviceInstance, UInt32 objectInstance, UInt32 arrayIndexForDoor, UInt32 offset, Byte* floorNumber, UInt32* direction, bool* more)
            {
                Console.WriteLine("FYI: Request for CallbackGetSequenceLiftAssignedLandingCall. deviceInstance={0}, objectInstance={1}, arrayIndexForDoor={2}, offset={3}",
                    deviceInstance, objectInstance, arrayIndexForDoor, offset);

                // 12.59.11 Assigned_Landing_Calls
                // This property, of type BACnetARRAY of BACnetAssignedLandingCalls, shall represent the current landing calls and their
                // direction for the lift represented by this object.Each array element represents the list of assigned landing calls for the door of
                // the car assigned to this array element.
                // Each element in BACnetAssignedLandingCalls consists of the universal floor number and the direction, of type
                // BACnetLiftCarDirection, which may be one of these values:
                //      - UP The landing call is for upward travel.
                //      - DOWN The landing call is for downward travel.
                //      - UP_AND_DOWN The landing call is for both upward and downward travel having been initiated by two different passengers.

                if (!database.lifts.ContainsKey(objectInstance))
                {
                    // Object does not exist. 
                    return false;
                }

                List<BACnetLandingCall> assignedLandingCalls;
                assignedLandingCalls = database.lifts[objectInstance].assignedLandingCalls[arrayIndexForDoor - 1];
                
                // Check for empty list
                if(assignedLandingCalls.Count == 0)
                {
                    return false;
                }

                // Get the landing call requested by the offset
                BACnetLandingCall landingCall = assignedLandingCalls[(int)offset];

                // Fill in the floor number, direction, and more values
                *floorNumber = landingCall.floorNumber;
                *direction = landingCall.direction;
                *more = true;
                // Check if there are any more values after this
                if (offset + 1 == assignedLandingCalls.Count)
                {
                    // No more values, set more to false
                    *more = false;
                }

                return true;
            }

            public bool CallbackGetSequenceLiftLandingDoorStatus(UInt32 deviceInstance, UInt32 objectInstance, UInt32 arrayIndexForDoor, UInt32 offset, Byte* floorNumber, UInt32* carDoorStatus, bool* more)
            {
                Console.WriteLine("FYI: Request for CallbackGetSequenceLiftLandingDoorStatus. deviceInstance={0}, objectInstance={1}, arrayIndexForDoor={2}, offset={3}",
                    deviceInstance, objectInstance, arrayIndexForDoor, offset);

                // 12.59.33Landing_Door_Status
                // This property, of type BACnetARRAY of BACnetLandingDoorStatus, represents the status of the landing doors on the floors
                // served by this lift.Each element of this array represents the list of landing doors for the door of the car assigned to this array
                // element.
                // A landing door status includes the universal floor number and the currently active door status for the landing door.The status
                // values that each landing door status can take on are:
                //      UNKNOWN The landing door status is unknown.
                //      NONE There is no landing door for the respective car door.
                //      CLOSING The landing door is closing.
                //      CLOSED The landing door is fully closed but not locked.
                //      OPENING The landing door is opening.
                //      OPENED The landing door is fully opened.
                //      SAFETY_LOCK The landing door is fully closed and locked.
                //      LIMITED_OPENED The landing door remains in a state between fully closed and fully opened.

                if (!database.lifts.ContainsKey(objectInstance))
                {
                    // Object does not exist. 
                    return false;
                }

                List<BACnetLandingDoor> landingDoorStatus;
                landingDoorStatus = database.lifts[objectInstance].landingDoorStatus[arrayIndexForDoor - 1];

                // Check for empty list
                if (landingDoorStatus.Count == 0)
                {
                    return false;
                }

                // Get the landing call requested by the offset
                BACnetLandingDoor landingDoor = landingDoorStatus[(int)offset];

                // Fill in the floor number, direction, and more values
                *floorNumber = landingDoor.floorNumber;
                *carDoorStatus = landingDoor.carDoorStatus;
                *more = true;
                // Check if there are any more values after this
                if (offset + 1 == landingDoorStatus.Count)
                {
                    // No more values, set more to false
                    *more = false;
                }

                return true;
            }

            public bool CallbackAcknowledgeAlarm(UInt32 deviceInstance, UInt32 acknowledgingProcessIdentifier, UInt16 eventObjectType, UInt32 eventObjectInstance, UInt16 eventStateAcknowledged, Byte eventTimeStampYear, Byte eventTimeStampMonth, Byte eventTimeStampDay, Byte eventTimeStampWeekday, Byte eventTimeStampHour, Byte eventTimeStampMinute, Byte eventTimeStampSecond, Byte eventTimeStampHundrethSecond, System.Byte* acknowledgementSource, UInt32 acknowledgementSourceLength, Byte acknowledgementSourceEncoding, bool timeOfAcknowledgementIsTime, bool timeOfAcknowledgementIsSequenceNumber, bool timeOfAcknowledgementIsDateTime, Byte timeOfAcknowledgementYear, Byte timeOfAcknowledgementMonth, Byte timeOfAcknowledgementDay, Byte timeOfAcknowledgementWeekday, Byte timeOfAcknowledgementHour, Byte timeOfAcknowledgementMinute, Byte timeOfAcknowledgementSecond, Byte timeOfAcknowledgementHundrethSecond, UInt16 timeOfAcknowledgementSequenceNumber, UInt32* errorCode)
            {
                // This function is primarily used to validate the acknowledgingSource
                // But in this example, only the parameters will be displayed
                Console.WriteLine("FYI: Request for CallbackAcknowledgeAlarm. deviceInstance={0}, acknowledgingProcessIdentifier={1}, eventObjectType={2}, eventObjectInstance={3}, eventStateAcknowledge={4}",
                    deviceInstance, acknowledgingProcessIdentifier, eventObjectType, eventObjectInstance, eventStateAcknowledged);
                Console.WriteLine("        eventTimeStamp={0}-{1}-{2} {3}:{4}:{5}.{6}",
                    eventTimeStampDay, eventTimeStampMonth, eventTimeStampYear + 1900, eventTimeStampHour, eventTimeStampMinute, eventTimeStampSecond, eventTimeStampHundrethSecond);
                byte[] temp = new byte[acknowledgementSourceLength];
                Marshal.Copy((IntPtr)acknowledgementSource, temp, 0, (int)acknowledgementSourceLength);
                var str = System.Text.Encoding.Default.GetString(temp);

                if(!str.Equals("TestDevice"))
                {
                    *errorCode = CASBACnetStackAdapter.ERROR_SERVICE_REQUEST_DENIED;
                    return false;
                }

                Console.WriteLine("        acknowledgingSource={0}", str);

                if(timeOfAcknowledgementIsTime)
                {
                    Console.WriteLine("        timeOfAcknowledgement (time)={0}:{1}:{2}.{3}", timeOfAcknowledgementHour, timeOfAcknowledgementMinute, timeOfAcknowledgementSecond, timeOfAcknowledgementHundrethSecond);
                }
                if(timeOfAcknowledgementIsSequenceNumber)
                {
                    Console.WriteLine("        timeOfAcknowledgement (sequenceNumber)={0}", timeOfAcknowledgementSequenceNumber);
                }
                if(timeOfAcknowledgementIsDateTime)
                {
                    Console.WriteLine("        timeOfAcknowledgement (datetime)={0}-{1}-{2} {3}:{4}:{5}.{6}",
                   timeOfAcknowledgementDay, timeOfAcknowledgementMonth, timeOfAcknowledgementYear + 1900, timeOfAcknowledgementHour, timeOfAcknowledgementMinute, timeOfAcknowledgementSecond, timeOfAcknowledgementHundrethSecond);
                }

                return true;
            }
        }
    }
}
