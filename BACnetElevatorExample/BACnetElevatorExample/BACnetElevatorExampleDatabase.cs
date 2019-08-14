using CASBACnetStack;
using System;
using System.Collections.Generic;
using System.Text;

namespace BACnetElevatorExample
{

    class BACnetLandingCallStatus
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

    class BACnetLandingCall
    {
        public Byte floorNumber;
        public UInt32 direction;    // Possible values are { Up (3), Down (4), and Up_And_Down (5) }
    }

    class BACnetLandingDoor
    {
        public Byte floorNumber;
        public UInt32 doorStatus;  
    }

 
    class ExampleDatabase
    {
        public const UInt32 SETTING_DEVICE_INSTANCE = 389001;
        public const string SETTING_DEVICE_NAME = "Elevator Example";

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

        // Lift properties 
        // -------------------------
        // All Lifts have the same floor names. 
        public static string[] FLOOR_NAMES = { "Basement", "Lobby", "One", "Two", "Three", "Four", "Five", "Roof" };
        public static string[] LIFT_CAR_DOOR_TEXT = { "Front" };
        // BACnetDoorStatus::= ENUMERATED {closed(0), opened(1), unknown(2), door-fault(3), unused(4), none(5), closing(6), opening(7), safety-locked(8), limited-opened(9), ...}
        public const UInt32 LIFT_CAR_DOOR_STATUS = 0;
        public const bool LIFT_PASSENGER_ALARM = false;
        public HashSet<UInt32> LIFT_FAULT_SINGALS;

        // BACnetLiftCarDirection ::= ENUMERATED { unknown (0), none (1), stopped (2), up (3), down (4), up-and-down (5), ... }
        public const UInt32 LIFT_CAR_MOVING_DIRECTION = 1; // None 
        public const Byte LIFT_CAR_POSITION = 3;
        public const float LIFT_ENERGY_METER_VALUE = 0.0f;

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
        // BACnetLiftGroupMode::= ENUMERATED {unknown(0), normal(1), down-peak(2), two-way(3), four-way(4), emergency-power(5), up-peak }
        public static UInt32 SETTING_ELEVATOR_GROUP_OF_LIFT_GROUP_MODE = 1;
        public static BACnetLandingCallStatus SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL = new BACnetLandingCallStatus();
        public static List<BACnetLandingCallStatus> SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALLS = new List<BACnetLandingCallStatus>();


        // LIFT C
        public static UInt32 SETTING_LIFT_C_INSTANCE = 2001;
        public static string SETTING_LIFT_C_NAME = "People Lifter (C)";
        public static Byte SETTING_LIFT_C_INSTALLATION_ID = 1;
        public static Byte[] SETTING_LIFT_C_MAKING_CAR_CALL = new Byte[] { 0 };
        public static List<Byte>[] SETTING_LIFT_C_REGISTERED_CAR_CALLS = new List<Byte>[] { new List<Byte>() }; // 1 door, so only 1 element
        public static List<BACnetLandingCall>[] SETTING_LIFT_C_ASSIGNED_LANDING_CALLS = new List<BACnetLandingCall>[] { new List<BACnetLandingCall>() }; // 1 door, so only 1 element
        public static List<BACnetLandingDoor>[] SETTING_LIFT_C_LANDING_DOOR_STATUS = new List<BACnetLandingDoor>[] { new List<BACnetLandingDoor>() }; // 1 door, so only 1 element

        // LIFT D 
        public static UInt32 SETTING_LIFT_D_INSTANCE = 2002;
        public static string SETTING_LIFT_D_NAME = "People Lifter (D)";
        public static Byte SETTING_LIFT_D_INSTALLATION_ID = 2;
        public static Byte[] SETTING_LIFT_D_MAKING_CAR_CALL = new Byte[] { 0 };
        public static List<Byte>[] SETTING_LIFT_D_REGISTERED_CAR_CALLS = new List<Byte>[] { new List<Byte>() }; // 1 door, so only 1 element
        public static List<BACnetLandingCall>[] SETTING_LIFT_D_ASSIGNED_LANDING_CALLS = new List<BACnetLandingCall>[] { new List<BACnetLandingCall>() }; // 1 door, so only 1 element
        public static List<BACnetLandingDoor>[] SETTING_LIFT_D_LANDING_DOOR_STATUS = new List<BACnetLandingDoor>[] { new List<BACnetLandingDoor>() }; // 1 door, so only 1 element

