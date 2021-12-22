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

## Alarms and Events

Use the following functions to enable alarms and events for Lifts and Escalator objects.

### AddNotificationClassObject

First, add a Notification Class object.  The Notification Class object contains all the information of how to handle event notifications.

```c++
bool BACnetStack_AddNotificationClassObject(
    const uint32_t deviceInstance, const uint32_t objectInstance, 
    const uint8_t toOffNormalPriority, const uint8_t toFaultPriority, const uint8_t toNormalPriority, 
    const bool toOffNormalAckRequired, const bool toFaultAckRequired, const bool toNormalAckRequired)
```

- **deviceInstance** [in] - The instance number of the device that owns this Notification Class Object.
- **objectInstance** [in] - The instance number of this Notification Class Object.
- **toOffNormalPriority** [in] - The priority for To_OffNormal events. Values are 0-255.
- **toFaultPriority** [in] - The priority for To_Fault events. Values are 0-255.
- **toNormalPriority** [in] - The priority for To_Normal events. Values are 0-255
- **toOffNormalAckRequired** [in] - Whether the To_OffNormal events must be acknowledged (true) or not (false);
- **toFaultAckRequired** [in] - Whether the To_Fault events must be acknowledged (true) or not (false);
- **toNormalAckRequired** [in] - Whether the To_Normal events must be acknowledged (true) or not (false);

### AddRecipientToNotificationClass

Once the Notification Class object has been created, the next step is to add a recipient to the Notification Class.
A recipient is the BACnet Device that will receive the notifications as well as the days and times that it can receive them.

This gets added to an existing Notification Class Object, therefore, a Notification Class Object must exist
before this function can be called.

```c++
bool BACnetStack_AddRecipientToNotificationClass(
    const uint32_t deviceInstance, const uint32_t notificationClassInstance, const uint8_t validDays, 
    const uint8_t fromTimeHour, const uint8_t fromTimeMinute, const uint8_t fromTimeSecond, const uint8_t fromTimeHundrethSecond, 
    const uint8_t toTimeHour, const uint8_t toTimeMinute, const uint8_t toTimeSecond, const uint8_t toTimeHundrethSecond, 
    const uint32_t processIdentifier, const bool issueConfirmedNotifications, 
    const bool transitionToOffNormal, const bool transitionToFault, const bool transitionToNormal, 
    const bool useRecipientDeviceChoice, const uint32_t recipientDeviceInstance, 
    const bool useRecipientAddressChoice, const uint16_t recipientNetworkNumber, const uint8_t* recipientMacAddress, const uint32_t recipientMacAddressLength);
```

- **deviceInstance** [in] - The instance number of the device that owns the Notification Class Object.
- **notificationClassInstance** [in] - The instance number of the Notification Class Object to add this recipient to.
- **validDays** [in] - A byte that represents each of the valid days that this recipient can receive notifications. Bit 0 = Monday, Bit 7 = Sunday. For all days set to 127 or 0x7F.
- **fromTimeHour** [in] - The hour value of the start time that this recipient can receive notifications. Default: 0
- **fromTimeMinute** [in] - The minute value of the start time that this recipient can receive notifications. Default: 0
- **fromTimeSecond** [in] - The second value of the start time that this recipient can receive notifications. Default: 0
- **fromTimeHundrethSecond** [in] - The hundreth-second value of the start time that this recipient can receive notifications. Default: 0
- **toTimeHour** [in] - The hour value of the end time that this recipient can receive notifications. Default: 23
- **toTimeMinute** [in] - The minute value of the end time that this recipient can receive notifications. Default: 59
- **toTimeSecond** [in] - The second value of the end time that this recipient can receive notifications. Default: 59
- **toTimeHundrethSecond** [in] - The hundreth second value of the end time that this recipient can receive notifications. Default: 99
- **processIdentifier** [in] - The handle of a process within the recipient device that is to receive the event notifications.
- **issueConfirmedNotifications** [in] - Whether to send confirmed notifications (true) or unconfirmed notifications (false).
- **transitionToOffNormal** [in] - Whether this recipient should be sent To_OffNormal events (true) or not (false).
- **transitionToFault** [in] - Whether this recipient should be sent To_Fault events (true) or not (false).
- **transitionToNormal** [in] - Whether this recipient should be sent To_Normal events (true) or not (false).
- **useRecipientDeviceChoice** [in] - Whether this recipient is a Device Identifier (true) or not (false). Only useRecipientDeviceChoice or useRecipientAddressChoice may be true, not both. Currently disabled.
- **recipientDeviceInstance** [in] - If the recipient is a Device Identifier, this is the device instance of the recipient. Currently disabled.
- **useRecipientAddressChoice** [in] - Whether this recipient is an Address (true) or not (false). Only useRecipientDeviceChoice or useRecipientAddressChoice may be true, not both.
- **recipientNetworkNumber** [in] - If the recipient is an Address, this is the network number of the recipient.  0 = local network.
- **recipientMacAddress** [in] - The BACnet MAC Address of the recipient. If BACnet IP, this is the 6-octet byte representation of the IP Address and Port of the device.
- **recipientMacAddressLength** [in] - The number of bytes of the macAddress.  If BACnet IP, this should be 6.

