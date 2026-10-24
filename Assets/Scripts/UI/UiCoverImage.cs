using UnityEngine;
using UnityEngine.UI;

namespace DATN.UI
{
    /// <summary>
    ///     Chan bi keo gian: anh nen se phong to/phu kin toan canvas theo kieu
    ///     "cover" (giong CSS object-fit: cover) — giu nguyen ty le anh, cat phan
    ///     du ra, tu dong cap nhat khi doi do phan giai man hinh.
    ///     Gan vao cac Image nen cua menu (BG) thay vi de anchor stretch mac dinh.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class UiCoverImage : MonoBehaviour
    {
        private RectTransform mRect;
        private Vector2 mReferenceSize = new(1920, 1080);

        private void Awake()
        {
            mRect = (RectTransform)transform;

            // Dua ve anchor giua de ty le hoan toan do dieu khien boi script
            mRect.anchorMin = Vector2.one * 0.5f;
            mRect.anchorMax = Vector2.one * 0.5f;
            mRect.pivot = Vector2.one * 0.5f;
            mRect.anchoredPosition = Vector2.zero;
            mRect.localScale = Vector3.one;

            // Lay kich thuoc that tu sprite (tuong thich moi anh nen khac nhau)
            var image = GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                mReferenceSize = image.sprite.rect.size;
            }

            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void Apply()
        {
            if (mRect == null) mRect = (RectTransform)transform;
            if (mRect.parent is not RectTransform parent) return;

            // Cho phep frame dau tien parent chua co kich thuoc
            if (parent.rect.width < 1f || parent.rect.height < 1f) return;

            float coverScale = Mathf.Max(
                parent.rect.width / mReferenceSize.x,
                parent.rect.height / mReferenceSize.y);

            mRect.sizeDelta = mReferenceSize * coverScale;
        }
    }
}
