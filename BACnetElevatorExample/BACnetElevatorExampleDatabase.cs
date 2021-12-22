using CASBACnetStack;
using System;
using System.Collections.Generic;
using System.Text;

namespace BACnetElevatorExample
{
    public class BACnetLandingCallStatus
    {
        public Byte floorNumber;
        public enum BACnetLandingCallStatusCommand
        {
            direction = 1,
            destination = 2
        }
        public BACnetLandingCallStatusCommand commandChoice = BACnetLandingCallStatusCommand.direction;
        public UInt32 commandVaue;
        public String floorText; 
    }

    public class BACnetLandingCall
    {
        public Byte floorNumber;
        public UInt32 direction;    // Possible values are { Up (3), Down (4), and Up_And_Down (5) }

        public static string[] carDirectionText = new string[] { "unknown", "none", "stopped", "up", "down", "up-and-down" };
    }

    public class BACnetLandingDoor
    {
        public Byte floorNumber;
        public UInt32 carDoorStatus;
        public static string[] carDoorStatusText = new string[] { "closed", "opened", "unknown", "door-fault", "unused", "none", "closing", "opening", "safety-locked", "limited-opened" };
    }

    public class BACnetBaseObject
    {
        public UInt32 instance;
        public string objectName;

        public BACnetBaseObject(UInt32 p_instance, string p_objectName)
        {
            this.instance = p_instance;
            this.objectName = p_objectName; 
        }
    }

    public class BACnetDeviceObject : BACnetBaseObject
    {
        public BACnetDeviceObject(UInt32 p_instance, string p_objectName) : base(p_instance, p_objectName )
        {

        }
    }

    public class BACnetLiftObject : BACnetBaseObject
    {
        public Byte installationID;
        public string[] carDoorText;
        // 12.59.9 Floor_Text
        // This property, of type BACnetARRAY of CharacterString, represents the descriptions or names for the floors. The universal
        // floor number serves as an index into this array. The size of this array shall match the highest universal floor number served
        // by this lift.
        public static string[] floorText = new string[] { "Basement", "Lobby", "One", "Two", "Three", "Four", "Five", "Roof" };
        public UInt32[] carDoorStatus; // See BACnetLandingDoor.carDoorStatusText
        public bool passengerAlarm;
        public UInt32 carMovingDirection; // See BACnetLandingCall.carDirectionText
        public Byte carPosition;
        public float energyMeter;
        public Byte groupID;

        public Byte[] makingCarCall;
        
        // 12.59.13 Registered_Car_Call
        // This property, of type BACnetARRAY of BACnetLiftCarCallList, represents the lists of currently registered car calls
        // (requests to stop at particular floors using a particular door) for this lift .Each array element represents the list of universal
        // floor numbers for which calls are registered for the door of the car assigned to this array element.
        public List<Byte>[] registeredCarCalls;

        public List<BACnetLandingCall>[] assignedLandingCalls;
        public List<BACnetLandingDoor>[] landingDoorStatus;
        public HashSet<UInt32> faultSignals;

