@ui
Feature: Speed Control
    The toolbar speed controls allow adjusting the emulation speed
    in increments of 100,000 Hz (0.1 MHz) between 1 MHz and the maximum.

    Background:
        Given the main window is open
        And the default emulation speed is 20000000

    Scenario: Default speed is 20 MHz
        Then the emulation speed should be 20000000
        And the speed label should display "20MHz"

    Scenario: Clicking faster increases speed by 100,000
        When I click the faster button
        Then the emulation speed should be 20100000
        And the speed label should display "20.1MHz"

    Scenario: Clicking slower decreases speed by 100,000
        When I click the slower button
        Then the emulation speed should be 19900000
        And the speed label should display "19.9MHz"

    Scenario: Multiple faster clicks accumulate
        When I click the faster button 5 times
        Then the emulation speed should be 20500000
        And the speed label should display "20.5MHz"

    Scenario: Multiple slower clicks accumulate
        When I click the slower button 10 times
        Then the emulation speed should be 19000000
        And the speed label should display "19MHz"

    Scenario: Slower button is disabled at minimum speed
        Given the emulation speed is set to 1000000
        Then the slower button should be disabled

    Scenario: Slower button does not go below minimum speed
        Given the emulation speed is set to 1000000
        When I click the slower button
        Then the emulation speed should be 1000000
        And the speed label should display "1MHz"

    Scenario: Faster button is enabled at minimum speed
        Given the emulation speed is set to 1000000
        Then the faster button should be enabled

    Scenario: Speed label formats whole MHz without decimal
        Given the emulation speed is set to 10000000
        Then the speed label should display "10MHz"

    Scenario: Speed label formats fractional MHz with one decimal
        Given the emulation speed is set to 12500000
        Then the speed label should display "12.5MHz"
