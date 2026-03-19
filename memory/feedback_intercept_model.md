---
name: CPU Intercept Handler Model
description: The correct pattern for CPU intercept handlers in this codebase
type: feedback
---

Intercept handlers are `Func<ICpu, IBus, bool>`, NOT `Action<ICpu, IBus>`.

**Why:** The old `Action` model required every handler to explicitly call `((Cpu6502Core)cpu).RTS(0, 0)`. Forgetting it left PC at the intercept address, causing the CPU to execute the ROM byte there (usually $00 = BRK) and hang. The new model has the CPU automatically perform RTS when the handler returns `true`.

**How to apply:**
- All intercept handlers return `bool`
- Return `true` to signal "do RTS and return to caller" (normal case)
- Return `false` to fall through to the ROM instruction (unusual — only if the firmware genuinely needs to continue executing ROM code)
- Never call `((Cpu6502Core)cpu).RTS(0, 0)` in a handler
- Error paths that set carry=true must still return `true` (they still need to RTS back to the caller)
