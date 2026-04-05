@ui
Feature: Emulator Display
    The EmulatorDisplay control manages emulation state, mouse input mode,
    speed, aspect ratio, and scaling for the DOS emulator rendering surface.

    Background:
        Given a new EmulatorDisplay control is created

    Scenario: Default EmulatorState is NoProgram
        Then the EmulatorState should be "NoProgram"

    Scenario: Default MouseInputMode is Relative
        Then the MouseInputMode should be "Relative"

    Scenario: Default IsMouseCursorCaptured is false
        Then IsMouseCursorCaptured should be false

    Scenario: Default EmulationSpeed is 20 MHz
        Then the EmulationSpeed should be 20000000

    Scenario: Default IsAspectRatioLocked is true
        Then IsAspectRatioLocked should be true

    Scenario: Setting MouseInputMode to Absolute
        When I set the MouseInputMode to "Absolute"
        Then the MouseInputMode should be "Absolute"

    Scenario: Setting MouseInputMode back to Relative
        Given the MouseInputMode is set to "Absolute"
        When I set the MouseInputMode to "Relative"
        Then the MouseInputMode should be "Relative"

    Scenario: Setting EmulationSpeed updates the property
        When I set the EmulationSpeed to 15000000
        Then the EmulationSpeed should be 15000000

    Scenario: Setting IsAspectRatioLocked to false
        When I set IsAspectRatioLocked to false
        Then IsAspectRatioLocked should be false

    Scenario: Default ScalingAlgorithm is None
        Then the ScalingAlgorithm should be "None"
