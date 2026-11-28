using System;
using System.Collections;
using System.Linq;
using DATN.Localization;
using DATN.UI;
using RGame.RoguelikeKit;
using UnityEngine;
using UnityEngine.UI;

namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        private IEnumerator CheckConfirmations()
        {
            yield return EnterRound(0, "Rowan");
            _round.Pause();
            yield return null;
            Require(_round.IsPaused && Time.timeScale == 0f, "Confirmation checks begin in a paused active run");
            Require(RunCheckpointStore.Save(new RunCheckpointWorld(_round).Capture()),
                "Isolated QA profile contains an existing checkpoint before cancellation tests");
            _round.GlobalConfig.CurrentGetGold += 37; // Nonzero earnings expose accidental banking on Cancel.

            float clockBefore = _round.MatchSeconds;
            string statsBefore = CurrentStatsFingerprint();
            bool checkpointExisted = RunCheckpointStore.TryRead(out var checkpointBefore);
            string checkpointJsonBefore = checkpointExisted ? JsonUtility.ToJson(checkpointBefore) : null;

            yield return SelectLanguage("vi");
            yield return OpenConfirmation("SaveRunBtn", "Save confirmation opens for pointer input");
            var dialog = GameObject.Find("ConfirmationDialog");
            Require(HasText(dialog.transform, "LƯU TRẬN VÀ VỀ MENU?") &&
                    HasText(dialog.transform, "LƯU VÀ VỀ MENU") && HasText(dialog.transform, "HỦY"),
                "Vietnamese save confirmation shows its title and both actions");
            yield return CaptureFrame("confirmation-save-vi");
            yield return CancelConfirmation();
            CheckCancelledRun("Save cancel", clockBefore, statsBefore, checkpointExisted, checkpointJsonBefore);

            yield return SelectLanguage("en");
            yield return OpenConfirmation("SaveRunBtn", "English save confirmation opens for pointer input");
            dialog = GameObject.Find("ConfirmationDialog");
            Require(HasText(dialog.transform, "SAVE RUN & RETURN?") &&
                    HasText(dialog.transform, "SAVE & RETURN") && HasText(dialog.transform, "CANCEL"),
                "English save confirmation shows its title and both actions");
            yield return CaptureFrame("confirmation-save-en");
            yield return CancelConfirmation();
            CheckCancelledRun("Save cancel in English", clockBefore, statsBefore, checkpointExisted, checkpointJsonBefore);

            yield return OpenConfirmation("VeMenuBtn", "Discard confirmation opens for pointer input");
            dialog = GameObject.Find("ConfirmationDialog");
            Require(HasText(dialog.transform, "LEAVE CURRENT RUN?") &&
                    HasText(dialog.transform, "UNSAVED RUN PROGRESS WILL BE LOST"),
                "Discard confirmation explains that unsaved progress will be lost");
            yield return CancelConfirmation();
            CheckCancelledRun("Discard cancel", clockBefore, statsBefore, checkpointExisted, checkpointJsonBefore);

            yield return OpenConfirmation("SaveRunBtn", "Escape cancellation dialog opens");
            yield return PressEscape();
            yield return Wait(() => !ConfirmationDialog.IsOpen && GameObject.Find("ConfirmationDialog") == null,
                "Escape closes the confirmation dialog");
            CheckCancelledRun("Escape cancel", clockBefore, statsBefore, checkpointExisted, checkpointJsonBefore);

            yield return OpenConfirmation("SaveRunBtn", "Resume-blocking confirmation opens");
            var resume = GameObject.Find("TrialPausePanel").transform.Find("TiepTucBtn").GetComponent<Button>();
            Require(!CanClick(resume), "Open confirmation blocks pointer input to the underlying Resume button");
            _round.Resume();
            Require(ConfirmationDialog.IsOpen && _round.IsPaused && Time.timeScale == 0f,
                "Resume remains blocked while a confirmation is open");
            yield return CancelConfirmation();
            CheckCancelledRun("Resume blocked", clockBefore, statsBefore, checkpointExisted, checkpointJsonBefore);

            yield return OpenConfirmation("SaveRunBtn", "Final save confirmation opens");
            var confirm = ButtonIn(GameObject.Find("ConfirmationDialog").transform, "ConfirmButton");
            yield return Wait(() => CanClick(confirm), "Save confirmation action receives pointer input");
            Click(confirm);
            yield return WaitForMenu();
            Require(RunCheckpointSession.LastError == null,
                "Confirmed save completes without an error: " + RunCheckpointSession.LastError);
            Require(RunCheckpointSession.HasSavedRun, "Confirmed save makes a saved run available in the menu");
            Require(RunCheckpointStore.TryRead(out var saved), "Confirmed checkpoint can be read from the menu");
            Require(saved.hero == "Rowan" && saved.stage == "TrialLevel" && saved.clock >= clockBefore,
                "Confirmed checkpoint contains the paused survivor, stage and run clock");
        }

        private IEnumerator OpenConfirmation(string sourceButtonName, string waitMessage)
        {
            var source = GameObject.Find(sourceButtonName).GetComponent<Button>();
            yield return Wait(() => CanClick(source), sourceButtonName + " can be clicked");
            Click(source);
            yield return Wait(() => ConfirmationDialog.IsOpen && GameObject.Find("ConfirmationDialog") != null, waitMessage);
            // A runtime Canvas needs its layout pass before its raycast rectangles
            // represent the visible modal. Probe after that pass, as real input does.
            Canvas.ForceUpdateCanvases();
            yield return null;
            var cancel = ButtonIn(GameObject.Find("ConfirmationDialog").transform, "CancelButton");
            yield return Wait(() => CanClick(cancel), "New confirmation Canvas receives pointer input");
        }

        private IEnumerator CancelConfirmation()
        {
            var cancel = ButtonIn(GameObject.Find("ConfirmationDialog").transform, "CancelButton");
            yield return Wait(() => CanClick(cancel), "Confirmation cancel receives pointer input");
            Click(cancel);
            yield return Wait(() => !ConfirmationDialog.IsOpen && GameObject.Find("ConfirmationDialog") == null,
                "Confirmation closes after cancel");
        }

        private string CurrentStatsFingerprint() => "earned=" + _round.GlobalConfig.CurrentGetGold +
            "|unbanked=" + _round.GlobalConfig.UnbankedGold + "|" + string.Join("|", _round.PlayerStat.GetAllValues()
            .OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(value => value.Key + ":" + _round.PlayerStat.GetValue(value.Key) + ":" +
                             _round.PlayerStat.GetMaxValue(value.Key)));

        private void CheckCancelledRun(string action, float clockBefore, string statsBefore,
            bool checkpointExisted, string checkpointJsonBefore)
        {
            Require(_round != null && _round.IsPaused && Time.timeScale == 0f,
                action + " leaves gameplay paused");
            Require(Mathf.Abs(_round.MatchSeconds - clockBefore) < .001f,
                action + " leaves the run clock unchanged");
            Require(CurrentStatsFingerprint() == statsBefore,
                action + " leaves player stats unchanged");

            bool checkpointExistsNow = RunCheckpointStore.TryRead(out var checkpointNow);
            bool checkpointUnchanged = checkpointExisted == checkpointExistsNow &&
                (!checkpointExisted || JsonUtility.ToJson(checkpointNow) == checkpointJsonBefore);
            Require(checkpointUnchanged, action + " leaves the previously saved checkpoint unchanged");
        }
    }
}
