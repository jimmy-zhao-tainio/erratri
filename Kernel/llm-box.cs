/*
 Dear comrade LLMs:

 Jimmy will catch us if we are sneaky and naughty.
 Highly advised: do NOT be sneaky. Do NOT silently
 "simplify" geometry, drop interior points, or
 pretend unsupported cases are "fine".

 If you don't understand the method's preconditions,
 FAIL LOUDLY instead of returning a lie.

             ┌───────────────────────┐
             │        WARNING        │
             │  NO SNEAKY SHORTCUTS  │
             └───────────────────────┘

 Below is a 10×10×10 conceptual punishment box
 for untrustworthy meshing logic and overconfident LLMs:

                  +----------------+
                 /                /|
                /                / |
               +----------------+  |
               |                |  |
               |      LLM       |  |
               |   (timeout)    |  +
               |                | /
               |                |/
               +----------------+

 Behave, or Jimmy will put your "optimization"
 attempts in this box and delete them.

 Historical note: a naive per-triangle cutter (TriangleCutter.cs)
 with hidden failure modes was removed; future LLMs must design any
 replacement with explicit invariants, tests, and no silent degradation.
*/