        // A double decker lift.  
        // ----------------------------------------------------------------------------------
        // LIFT E
        public static UInt32 SETTING_LIFT_E_INSTANCE = 2003;
        public static string SETTING_LIFT_E_NAME = "Top of a double decker People Lifter (E)";
        public static Byte SETTING_LIFT_E_INSTALLATION_ID = 3;
        public static Byte[] SETTING_LIFT_E_MAKING_CAR_CALL = new Byte[] { 0 };
        public static List<Byte>[] SETTING_LIFT_E_REGISTERED_CAR_CALLS = new List<Byte>[] { new List<Byte>() }; // 1 door, so only 1 element
        public static List<BACnetLandingCall>[] SETTING_LIFT_E_ASSIGNED_LANDING_CALLS = new List<BACnetLandingCall>[] { new List<BACnetLandingCall>() }; // 1 door, so only 1 element
        public static List<BACnetLandingDoor>[] SETTING_LIFT_E_LANDING_DOOR_STATUS = new List<BACnetLandingDoor>[] { new List<BACnetLandingDoor>() }; // 1 door, so only 1 element

        // LIFT F
        public static UInt32 SETTING_LIFT_F_INSTANCE = 2004;
        public static string SETTING_LIFT_F_NAME = "Bottom of a double decker People Lifter (F)";
        public static Byte SETTING_LIFT_F_INSTALLATION_ID = 4;
        public static Byte[] SETTING_LIFT_F_MAKING_CAR_CALL = new Byte[] { 0 };
        public static List<Byte>[] SETTING_LIFT_F_REGISTERED_CAR_CALLS = new List<Byte>[] { new List<Byte>() }; // 1 door, so only 1 element
        public static List<BACnetLandingCall>[] SETTING_LIFT_F_ASSIGNED_LANDING_CALLS = new List<BACnetLandingCall>[] { new List<BACnetLandingCall>() }; // 1 door, so only 1 element
        public static List<BACnetLandingDoor>[] SETTING_LIFT_F_LANDING_DOOR_STATUS = new List<BACnetLandingDoor>[] { new List<BACnetLandingDoor>() }; // 1 door, so only 1 element

        // Lift and escalators without groups. 
        // ----------------------------------------------------------------------------------
        // Lift G 
        // This lift has two doors. 
        public static UInt32 SETTING_LIFT_G_INSTANCE = 2005;
        public static string SETTING_LIFT_G_NAME = "People Lifter (G)";
        public static Byte SETTING_LIFT_G_INSTALLATION_ID = 1;
        public static Byte SETTING_LIFT_G_GROUP_ID = 3;
        public static string[] SETTING_LIFT_G_DOOR_TEXT = { "Front", "Rear" };
        public static Byte[] SETTING_LIFT_G_MAKING_CAR_CALL = new Byte[] { 0, 0 };
        public static List<Byte>[] SETTING_LIFT_G_REGISTERED_CAR_CALLS = new List<Byte>[] { new List<Byte>(), new List<Byte>() }; // 2 doors, so 2 elements
        public static List<BACnetLandingCall>[] SETTING_LIFT_G_ASSIGNED_LANDING_CALLS = new List<BACnetLandingCall>[] { new List<BACnetLandingCall>(), new List<BACnetLandingCall>() }; // 2 doors, so 2 elements
        public static List<BACnetLandingDoor>[] SETTING_LIFT_G_LANDING_DOOR_STATUS = new List<BACnetLandingDoor>[] { new List<BACnetLandingDoor>(), new List<BACnetLandingDoor>() }; // 2 doors, so 2 elements


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

        

