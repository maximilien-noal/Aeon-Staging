@ui
Feature: Palette Dialog
    The Color Palette dialog displays the current VGA palette as a
    16x16 grid of 256 color swatches for debugging purposes.

    Background:
        Given the palette dialog is open

    Scenario: Palette dialog opens correctly
        Then the palette dialog should be visible

    Scenario: Palette dialog title is "Color Palette"
        Then the palette dialog title should be "Color Palette"

    Scenario: Palette dialog contains a 16x16 uniform grid
        Then the palette grid should have 16 rows
        And the palette grid should have 16 columns

    Scenario: Palette dialog contains 256 color rectangles
        Then the palette grid should contain 256 color rectangles

    Scenario: Palette dialog has correct window size
        Then the palette dialog width should be 300
        And the palette dialog height should be 300
