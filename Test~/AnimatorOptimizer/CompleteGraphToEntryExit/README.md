# CompleteGraphToEntryExit Tests

This directory contains tests for the `CompleteGraphToEntryExit` animator optimizer pass.

## Test Structure

- `CompleteGraphToEntryExitTest.cs` - NUnit test class with test methods
- `GenerateTestControllers.cs` - Editor script to generate test animator controllers
- `*.controller` - Unity Animator Controller assets (input and expected output)

## Generating Test Assets

The test animator controllers need to be generated using Unity Editor:

1. Open a Unity project with AvatarOptimizer installed as a UPM package
2. Copy the Test~ directory into your project's Assets folder temporarily
3. In Unity Editor menu, select: **Avatar Optimizer > Tests > Generate CompleteGraphToEntryExit Test Controllers**
4. The script will generate all required `.controller` files in `Assets/Test~/AnimatorOptimizer/CompleteGraphToEntryExit/`
5. Copy the generated `.controller` and `.controller.meta` files back to the repository's `Test~/AnimatorOptimizer/CompleteGraphToEntryExit/` directory
6. Remove the Test~ directory from your Assets folder

## Test Cases

### Convertible Cases

1. **SimpleCompleteGraph** - Basic 2-state complete graph
   - Tests the fundamental conversion from complete graph to entry-exit pattern
   - All states connected to all other states (including self)

2. **CompleteGraphWithSelfTransitions** - Complete graph with explicit self-transitions
   - Tests preservation of self-transitions during conversion

3. **CompleteGraphWithDifferentConditions** - 3-state complete graph
   - Each target state has different entry conditions
   - Tests handling of multiple different conditions

### Non-Convertible Cases

4. **NonConvertibleIncompleteGraph** - Graph missing some transitions
   - Should NOT be converted (graph is not complete)

5. **NonConvertibleDifferentTransitionSettings** - Transitions with different durations
   - Should NOT be converted (violates same-settings requirement)

6. **NonConvertibleHasStateMachine** - Layer with child state machine
   - Should NOT be converted (child state machines not supported)

7. **NonConvertibleDifferentConditionsForSameTarget** - Same target, different conditions
   - Should NOT be converted (violates same-conditions-per-target requirement)

## Implementation Notes

The `CompleteGraphToEntryExit` optimizer converts complete graph state machines to entry-exit pattern:
- **Complete graph**: Every state has transitions to all other states (including self)
- **Entry-exit pattern**: Entry transitions based on conditions, exit transitions with combined conditions

### Conversion Requirements

For a layer to be convertible:
- No child state machines
- No synced layers
- States form a complete graph (all-to-all connectivity)
- All transitions from same source have same settings (duration, exitTime, etc.)
- All transitions to same target have identical conditions

### Conversion Process

1. Create entry transition for each state with its target conditions
2. Create exit transitions from each state with all possible target conditions
3. Preserve self-transitions (they remain as state transitions)
