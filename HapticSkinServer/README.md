# HapticSkinServer

This folder contains the source code necessary to implement a server application receiving as input the motor intensities to represent the touch captured by SkinAnalyser.
This data is received over the network (TCP connection via port 51470).

The C# script `HapticSkinServer.cs` provides an implementation for this connection.

All that remains is to transmit intensities to the hardware that controls the motors.

Two examples are proposed: 
* `BHapticsSkinServer` to control hardware provided by BHaptics. The *bHapticsPlayer* software is required
* `HapticVestSkinServer` to control the vest prototyped by the UT

## Build and run (mono)

Build :

```console
csc HapticSkinServer.cs
```

Run :

```console
mono HapticSkinServer.exe
```

This will work with `HapticSkinServer.cs` and `HapticVestSkinServer` but not with `BHapticsSkinServer` which is a Visual Studio solution.
