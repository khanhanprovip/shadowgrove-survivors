using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DATN.Localization;
using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        private IEnumerator CheckUiInteractions()
        {
            var failures = new List<string>();
            yield return Wait(() => FindFirstObjectByType<UIMainMenu>() != null && EventSystem.current != null,
                "Main menu and EventSystem are ready for UI interaction checks");

            yield return CheckUiQuitButton(failures);
            yield return CheckUiVolumeSteps(failures);
            yield return CheckUiOtherSettings(failures);
            yield return CheckUiRapidSettingsOpenClose(failures);
            yield return CheckUiRapidNewGame(failures);
            yield return CheckUiBackAndModalBlocking(failures);

            if (failures.Count > 0)
                throw new InvalidOperationException("UI interaction failures: " + string.Join(" | ", failures));
        }

        private IEnumerator CheckUiBackAndModalBlocking(List<string> failures)
        {
            var menu = FindFirstObjectByType<UIMainMenu>();
            var continueButton = GameObject.Find("ButtonContinueRun")?.GetComponent<Button>();
            if (continueButton != null)
            {
                CheckUiInteraction(continueButton.interactable == RunCheckpointSession.HasSavedRun,
                    "Continue Saved Run enabled state matches checkpoint availability", failures);
                CheckUiInteraction(CanClick(continueButton) == RunCheckpointSession.HasSavedRun,
                    "Continue Saved Run pointer hit matches its enabled/disabled state", failures);
            }

            var newGame = ButtonIn(menu, "ButtonNewGame");
            yield return WaitForUiButton(newGame, "New Game receives the live pointer before Character Back check");
            Click(newGame);
            yield return Wait(() => FindFirstObjectByType<CharacterSelectPanel>() != null,
                "Character selection opens from New Game");
            var characters = FindFirstObjectByType<CharacterSelectPanel>();
            var characterBack = ButtonIn(characters, "ButtonBack");
            yield return WaitForUiButton(characterBack, "Character Back receives the live pointer after layout");
            Click(characterBack);
            yield return Wait(() => FindFirstObjectByType<CharacterSelectPanel>() == null,
                "Character Back returns to the main menu");
            CheckUiInteraction(FindFirstObjectByType<UIMainMenu>() != null,
                "Character Back leaves the main menu available", failures);

            yield return WaitForUiButton(newGame, "New Game receives the live pointer before reopening Character Select");
            Click(newGame);
            yield return Wait(() => FindFirstObjectByType<CharacterSelectPanel>() != null,
                "Character selection reopens");
            characters = FindFirstObjectByType<CharacterSelectPanel>();
            yield return SelectRowan(characters, failures);
            var characterConfirm = ButtonIn(characters, "ButtonConfirm");
            yield return WaitForUiButton(characterConfirm, "Character Confirm receives the live pointer");
            Click(characterConfirm);
            yield return Wait(() => FindFirstObjectByType<LevelSelectPanel>() != null,
                "Level selection opens after character confirmation");
            var levels = FindFirstObjectByType<LevelSelectPanel>();
            var levelBack = ButtonIn(levels, "ButtonBack");
            yield return WaitForUiButton(levelBack, "Level Back receives the live pointer after layout");
            Click(levelBack);
            yield return Wait(() => FindFirstObjectByType<LevelSelectPanel>() == null,
                "Level Back closes level selection");
            CheckUiInteraction(FindFirstObjectByType<CharacterSelectPanel>() != null,
                "Level Back returns to the still-open character selection", failures);
            characters = FindFirstObjectByType<CharacterSelectPanel>();
            characterBack = ButtonIn(characters, "ButtonBack");
            yield return WaitForUiButton(characterBack, "Character Back receives the live pointer after Level Back");
            Click(characterBack);
            yield return Wait(() => FindFirstObjectByType<CharacterSelectPanel>() == null,
                "Character Back returns after level selection");

            yield return WaitForUiButton(newGame, "New Game receives the live pointer before lobby Back check");
            Click(newGame);
            yield return Wait(() => FindFirstObjectByType<CharacterSelectPanel>() != null,
                "Character selection opens for lobby Back check");
            characters = FindFirstObjectByType<CharacterSelectPanel>();
            yield return SelectRowan(characters, failures);
            characterConfirm = ButtonIn(characters, "ButtonConfirm");
            yield return WaitForUiButton(characterConfirm, "Character Confirm receives the live pointer for lobby Back check");
            Click(characterConfirm);
            yield return Wait(() => FindFirstObjectByType<LevelSelectPanel>() != null,
                "Level selection opens for lobby Back check");
            levels = FindFirstObjectByType<LevelSelectPanel>();
            var trial = levels.GetComponentsInChildren<LevelItem>().Single(item => item.Config.name == "TrialLevel");
            yield return WaitForUiButton(trial.GetComponent<Button>(), "Trial Forest card receives the live pointer");
            Click(trial.GetComponent<Button>());
            var levelConfirm = ButtonIn(levels, "ButtonConfirm");
            yield return WaitForUiButton(levelConfirm, "Level Confirm receives the live pointer");
            Click(levelConfirm);
            yield return Wait(() => GameObject.Find("Forest_Lobby") != null,
                "Lobby opens after stage confirmation");
            var lobby = GameObject.Find("Forest_Lobby").transform;
            var lobbyBack = ButtonIn(lobby, "LobbyBackBtn");
            yield return WaitForUiButton(lobbyBack, "Lobby Change Land receives the live pointer");
            Click(lobbyBack);
            yield return Wait(() => GameObject.Find("Forest_Lobby") == null,
                "Lobby Back returns to the level list");
            CheckUiInteraction(FindFirstObjectByType<LevelSelectPanel>() != null,
                "Lobby Back leaves level selection available", failures);
            levels = FindFirstObjectByType<LevelSelectPanel>();
            levelBack = ButtonIn(levels, "ButtonBack");
            yield return WaitForUiButton(levelBack, "Level Back receives the live pointer after Lobby Back");
            Click(levelBack);
            yield return Wait(() => FindFirstObjectByType<LevelSelectPanel>() == null,
                "Level Back closes the list after leaving the lobby");
            characters = FindFirstObjectByType<CharacterSelectPanel>();
            characterBack = ButtonIn(characters, "ButtonBack");
            yield return WaitForUiButton(characterBack, "Character Back receives the live pointer after Lobby Back");
            Click(characterBack);
            yield return Wait(() => FindFirstObjectByType<CharacterSelectPanel>() == null,
                "Character Back returns to the menu after leaving the lobby");

            menu = FindFirstObjectByType<UIMainMenu>();
            var settingsButton = ButtonIn(menu, "ButtonSetting");
            yield return WaitForUiButton(settingsButton, "Settings button receives the live pointer");
            Click(settingsButton);
            yield return Wait(() => FindFirstObjectByType<SettingPanel>() != null,
                "Settings opens for modal click-through check");
            var settings = FindFirstObjectByType<SettingPanel>();
            yield return ProbeUiModal(menu, settings, "Settings", failures);
            var closeSettings = ButtonIn(settings, "Button Close");
            yield return WaitForUiButton(closeSettings, "Settings Close receives the live pointer");
            Click(closeSettings);
            yield return Wait(() => FindFirstObjectByType<SettingPanel>() == null,
                "Settings closes after modal check");

            menu = FindFirstObjectByType<UIMainMenu>();
            var powerUpButton = ButtonIn(menu, "ButtonPowerUp");
            yield return WaitForUiButton(powerUpButton, "Power Ups button receives the live pointer");
            Click(powerUpButton);
            yield return Wait(() => FindFirstObjectByType<PowerUpPanel>() != null,
                "Power Ups opens for modal click-through check");
            var powerUps = FindFirstObjectByType<PowerUpPanel>();
            yield return ProbeUiModal(menu, powerUps, "Power Ups", failures);
            var closePowerUps = ButtonIn(powerUps, "Button Close");
            yield return WaitForUiButton(closePowerUps, "Power Ups Close receives the live pointer");
            Click(closePowerUps);
            yield return Wait(() => FindFirstObjectByType<PowerUpPanel>() == null,
                "Power Ups closes after modal check");
        }

        private IEnumerator CheckUiRapidNewGame(List<string> failures)
        {
            var menu = FindFirstObjectByType<UIMainMenu>();
            var newGame = ButtonIn(menu, "ButtonNewGame");
            yield return WaitForUiButton(newGame, "New Game receives the live pointer before rapid-click check");

            int before = CountUiPanels<CharacterSelectPanel>();
            CheckUiInteraction(before == 1,
                "Rapid New Game starts with exactly one Character Select panel instance", failures);
            Vector2 fixedPosition = PointerAt(newGame).position;
            for (int click = 1; click <= 3; click++)
            {
                Canvas.ForceUpdateCanvases();
                GameObject topHit = null;
                GameObject clickHandler = null;
                try
                {
                    clickHandler = DispatchUiPointerClick(fixedPosition, out topHit);
                }
                catch (Exception error)
                {
                    CheckUiInteraction(false,
                        "Rapid New Game click " + click + " completed through the live top hit: " + error.GetType().Name,
                        failures);
                }

                if (clickHandler != newGame.gameObject)
                {
                    var charactersAtPoint = FindFirstObjectByType<CharacterSelectPanel>();
                    bool coveredByCharacterOverlay = topHit != null && charactersAtPoint != null &&
                        (topHit.transform == charactersAtPoint.transform ||
                         topHit.transform.IsChildOf(charactersAtPoint.transform));
                    string route = coveredByCharacterOverlay
                        ? clickHandler == null ? "blocked by the Character Select overlay" :
                            "routed to Character Select overlay handler '" + clickHandler.name + "'"
                        : clickHandler == null ? "blocked at top hit '" + (topHit != null ? topHit.name : "<none>") + "'" :
                            "routed to top-hit handler '" + clickHandler.name + "'";
                    Record("INFO", "Rapid New Game click " + click + " did not reach New Game; " + route + ".");
                }
                else
                {
                    Record("INFO", "Rapid New Game click " + click + " reached New Game through its fresh raycast top hit.");
                }
            }

            Canvas.ForceUpdateCanvases();
            yield return null;
            int totalAfter = CountUiPanels<CharacterSelectPanel>();
            int activeAfter = CountActiveUiPanels<CharacterSelectPanel>();
            CheckUiInteraction(totalAfter == 1 && activeAfter == 1,
                "Three rapid New Game clicks leave exactly one active Character Select panel instance", failures);

            var characters = FindFirstObjectByType<CharacterSelectPanel>();
            if (characters == null)
            {
                CheckUiInteraction(false, "Rapid New Game panel remains open for Back verification", failures);
                yield break;
            }

            var back = ButtonIn(characters, "ButtonBack");
            yield return WaitForUiButton(back,
                "Character Back receives the live pointer after rapid New Game clicks and Canvas layout");
            Click(back);
            yield return Wait(() => FindFirstObjectByType<CharacterSelectPanel>() == null,
                "Character Back returns to the menu after rapid New Game clicks");
            CheckUiInteraction(FindFirstObjectByType<UIMainMenu>() != null &&
                               CountUiPanels<CharacterSelectPanel>() == 1 &&
                               CountActiveUiPanels<CharacterSelectPanel>() == 0,
                "Character Back leaves one inactive panel instance and the main menu available", failures);
        }

        private IEnumerator CheckUiRapidSettingsOpenClose(List<string> failures)
        {
            var menu = FindFirstObjectByType<UIMainMenu>();
            var settingsButton = ButtonIn(menu, "ButtonSetting");
            yield return WaitForUiButton(settingsButton, "Settings receives the live pointer before rapid-open check");

            int before = CountUiPanels<SettingPanel>();
            CheckUiInteraction(before == 1, "Rapid Settings starts with exactly one Settings panel instance", failures);
            Vector2 fixedPosition = PointerAt(settingsButton).position;
            for (int click = 1; click <= 3; click++)
            {
                Canvas.ForceUpdateCanvases();
                GameObject topHit = null;
                GameObject clickHandler = null;
                try
                {
                    clickHandler = DispatchUiPointerClick(fixedPosition, out topHit);
                }
                catch (Exception error)
                {
                    CheckUiInteraction(false,
                        "Rapid Settings open click " + click + " completed through the live top hit: " + error.GetType().Name,
                        failures);
                }

                if (clickHandler != settingsButton.gameObject)
                {
                    var settingsAtPoint = FindFirstObjectByType<SettingPanel>();
                    bool coveredBySettings = topHit != null && settingsAtPoint != null &&
                        (topHit.transform == settingsAtPoint.transform || topHit.transform.IsChildOf(settingsAtPoint.transform));
                    string route = coveredBySettings
                        ? clickHandler == null ? "blocked by the Settings overlay" :
                            "routed to Settings overlay handler '" + clickHandler.name + "'"
                        : clickHandler == null ? "blocked at top hit '" + (topHit != null ? topHit.name : "<none>") + "'" :
                            "routed to top-hit handler '" + clickHandler.name + "'";
                    Record("INFO", "Rapid Settings open click " + click + " did not reach Settings; " + route + ".");
                }
                else
                {
                    Record("INFO", "Rapid Settings open click " + click + " reached Settings through its fresh raycast top hit.");
                }
            }

            Canvas.ForceUpdateCanvases();
            yield return null;
            CheckUiInteraction(CountUiPanels<SettingPanel>() == 1 && CountActiveUiPanels<SettingPanel>() == 1,
                "Three rapid Settings clicks leave exactly one active Settings panel instance", failures);
            yield return CloseSettingsAndCheckSinglePanel(failures, "after rapid open clicks");

            for (int cycle = 1; cycle <= 2; cycle++)
            {
                menu = FindFirstObjectByType<UIMainMenu>();
                settingsButton = ButtonIn(menu, "ButtonSetting");
                yield return WaitForUiButton(settingsButton, "Settings receives the live pointer before reopen cycle " + cycle);
                Click(settingsButton);
                yield return Wait(() => FindFirstObjectByType<SettingPanel>() != null,
                    "Settings opens for open/close cycle " + cycle);
                CheckUiInteraction(CountUiPanels<SettingPanel>() == 1 && CountActiveUiPanels<SettingPanel>() == 1,
                    "Settings cycle " + cycle + " has one active panel instance", failures);
                yield return CloseSettingsAndCheckSinglePanel(failures, "after open/close cycle " + cycle);
            }
        }

        private IEnumerator CloseSettingsAndCheckSinglePanel(List<string> failures, string context)
        {
            var settings = FindFirstObjectByType<SettingPanel>();
            if (settings == null)
            {
                CheckUiInteraction(false, "Settings panel is available to close " + context, failures);
                yield break;
            }

            var close = ButtonIn(settings, "Button Close");
            yield return WaitForUiButton(close, "Settings Close receives the live pointer " + context);
            Click(close);
            yield return Wait(() => FindFirstObjectByType<SettingPanel>() == null,
                "Settings closes " + context);
            CheckUiInteraction(CountUiPanels<SettingPanel>() == 1 && CountActiveUiPanels<SettingPanel>() == 0,
                "Settings close " + context + " leaves one inactive panel instance", failures);
        }

        private GameObject DispatchUiPointerClick(Vector2 position, out GameObject topHit)
        {
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = position,
                button = PointerEventData.InputButton.Left,
                eligibleForClick = true
            };
            var hits = Raycast(pointer);
            topHit = hits.Count > 0 ? hits[0].gameObject : null;
            if (topHit == null) return null;

            var clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(topHit);
            ExecuteEvents.ExecuteHierarchy(topHit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(topHit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(topHit, pointer, ExecuteEvents.pointerClickHandler);
            return clickHandler;
        }

        private static int CountUiPanels<T>() where T : Component =>
            FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

        private static int CountActiveUiPanels<T>() where T : Component =>
            FindObjectsByType<T>(FindObjectsSortMode.None).Count(panel => panel.gameObject.activeInHierarchy);

        private IEnumerator SelectRowan(CharacterSelectPanel panel, List<string> failures)
        {
            var rowan = panel.GetComponentsInChildren<CharacterItem>().Single(item =>
                item.GetComponentsInChildren<TMP_Text>().Any(label => label.text == "Rowan"));
            var button = rowan.GetComponent<Button>();
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return WaitForUiButton(button, "Rowan card receives the live pointer after layout");
            var pointer = PointerAt(button);
            var hits = Raycast(pointer);
            var hoverHandler = hits.Count > 0
                ? ExecuteEvents.GetEventHandler<IPointerEnterHandler>(hits[0].gameObject)
                : null;
            var hoverTarget = hits.Count > 0
                ? ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerEnterHandler)
                : null;
            CheckUiInteraction(hoverHandler == rowan.gameObject && hoverTarget == rowan.gameObject,
                "Hero card receives a real EventSystem hover through its visible raycast target", failures);
            if (hits.Count > 0)
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerExitHandler);

            Click(button); // Uses the live top hit and sends pointer down/up/click.
            CheckUiInteraction(rowan.IsSelected, "Hero card pointer click selects Rowan", failures);
        }

        private IEnumerator ProbeUiModal(Component menu, Component modal, string label, List<string> failures)
        {
            var behind = ButtonIn(menu, "ButtonNewGame");
            // Opening/reordering a cached panel updates Graphic depths at the
            // next Canvas pass; probe the displayed modal, not its opening event.
            yield return null;
            Canvas.ForceUpdateCanvases();
            var pointer = PointerAt(behind);
            var hits = Raycast(pointer);
            var top = hits.Count > 0 ? hits[0].gameObject : null;
            var topHandler = top != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(top) : null;
            bool modalOwnsTopHit = top != null &&
                (top.transform == modal.transform || top.transform.IsChildOf(modal.transform));
            bool blocked = top == null || topHandler != behind.gameObject && modalOwnsTopHit;
            Record("INFO", label + " top=" + (top != null ? top.name : "<none>") +
                " ownsTop=" + modalOwnsTopHit + " behindHandler=" + (topHandler == behind.gameObject));

            if (blocked && top != null)
            {
                ExecuteEvents.ExecuteHierarchy(top, pointer, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.ExecuteHierarchy(top, pointer, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(top, pointer, ExecuteEvents.pointerClickHandler);
            }
            else if (topHandler == behind.gameObject)
            {
                Click(behind);
            }

            yield return null;
            bool leaked = FindFirstObjectByType<CharacterSelectPanel>() != null;
            CheckUiInteraction(blocked && !leaked,
                label + " blocks the real pointer event from opening the underlying New Game panel", failures);

            if (leaked)
            {
                var characters = FindFirstObjectByType<CharacterSelectPanel>();
                Click(ButtonIn(characters, "ButtonBack"));
                yield return Wait(() => FindFirstObjectByType<CharacterSelectPanel>() == null,
                    "Close character panel opened during click-through probe");
            }
        }

        private IEnumerator CheckUiVolumeSteps(List<string> failures)
        {
            var menu = FindFirstObjectByType<UIMainMenu>();
            var settingsButton = ButtonIn(menu, "ButtonSetting");
            yield return WaitForUiButton(settingsButton, "Settings button receives the live pointer before volume checks");
            Click(settingsButton);
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return Wait(() => FindFirstObjectByType<SettingPanel>() != null &&
                                    FindFirstObjectByType<SettingPanel>().GetComponentsInChildren<Slider>().Length == 3,
                "Settings and all three generated volume sliders are ready");

            var panel = FindFirstObjectByType<SettingPanel>();
            var settings = Resources.FindObjectsOfTypeAll<SettingsSO>().Single(asset => asset.name == "Settings");
            int[] originalValues =
            {
                Mathf.RoundToInt(settings.MasterVolume * 10f),
                Mathf.RoundToInt(settings.MusicVolume * 10f),
                Mathf.RoundToInt(settings.SfxVolume * 10f)
            };
            var items = panel.GetComponentsInChildren<SettingItem>();
            var rows = new[] { "Setting_Item", "Setting_Item (1)", "Setting_Item (2)" };
            for (int channel = 0; channel < rows.Length; channel++)
            {
                var item = items.Single(candidate => candidate.name == rows[channel]);
                var slider = item.GetComponentInChildren<Slider>(true);
                var previous = ButtonIn(item, "ButtonPrevious");
                var next = ButtonIn(item, "ButtonNext");
                Canvas.ForceUpdateCanvases();
                yield return null;
                yield return Wait(() => CanClick(previous) && CanClick(next),
                    item.name + " volume arrows receive live pointer input");
                CheckUiInteraction(slider != null && Mathf.Approximately(slider.minValue, 0f) &&
                                   Mathf.Approximately(slider.maxValue, 10f) && slider.wholeNumbers,
                    item.name + " volume slider exposes whole-number steps from 0 through 10", failures);

                for (int n = 0; n < 11 && ReadDisplayedSettingValue(item) != 0; n++)
                {
                    Click(previous);
                    yield return null;
                }
                CheckVolumeValue(item, slider, settings, channel, 0, failures);

                Click(previous);
                yield return null;
                CheckVolumeValue(item, slider, settings, channel, 0, failures);

                for (int value = 1; value <= 10; value++)
                {
                    Click(next);
                    yield return null;
                    CheckVolumeValue(item, slider, settings, channel, value, failures);
                }

                Click(next);
                yield return null;
                CheckVolumeValue(item, slider, settings, channel, 10, failures);

                for (int value = 9; value >= 0; value--)
                {
                    Click(previous);
                    yield return null;
                    CheckVolumeValue(item, slider, settings, channel, value, failures);
                }

                Click(previous);
                yield return null;
                CheckVolumeValue(item, slider, settings, channel, 0, failures);
            }

            for (int channel = 0; channel < rows.Length; channel++)
            {
                var item = items.Single(candidate => candidate.name == rows[channel]);
                var slider = item.GetComponentInChildren<Slider>(true);
                var previous = ButtonIn(item, "ButtonPrevious");
                var next = ButtonIn(item, "ButtonNext");
                int target = originalValues[channel];
                for (int step = 0; step < 10 && ReadDisplayedSettingValue(item) != target; step++)
                {
                    var arrow = ReadDisplayedSettingValue(item) > target ? previous : next;
                    yield return WaitForUiButton(arrow, item.name + " volume arrow receives pointer input while restoring its original value");
                    Click(arrow);
                    yield return null;
                }
                CheckVolumeValue(item, slider, settings, channel, target, failures);
            }

            var close = ButtonIn(panel, "Button Close");
            yield return WaitForUiButton(close, "Settings Close receives the live pointer after volume checks");
            Click(close);
            yield return Wait(() => FindFirstObjectByType<SettingPanel>() == null,
                "Settings closes after the volume boundary checks");
        }

        private void CheckVolumeValue(SettingItem item, Slider slider, SettingsSO settings, int channel,
            int expected, List<string> failures)
        {
            bool labelMatches = ReadDisplayedSettingValue(item) == expected;
            bool sliderMatches = slider != null && Mathf.RoundToInt(slider.value) == expected;
            float stored = channel == 0 ? settings.MasterVolume : channel == 1 ? settings.MusicVolume : settings.SfxVolume;
            bool settingMatches = Mathf.Abs(stored - expected / 10f) < .001f;
            CheckUiInteraction(labelMatches && sliderMatches && settingMatches,
                item.name + " UI, slider and saved setting equal " + expected + "/10", failures);
        }

        private static int ReadDisplayedSettingValue(SettingItem item)
        {
            foreach (var label in item.GetComponentsInChildren<TMP_Text>(true))
                if (int.TryParse(label.text.Trim(), out int value)) return value;
            return -1;
        }

        private static string ReadDisplayedSettingText(SettingItem item) =>
            item != null ? item.transform.Find("CurrentOption")?.GetComponentInChildren<TMP_Text>(true)?.text.Trim() : null;

        private IEnumerator CheckUiOtherSettings(List<string> failures)
        {
            var menu = FindFirstObjectByType<UIMainMenu>();
            var settingsButton = ButtonIn(menu, "ButtonSetting");
            yield return WaitForUiButton(settingsButton, "Settings button receives the live pointer before other-setting checks");
            Click(settingsButton);
            yield return Wait(() => FindFirstObjectByType<SettingPanel>() != null &&
                                    FindFirstObjectByType<SettingPanel>().GetComponentsInChildren<Slider>().Length == 3,
                "Settings and its setting rows initialize before other-setting checks");

            var panel = FindFirstObjectByType<SettingPanel>();
            var settings = Resources.FindObjectsOfTypeAll<SettingsSO>().Single(asset => asset.name == "Settings");
            var resolutionRow = ConfiguredSettingItem(panel, "resolutionItem");
            var fullscreenRow = ConfiguredSettingItem(panel, "fullscreenItem");
            var showDamageRow = ConfiguredSettingItem(panel, "showDamageItem");
            int originalResolutionIndex = settings.ResolutionsIndex;
            bool originalFullscreenSetting = settings.IsFullscreen;
            bool originalShowDamage = settings.IsShowDamage;
            float originalMasterVolume = settings.MasterVolume;
            float originalMusicVolume = settings.MusicVolume;
            float originalSfxVolume = settings.SfxVolume;
            int originalScreenWidth = Screen.width;
            int originalScreenHeight = Screen.height;
            FullScreenMode originalScreenMode = Screen.fullScreenMode;
            Resolution[] resolutions = Screen.resolutions;

            try
            {
                CheckUiInteraction(resolutionRow != null && fullscreenRow != null,
                    "Settings exposes its configured Resolution and Full Screen rows", failures);

                if (showDamageRow == null)
                {
                    Record("INFO", "Show Damage row is not configured on this Settings panel; row check skipped.");
                }
                else
                {
                    CheckUiInteraction(true, "Settings exposes its configured Show Damage row", failures);
                }

                if (resolutionRow != null)
                {
                    var previous = ButtonIn(resolutionRow, "ButtonPrevious");
                    var next = ButtonIn(resolutionRow, "ButtonNext");
                    yield return Wait(() => CanClick(previous) && CanClick(next),
                        "Resolution arrows receive live pointer input");
                    bool validIndex = originalResolutionIndex >= 0 && originalResolutionIndex < resolutions.Length;
                    if (validIndex)
                    {
                        string expected = DisplayResolution(resolutions[originalResolutionIndex]);
                        CheckUiInteraction(ReadDisplayedSettingText(resolutionRow) == expected,
                            "Resolution row displays the configured SettingsSO resolution index", failures);
                    }

                    int testIndex = -1;
                    if (validIndex && !originalFullscreenSetting && !Screen.fullScreen)
                    {
                        foreach (int candidate in new[] { originalResolutionIndex - 1, originalResolutionIndex + 1 })
                        {
                            if (candidate >= 0 && candidate < resolutions.Length &&
                                resolutions[candidate].width <= originalScreenWidth &&
                                resolutions[candidate].height <= originalScreenHeight)
                            {
                                testIndex = candidate;
                                break;
                            }
                        }
                    }

                    if (testIndex >= 0)
                    {
                        var arrow = testIndex < originalResolutionIndex ? previous : next;
                        yield return WaitForUiButton(arrow, "Resolution arrow receives the live pointer for a safe windowed mode");
                        Click(arrow);
                        yield return Wait(() => settings.ResolutionsIndex == testIndex,
                            "Resolution pointer click updates SettingsSO index");
                        var targetResolution = resolutions[testIndex];
                        yield return Wait(() => Screen.width == targetResolution.width &&
                                                Screen.height == targetResolution.height &&
                                                !Screen.fullScreen,
                            "Resolution pointer click applies its safe windowed size");
                        CheckUiInteraction(ReadDisplayedSettingText(resolutionRow) == DisplayResolution(targetResolution) &&
                                           settings.ResolutionsIndex == testIndex &&
                                           Screen.width == targetResolution.width && Screen.height == targetResolution.height,
                            "Resolution row, SettingsSO and Screen agree after pointer input", failures);

                        arrow = originalResolutionIndex < testIndex ? previous : next;
                        for (int index = testIndex; index != originalResolutionIndex;
                             index += originalResolutionIndex < testIndex ? -1 : 1)
                        {
                            yield return WaitForUiButton(arrow, "Resolution arrow receives the live pointer while restoring its original index");
                            Click(arrow);
                            yield return null;
                        }
                        yield return Wait(() => settings.ResolutionsIndex == originalResolutionIndex,
                            "Resolution row restores its original SettingsSO index");
                    }
                    else
                    {
                        string reason = !validIndex ? "the saved resolution index is outside the available modes" :
                            originalFullscreenSetting || Screen.fullScreen ? "the app is currently fullscreen" :
                            "no neighboring mode fits within the starting window dimensions";
                        Record("INFO", "Resolution mode change skipped because " + reason + ".");
                    }
                }

                if (fullscreenRow != null)
                    yield return CheckUiBooleanSetting(fullscreenRow, () => settings.IsFullscreen,
                        originalFullscreenSetting, "Full Screen", true, failures);

                if (showDamageRow != null)
                    yield return CheckUiBooleanSetting(showDamageRow, () => settings.IsShowDamage,
                        originalShowDamage, "Show Damage", false, failures);
            }
            finally
            {
                try
                {
                    RestoreUiSettingsRows(settings, resolutionRow, fullscreenRow, showDamageRow,
                        originalResolutionIndex, originalFullscreenSetting, originalShowDamage);
                }
                finally
                {
                    Screen.SetResolution(originalScreenWidth, originalScreenHeight, originalScreenMode);
                    if (settings != null &&
                        (settings.ResolutionsIndex != originalResolutionIndex ||
                         settings.IsFullscreen != originalFullscreenSetting || settings.IsShowDamage != originalShowDamage))
                    {
                        settings.SaveSettings(originalMusicVolume, originalSfxVolume, originalMasterVolume,
                            originalResolutionIndex, originalFullscreenSetting, originalShowDamage);
                    }
                }
            }

            yield return Wait(() => settings.ResolutionsIndex == originalResolutionIndex &&
                                    settings.IsFullscreen == originalFullscreenSetting &&
                                    settings.IsShowDamage == originalShowDamage &&
                                    Screen.width == originalScreenWidth && Screen.height == originalScreenHeight &&
                                    Screen.fullScreenMode == originalScreenMode,
                "Settings rows and the original screen mode are restored");
            CheckUiInteraction(settings.ResolutionsIndex == originalResolutionIndex &&
                               settings.IsFullscreen == originalFullscreenSetting &&
                               settings.IsShowDamage == originalShowDamage &&
                               Screen.width == originalScreenWidth && Screen.height == originalScreenHeight &&
                               Screen.fullScreenMode == originalScreenMode,
                "Resolution, Full Screen, Show Damage and original app display settings are restored", failures);

            var close = ButtonIn(panel, "Button Close");
            yield return WaitForUiButton(close, "Settings Close receives the live pointer after other-setting checks");
            Click(close);
            yield return Wait(() => FindFirstObjectByType<SettingPanel>() == null,
                "Settings closes after other-setting checks");
        }

        private IEnumerator CheckUiBooleanSetting(SettingItem item, Func<bool> read, bool original,
            string label, bool checkScreenFullscreen, List<string> failures)
        {
            var previous = ButtonIn(item, "ButtonPrevious");
            var next = ButtonIn(item, "ButtonNext");
            yield return Wait(() => CanClick(previous) && CanClick(next),
                label + " arrows receive live pointer input");
            string originalText = BooleanSettingText(original);
            CheckUiInteraction(ReadDisplayedSettingText(item) == originalText,
                label + " row displays its original SettingsSO value", failures);

            bool changed = !original;
            var changeButton = changed ? next : previous;
            yield return WaitForUiButton(changeButton, label + " arrow receives the live pointer to change its value");
            Click(changeButton);
            yield return Wait(() => read() == changed,
                label + " pointer click updates SettingsSO");
            if (checkScreenFullscreen)
                yield return Wait(() => Screen.fullScreen == changed,
                    label + " pointer click updates Screen.fullScreen");
            CheckUiInteraction(read() == changed && ReadDisplayedSettingText(item) == BooleanSettingText(changed) &&
                               (!checkScreenFullscreen || Screen.fullScreen == changed),
                label + " row, SettingsSO and applicable Screen state agree after pointer input", failures);

            var restoreButton = original ? next : previous;
            yield return WaitForUiButton(restoreButton, label + " arrow receives the live pointer to restore its value");
            Click(restoreButton);
            yield return Wait(() => read() == original,
                label + " pointer click restores the original SettingsSO value");
            if (checkScreenFullscreen)
                yield return Wait(() => Screen.fullScreen == original,
                    label + " pointer click restores the original Screen.fullScreen state");
            CheckUiInteraction(read() == original && ReadDisplayedSettingText(item) == originalText &&
                               (!checkScreenFullscreen || Screen.fullScreen == original),
                label + " row and SettingsSO return to their original value", failures);
        }

        private static void RestoreUiSettingsRows(SettingsSO settings, SettingItem resolutionRow,
            SettingItem fullscreenRow, SettingItem showDamageRow, int originalResolutionIndex,
            bool originalFullscreen, bool originalShowDamage)
        {
            if (settings == null) return;
            if (resolutionRow != null && originalResolutionIndex >= 0 &&
                originalResolutionIndex < Screen.resolutions.Length)
            {
                var previous = ButtonIn(resolutionRow, "ButtonPrevious");
                var next = ButtonIn(resolutionRow, "ButtonNext");
                for (int step = 0; settings.ResolutionsIndex != originalResolutionIndex &&
                     step < Screen.resolutions.Length + 1; step++)
                    Click(settings.ResolutionsIndex > originalResolutionIndex ? previous : next);
            }
            if (fullscreenRow != null && settings.IsFullscreen != originalFullscreen)
                Click(ButtonIn(fullscreenRow, originalFullscreen ? "ButtonNext" : "ButtonPrevious"));
            if (showDamageRow != null && settings.IsShowDamage != originalShowDamage)
                Click(ButtonIn(showDamageRow, originalShowDamage ? "ButtonNext" : "ButtonPrevious"));
        }

        private static SettingItem ConfiguredSettingItem(SettingPanel panel, string fieldName)
        {
            var field = typeof(SettingPanel).GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return field?.GetValue(panel) as SettingItem;
        }

        private static string BooleanSettingText(bool value) =>
            value ? DATN.UI.ForestUI.Tr("Bật", "ON") : DATN.UI.ForestUI.Tr("Tắt", "OFF");

        private static string DisplayResolution(Resolution resolution) =>
            $"{resolution.width}x{resolution.height} @{Math.Round(resolution.refreshRateRatio.value)}Hz";

        private IEnumerator CheckUiQuitButton(List<string> failures)
        {
            var menu = FindFirstObjectByType<UIMainMenu>();
            var quit = ButtonIn(menu, "ButtonExit");
            string originalLanguage = LanguageManager.GetCurrentCode();
            yield return SelectLanguage("vi");
            yield return WaitForUiButton(quit, "Quit button receives the live pointer before opening confirmation");
            bool quitRequested = false;
            Func<bool> interceptQuit = () =>
            {
                quitRequested = true;
                return false;
            };

            Application.wantsToQuit += interceptQuit;
            try
            {
                try
                {
                    Click(quit); // Exercises the actual ButtonExit callback through the EventSystem.
                }
                catch (Exception error)
                {
                    CheckUiInteraction(false, "ButtonExit callback completed without throwing: " + error.GetType().Name,
                        failures);
                }

                yield return Wait(() => DATN.UI.ConfirmationDialog.IsOpen,
                    "Quit opens the confirmation dialog through the live pointer");
                var dialog = GameObject.Find("ConfirmationDialog");
                CheckUiInteraction(dialog != null && !quitRequested && FindFirstObjectByType<UIMainMenu>() != null,
                    "Quit opens confirmation and keeps the menu alive without requesting application quit", failures);
                if (dialog == null)
                    yield break;

                yield return WaitForQuitDialogLanguage(dialog.transform, "vi", failures);
                yield return ProbeUiModal(menu, dialog.GetComponent<DATN.UI.ConfirmationDialog>(),
                    "Vietnamese Quit confirmation", failures);
                yield return CaptureFrame("quit-confirmation-vi");

                var cancel = ButtonIn(dialog.transform, "CancelButton");
                yield return WaitForUiButton(cancel, "Quit confirmation Cancel receives the live pointer");
                Click(cancel);
                yield return Wait(() => !DATN.UI.ConfirmationDialog.IsOpen,
                    "Cancel closes Quit confirmation");
                CheckUiInteraction(FindFirstObjectByType<UIMainMenu>() != null && !quitRequested &&
                                   FindFirstObjectByType<CharacterSelectPanel>() == null,
                    "Cancel leaves the main menu open without requesting application quit", failures);

                yield return SelectLanguage("en");
                menu = FindFirstObjectByType<UIMainMenu>();
                quit = ButtonIn(menu, "ButtonExit");
                yield return WaitForUiButton(quit, "Quit button receives the live pointer before reopening confirmation");
                Click(quit);
                yield return Wait(() => DATN.UI.ConfirmationDialog.IsOpen,
                    "Quit confirmation reopens through the live pointer");
                dialog = GameObject.Find("ConfirmationDialog");
                CheckUiInteraction(dialog != null && !quitRequested,
                    "Reopening Quit confirmation still does not request application quit", failures);
                if (dialog == null)
                    yield break;

                yield return WaitForQuitDialogLanguage(dialog.transform, "en", failures);
                yield return ProbeUiModal(menu, dialog.GetComponent<DATN.UI.ConfirmationDialog>(),
                    "English Quit confirmation", failures);
                yield return CaptureFrame("quit-confirmation-en");

                yield return PressEscape();
                yield return Wait(() => !DATN.UI.ConfirmationDialog.IsOpen,
                    "Escape cancels Quit confirmation");
                CheckUiInteraction(FindFirstObjectByType<UIMainMenu>() != null && !quitRequested &&
                                   FindFirstObjectByType<CharacterSelectPanel>() == null,
                    "Escape leaves the main menu open without requesting application quit", failures);

                menu = FindFirstObjectByType<UIMainMenu>();
                quit = ButtonIn(menu, "ButtonExit");
                yield return WaitForUiButton(quit, "Quit button receives the live pointer before final confirmation");
                Click(quit);
                yield return Wait(() => DATN.UI.ConfirmationDialog.IsOpen,
                    "Quit confirmation reopens after Escape cancellation");
                dialog = GameObject.Find("ConfirmationDialog");
                CheckUiInteraction(dialog != null && !quitRequested,
                    "Reopening after Escape does not request application quit", failures);
                if (dialog == null)
                    yield break;

                var confirm = ButtonIn(dialog.transform, "ConfirmButton");
                yield return WaitForUiButton(confirm, "Quit confirmation Confirm receives the live pointer");
                try
                {
                    Click(confirm);
                }
                catch (Exception error)
                {
                    CheckUiInteraction(false, "Quit confirmation Confirm completed through the live pointer: " +
                        error.GetType().Name, failures);
                }

                yield return Wait(() => quitRequested,
                    "Confirm requests application quit and the wantsToQuit interceptor rejects it");
                yield return Wait(() => !DATN.UI.ConfirmationDialog.IsOpen,
                    "Confirmed Quit closes the dialog after the intercepted quit request");
                CheckUiInteraction(FindFirstObjectByType<UIMainMenu>() != null,
                    "Rejected application quit leaves the main menu running", failures);
            }
            finally
            {
                Application.wantsToQuit -= interceptQuit;
                if (DATN.UI.ConfirmationDialog.IsOpen)
                    DATN.UI.ConfirmationDialog.CancelCurrent();
                var restoreLocale = UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales?.Locales
                    .FirstOrDefault(locale => locale.Identifier.Code == originalLanguage);
                if (restoreLocale != null)
                    LanguageManager.Instance?.SetLanguage(restoreLocale);
            }
        }

        private IEnumerator WaitForQuitDialogLanguage(Transform dialog, string language, List<string> failures)
        {
            Canvas.ForceUpdateCanvases();
            yield return null; // Let the newly created overlay Canvas register its raycaster.
            string expectedTitle = language == "vi" ? "THOÁT TRÒ CHƠI?" : "QUIT GAME?";
            string expectedConfirm = language == "vi" ? "THOÁT GAME" : "QUIT GAME";
            string expectedCancel = language == "vi" ? "HỦY" : "CANCEL";
            var title = dialog.Find("DialogPanel/DialogTitle")?.GetComponent<TMP_Text>();
            var confirm = ButtonIn(dialog, "ConfirmButton").GetComponentInChildren<TMP_Text>(true);
            var cancel = ButtonIn(dialog, "CancelButton").GetComponentInChildren<TMP_Text>(true);
            yield return Wait(() => LanguageManager.GetCurrentCode() == language &&
                                   title != null && title.text == expectedTitle &&
                                   confirm != null && confirm.text == expectedConfirm &&
                                   cancel != null && cancel.text == expectedCancel,
                language.ToUpperInvariant() + " Quit confirmation labels are localized");
            CheckUiInteraction(title != null && title.text == expectedTitle &&
                               confirm != null && confirm.text == expectedConfirm &&
                               cancel != null && cancel.text == expectedCancel,
                language.ToUpperInvariant() + " Quit confirmation title and actions are localized", failures);
        }

        private IEnumerator WaitForUiButton(Button button, string step)
        {
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return Wait(() => CanClick(button), step);
        }

        private void CheckUiInteraction(bool condition, string detail, List<string> failures)
        {
            if (condition)
                Record("PASS", detail);
            else
            {
                Record("FAIL", detail);
                failures.Add(detail);
            }
        }
    }
}
