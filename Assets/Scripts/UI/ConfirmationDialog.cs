using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DATN.UI
{
    public sealed class ConfirmationDialog : MonoBehaviour
    {
        private static ConfirmationDialog current;
        private Action confirmed;
        private GameObject previousSelection;
        private bool resolved;
        private int openedFrame;
        public static bool IsOpen => current != null && !current.resolved;

        public static void Show(Transform owner, string viTitle, string enTitle,
            string viMessage, string enMessage, Action onConfirm,
            string viAction = "XÁC NHẬN", string enAction = "CONFIRM")
        {
            if (IsOpen) return;
            var root = new GameObject("ConfirmationDialog", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ConfirmationDialog));
            SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
            current = root.GetComponent<ConfirmationDialog>();
            current.openedFrame = Time.frameCount;
            current.confirmed = onConfirm;
            current.previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var shade = ForestUI.Picture(root.transform, "ModalShade", null, 0, 0, 1, 1);
            shade.color = new Color(.01f, .025f, .03f, .88f);
            shade.raycastTarget = true;
            var panel = ForestUI.Picture(root.transform, "DialogPanel", "Panel", .26f, .30f, .74f, .70f);
            panel.raycastTarget = true;
            ForestUI.Label(panel.transform, "DialogTitle", viTitle, enTitle, 38,
                .07f, .73f, .93f, .91f, TextAlignmentOptions.Center, ForestUI.Gold);
            ForestUI.Label(panel.transform, "DialogMessage", viMessage, enMessage, 28,
                .08f, .32f, .92f, .70f, TextAlignmentOptions.Center);
            var dialog = current;
            var cancel = ForestUI.NewButton(panel.transform, "CancelButton", "HỦY", "CANCEL",
                () => dialog.Resolve(false), .07f, .09f, .46f, .25f);
            ForestUI.NewButton(panel.transform, "ConfirmButton", viAction, enAction,
                () => dialog.Resolve(true), .54f, .09f, .93f, .25f, true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(cancel.gameObject);
        }

        public static void CancelCurrent() { if (IsOpen) current.Resolve(false); }

        private void Update()
        {
            if (Time.frameCount > openedFrame && UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) Resolve(false);
        }

        private void Resolve(bool accept)
        {
            if (resolved) return;
            resolved = true;
            RGame.RoguelikeKit.ShadowgroveAudio.Play(accept ? RGame.RoguelikeKit.SoundCue.UiConfirm : RGame.RoguelikeKit.SoundCue.UiSelect);
            if (current == this) current = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
            if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            if (accept) confirmed?.Invoke();
        }

        private void OnDestroy() { if (current == this) current = null; }
    }
}
