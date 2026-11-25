# Shadowgrove Survivors

## GitHub delivery

- The user's repository is `https://github.com/khanhanprovip/shadowgrove-survivors` (`origin`, branch `main`). The original kit remains `upstream`; retain its license and attribution.
- The user requests that completed, verified changes be committed and pushed to their repository automatically. Report the commit hash and push result after each completed batch; keep unfinished changes outside that commit. Do not ask again for an ordinary push already covered by this instruction.
- Keep build outputs, QA logs/profiles, Unity caches and downloaded tool binaries out of Git. Do not force-push or rewrite existing history.

Unity 2D project. Read `.agents/skills/using-unity/SKILL.md` and its relevant reference before Unity work. The project-local CLI and operator guide are in `Tools/UnityHarness/README.md`.

## Development loop

- Inspect Git status first; preserve existing edits. Use the exact Editor version from `ProjectSettings/ProjectVersion.txt`.
- Use the official project-local Unity CLI/Pipeline for Editor inspection and scene/prefab authoring. Keep multi-line authoring scripts outside Assets and execute with `script run`.
- Start gameplay from `Assets/RGame/RoguelikeKit/Scenes/Initialization.unity`.
- The three survivor prefabs use the built-in sprite material. Hit feedback must support shaders without `_BlendColor`/`_Alpha` and preserve sprite opacity; `hit-feedback` verifies this on the actual prefabs.
- Rowan/Lyra/Kael sheets have 4 Idle frames in the top row, 6 Run frames in the middle and 6 Attack frames in the bottom. The last two top cells are transparent. Verify rendered pixel coverage, since a non-null Sprite can still be completely invisible.
- Read a console cursor before changes; inspect errors since that cursor after verification. Do not clear the console. Confirm frames advance when verifying Play Mode.
- For existing Editor sessions, use `python Tools/UnityHarness/harness.py live -- <command>`. Always target this project; the wrapper supplies the project path.
- Close the project's Editor before cold `probe`, `build`, `verify`, or `legacy` operations. Do not terminate a user's Editor to free the project lock.

## Verification

```powershell
./Tools/UnityHarness/harness.ps1 doctor
python -m unittest discover -s Tools/UnityHarness -p test_harness.py -v
./Tools/UnityHarness/harness.ps1 probe
./Tools/UnityHarness/harness.ps1 verify --suites all
```

Choose suites relevant to the change. Available Windows suites: menus, backgrounds, skills, ui, survival, killtarget, endless, animations, hit-feedback, skill-lifetime, audio, settings-write, settings-read, art, combat-pickups, survival-boundary. Run settings-write before settings-read in the same invocation. `backgrounds` visits all ten stages and verifies terrain identity, world bounds, movement and pause. `legacy --case Smoke45|Flow65|Full100|Lose60|KillTargetLevel` wraps existing Editor tests; verify their actual reports before claiming success.

Results live under `Tools/UnityHarness/artifacts/`. Inspect summary, individual assertions, runtime logs and process exit codes. A missing report, timeout, FAIL assertion, exception or nonzero exit is a failure even when other assertions pass. Hidden-window screenshots do not prove visual quality. Report what was executed and what remains unverified.

Use isolated QA profiles; never reset real player saves to make a test pass. Keep existing gameplay/art direction unless the task calls for changing it. Use existing local tools first; obtain user approval before introducing paid services or API usage.

Boss/balance suites: `balance`, `pause-skills`, `spawn-pacing`, `bosses` (use `--timeout 750` for bosses). Bosses are Endless-only, 300 in-game seconds apart, five types with random attacks, first 300 HP then +300 per encounter. Preserve purchased permanent levels. Resource boss prefabs must receive the current GlobalConfigSO/PoolRuntimeSO context at runtime, rather than embedding duplicate mutable singleton assets. Keep giant bosses behind the survivor. Boss casts use visible windup/body motion; do not restore precast floor markers or cast-name warnings. Active damage effects remain visible. Boss arrival banners must be localized, nonblocking, and pause with gameplay. Author the five prefab native sprite-frame clips/controllers and animation components with Tools/Art/BossFrameAnimationAuthoring.cs through the Unity API.

The user prefers game audio muted during automated playtests. Use `Tools/UnityHarness/game-audio.ps1` to mute Shadowgrove sessions only; keep Windows output and other apps unchanged. A bounded `-WatchSeconds` can cover new Player launches. Keep Unity's audio engine running so audio regression tests can measure its output. Stop any mute watcher created for the run when the work finishes.