        public BACnetLiftObject(UInt32 p_instance, string p_objectName, Byte p_installationID, Byte p_groupID) : base(p_instance, p_objectName)
        {
            this.installationID = p_installationID;
            this.groupID = p_groupID; 

            this.makingCarCall = new Byte[] { 0 };
            
            // 12.59.10 Car_Door_Text
            // This property, of type BACnetARRAY of CharacterString, represents the descriptions or names for the doors of the lift car.
            // Each array element represents the description or name for the door of the car assigned to this array element.
            this.carDoorText = new string[] {"Front"};

            

            // BACnetlifts[LIFT_C_INSTANCE]arDirection ::= ENUMERATED { unknown (0), none (1), stopped (2), up (3), down (4), up-and-down (5), ... }
            this.carMovingDirection = 1; // None 
            this.carPosition = 3;
            this.energyMeter = 0.0f;
            
            this.passengerAlarm = false;

            this.registeredCarCalls = new List<Byte>[] { new List<Byte>() }; // 1 door, so only 1 element
            this.assignedLandingCalls = new List<BACnetLandingCall>[] { new List<BACnetLandingCall>() }; // 1 door, so only 1 element

            // 12.59.33 Landing_Door_Status
            // This property, of type BACnetARRAY of BACnetLandingDoorStatus, represents the status of the landing doors on the floors
            // served by this lift.Each element of this array represents the list of landing doors for the door of the car assigned to this array
            // element. A landing door status includes the universal floor number and the currently active door status for the landing door. The 
            // values that each landing door status can take on are:
            // - UNKNOWN The landing door status is unknown. 
            // - NONE There is no landing door for the respective car door.
            // - CLOSING The landing door is closing.
            // - CLOSED The landing door is fully closed but not locked.
            // - OPENING The landing door is opening.
            // - OPENED The landing door is fully opened.
            // - SAFETY_LOCK The landing door is fully closed and locked.
            // - LIMITED_OPENED The landing door remains in a state between fully closed and fully opened.
            
            this.landingDoorStatus = new List<BACnetLandingDoor>[] { new List<BACnetLandingDoor>() }; // 1 door, so only 1 element           

            // 12.59.17 Car_Door_Status
            // This property, of type BACnetARRAY of BACnetDoorStatus, indicates the status of the doors on the car.Each array element
            // indicates the status of the car door assigned to this array element. 
            // BACnetDoorStatus::= ENUMERATED {closed(0), opened(1), unknown(2), door-fault(3), unused(4), none(5), closing(6), opening(7), safety-locked(8), limited-opened(9), ...}
            this.carDoorStatus = new UInt32[carDoorText.Length];
            for (int i = 0; i < carDoorText.Length; i++) {
                this.carDoorStatus[i] = 0; // Close = 0 
            }

            // BACnetlifts[LIFT_F_INSTANCE]ault ::= ENUMERATED { controller-fault (0), drive-and-motor-fault (1), governor-and-safety-gear-fault (2), lift-shaft-device-fault (3), 
            //                                  power-supply-fault (4), safety-interlock-fault (5), door-closing-fault (6), door-opening-fault (7), 
            //                                  car-stopped-outside-landing-zone (8), call-button-stuck (9), start-failure (10), controller-supply-fault (11), 
            //                                  self-test-failure (12), runtime-limit-exceeded (13), position-lost (14), drive-temperature-exceeded (15), 
            //                                  load-measurement-fault (16), ... }
            this.faultSignals = new HashSet<UInt32>();

            // Landing Door Status - set all landing doors to closed
            this.landingDoorStatus[0].Clear();
            for (int i = 0; i < floorText.Length; i++) {
                this.landingDoorStatus[0].Add(new BACnetLandingDoor { floorNumber = (byte) i, carDoorStatus = CASBACnetStackAdapter.DOOR_STATUS_CLOSED });
            }
        }
    }


    class ExampleDatabase
    {
        public BACnetDeviceObject device;

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
        //           |                           |         |          \-------------- Single lift without group, Double door. (Front and Rear)
        //           |                           |         \------------------------- Double decker Lift
        //           |                           \----------------------------------- Mulitple of lifts in a group
        //           \--------------------------------------------------------------- Mulitple of escalator in a group
        // 

        // Properties of the Elevator Group Object Type
        // ------------------------------------------------------------------------------------
        // 12.58.5 Machine_Room_ID
        // This property, of type BACnetObjectIdentifier, shall reference the Positive Integer 
        // Value Object whose Present_Value property contains the identification number for the 
        // machine room that contains the group of lifts or escalators represented by this object. 
        // If there is no such identification number, this property shall contain an object 
        // instance number of 4194303.
        // 
        // 12.58.6 Group_ID
        // This property, of type Unsigned8, shall represent the identification number for the 
        // group of lifts or escalators represented by this object. This identification number 
        // shall be unique for the groups in this machine room, but might not be otherwise 
        // unique in the building.

        public static UInt32 LIFT_C_INSTANCE = 2001;
        public static UInt32 LIFT_D_INSTANCE = 2002;
        public static UInt32 LIFT_E_INSTANCE = 2003;
        public static UInt32 LIFT_F_INSTANCE = 2004;
        public static UInt32 LIFT_G_INSTANCE = 2005;

