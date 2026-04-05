@ui
Feature: Performance Window
    The Performance Window displays real-time processor and memory metrics
    for the running emulator in expandable sections.

    Background:
        Given the performance window is open

    Scenario: Performance window opens correctly
        Then the performance window should be visible

    Scenario: Performance window contains processor expander
        Then the "processorExpander" expander should exist
        And the "processorExpander" expander should be expanded

    Scenario: Performance window contains memory expander
        Then the "memoryExpander" expander should exist
        And the "memoryExpander" expander should be expanded

    Scenario: Processor expander has instructions label
        Then the "instructionsLabel" text block should exist in the processor expander

    Scenario: Processor expander has IPS label
        Then the "ipsLabel" text block should exist in the processor expander

    Scenario: Memory expander has conventional memory label
        Then the "conventionalMemoryLabel" text block should exist in the memory expander

    Scenario: Memory expander has expanded memory label
        Then the "expandedMemoryLabel" text block should exist in the memory expander

    Scenario: Memory expander has extended memory label
        Then the "extendedMemoryLabel" text block should exist in the memory expander
