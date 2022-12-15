# OCB Electricity Wire Colors Mod - 7 Days to Die (A20) Addon

This Mod changes the wire pulse color according to the power state.
It makes it easier to see which part of your cable-mess is missing
a proper connection, or to easier see which items a trigger enables.

<img src="Screens/game-wire-colors.jpg" alt="Wire Colors shown in-game" height="320"/>

This module was broken out of my [Electricity Overhaul Mod][1].  
You can use it standalone or alongside [Electricity Overhaul Mod][1].

[![GitHub CI Compile Status][4]][3]

### Download and Install

Simply [download here from GitHub][2] and put into your A20 Mods folder:

- https://github.com/OCB7D2D/ElectricityOverhaul/archive/master.zip (master branch)

## Changelog

## Version 1.0.2

- Fix potential null pointer access in `UpdateWireColor`

## Version 1.0.1

- Fix issue when map is loading with wire tool equipped
- Disable optimization since it seems to fail sometimes

## Version 1.0.0

- Refactor code to be much cleaner and more robust

## Version 0.9.0

- Remove BepInEx requirement (refactored completely)

## Version 0.8.0

- Refactor for A20 compatibility

## Compatibility

I've developed and tested this Mod against version a20.b238

[1]: https://github.com/OCB7D2D/ElectricityOverhaul
[2]: https://github.com/OCB7D2D/ElectricityOverhaul/releases
[3]: https://github.com/OCB7D2D/ElectricityOverhaul/actions/workflows/ci.yml
[4]: https://github.com/OCB7D2D/ElectricityOverhaul/actions/workflows/ci.yml/badge.svg
