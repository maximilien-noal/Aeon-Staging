@ui
Feature: Full Screen
    The main window supports full screen mode toggled via the View menu
    or the Alt+Enter keyboard shortcut. Full screen hides the menu
    container and sets the window state to FullScreen.

    Background:
        Given the main window is open

    Scenario: Default window state is Normal
        Then the window state should be "Normal"

    Scenario: Entering full screen sets window state to FullScreen
        When I toggle full screen mode
        Then the window state should be "FullScreen"

    Scenario: Entering full screen hides the menu container
        When I toggle full screen mode
        Then the menu container should not be visible

    Scenario: Exiting full screen restores window state to Normal
        Given the window is in full screen mode
        When I toggle full screen mode
        Then the window state should be "Normal"

    Scenario: Exiting full screen restores menu container visibility
        Given the window is in full screen mode
        When I toggle full screen mode
        Then the menu container should be visible

    Scenario: Full screen sets background to black
        When I toggle full screen mode
        Then the window background should be black

    Scenario: Exiting full screen restores background
        Given the window is in full screen mode
        When I toggle full screen mode
        Then the window background should not be black
