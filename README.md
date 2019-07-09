# BACnet Elevator Example

This project was designed as an example of a BACnet IP server that implments Elevator groups, lifts, Escalator objects using C# and the [CAS BACnet stack](https://www.bacnetstack.com/).

The server device has a fixed set of BACnet objects:

- Elevator group (1000) - This elevator group contains only escalators.
  - Escalator A (1001)
  - Escalator B (1002)
- Elevator group (2000) - This elevator group contains only lifts.
  - Lift C (2001)
  - Lift D (2002)
  - Double deck lift
    - Lift E Top deck (2003)
    - Lift F Bottom deck (2003)
- Lift G (2005) - This lift does not belong to a elevator group. Has two doors (Front and Rear)
- Escalator H (1003) - This escalators does not belong to a elevator group
- Positive integer value (1) - This is the machine room ID for Elevator group (1000)
- Positive integer value (2) - This is the machine room ID for Elevator group (2000)

```txt
*-------------------* *------------------------------*
|                   | |                              |
| |======| |======| | |                     *------* |           |======|
| |======| |======| | |                     |  E   | |           |======|
| |======| |======| | |                     | 2003 | |           |======|
| |======| |======| | | *------*  *------*  *------* | *------*  |======|
| |= A ==| |= B ==| | | |  C   |  |  D   |  |  F   | | |  G   |  |= H ==|
| | 1001 | | 1002 | | | | 2001 |  | 2002 |  | 2004 | | | 2005 |  | 1003 |
| |======| |======| | | *------*  *------|  *------* | *------*  |======|
|       1000        | |             2000             |     ^        ^
*-------------------* *------------------------------*     |        |
          ^                           ^         ^          |        \--- Single escalator without group
          |                           |         |          \------------ Single lift without group, with double doors
          |                           |         \----------------------- Double decker Lift
          |                           \--------------------------------- Mulitple of lifts in this elevator group
          \------------------------------------------------------------- Mulitple of escalator in this elevator group
```

## CAS BACnet Stack Elevator groups, lifts, Escalator functions

The specific function in the CAS BACnet stack for adding or manupliating the Elevator groups, lifts, Escalator object types.

### AddElevatorGroupObject

Adds an Elevator Group Object. The device must be added first before this function can be called. The Elevator Group Object references various Lift or Escalator objects. If adding escalators or lifts to an Elevator Group Object, users must add the Elevator Group object first.

```c++
bool BACnetStack_AddElevatorGroupObject(
    const uint32_t deviceInstance, const uint32_t objectInstance,
    const uint32_t machineRoomId, const uint8_t groupId, const bool isGroupOfLifts,
    const bool supportLandingCallStatus)
```

- **deviceInstance** [IN] - The instance number of the device that owns this Elevator Group Object.
- **objectInstance** [IN] - The instance number of this Elevator Group Object.
- **machineRoomId** [IN] - The instance number of the Positive Integer Value Object whose present value property represents the Machine Room ID. If the Positive Integer Value Object does not exist, this function will create it.
- **groupId** [IN] - The group ID for this Elevator Group object.
- **isGroupOfLifts** [IN] - A flag that sets the Elevator Group object to be a group of Lift Objects (true) or Escalator Objects (false).
- **supportLandingCallStatus** [IN] - A flag to enable the Landing Calls and Landing Call Control properties.

return True if successful, false if failed.

### AddLiftOrEscalatorObject

Adds a Lift or Escalator Object. The device must be added first before this function can be called. If adding to an Elevator Group Object, the Elevator Group object must be created first.

```c++
bool BACnetStack_AddLiftOrEscalatorObject(
    const uint32_t deviceInstance, const uint16_t objectType, 
    const uint32_t objectInstance, const uint32_t elevatorGroupInstance,
    const uint8_t groupId, const uint8_t installationId)
```

