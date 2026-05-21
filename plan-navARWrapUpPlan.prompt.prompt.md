## Plan: Navigation-First Decoupling

Realign the refactor to the original architecture order: extract navigation orchestration first, then split presenters, then remove implicit wiring, introduce a presentation state machine, normalize async choreography, and finally clean up QR test behavior. The main constraint is that the app must remain functional after each phase, so every phase ends with a compile check plus a narrow editor smoke test before the next phase starts. `NavigationSceneContext` should become the stable scene-level dependency boundary used by the new coordination layer.

**Steps**
1. Extract navigation orchestration from `UIManager` into a dedicated coordinator slice first. Keep the first cut narrow: move path calculation coordination, entrance selection, floor-transition prompting, and transition resumption into `INavigationCoordinator` or an equivalent coordination service. Keep `UIManager` as the temporary entry point that subscribes to coordinator events so the app still works while logic is being lifted out. This phase should reduce direct calculator creation and stop `UIManager` from owning navigation state.
2. Preserve `NavigationSceneContext` as the single scene wiring surface for navigation services. The coordinator should resolve alignment, path calculator, and path renderer through this context instead of scattered `FindObjectOfType` calls. If a dependency is missing, fail loudly and clearly so debugging stays simple in a fragile project, but do not add extra fallback discovery paths unless they are already required for current behavior. This step depends on phase 1.
3. Split screen responsibilities into presenters after the navigation slice is stable. Create or extend `IScreenPresenter`-style presenters for the UI screens that still live in `UIManager`, starting with the least risky views and reusing `ScreenBinders` only as a temporary bridge. Each presenter should own its own `VisualTreeAsset` references, element queries, and click wiring. `UIManager` should shrink to a router that selects the active presenter based on `AppState` and forwards a small set of callbacks.
4. Keep `QrScannerPresenter` as the model for the presenter split and align the QR flow to it. The QR presenter should remain responsible for showing and hiding the scan view and triggering the next state transition after a scan. Only remove QR handling from `UIManager` once that screen behaves independently in the editor. This step depends on phase 3 so the screen routing contract is stable.
5. Replace service-locator lookups with explicit composition from the bootstrapper. Introduce a small `ServiceContainer` or an equivalent explicit wiring approach so `AppBootstrapper` composes dependencies once and passes them into the router and coordinators. Remove redundant runtime discovery from `UIManager` and other presentation-facing classes in controlled slices so the wiring becomes deterministic and easier to debug. This phase should preserve `NavigationSceneContext` as the handoff point for scene-scoped services.
6. Introduce a presentation state machine after the presenters are separated. Add a `ScreenStateMachine` that maps `AppState` to `IScreenPresenter` instances and centralizes transition rules that are still embedded in `UIManager`. This makes the flow data-driven and reduces the chance that a new screen accidentally bypasses the router logic. This step depends on phase 3.
7. Normalize coroutine and async choreography once navigation and presentation ownership are separated. Move the remaining coroutine-heavy sequencing into a `NavigationSequencer` or equivalent, with explicit start, stop, and cancel entry points. Keep the sequencing isolated so state transitions, drawing, and floor loading can be validated independently instead of being interleaved across `UIManager`. This step depends on phases 1, 3, and 6.
8. Fix QR test data handling last, after the QR flow is isolated. Replace the hardcoded scanner payload in `ZxingWebCamScanner.cs` with a build- or config-driven switch so production scanning uses real payloads while editor testing still has a controlled fallback. Keep the diagnostics useful, but make sure the production path cannot silently bypass the actual barcode result. This step depends on phase 4.

**Relevant files**
- `c:/Users/selam/Desktop/school/Final Year Project/NavAR_APP/Assets/Scripts/Presentation/UIManager.cs` — current ownership boundary to shrink in phases rather than all at once.
- `c:/Users/selam/Desktop/school/Final Year Project/NavAR_APP/Assets/Scripts/Infrastructure/Navigation/NavigationSceneContext.cs` — scene-level wiring surface for alignment, routing, and rendering dependencies.
- `c:/Users/selam/Desktop/school/Final Year Project/NavAR_APP/Assets/Scripts/Presentation/QrScannerPresenter.cs` — existing presenter that should guide the QR-side split.
- `c:/Users/selam/Desktop/school/Final Year Project/NavAR_APP/Assets/Scripts/Presentation/Controllers/ScreenBinders.cs` — temporary adapter layer to reuse until each presenter owns its own UI binding.
- `c:/Users/selam/Desktop/school/Final Year Project/NavAR_APP/Assets/Scripts/Presentation/Controllers/NavigationBarController.cs` — keep the navigation chrome separate from the screen ownership split.
- `c:/Users/selam/Desktop/school/Final Year Project/NavAR_APP/Assets/Scripts/Bootstrapper/AppBootstrapper.cs` — composition root to simplify once the wiring boundaries are explicit.
- `c:/Users/selam/Desktop/school/Final Year Project/NavAR_APP/Assets/Scripts/Infrastructure/ZxingWebCamScanner.cs` — target for removing hardcoded test payload behavior.
- `c:/Users/selam/Desktop/school/Final Year Project/NavAR_APP/Assets/Scripts/Core/State/AppStateManager.cs` — central state contract that the new router and state machine must preserve.

**Verification**
1. After each phase, run the Unity C# diagnostics check on the touched scripts and stop immediately if any compile or type issue appears.
2. After phase 1, smoke-test the end-to-end navigation path in the editor and confirm the app still calculates a path and transitions without `UIManager` creating the navigation services itself.
3. After phase 3, verify each screen still loads and navigates correctly through the presenter/router split without missing UI references.
4. After phase 4, confirm QR scanning still starts and stops correctly and the app transitions to the intended next state after a scan.
5. After phase 5, restart the relevant scene and verify the bootstrapper wires dependencies once without relying on repeated runtime discovery.
6. After phase 7 and phase 8, validate floor transition and QR behavior in the editor with the debug paths disabled or minimized.

**Decisions**
- Keep the original six-phase architecture as the planning contract.
- Favor explicit dependency composition and `NavigationSceneContext` over runtime service discovery.
- Keep `UIManager` functional during the transition, but reduce it phase by phase until it is mostly a router.
- Preserve current `AppState` semantics during the refactor so the UI flow does not need a simultaneous state-model rewrite.
- Exclude unrelated data-model redesign, scene content changes, and feature expansion from this session.

**Further Considerations**
1. The safest implementation sequence is to make phase 1 a two-slice cut: coordinator extraction first, then route `UIManager` to it, with a validation check in between.
2. If you want the clearest debugging path, keep diagnostics on for the first phase only, then reduce noisy logs after the coordinator is stable.
3. If you want the least risky presenter split, start with the navigation-neutral screens first and leave QR and AR navigation until the routing layer is stable.