### EnableAlarmsAndEventsForObject

After at least one recipient has been added, it is time to enable alarms and events for each object that will support intrinsic alarming.
To start this process, call the EnableAlarmsAndEventsForObject function to setup some preliminary alarm and event settings.

The object and the notification class object must exist before calling this function, otherwise the function will return false.

```c++
bool BACnetStack_EnableAlarmsAndEventsForObject(
    const uint32_t deviceInstance, const uint16_t objectType, const uint32_t objectInstance, const uint32_t notificationClassInstance, 
    const uint8_t notifyType, const bool enableToOffNormalEvent, const bool enableToFaultEvent, const bool enableToNormalEvent, const bool enableEventDetection)
```

- **deviceInstance** [in] - The instance number of the device that owns this object.
- **objectType** [in] - The object type of the object for alarms and events.
- **objectType** [in] - The object instance of the object for alarms and events.
- **notificationClassInstance** [in] - The instance number of the notification class object that will handle the event notifications generated by this object.
- **notifyType** [in] - The type of notifications that this object generates. 0 = Alarm, 1 = Event.
- **enableToOffNormalEvent** [in] - Whether the To_OffNormal event is enabled (true) or not (false). Default: true.
- **enableToFaultEvent** [in] - Whether the To_Fault event is enabled (true) or not (false). Default: true.
- **enableToNormalEvent** [in] - Whether the To_Normal event is enabled (true) or not (false). Default: true.
- **enableEventDetection** [in] - Whether event detection is enabled (true) or not (false). Default: true

### Set up the intrinsic event and fault algorithms

After alarms and events have been enabled on the object, enable either the intrinsic event algorithm or the instric fault algorithm or both.
The type of event or fault algorithm to use intrinsically will be determined by the ObjectType.  
Since this example only uses Lifts and Escalators, then they will use the following algorithms:
- Event Algorithm:  ChangeOfState monitoring the PassengerAlarms property
- Fault Algorithm:  FaultsListed monitoring the FaultSignals property

For other intrinsic algorithm/object combinations, please refer to the CAS BACnet Stack - Alarms and Events pdf manual.

To enable these algorithm, use the functions described below:

#### SetIntrinsicChangeOfStateAlgorithmBool

To enable the intrinsic ChangeOfState event algorithm, use the following function.
Note: the object must exist first and must have called the EnableAlarmsAndEventsForObject function before calling this one.

```c++
bool BACnetStack_SetIntrinsicChangeOfStateAlgorithmBool(
    const uint32_t deviceInstance, const uint16_t objectType, const uint32_t objectInstance, const bool alarmValue, 
    const uint32_t timeDelay, const bool useTimeDelayNormal, const uint32_t timeDelayNormal, const bool enable)
```

