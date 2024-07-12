# Change Log

## Version 0.0.x

### 0.0.8 (2024-Jul-12)

- Updated to CAS BACnet Stack Version 4.3.8.x, 
- Updated to BACnet Stack Adapter version: 0.0.18

### 0.0.7 (2022-Aug-26)

- Added SubscribeCOVPropertyMultiple
- Added SetCOVSettings

### 0.0.6 (2021-Dec-22)

- Updated to CAS BACnet Stack Version 3.26.0
- Enabled Alarms and Events for all the lift objects
- Set the lift object to use the following alarm and event intrinsic algorithms:
  - ChangeOfState for the PassengerAlarm property
  - FaultsListed for the FaultSignals property
- Enabled the AcknowledgeAlarm and GetEventInformation services

### 0.0.5 (2020-Sep-28)

- Updated to CAS BACnet Stack Version 3.19.0.x

### 0.0.4 (2020-Jan-17)

- Added COV property

### 0.0.3 (2019-Sep-04)

- Added more optional lift properties
- Added example of subscribe cov property and fault signals

### 0.0.2 (2019-Jul-04)

- Updated example to allow for writing to MakingCarCalls property of Lift
- Added Landing_Calls and Landing_Call_Control to the ElevatorGroup object
- Updated example to use SetPropertyWriteableByGroup

### 0.0.1 (2019-Jun-04)

- Inital release