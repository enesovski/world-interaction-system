# Phase 1 — Mouse Interaction Demo

Open `SampleScene.unity`, enter Play mode, and left-click the ground in Game view. Each press sends one interaction to both debug responders: cyan and amber rings appear around the hit point and disappear after two seconds. Holding the button does not send additional impulses. Each responder reuses up to 16 markers.

The scene uses one normal view camera. There are no interaction capture cameras or simulation textures.

## Architecture

`MouseInteractionSource → InteractionManager → IInteractionResponder`

- The source raycasts against its surface layers and publishes an immutable world-space sample.
- An `InteractionChannel` asset identifies interested responders. Multiple effects can subscribe to the same asset.
- The manager queues samples and delivers them in `LateUpdate`, then calls `Simulate(deltaTime)` once per registered responder, including frames without input.
- `Receive` accepts input; `Simulate` advances the effect. Future water, snow, and grass responders own their simulation state and shader bindings.

Samples published during delivery or simulation wait for the next frame. Responder order is unspecified. Disabling the manager discards queued samples; publishing while disabled has no effect. Disabling a debug responder unregisters it and clears its visible markers. Exceptions in one responder are logged while unrelated responders continue.

Ground and water surface mapping belongs to future responders. This phase supplies routing and a visual test, with click impulses only.

## Adding a responder

Implement `IInteractionResponder`, register with the manager in `OnEnable`, and unregister in `OnDisable`. Keep effect-specific work in that responder. Use `Receive(in InteractionSample sample)` to accumulate input and `Simulate(float deltaTime)` to consume it and advance state.

For another source, construct an `InteractionSample` and call `manager.Publish(channel, in sample)` during `Update`, before the manager's frame step. Position and radius use world-space metres; strength is normalized to 0–1. The initial mouse impulse has zero velocity.

## Tests

Use Unity's Test Runner. EditMode tests cover routing, frame boundaries, registration, disable behavior, exception isolation, and marker reuse/expiry. PlayMode tests inject mouse input and check that clicks reach both responders while held input and missed raycasts do not create extra interactions.