- **deviceInstance** [in] - The instance number of the device that owns this object.
- **objectType** [in] - The object type of the object for alarms and events.
- **objectType** [in] - The object instance of the object for alarms and events.
- **alarmValue** [in] - What the alarm value is
- **timeDelay** [in] - The time in seconds that the offnormal conditions must exist before an offnormal event state is indicated.
- **useTimeDelayNormal** [in] - Whether to use a different time delay value for the timeDelayNormal, or just use the timeDelay value.
- **timeDelayNormal** [in] - The time in seconds that the Normal conditions must exist before a normal event state is indicated.
- **enable** [in] - Whether to enable (true) or disable (false) this algorithm. Default: true

#### SetFaultOutOfRangeAlgorithmReal

To enable the intrinsic FaultsListed fault algorithm, use the following function.
Note: the object must exist first and must have called the EnableAlarmsAndEventsForObject function before calling this one.

```c++
bool BACnetStack_SetFaultOutOfRangeAlgorithmReal(
    const uint32_t deviceInstance, const uint16_t objectType, const uint32_t objectInstance, const float faultLowLimit, const float faultHighLimit, const bool enable)
```

- **deviceInstance** [in] - The instance number of the device that owns this object.
- **objectType** [in] - The object type of the object for alarms and events.
- **objectType** [in] - The object instance of the object for alarms and events.
- **enable** [in] - Whether to enable (true) or disable (false) this algorithm. Default: true

### Alarms and Events Callbacks

Some alarm and event functionality requires callback functions.  
As of version 3.26.0 of the CAS BACnet Stack, the only Alarm and Event service that can use a callback is the following:
- AcknowledgeAlarm

#### CallbackAcknowledgeAlarm

The CallbackAcknowledgeAlarm function is a callback function that a user implement and registers with the CAS BACnet Stack to 
authorize alarm acknowledges and to log them.  This callback is called when the CAS BACnet Stack receives an AcknowledgeAlarm BACnet message.

The primary use of this callback is to validate acknowledgement source, to determine if it is authorized to acknowledge alarms.  Users
can also use this callback to log the acknowledges. 

```c++
bool(*FPCallbackAcknowledgeAlarm) (
    const uint32_t deviceInstance,
    const uint32_t acknowledgingProcessIdentifier,
    const uint16_t eventObjectType,
    const uint32_t eventObjectInstance,
    const uint16_t eventStateAcknowledged,
    const uint8_t eventTimeStampYear,
    const uint8_t eventTimeStampMonth,
    const uint8_t eventTimeStampDay,
    const uint8_t eventTimeStampWeekday,
    const uint8_t eventTimeStampHour,
    const uint8_t eventTimeStampMinute,
    const uint8_t eventTimeStampSecond,
    const uint8_t eventTimeStampHundrethSecond,
    const char* acknowledgementSource,
    const uint32_t acknowledgementSourceLength,
    const uint8_t acknowledgementSourceEncoding,
    const bool timeOfAcknowledgementIsTime,
    const bool timeOfAcknowledgementIsSequenceNumber,
    const bool timeOfAcknowledgementIsDateTime,
    const uint8_t timeOfAcknowledgementYear,
    const uint8_t timeOfAcknowledgementMonth,
    const uint8_t timeOfAcknowledgementDay,
    const uint8_t timeOfAcknowledgementWeekday,
    const uint8_t timeOfAcknowledgementHour,
    const uint8_t timeOfAcknowledgementMinute,
    const uint8_t timeOfAcknowledgementSecond,
    const uint8_t timeOfAcknowledgementHundrethSecond,
    const uint16_t timeOfAcknowledgementSequenceNumber,
    uint32_t * errorCode)
```

