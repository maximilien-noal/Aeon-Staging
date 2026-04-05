@ui
Feature: Main Window Structure
    The main window of the Aeon DOS emulator should have a proper layout
    including menus, toolbar, and emulator display area.

    Background:
        Given the main window is open

    Scenario: Window opens with correct default size
        Then the window width should be 800
        And the window height should be 600

    Scenario: Window has minimum size constraints
        Then the window minimum width should be 360
        And the window minimum height should be 270

    Scenario: Menu bar contains all top-level menus
        Then the main menu should have 4 items
        And the main menu should contain a "_Aeon" menu
        And the main menu should contain a "_Edit" menu
        And the main menu should contain a "_View" menu
        And the main menu should contain a "_Debug" menu

    Scenario: Aeon menu has expected items
        When I open the "_Aeon" menu
        Then the menu should contain the following items
            | Header                        |
            | _Quick Launch Program...      |
            | _Quick Launch Command Prompt... |
            | _Pause                        |
            | R_esume                       |
            | E_xit                         |

    Scenario: Edit menu has Copy Screen item
        When I open the "_Edit" menu
        Then the menu should contain the following items
            | Header       |
            | _Copy Screen |

    Scenario: View menu has expected items
        When I open the "_View" menu
        Then the menu should contain the following items
            | Header               |
            | _Toolbar             |
            | _Lock Aspect Ratio   |
            | _Full Screen         |
            | _Performance Window  |

    Scenario: View menu Toolbar item is checked by default
        When I open the "_View" menu
        Then the "_Toolbar" menu item should be checked

    Scenario: Debug menu has Color Palette item
        When I open the "_Debug" menu
        Then the menu should contain the following items
            | Header        |
            | Color Palette |

    Scenario: Toolbar is visible by default
        Then the toolbar should be visible

    Scenario: Toolbar contains expected controls
        Then the toolbar should contain a "mouseIntegrationButton" toggle button
        And the toolbar should contain a "slowerButton" button
        And the toolbar should contain a "fasterButton" button
        And the toolbar should contain a "speedLabel" text block

    Scenario: Speed label shows default speed
        Then the speed label should display "20MHz"

    Scenario: Emulator display control exists
        Then the "emulatorDisplay" control should exist

    Scenario: Menu container is visible by default
        Then the menu container should be visible