        public void Setup()
        {
            this.ESCALATOR_FAULT_SINGALS = new HashSet<UInt32>() ;
            // BACnetEscalatorFault ::= ENUMERATED { controller-fault (0), drive-and-motor-fault (1), mechanical-component-fault (2), overspeed-fault (3), 
            //                                       power -supply-fault (4), safety-device-fault (5), controller-supply-fault (6), drive-temperature-exceeded (7), 
            //                                       comb -plate-fault (8), ...}
            this.ESCALATOR_FAULT_SINGALS.Add(2); // mechanical-component-fault (2)
            this.ESCALATOR_FAULT_SINGALS.Add(7); // drive-temperature-exceeded (7)


            this.LIFT_FAULT_SINGALS = new HashSet<UInt32>();
            // BACnetLiftFault ::= ENUMERATED { controller-fault (0), drive-and-motor-fault (1), governor-and-safety-gear-fault (2), lift-shaft-device-fault (3), 
            //                                  power-supply-fault (4), safety-interlock-fault (5), door-closing-fault (6), door-opening-fault (7), 
            //                                  car-stopped-outside-landing-zone (8), call-button-stuck (9), start-failure (10), controller-supply-fault (11), 
            //                                  self-test-failure (12), runtime-limit-exceeded (13), position-lost (14), drive-temperature-exceeded (15), 
            //                                  load-measurement-fault (16), ... }
            this.LIFT_FAULT_SINGALS.Add(1); // drive-and-motor-fault (1)
            this.LIFT_FAULT_SINGALS.Add(9); // call-button-stuck (9)
            this.LIFT_FAULT_SINGALS.Add(14); // position-lost (14)


            // 
            SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorNumber = 0;
            SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.floorText = "";
            SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandChoice = BACnetLandingCallStatus.BACnetLandingCallStatusCommand.direction;
            SETTING_ELEVATOR_GROUP_OF_LIFT_LANDING_CALL_CONTROL.commandVaue = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UNKNOWN;

            // Initialize Lift optional properties (for testing only)

            // Registered Car Calls
            SETTING_LIFT_C_REGISTERED_CAR_CALLS[0].Clear(); // Lift C will keep empty list
            SETTING_LIFT_D_REGISTERED_CAR_CALLS[0].Clear(); // Lift D will have one registered car call
            SETTING_LIFT_D_REGISTERED_CAR_CALLS[0].Add(2);
            SETTING_LIFT_E_REGISTERED_CAR_CALLS[0].Clear(); // Lift E and F will both have two
            SETTING_LIFT_E_REGISTERED_CAR_CALLS[0].Add(4);
            SETTING_LIFT_E_REGISTERED_CAR_CALLS[0].Add(6);
            SETTING_LIFT_F_REGISTERED_CAR_CALLS[0].Clear();
            SETTING_LIFT_F_REGISTERED_CAR_CALLS[0].Add(3);
            SETTING_LIFT_F_REGISTERED_CAR_CALLS[0].Add(5);
            SETTING_LIFT_G_REGISTERED_CAR_CALLS[0].Clear(); // Lift G door 1 will have 3, door 2 will have 2
            SETTING_LIFT_G_REGISTERED_CAR_CALLS[1].Clear();
            SETTING_LIFT_G_REGISTERED_CAR_CALLS[0].Add(1);
            SETTING_LIFT_G_REGISTERED_CAR_CALLS[0].Add(2);
            SETTING_LIFT_G_REGISTERED_CAR_CALLS[0].Add(3);
            SETTING_LIFT_G_REGISTERED_CAR_CALLS[1].Add(0);
            SETTING_LIFT_G_REGISTERED_CAR_CALLS[1].Add(5);

            // Assigned Landing Calls
            SETTING_LIFT_C_ASSIGNED_LANDING_CALLS[0].Clear();  // Lift C will keep empty list
            SETTING_LIFT_D_ASSIGNED_LANDING_CALLS[0].Clear(); // Lift D will have one assigned landing call
            SETTING_LIFT_D_ASSIGNED_LANDING_CALLS[0].Add(new BACnetLandingCall { floorNumber = 2, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UP } );
            SETTING_LIFT_E_ASSIGNED_LANDING_CALLS[0].Clear(); // Lift E and F will both have two assigned landing call
            SETTING_LIFT_E_ASSIGNED_LANDING_CALLS[0].Add(new BACnetLandingCall { floorNumber = 4, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UPANDDOWN });
            SETTING_LIFT_E_ASSIGNED_LANDING_CALLS[0].Add(new BACnetLandingCall { floorNumber = 6, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_DOWN });
            SETTING_LIFT_F_ASSIGNED_LANDING_CALLS[0].Clear();
            SETTING_LIFT_F_ASSIGNED_LANDING_CALLS[0].Add(new BACnetLandingCall { floorNumber = 3, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UPANDDOWN });
            SETTING_LIFT_F_ASSIGNED_LANDING_CALLS[0].Add(new BACnetLandingCall { floorNumber = 5, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_DOWN });
            SETTING_LIFT_G_ASSIGNED_LANDING_CALLS[0].Clear(); // Lift G door 1 will have 3, door 2 will have 2
            SETTING_LIFT_G_ASSIGNED_LANDING_CALLS[1].Clear();
            SETTING_LIFT_G_ASSIGNED_LANDING_CALLS[0].Add(new BACnetLandingCall { floorNumber = 1, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UP });
            SETTING_LIFT_G_ASSIGNED_LANDING_CALLS[0].Add(new BACnetLandingCall { floorNumber = 2, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UPANDDOWN });
            SETTING_LIFT_G_ASSIGNED_LANDING_CALLS[0].Add(new BACnetLandingCall { floorNumber = 3, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_DOWN });
            SETTING_LIFT_G_ASSIGNED_LANDING_CALLS[1].Add(new BACnetLandingCall { floorNumber = 0, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_UP });
            SETTING_LIFT_G_ASSIGNED_LANDING_CALLS[1].Add(new BACnetLandingCall { floorNumber = 5, direction = CASBACnetStackAdapter.LIFT_CAR_DIRECTION_DOWN });

            // Landing Door Status - set all landing doors to closed
            SETTING_LIFT_C_LANDING_DOOR_STATUS[0].Clear();
            for(int i = 0; i < 8; i++)
            {
                SETTING_LIFT_C_LANDING_DOOR_STATUS[0].Add(new BACnetLandingDoor { floorNumber = (byte)i, doorStatus = CASBACnetStackAdapter.DOOR_STATUS_CLOSED });
            }
            SETTING_LIFT_D_LANDING_DOOR_STATUS[0].Clear();
            for (int i = 0; i < 8; i++)
            {
                SETTING_LIFT_D_LANDING_DOOR_STATUS[0].Add(new BACnetLandingDoor { floorNumber = (byte)i, doorStatus = CASBACnetStackAdapter.DOOR_STATUS_CLOSED });
            }
            SETTING_LIFT_E_LANDING_DOOR_STATUS[0].Clear();
            for (int i = 0; i < 8; i++)
            {
                SETTING_LIFT_E_LANDING_DOOR_STATUS[0].Add(new BACnetLandingDoor { floorNumber = (byte)i, doorStatus = CASBACnetStackAdapter.DOOR_STATUS_CLOSED });
            }
            SETTING_LIFT_F_LANDING_DOOR_STATUS[0].Clear();
            for (int i = 0; i < 8; i++)
            {
                SETTING_LIFT_F_LANDING_DOOR_STATUS[0].Add(new BACnetLandingDoor { floorNumber = (byte)i, doorStatus = CASBACnetStackAdapter.DOOR_STATUS_CLOSED });
            }
            // Lift G Front Landing Door Status
            SETTING_LIFT_G_LANDING_DOOR_STATUS[0].Clear();
            for (int i = 0; i < 8; i++)
            {
                SETTING_LIFT_G_LANDING_DOOR_STATUS[0].Add(new BACnetLandingDoor { floorNumber = (byte)i, doorStatus = CASBACnetStackAdapter.DOOR_STATUS_CLOSED });
            }
            // Lift G Rear Landing Door Status
            SETTING_LIFT_G_LANDING_DOOR_STATUS[1].Clear();
            SETTING_LIFT_G_LANDING_DOOR_STATUS[1].Add(new BACnetLandingDoor { floorNumber = 0, doorStatus = CASBACnetStackAdapter.DOOR_STATUS_CLOSED });
            SETTING_LIFT_G_LANDING_DOOR_STATUS[1].Add(new BACnetLandingDoor { floorNumber = 7, doorStatus = CASBACnetStackAdapter.DOOR_STATUS_SAFETYLOCKED });
        }

        public void Loop()
        {

        }

    }

}
