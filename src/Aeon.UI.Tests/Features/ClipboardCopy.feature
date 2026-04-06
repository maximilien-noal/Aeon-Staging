@ui
Feature: Clipboard Copy
    The Edit > Copy Screen command copies the current emulator display
    bitmap to the clipboard as a PNG image.

    Scenario: Copy screen when no program is loaded does not crash
        Given the main window is open
        And no program is loaded in the emulator
        When I invoke the Copy Screen command
        Then no exception should be thrown

    Scenario: Copy screen produces expected bitmap after VGA rendering
        Given the main window is open
        And a VGA mode 13h pattern program has been loaded and executed
        When I invoke the Copy Screen command
        Then the copied bitmap should match the display bitmap exactly
