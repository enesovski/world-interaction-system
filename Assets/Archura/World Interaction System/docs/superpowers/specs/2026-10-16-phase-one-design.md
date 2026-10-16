# World Interaction System — Phase 1

Implement the Phase 1 architecture approved in chat: a minimal surface interaction core and a working mouse demo in Unity 6000.3.11f1 with URP.

## Responsibilities

- `InteractionSample`: immutable world-space position, velocity, radius in metres, and normalized strength.
- `InteractionChannel`: an asset identifying a group of interested responders.
- `InteractionManager`: registration, queued delivery, and one simulation step per active responder per frame.
- `IInteractionResponder`: `Receive(in InteractionSample)` accumulates inputs; `Simulate(float deltaTime)` advances state.
- `MouseInteractionSource`: one impulse per left-button press on a collider, using the existing Input System support.
- `DebugResponder`: a bounded set of temporary ground-plane rings visible in Game view, with optional batch logging.

## Frame and lifecycle contract

Sources publish before the manager's LateUpdate. The manager swaps queues, delivers the batch by channel, then simulates responders once. Callback-generated samples wait until the next frame. Responder order is unspecified. Duplicate registration is harmless. Unregistering the final channel stops simulation. Disabling the manager clears queued inputs and submissions while disabled are discarded. Disabled debug responders unregister and clear their markers.

Exceptions in a responder are logged without preventing unrelated responders or subsequent frames from running. The manager remains a scene component; there is no singleton or service framework.

## Demo and validation

Wire `Samples/SampleScene.unity` with one view camera, a ground collider, a manager, a mouse source, and two debug responders sharing a channel. Use separate ring colours and radii so delivery to both responders is visible. Include an unrelated channel in automated tests to establish isolation.

Use Unity tests for queued delivery, channel isolation, duplicate registration, multi-channel lifecycle, callback publication, disabling, and exception isolation. Verify real mouse input in Play mode and inspect the rendered result and Console.

## Limits

Click impulses only. Surface simulation, continuous influence semantics, snow/water shaders, spatial indexing, and GPU buffers are later phases. Debug visualization is deliberately bounded and reuses its renderers.
