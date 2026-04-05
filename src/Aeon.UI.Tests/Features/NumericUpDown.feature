@ui
Feature: Numeric Up Down Control
    The NumericUpDown custom control provides a spinner input with
    configurable minimum, maximum, step values, and value clamping.

    Background:
        Given a new NumericUpDown control is created

    Scenario: Default value is 0
        Then the NumericUpDown value should be 0

    Scenario: Default minimum value is 0
        Then the NumericUpDown minimum value should be 0

    Scenario: Default maximum value is 100
        Then the NumericUpDown maximum value should be 100

    Scenario: Default step value is 1
        Then the NumericUpDown step value should be 1

    Scenario: Default IsReadOnly is false
        Then the NumericUpDown IsReadOnly should be false

    Scenario: Up button increments value by step
        Given the NumericUpDown value is 10
        And the NumericUpDown step value is 5
        When I click the up button
        Then the NumericUpDown value should be 15

    Scenario: Down button decrements value by step
        Given the NumericUpDown value is 10
        And the NumericUpDown step value is 5
        When I click the down button
        Then the NumericUpDown value should be 5

    Scenario: Value does not exceed maximum
        Given the NumericUpDown value is 98
        And the NumericUpDown maximum value is 100
        And the NumericUpDown step value is 5
        When I click the up button
        Then the NumericUpDown value should be 100

    Scenario: Value does not go below minimum
        Given the NumericUpDown value is 2
        And the NumericUpDown minimum value is 0
        And the NumericUpDown step value is 5
        When I click the down button
        Then the NumericUpDown value should be 0

    Scenario: Setting value below minimum coerces to minimum
        Given the NumericUpDown minimum value is 10
        When I set the NumericUpDown value to 5
        Then the NumericUpDown value should be 10

    Scenario: Setting value above maximum coerces to maximum
        Given the NumericUpDown maximum value is 50
        When I set the NumericUpDown value to 75
        Then the NumericUpDown value should be 50

    Scenario: Custom step value applies to increment
        Given the NumericUpDown value is 0
        And the NumericUpDown step value is 10
        When I click the up button
        Then the NumericUpDown value should be 10

    Scenario: Custom step value applies to decrement
        Given the NumericUpDown value is 30
        And the NumericUpDown step value is 10
        When I click the down button
        Then the NumericUpDown value should be 20
