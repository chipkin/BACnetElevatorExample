# Windows BACnet Elevator Example

In this CAS BACnet Stack example, we create a BACnet IP server with Elevator groups, Lifs, Escalator objects using C#. This project was designed as an example for someone that wants to implment Elevator groups, Lifs, Escalator objects in a BACnet IP server using C#.

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

## CAS BACnet Stack Elevator groups, Lifs, Escalator functions

These are the specific function for adding or manupliating the Elevator groups, Lifs, Escalator object types.

### AddElevatorGroupObject

Adds an Elevator Group Object. The device must be added first before this function can be called. The Elevator Group Object references various Lift or Escalator objects. If adding escalators or lifts to an Elevator Group Object, users must add the Elevator Group object first.

```c++
bool BACnetStack_AddElevatorGroupObject(const uint32_t deviceInstance, const uint32_t objectInstance, const uint32_t machineRoomId, const uint8_t groupId, const bool isGroupOfLifts, const bool supportLandingCallStatus)
```

- deviceInstance [IN] - The instance number of the device that owns this Elevator Group Object.
- objectInstance [IN] - The instance number of this Elevator Group Object.
- machineRoomId [IN] - The instance number of the Positive Integer Value Object whose present value property represents the Machine Room ID. If the Positive Integer Value Object does not exist, this function will create it.
- groupId [IN] - The group ID for this Elevator Group object.
- isGroupOfLifts [IN] - A flag that sets the Elevator Group object to be a group of Lift Objects (true) or Escalator Objects (false).
- supportLandingCallStatus [IN] - A flag to enable the Landing Calls and Landing Call Control properties.

return True if successful, false if failed.

### AddLiftOrEscalatorObject

Adds a Lift or Escalator Object. The device must be added first before this function can be called. If adding to an Elevator Group Object, the Elevator Group object must be created first.

```c++
bool BACnetStack_AddLiftOrEscalatorObject(const uint32_t deviceInstance, const uint16_t objectType, const uint32_t objectInstance, const uint32_t elevatorGroupInstance, const uint8_t groupId, const uint8_t installationId)
```

- deviceInstance [IN] - The instance number of the device that owns this Lift or Escalator Object.
- objectType [IN] - The object type of this object.  Must be either Lift or Escalator.
- objectInstance [IN] - The instance number of this Lift or Escalator Object.
- elevatorGroupInstance [IN] - The instance number of the Elevator Group Object that this Lift or Escalator Object belongs to. If none exists, use 4194303 to specify that this Lift or Escalator Object is not part of an Elevator Group Object.
- groupId [IN] - The groupId assigned to this Lift or Escalator Object.
- installationId [IN] - The installationId assigned to this Lift or Escalator Object.  If this Lift or Escalator Object belongs to an Elevator Group Object, it must have a unique installationId among all other Lift or Escalator Objects in the Elevator Group Object.

return True if successful, false if failed.

### SetLiftHigherLowerDeck

Sets the HigherDeck and LowerDeck properties of the Lift Object. If the HigherDeck and LowerDeck properties have not been enabled for this Lift Object, then this function will enable them. This function is used to specify a Lift Object as a Multi-Deck Lift. If the Lift Object is the highest deck, then set the higherObjectInstance to 4194303. If the Lift Object is the lowest deck, then set the lowerObjectInstance to 4194303.

```c++
bool BACnetStack_SetLiftHigherLowerDeck(const uint32_t deviceInstance, const uint32_t objectInstance, const uint32_t higherDeckObjectInstance, const uint32_t lowerDeckObjectInstance)
```

- deviceInstance [IN] - The instance of the device that contains the Lift object.
- objectInstance [IN] - The instance of the Lift object to update.
- higherDeckObjectInstance [IN] - The instance of a Lift object that is a higher deck to this Lift object.  If this Lift Object is the highest deck, then set this to 4194303.
- lowerDeckObjectInstance [IN] - The instance of a Lift object that is a lower deck to this Lift object.  If this Lift Object is the lowest deck, then set this to 4194303.

return True if successful, otherwise false.

### SetLiftOrEscalatorEnergyMeterRef

Sets the EnergyMeterRef property of a Lift or Escalator object. If the EnergyMeterRef property has not been enabled for this Lift or Escalator object, then this function will enable it.

If a Lift object already has the EnergyMeter property enabled and if the energyMeterRefObjectInstance is not set to 4194303, then the CAS BACnet Stack will use a value of 0.0 for the Energy Meter propery. Note: The above only applies to Lift objects, not Escalator objects

From the Spec: If the Energy_Meter_Ref property is present and initialized (contains an instance other than 4194303), then the Energy_Meter property, if present, shall contain a value of 0.0.

```c++
bool BACnetStack_SetLiftOrEscalatorEnergyMeterRef(const uint32_t deviceInstance, const uint16_t objectType, const uint32_t objectInstance, const bool energyMeterRefUseDevice, const uint32_t energyMeterRefDeviceInstance, const uint16_t energyMeterRefObjectType, const uint32_t energyMeterRefObjectInstance)
```

- deviceInstance [IN] - The instance of the device that contains the Lift object.
- objectType [IN] - The BACnet Object Type of this object.  Must be either Lift or Escalator.  See #CASBACnetStack::BACnetObjectType for more info.
- objectInstance [IN] - The instance of the Lift object to update.
- energyMeterRefUseDevice [IN] - Flag that specifies whether the energy meter reference has a device identifier (true) or not (false).
- energyMeterRefDeviceInstance [IN] - The instance of the Device that contains the object that is the Energy Meter Ref.
- energyMeterRefObjectType [IN] - The BACnet Object Type of the object that is the Energy Meter Ref.  #CASBACnetStack::BACnetObjectType for more info.
- energyMeterRefObjectInstance [IN] - The instance of the object that is the Energy Meter Ref.

## Building

A Visual studios 2019 project is included with this project.

This project also auto built using [Gitlab CI](https://docs.gitlab.com/ee/ci/) on every commit.
