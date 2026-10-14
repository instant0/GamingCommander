## Status

**EOSH launcher reverse-engineering (DISCOVERY) — in progress (2026-09-10).**

Reconcile keep/remove gate model **corrected** in `planning/126-facts-eosh-re.md`:
- `vtable[0xd0]` = log-only (not a gate); 7-field hash lookup = duplicate detector (both paths REMOVE).
- Real keep gates: `[obj+0xec]` skip flag + `vtable[0x48]` on global singleton `0x14316fed0`.
- Reconcile invoked via delegate/event (no direct call sites).
- EOSH `get-installed-apps-response` schema strings mapped.

## Next (RE)

1. **Identify class + contents of global `0x14316fed0`** — find which object/manager creates/populates it; xref-scan write sites and the ensure-call at `0x1413e5381`; determine whether `vtable[0x48]`'s set is server-fed (expected) or local. This settles TR/DS-kept vs orphans-removed definitively.
2. Map phase-1 → 0x68-stride hash table relationship in reconcile.
3. Trace `0x141dcab60` / `0x141dadff0` (local `.item` enumeration → 5-FString records).
4. Identify phase-2 local-install class (vtable 0xc0/0xd0).
5. Confirm `vtable[0x48]` semantics.

## Standing (app code)

Plan `125-stabilization-refactor.md` COMPLETE (2026-09-09). Next app-code plan: `planning/123-detection-bugfixes.md` (PLANNED) — not started; read before implementing. Standing rules unchanged: one file at a time; new files before removal; behavior-preserving; Windows paths opaque; full build + suite after each step.