# Shadowgrove implementation — November 2026

Stage 9 retains Endless: ordinary waves, increasing difficulty and a boss every 300 match seconds. Boss Rush is stage 10: no ordinary mobs, level 11 and ten initial upgrade selections, then the first boss entrance immediately. After each boss, five level rewards and 500 gold are granted. The five-second countdown starts after all reward choices finish. Both modes shuffle all five boss types before repeating, starting at 300 HP and increasing by 300 per encounter without resetting between bags.

All real level gains heal living players to maximum HP in every mode. Restoring a checkpoint does not heal or resurrect the survivor. Armor reductions cannot turn incoming damage into healing; damage remains at least one.

Stages 1–8 use deterministic connected layouts. Stages 3, 5 and 8 require a themed boss after 120 seconds; the other Survival stages retain timer wins and KillTarget stages retain 30 kills. Players enter the central arena before a Survival finale. Boss Rush stays within its arena; Endless remains unbounded as before.

Rowan, Lyra and Kael have fixed passives and exclusive automatic starters with five ranks. Generic upgrade choices remain available. Legacy checkpoints keep their original starter upgrade track and mode; purchased permanent upgrades are preserved. UI describes the passives and starters in Vietnamese and English.

Boss effects reuse authored pixel sprites with pooled renderers and shared materials. Windup effects remain on the body/hands; no precast floor warning markers. Active attack geometry remains visible. Eight original procedural SFX sketches are routed through the existing mixer with bounded voices; they are not recorded natural foley.

New Windows suites: `level-heal`, `hero-kits`, `boss-rush`, `campaign-bosses`, `endless-boundary`. Existing `bosses` covers all 25 attacks and native animation renders. The live-animation sample temporarily removes the boss from player targeting, because direct-damage weapons bypass disabled colliders. The test retains its unchanged-HP assertion and restores the roster afterward. Boss Rush's death test uses one HP and negative armor, then calls the actual PlayerHit path rather than simulating a result screen.

Builds and raw QA logs are excluded from Git. The tracked verification JSON links the actual local runs; interrupted/failed runs are retained and distinguished from successful reruns. Human evaluation of SFX timbre and longer-term balance remains separate from automated checks.
