@ui
Feature: Mouse Integration
    The mouse integration toggle button switches between Relative and Absolute
    mouse input modes on the emulator display.

    Background:
        Given the main window is open

    Scenario: Default mouse mode is Relative
        Then the mouse input mode should be "Relative"

    Scenario: Mouse integration button is unchecked by default
        Then the mouse integration button should not be checked

    Scenario: Toggling mouse integration enables Absolute mode
        When I click the mouse integration button
        Then the mouse input mode should be "Absolute"
        And the mouse integration button should be checked

    Scenario: Toggling mouse integration twice returns to Relative mode
        When I click the mouse integration button
        And I click the mouse integration button
        Then the mouse input mode should be "Relative"
        And the mouse integration button should not be checked
