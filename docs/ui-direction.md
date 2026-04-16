# UI Direction

## Design Intent

GamingCommander should feel like a Norton Commander-style application in workflow and visual language:

- dual-pane mental model,
- keyboard-first navigation,
- mouse selection and click interaction for UI elements,
- function-key driven actions,
- dense, information-rich presentation,
- retro text-mode inspired styling.

## Important Constraint

This is **not** intended to be a rigid DOS-resolution clone.

The UI must support modern resizable windows and adapt to available width and height similar to contemporary terminal applications such as Windows Terminal-hosted tools.

## Baseline Requirements

- Resize-aware shell layout
- Pane widths that can expand or rebalance with window size
- Mouse-aware controls for list selection, focusing, and future split-pane interactions
- Status and command bars that remain usable across sizes
- Scrollable lists and detail areas
- Graceful behavior at both compact and wide window sizes
- No assumption of 80-column layout limits

## Phase 0 Proof-of-Concept Expectations

The first UI spike should prove:

- the main shell can resize cleanly,
- pane layout updates correctly as the window changes,
- mouse selection works without replacing keyboard-first navigation,
- keyboard navigation remains stable during resize,
- the retro visual style can be applied without locking the layout to fixed dimensions.
