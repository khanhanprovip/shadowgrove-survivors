# Boss sprite animation art brief

Production assets: `Assets/Resources/SGBosses/AnimationSheets/`. Generated with the built-in imagegen tool using each original sprite in `Assets/Resources/SGBosses/Sprites/` as the identity/style reference. No API key or external paid service was added. Original sprites and atlas remain available for comparison.

The selected sheets are actual RGBA images (1254 × 1254). Each has a uniform 4 × 4 layout; authoring rounds cell boundaries to whole pixels. Import through `BossFrameAnimationAuthoring.Run()`, not manual .meta/YAML edits. Idle0 visible alpha sets the common PPU and foot pivot. Other poses keep that physical scale.

## Shared generation prompt

Create a production 4-column by 4-row sprite animation sheet of the exact referenced boss, preserving its anatomy, costume, weapon, palette and dense crisp pixel-painted dark fantasy style. Transparent alpha background, no text, grid lines, UI or environment. Sixteen equal cells; one complete same-scale character in each cell, fixed camera and light, consistent foot baseline, safe padding so no part crosses a cell boundary. Use genuinely distinct articulated poses, never scaled duplicates.

Top row: four idle poses. Second row: four locomotion poses. Third row: draw back/coil, raised anticipation, attack release, follow-through. Last row: hit recoil, recovery, collapse, visible death remains. No blank cells. Preserve identity in every frame.

## Selected character directions

- **EclipseWraith:** exact jade hooded specter, gold skeletal armor, eclipsed green halo and antique crescent staff. Keep the staff upright/planted in its left hand throughout live poses. Idle free right claw draws a small circle; locomotion is a diagonal ethereal drift with cloak streaming behind. Attack pulls the right claw to chest, raises a small jade orb near face, then thrusts the right palm forward and extends the claw on follow-through. No physical scythe slash. Staff tilts only during death.
- **ThornRegent:** bark-and-bone forest king, thorn antlers, gold chest ember, moss armor, branch fists and root skirt. Four heavy alternating root strides with planted roots and counter-swinging shoulders. Attack compresses the root base, lifts both branch fists, slams down and follows through with a crouched trunk. Death becomes a collapsed stump/crown, no gore.
- **FrostMourner:** pale skull queen, cyan crystal crown/wings, icy white ribbons and black claws. Graceful leaning/gliding cycle with shoulders and ribbons changing independently. Attack folds hands/crystal wings, raises claws and fans wings, thrusts forward with a close frost arc, then extended-hand follow-through. Death folds into an icy robe/crystal heap.
- **CrimsonHarvester:** crimson hood, gold bone armor, curled horns, enormous execution scythe and ragged red robe. Aggressive advancing stride/glide, lowered torso and shoulder weight shifts. Attack twists to draw the scythe back, raises it overhead, cuts diagonally with the whole torso and follows through low. Full scythe blade stays in frame. Death buckles into a red cloth/skeleton heap with scythe visible, no gore.
- **AbyssOracle:** giant golden vertical-slit eye, gold crown, violet robe, two small floating eye sigils and many purple tentacles. Idle eye looks left/center/right while tips curl asynchronously. Locomotion alternates compressed planted lower tentacles and extending/pulling tentacles, like peristaltic crawling, without a human walk or generic floating bounce. Attack retracts all tentacles, coils and narrows the eye, then opens the eye and shoots tentacles outward/forward, followed by curled hooks. Death closes the eye and sinks into a tentacle/crown knot. No long beam or detached large VFX.

## Selected generated files

| Boss | Generated source filename |
| --- | --- |
| EclipseWraith | exec-549b9bb5-8569-4d3d-acf9-6963492f0f8f.png |
| ThornRegent | exec-089d0ac8-6632-4d6c-8ee2-9fda162b294b.png |
| FrostMourner | exec-42fa9989-ff0d-44d6-80ed-f1358f716edc.png |
| CrimsonHarvester | exec-4e2332ea-ab4b-484b-9e00-ddd4f6a1dec5.png |
| AbyssOracle | exec-21fc01d9-87d7-4a1f-b7f1-4a06cea41c6a.png |

## Smoother locomotion and combat sheets (2026-11-25)

The current bank uses 36 sprites per boss: Idle 8, Move 8, Windup 8, Attack 8, Hit 2 and Death 2. Locomotion sheets have two rows of eight ordered idle poses followed by two rows of eight ordered moving poses. Combat sheets have eight anticipation poses followed by eight release/follow-through poses. The original sheets above supply only Hit and Death. Keep all poses at the same body scale; added intermediate poses must articulate the body, not rescale it. Keep root strides upright and the scythe inside every cell.

| Boss | Locomotion source | Combat source |
| --- | --- | --- |
| EclipseWraith | exec-c2dc39b7-a1d3-43a6-87a1-2428a72d4891.png | exec-52d3914e-0d6e-40bc-9f9f-3bc9257f26fb.png |
| ThornRegent | exec-e958b316-27eb-4130-b068-aee5817bff53.png | exec-942e5366-f96c-4493-b418-6641fa8c4b4a.png |
| FrostMourner | exec-c1dfd7f1-6d58-4394-9f04-612136a5c2d6.png | exec-86bb5b80-2535-468e-a915-0b298cffa359.png |
| CrimsonHarvester | exec-98dc8f59-12e7-4f78-90aa-adbd99635247.png | exec-d4efa88e-870f-42a8-a56f-36c7d14f82ad.png |
| AbyssOracle | exec-3fcb5351-fcfc-4ed4-83c8-af44c623d40e.png | exec-709978b9-1de6-4412-9120-e3986adf89de.png |

## Native clip timing

| Boss | Idle cycle | Move cycle |
| --- | ---: | ---: |
| EclipseWraith | 1.40 s | 0.96 s |
| ThornRegent | 1.80 s | 1.20 s |
| FrostMourner | 1.10 s | 0.80 s |
| CrimsonHarvester | 1.00 s | 0.64 s |
| AbyssOracle | 1.50 s | 1.04 s |

Key holds also differ inside each clip. Gameplay windup remains 1.1 s, attack phase 0.9 s, hit clip 0.18 s and death dissolve 0.68 s. The five 6-state controllers use 180 selected sprites and 30 clips. The frame driver changes pose only; combat rules, stats, saves and boss schedule are independent. Stable bounds from the complete frame bank prevent the camera and nameplate bouncing with individual poses.

Skill VFX use pooled procedural lines with jade spirit spirals, bark roots/leaves, cyan crystal facets, crimson scythe crescents and violet eyes/tendrils. Hand/chest windup effects stay on the boss; active attack geometry keeps its existing damage border. No floor warning or cast-name cue is restored. Alpha is bounded, buffers are reused, and scaled-time animation freezes with pause.

The side-by-side preview samples these actual native clips. Its hit and death phases are slowed for visibility; it is not a recording of an autonomous match.
