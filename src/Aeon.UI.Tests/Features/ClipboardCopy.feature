@ui
Feature: Clipboard Copy
    The Edit > Copy Screen command copies the current emulator display
    bitmap to the clipboard as a PNG image. The exported image must
    faithfully represent the framebuffer pixel data.

    Scenario: Copy screen when no program is loaded does not crash
        Given the main window is open
        And no program is loaded in the emulator
        When I invoke the Copy Screen command

    Scenario: Export display bitmap produces valid PNG bytes
        Given the main window is open
        And the display has a 320x200 test pattern rendered
        When I export the display bitmap as PNG bytes
        Then the PNG bytes should not be empty
        And the PNG should decode to a 320x200 image

    Scenario: Exported bitmap pixels match the rendered framebuffer
        Given the main window is open
        And the display has a 320x200 test pattern rendered
        When I export the display bitmap as PNG bytes
        And I decode the PNG to pixel data
        Then pixel 0,0 should have BGRA value 0x80,0x00,0x00,0xFF
        And pixel 1,0 should have BGRA value 0x00,0x80,0x00,0xFF
        And pixel 2,0 should have BGRA value 0x00,0x00,0x80,0xFF
        And not all pixels should be zero
