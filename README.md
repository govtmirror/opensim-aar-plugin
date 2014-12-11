aarModule
=========

After Action Review (AAR) module for OpenSimulator

This module requires the NPC module to be enabled in order to be able to play back captured events.

This module can capture all public avatar actions in a region.  By public, I mean local chat, broadcast animations, and interactions with the region.  IM's and private actions are not captured.  The recorded scene can be persisted into an invisible prim in the world, and played back at a later time.  As the recording is persisted in the world its-self, it should be portable with the region through OAR files.

## Configuration

As stated above, this module requires the NPC module in order to function correctly.  The prebuild.xml is also missing an assembly reference or two, but this compiles against Opensim master as of Dec 11, 2014 once those are added.

In OpenSim.ini, enable the aarModule by inserting

```
[AARModule]
        Enabled = true
```

## Commands

The module introduces several commands to the region console:

Command | Description
------------|-------------
aar record | Begin capturing a new event to memory.  Any event in memory not saved is discarded
aar stop | Stop capturing the event to memory
aar save | Persist the recorded event from memory into the aar storage prim
aar list | List recorded sessions, sessions are identified by the date-time string of when the recording was initiated
aar load <identifier> | Halt normal script functions in a region, load the specified session, and initialise for playback
aar play | Begin iterating through the currently loaded session.
aar pause | Pause the playback of the session.
aar unload | Halt session playback and resume normal region functionality
aar purge | Delete all stored artifacts relating to persisted sessions fromt he aar storage prim

At this time, AARModule does not contain controls for rewinding, replaying, or stepping through the session playback.  Session playback continues at real time until the end pf the captured session is reached.  This module does not have a function to selectively delete a single session.