        public Dictionary<UInt32, BACnetLiftObject> lifts;

        // Two escalators 
        // ----------------------------------------------------------------------------------
        // ESCALATOR GROUP 
        public static UInt32 SETTING_ELEVATOR_GROUP_OF_ESCALATOR_INSTANCE = 1000;
        public static string SETTING_ELEVATOR_GROUP_OF_ESCALATOR_NAME = "ESCALATOR Group";
        public static UInt32 SETTING_ELEVATOR_GROUP_OF_ESCALATOR_MACHINE_ROOM_ID = 1;
        public static Byte SETTING_ELEVATOR_GROUP_OF_ESCALATOR_GROUP_ID = 1;

        // ESCALATOR A 
        public static UInt32 SETTING_ESCALATOR_A_INSTANCE = 1001;
        public static string SETTING_ESCALATOR_A_NAME = "Moving sidewalk (A)";
        public static Byte SETTING_ESCALATOR_A_INSTALLATION_ID = 1;

        // ESCALATOR B
        public static UInt32 SETTING_ESCALATOR_B_INSTANCE = 1002;
        public static string SETTING_ESCALATOR_B_NAME = "Moving sidewalk (B)";
        public static Byte SETTING_ESCALATOR_B_INSTALLATION_ID = 2;


        // Group of lifts. 
        // ----------------------------------------------------------------------------------
        // LIFT GROUP 
        public static UInt32 SETTING_ELEVATOR_GROUP_OF_LIFT_INSTANCE = 2000;
        public static string SETTING_ELEVATOR_GROUP_OF_LIFT_NAME = "LIFT Group";
        public static UInt32 SETTING_ELEVATOR_GROUP_OF_LIFT_MACHINE_ROOM_ID = 2;
        public static Byte SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_ID = 2;
        // BACnetlifts[LIFT_G_INSTANCE]roupMode::= ENUMERATED {unknown(0), normal(1), down-peak(2), two-way(3), four-way(4), emergency-power(5), up-peak }
        public static UInt32 SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_MODE = 1;
        public static BACnetLandingCallStatus SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL = new BACnetLandingCallStatus();
        public static List<BACnetLandingCallStatus> SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALLS = new List<BACnetLandingCallStatus>();
        
        // ESCALATOR H
        public static UInt32 SETTING_ESCALATOR_H_INSTANCE = 1003;
        public static string SETTING_ESCALATOR_H_NAME = "Moving sidewalk (H)";
        public static Byte SETTING_ESCALATOR_H_INSTALLATION_ID = 1;
        public static Byte SETTING_ESCALATOR_H_GROUP_ID = 4;

        // Positive Integer Value 
        // ----------------------------------------------------------------------------------
        public static string SETTING_MACHINE_ROOM_1_NAME = "Machine room (1)";
        public static string SETTING_MACHINE_ROOM_2_NAME = "Machine room (2)";

        
        // ESCALATOR properties 
        // -------------------------
        // BACnetEscalatorOperationDirection::= ENUMERATED { unknown(0), stopped(1), up-rated-speed(2), up-reduced-speed(3), down-rated-speed(4), down-reduced-speed(5), ...}
        public const UInt32 ESCALATOR_OPERATION_DIRECTION = 0;
        public const bool ESCALATOR_PASSENGER_ALARM = true;
        public HashSet<UInt32> ESCALATOR_FAULT_SINGALS;
        public const float ESCALATOR_ENERGY_METER_VALUE = 0.0f;

        // NOTIFICATION CLASS properties
        public static UInt32 SETTING_NOTIFICATION_CLASS_INSTANCE = 100;
        public const Byte NOTIFICATION_CLASS_TOOFFNORMAL_PRIORITY = 10;
        public const Byte NOTIFICATION_CLASS_TOFAULT_PRIORITY = 100;
        public const Byte NOTIFICATION_CLASS_TONORMAL_PRIORITY = 200;
        public const bool NOTIFICATION_CLASS_TOOFFNORMAL_ACKREQUIRED = true;
        public const bool NOTIFICATION_CLASS_TOFAULT_ACKREQUIRED = true;
        public const bool NOTIFICATION_CLASS_TONORMAL_ACKREQUIRED = true;