- **deviceInstance** [IN] - The instance number of the device that owns this Lift or Escalator Object.
- **objectType** [IN] - The object type of this object.  Must be either Lift or Escalator.
- **objectInstance** [IN] - The instance number of this Lift or Escalator Object.
- **elevatorGroupInstance** [IN] - The instance number of the Elevator Group Object that this Lift or Escalator Object belongs to. If none exists, use 4194303 to specify that this Lift or Escalator Object is not part of an Elevator Group Object.
- **groupId** [IN] - The groupId assigned to this Lift or Escalator Object.
- **installationId** [IN] - The installationId assigned to this Lift or Escalator Object.  If this Lift or Escalator Object belongs to an Elevator Group Object, it must have a unique installationId among all other Lift or Escalator Objects in the Elevator Group Object.

return True if successful, false if failed.

### SetLiftHigherLowerDeck

Sets the HigherDeck and LowerDeck properties of the Lift Object. If the HigherDeck and LowerDeck properties have not been enabled for this Lift Object, then this function will enable them. This function is used to specify a Lift Object as a Multi-Deck Lift. If the Lift Object is the highest deck, then set the higherObjectInstance to 4194303. If the Lift Object is the lowest deck, then set the lowerObjectInstance to 4194303.

```c++
bool BACnetStack_SetLiftHigherLowerDeck(
    const uint32_t deviceInstance, const uint32_t objectInstance,
    const uint32_t higherDeckObjectInstance, const uint32_t lowerDeckObjectInstance)
```

- **deviceInstance** [IN] - The instance of the device that contains the Lift object.
- **objectInstance** [IN] - The instance of the Lift object to update.
- **higherDeckObjectInstance** [IN] - The instance of a Lift object that is a higher deck to this Lift object.  If this Lift Object is the highest deck, then set this to 4194303.
- **lowerDeckObjectInstance** [IN] - The instance of a Lift object that is a lower deck to this Lift object.  If this Lift Object is the lowest deck, then set this to 4194303.

return True if successful, otherwise false.

### SetLiftOrEscalatorEnergyMeterRef

Sets the EnergyMeterRef property of a Lift or Escalator object. If the EnergyMeterRef property has not been enabled for this Lift or Escalator object, then this function will enable it.

If a Lift object already has the EnergyMeter property enabled and if the energyMeterRefObjectInstance is not set to 4194303, then the CAS BACnet Stack will use a value of 0.0 for the Energy Meter propery. Note: The above only applies to Lift objects, not Escalator objects

From the Spec: If the Energy_Meter_Ref property is present and initialized (contains an instance other than 4194303), then the Energy_Meter property, if present, shall contain a value of 0.0.

```c++
bool BACnetStack_SetLiftOrEscalatorEnergyMeterRef(
    const uint32_t deviceInstance, const uint16_t objectType,
    const uint32_t objectInstance, const bool energyMeterRefUseDevice,
    const uint32_t energyMeterRefDeviceInstance, const uint16_t energyMeterRefObjectType,
    const uint32_t energyMeterRefObjectInstance)
```

- **deviceInstance** [IN] - The instance of the device that contains the Lift object.
- **objectType** [IN] - The BACnet Object Type of this object.  Must be either Lift or Escalator. See #CASBACnetStack::BACnetObjectType for more info.
- **objectInstance** [IN] - The instance of the Lift object to update.
- **energyMeterRefUseDevice** [IN] - Flag that specifies whether the energy meter reference has a device identifier (true) or not (false).
- **energyMeterRefDeviceInstance** [IN] - The instance of the Device that contains the object that is the Energy Meter Ref.
- **energyMeterRefObjectType** [IN] - The BACnet Object Type of the object that is the Energy Meter Ref.  #CASBACnetStack::BACnetObjectType for more info.
- **energyMeterRefObjectInstance** [IN] - The instance of the object that is the Energy Meter Ref.

## Enumerations

Enumerations used by the Elevator groups, Lift, Escalator object types properties.

### Elevator Group Object Type

#### Group_Mode

This property, of type BACnetLiftGroupMode, shall convey the operating mode of the group of lifts.

##### BACnetLiftGroupMode Enumeration

- unknown (0) - The current operating mode of the lift group is unknown.
- normal (1) - The lift group is in normal operating mode.
- down-peak (2) - Most passengers want to leave the building.
- two-way (3) - Many passengers want to get to, or leave, a particular floor.
- four-way (4) - Many passengers want to move between two particular floors.
- emergency-power (5) - The whole lift group is operating under an emergency power supply.
- up-peak (6) - Most passengers gather at the main terminal, usually the ground floor, to get to different floors of the building