- **deviceInstance** [in] - The BACnet device instance receiving the AcknowledgeAlarm request.
- **acknowledgingProcessIdentifier** [in] - The acknowledging process.
- **eventObjectType** [in] - The type of the object that created the event notification being acknowledged.
- **eventObjectInstance** [in] - The instance of the object that created the event notification being acknowledged.
- **eventStateAcknowledged** [in] - The event state of the event notification being acknowledged.
- **eventTimeStampYear** [in] - The year of the timestamp of the event notification being acknowledged.
- **eventTimeStampMonth** [in] - The month of the timestamp of the event notification being acknowledged.
- **eventTimeStampDay** [in] - The day of the timestamp of the event notification being acknowledged.
- **eventTimeStampWeekday** [in] - The weekday of the timestamp of the event notification being acknowledged.
- **eventTimeStampHour** [in] - The hour of the timestamp of the event notification being acknowledged.
- **eventTimeStampMinute** [in] - The minute of the timestamp of the event notification being acknowledged.
- **eventTimeStampSecond** [in] - The second of the timestamp of the event notification being acknowledged.
- **eventTimeStampHundrethSecond** [in] - The hundtreth second of the timestamp of the event notification being acknowledged.
- **acknowledgementSource** [in] - The identity of the operator or process that is acknowledging the event.
- **acknowledgementSourceLength** [in] - The length of the acknowledgement source.
- **acknowledgementSourceEncoding** [in] - The encoding of the acknowledgment source.
- **timeOfAcknowledgementIsTime** [in] - A flag that specifies that the user is acknowledging the event using a Time for the Time of Acknowledgement. Uses the timeOfAcknowledgementHour, timeOfAcknowledgementMinute, timeOfAcknowledgementSecond, timeOfAcknowledgementHundrethSecond parameters. All others will be ignored.
- **timeOfAcknowledgementIsSequenceNumber** [in] - A flag that specifies that the user is acknowledging the event using a SequenceNumber for the Time of Acknowledgement. Uses the timeOfAcknowledgementSequenceNumber parameter. All others will be ignored.
- **timeOfAcknowledgementIsDateTime** [in] - A flag that specifies that the user is acknowledging the event using a Time for the Time of Acknowledgement. Uses the timeOfAcknowledgementYear, timeOfAcknowledgementMonth, timeOfAcknowledgementDay, timeOfAcknowledgementHour, timeOfAcknowledgementWeekday, timeOfAcknowledgementMinute, timeOfAcknowledgementSecond, timeOfAcknowledgementHundrethSecond parameters. All others will be ignored.
- **timeOfAcknowledgementYear** [in] - The year of the Time of Acknowledgement if it is a DateTime.
- **timeOfAcknowledgementMonth** [in] - The month of the Time of Acknowledgement if it is a DateTime.
- **timeOfAcknowledgementDay** [in] - The day of the Time of Acknowledgement if it is a DateTime.
- **timeOfAcknowledgementWeekday** [in] - The weekday of the Time of Acknowledgement if it is a DateTime.
- **timeOfAcknowledgementHour** [in] - The hour of the Time of Acknowledgement if it is a DateTime or Time.
- **timeOfAcknowledgementMinute** [in] - The minute of the Time of Acknowledgement if it is a DateTime or Time.
- **timeOfAcknowledgementSecond** [in] - The second of the Time of Acknowledgement if it is a DateTime or Time.
- **timeOfAcknowledgementHundrethSecond** [in] - The hundrethSecond of the Time of Acknowledgement if it is a DateTime or Time.
- **timeOfAcknowledgementSequenceNumber** [in] - The sequence number of the Time of Acknowledgement if it is a SequenceNumber.
- **errorCode** [out] - The error that a user sets in the callback if the AcknowledgeAlarm fails.  Usually will be SERVICE_REQUEST_DENIED (29)

#### RegisterCallbackAcknowledgeAlarm

To register the CallbackAcknowledgeAlarm, use the following function:

```c++
void BACnetStack_RegisterCallbackAcknowledgeAlarm(bool(*FPCallbackAcknowledgeAlarm))
```

- **FPCallbackAcknowledgeAlarm** [in] - The function pointer for the CallbackAcknowledgeAlarm function.  See #CASBACnetStack::FPCallbackAcknowledgeAlarm for more info.

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

To build this project, copy the CAS BACnet Stack DLL/SO file into the project output directory. Then using the following command to build run with donet

```bash

cd BACnetElevatorExample
dotnet publish -c Release
cd ../bin/netcoreapp2.1/publish/
--- Copy libCASBACnetStack_x64_Release.so into the /bin/netcoreapp2.1/publish/ folder ---
dotnet BACnetElevatorExample.dll

```

### Windows

A [Visual studios 2019](https://visualstudio.microsoft.com/vs/) project is included with this project.