        public ExampleDatabase()
        {
            device = new BACnetDeviceObject(389001, "Elevator Example");

            this.ESCALATOR_FAULT_SINGALS = new HashSet<UInt32>();
            // BACnetEscalatorFault ::= ENUMERATED { controller-fault (0), drive-and-motor-fault (1), mechanical-component-fault (2), overspeed-fault (3), 
            //                                       power -supply-fault (4), safety-device-fault (5), controller-supply-fault (6), drive-temperature-exceeded (7), 
            //                                       comb -plate-fault (8), ...}
            this.ESCALATOR_FAULT_SINGALS.Add(2); // mechanical-component-fault (2)
            this.ESCALATOR_FAULT_SINGALS.Add(7); // drive-temperature-exceeded (7)

            // Create all the lifts 
            lifts = new Dictionary<UInt32, BACnetLiftObject>();
            lifts[LIFT_C_INSTANCE] = new BACnetLiftObject(LIFT_C_INSTANCE, "People lifts (C)", 1, 2);
            lifts[LIFT_D_INSTANCE] = new BACnetLiftObject(LIFT_D_INSTANCE, "People lifts (D)", 2, 2);
            lifts[LIFT_E_INSTANCE] = new BACnetLiftObject(LIFT_E_INSTANCE, "Top of a double decker People lifts (E)", 3, 2);
            lifts[LIFT_F_INSTANCE] = new BACnetLiftObject(LIFT_F_INSTANCE, "Bottom of a double decker People lifts (F)", 4, 2);
            lifts[LIFT_G_INSTANCE] = new BACnetLiftObject(LIFT_G_INSTANCE, "People lifts (G)", 1, 3);

            // Lift G, Has two doors, Front and back
            lifts[LIFT_G_INSTANCE].carDoorText = new string[] { "Front", "Rear" };
            lifts[LIFT_G_INSTANCE].makingCarCall = new Byte[] { 0, 0 };
            lifts[LIFT_G_INSTANCE].registeredCarCalls = new List<Byte>[] { new List<Byte>(), new List<Byte>() }; // 2 doors, so 2 elements
            lifts[LIFT_G_INSTANCE].assignedLandingCalls = new List<BACnetLandingCall>[] { new List<BACnetLandingCall>(), new List<BACnetLandingCall>() }; // 2 doors, so 2 elements
            lifts[LIFT_G_INSTANCE].landingDoorStatus = new List<BACnetLandingDoor>[] { new List<BACnetLandingDoor>(), new List<BACnetLandingDoor>() }; // 2 doors, so 2 elements
            lifts[LIFT_G_INSTANCE].carDoorStatus = new UInt32[lifts[LIFT_G_INSTANCE].carDoorText.Length];
            lifts[LIFT_G_INSTANCE].carDoorStatus[0] = 0; // Close = 0 
            lifts[LIFT_G_INSTANCE].carDoorStatus[1] = 0; // Close = 0 


            // ELEVATOR_GROUP
            SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorNumber = 0;
            SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorText = "";
            SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice = BACnetLandingCallStatus.BACnetLandingCallStatusCommand.direction;
            SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandVaue = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UNKNOWN;

            // Initialize Lift optional properties (for testing only)
            // ------------

            // faultSignals
            lifts[LIFT_D_INSTANCE].faultSignals.Add(1); // drive-and-motor-fault (1)
            lifts[LIFT_D_INSTANCE].faultSignals.Add(9); // call-button-stuck (9)
            lifts[LIFT_D_INSTANCE].faultSignals.Add(14); // position-lost (14)

            // Registered Car Calls
            lifts[LIFT_C_INSTANCE].registeredCarCalls[0].Clear(); // Lift C will keep empty list
            lifts[LIFT_D_INSTANCE].registeredCarCalls[0].Clear(); // Lift D will have one registered car call
            lifts[LIFT_D_INSTANCE].registeredCarCalls[0].Add(2);
            lifts[LIFT_E_INSTANCE].registeredCarCalls[0].Clear(); // Lift E and F will both have two
            lifts[LIFT_E_INSTANCE].registeredCarCalls[0].Add(4);
            lifts[LIFT_E_INSTANCE].registeredCarCalls[0].Add(6);
            lifts[LIFT_F_INSTANCE].registeredCarCalls[0].Clear();
            lifts[LIFT_F_INSTANCE].registeredCarCalls[0].Add(3);
            lifts[LIFT_F_INSTANCE].registeredCarCalls[0].Add(5);
            lifts[LIFT_G_INSTANCE].registeredCarCalls[0].Clear(); // Lift G door 1 will have 3, door 2 will have 2
            lifts[LIFT_G_INSTANCE].registeredCarCalls[1].Clear();
            lifts[LIFT_G_INSTANCE].registeredCarCalls[0].Add(1);
            lifts[LIFT_G_INSTANCE].registeredCarCalls[0].Add(2);
            lifts[LIFT_G_INSTANCE].registeredCarCalls[0].Add(3);
            lifts[LIFT_G_INSTANCE].registeredCarCalls[1].Add(0);
            lifts[LIFT_G_INSTANCE].registeredCarCalls[1].Add(5);

            // Assigned Landing Calls
            lifts[LIFT_C_INSTANCE].assignedLandingCalls[0].Clear();  // Lift C will keep empty list            
            lifts[LIFT_D_INSTANCE].assignedLandingCalls[0].Clear(); // Lift D will have one assigned landing call
            lifts[LIFT_D_INSTANCE].assignedLandingCalls[0].Add(new BACnetLandingCall { floorNumber = 2, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UP } );
            lifts[LIFT_E_INSTANCE].assignedLandingCalls[0].Clear(); // Lift E and F will both have two assigned landing call
            lifts[LIFT_E_INSTANCE].assignedLandingCalls[0].Add(new BACnetLandingCall { floorNumber = 4, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UPANDDOWN });
            lifts[LIFT_E_INSTANCE].assignedLandingCalls[0].Add(new BACnetLandingCall { floorNumber = 6, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_DOWN });
            lifts[LIFT_F_INSTANCE].assignedLandingCalls[0].Clear();
            lifts[LIFT_F_INSTANCE].assignedLandingCalls[0].Add(new BACnetLandingCall { floorNumber = 3, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UPANDDOWN });
            lifts[LIFT_F_INSTANCE].assignedLandingCalls[0].Add(new BACnetLandingCall { floorNumber = 5, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_DOWN });
            lifts[LIFT_G_INSTANCE].assignedLandingCalls[0].Clear(); // Lift G door 1 will have 3, door 2 will have 2
            lifts[LIFT_G_INSTANCE].assignedLandingCalls[1].Clear();
            lifts[LIFT_G_INSTANCE].assignedLandingCalls[0].Add(new BACnetLandingCall { floorNumber = 1, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UP });
            lifts[LIFT_G_INSTANCE].assignedLandingCalls[0].Add(new BACnetLandingCall { floorNumber = 2, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UPANDDOWN });
            lifts[LIFT_G_INSTANCE].assignedLandingCalls[0].Add(new BACnetLandingCall { floorNumber = 3, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_DOWN });
            lifts[LIFT_G_INSTANCE].assignedLandingCalls[1].Add(new BACnetLandingCall { floorNumber = 0, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UP });
            lifts[LIFT_G_INSTANCE].assignedLandingCalls[1].Add(new BACnetLandingCall { floorNumber = 5, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_DOWN });

            // Lift G Rear Landing Door Status
            lifts[LIFT_G_INSTANCE].landingDoorStatus[1].Clear();
            lifts[LIFT_G_INSTANCE].landingDoorStatus[1].Add(new BACnetLandingDoor { floorNumber = 0, carDoorStatus = CASBACnetStackAdapter.DOOR_STATUS_CLOSED });
            lifts[LIFT_G_INSTANCE].landingDoorStatus[1].Add(new BACnetLandingDoor { floorNumber = 7, carDoorStatus = CASBACnetStackAdapter.DOOR_STATUS_SAFETYLOCKED });


        }

        public void Loop()
        {

        }
    }
}
