using System.Collections.Generic;
using UnityEngine;
namespace DATN.World
{
    // A bounded cosmetic pool. No colliders, stats, events or damage operations.
    public sealed class JadeImpact : MonoBehaviour
    {
        private static readonly List<JadeImpact> Pool = new();
        private static Sprite _sprite;
        private SpriteRenderer _renderer;
        private float _remaining;
        public static void Show(SpriteRenderer target)
        {
            if (target == null) return;
            if (_sprite == null) _sprite = Resources.Load<Sprite>("SGJade/VFX/Impact");
            if (_sprite == null) return;
            Pool.RemoveAll(item => item == null);
            var spark = Pool.Find(item => !item.gameObject.activeSelf);
            if (spark == null)
            {
                if (Pool.Count >= 24) return;
                var go = new GameObject("Jade_HitSpark", typeof(SpriteRenderer));
                spark = go.AddComponent<JadeImpact>();
                spark._renderer = go.GetComponent<SpriteRenderer>();
                Pool.Add(spark);
            }
            spark._renderer.sprite = _sprite;
            spark._renderer.sortingLayerID = target.sortingLayerID;
            spark._renderer.sortingOrder = target.sortingOrder + 5;
            spark._renderer.color = new Color(.95f, .92f, 1f, .9f);
            spark.transform.position = target.bounds.center;
            spark.transform.localScale = Vector3.one * .28f;
            spark._remaining = .13f;
            spark.gameObject.SetActive(true);
        }
        private void Update()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) { gameObject.SetActive(false); return; }
            float fraction = _remaining / .13f;
            _renderer.color = new Color(.95f, .92f, 1f, fraction * .9f);
            transform.localScale = Vector3.one * Mathf.Lerp(.42f, .28f, fraction);
        }
    }
}