### Lift Object Type

#### Car_Moving_Direction

This property, of type BACnetLiftCarDirection, represents whether or not this lift's car is moving, and if so, in which direction. Car_Moving_Direction can take on one of these values:

- UNKNOWN (0) - The current moving direction of the lift is unknown.
- STOPPED (2) - The lift car is not moving.
- UP (3) - The lift car is moving upward.
- DOWN (4) - The lift car is moving downward.

#### Car_Assigned_Direction

This property, of type BACnetLiftCarDirection, represents the direction the lift is assigned to move, based on current car calls. Car_Assigned_Direction can take on these values:

- UNKNOWN (0) - The direction assigned to the lift is unknown.
- NONE (1) - No direction is assigned to the lift car.
- UP (3) - The lift car is assigned to move upward.
- DOWN (4) - The lift car is assigned to move downward.
- UP_AND_DOWN (5) - The lift car is assigned to either move upward or downward.
- <Proprietary Enum Values> A vendor may use other proprietary enumeration values to allow proprietary lift car direction assignments other than those defined by the standard. For proprietary extensions

#### Car_Door_Status

BACnetDoorStatus ::= ENUMERATED

- closed (0)
- opened (1)
- unknown (2)
- door-fault (3)
- unused (4)
- none (5)
- closing (6)
- opening (7)
- safety-locked (8)
- limited-opened (9)

#### Car_Door_Command

BACnetLiftCarDoorCommand ::= ENUMERATED

- none (0)
- open (1)
- close (2)

#### Car_Mode

BACnetLiftCarMode ::= ENUMERATED

- unknown (0)
- normal (1), -- in service
- vip (2)
- homing (3)
- parking (4)
- attendant-control (5)
- firefighter-control (6)
- emergency-power (7)
- inspection (8)
- cabinet-recall (9)
- earthquake-operation (10)
- fire-operation (11)
- out-of-service (12)
- occupant-evacuation (13)

#### Car_Drive_Status

BACnetLiftCarDriveStatus ::= ENUMERATED

- unknown (0)
- stationary (1)
- braking (2)
- accelerate (3)
- decelerate (4)
- rated-speed (5)
- single-floor-jump (6)
- two-floor-jump (7)
- three-floor-jump (8)
- multi-floor-jump (9)

#### Fault_Signals

BACnetLiftFault ::= ENUMERATED

- controller-fault (0)
- drive-and-motor-fault (1)
- governor-and-safety-gear-fault (2)
- lift-shaft-device-fault (3)
- power-supply-fault (4)
- safety-interlock-fault (5)
- door-closing-fault (6)
- door-opening-fault (7)
- car-stopped-outside-landing-zone (8)
- call-button-stuck (9)
- start-failure (10)
- controller-supply-fault (11)
- self-test-failure (12)
- runtime-limit-exceeded (13)
- position-lost (14)
- drive-temperature-exceeded (15)
- load-measurement-fault (16)

### Escalator Object Type

#### Operation_Direction

BACnetEscalatorOperationDirection ::= ENUMERATED

- unknown (0)
- stopped (1)
- up-rated-speed (2)
- up-reduced-speed (3)
- down-rated-speed (4)
- down-reduced-speed (5)

#### Escalator_Mode

BACnetEscalatorMode ::= ENUMERATED

- unknown (0)
- stop (1)
- up (2)
- down (3)
- inspection (4)
- out-of-service (5)

#### Fault_Signals

BACnetEscalatorFault ::= ENUMERATED

- controller-fault (0)
- drive-and-motor-fault (1)
- mechanical-component-fault (2)
- overspeed-fault (3)
- power-supply-fault (4)
- safety-device-fault (5)
- controller-supply-fault (6)
- drive-temperature-exceeded (7)
- comb-plate-fault (8)

## Building

This project also auto built using [Gitlab CI](https://docs.gitlab.com/ee/ci/) on every commit.

### Windows

A [Visual studios 2019](https://visualstudio.microsoft.com/vs/) project is included with this project.
