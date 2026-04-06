@ui
Feature: Configuration
    The Aeon emulator loads configuration from JSON files to control
    startup behavior, drive mappings, speed, mouse mode, and display title.

    Scenario: Config with custom speed applies correctly
        Given a configuration file with the following JSON
            """
            {
                "speed": 15000000
            }
            """
        When the configuration is loaded
        Then the configuration EmulationSpeed should be 15000000

    Scenario: Config with mouse-absolute sets Absolute mode
        Given a configuration file with the following JSON
            """
            {
                "mouse-absolute": true
            }
            """
        When the configuration is loaded
        Then the configuration IsMouseAbsolute should be true

    Scenario: Config with mouse-absolute false sets Relative mode
        Given a configuration file with the following JSON
            """
            {
                "mouse-absolute": false
            }
            """
        When the configuration is loaded
        Then the configuration IsMouseAbsolute should be false

    Scenario: Config with title changes window title
        Given a configuration file with the following JSON
            """
            {
                "title": "My DOS Game"
            }
            """
        When the configuration is loaded
        Then the configuration Title should be "My DOS Game"

    Scenario: Config with drive mappings sets up drives
        Given a configuration file with the following JSON
            """
            {
                "drives": {
                    "C": { "type": "Fixed", "host-path": "/mnt/dos/games" },
                    "D": { "type": "Fixed", "host-path": "/mnt/dos/apps" }
                }
            }
            """
        When the configuration is loaded
        Then the configuration should have 2 drive mappings
        And the configuration should have a "C" drive
        And the configuration should have a "D" drive

    Scenario: Quick launch config sets up C drive
        Given a quick launch configuration for "/mnt/dos/games" launching "GAME.EXE"
        Then the configuration Launch should be "GAME.EXE"
        And the configuration should have a "C" drive

    Scenario: Config with physical memory size
        Given a configuration file with the following JSON
            """
            {
                "physical-memory": 16777216
            }
            """
        When the configuration is loaded
        Then the configuration PhysicalMemorySize should be 16777216

    Scenario: Config merges with global config using null coalescing
        Given a base configuration with speed 10000000
        And an overlay configuration with title "Merged Title"
        When the overlay is merged into the base configuration
        Then the merged configuration speed should be 10000000
        And the merged configuration title should be "Merged Title"

    Scenario: Config with startup path
        Given a configuration file with the following JSON
            """
            {
                "startup-path": "C:\\"
            }
            """
        When the configuration is loaded
        Then the configuration StartupPath should be "C:\\"

    Scenario: Config with MIDI engine
        Given a configuration file with the following JSON
            """
            {
                "midi-engine": "MeltySynth"
            }
            """
        When the configuration is loaded
        Then the configuration MidiEngine should be "MeltySynth"
