using System.Collections.Generic;
using RGame.RoguelikeKit;
using UnityEngine;
namespace DATN.World
{
    public sealed class ThornSlashEffect : MonoBehaviour
    {
        private static readonly List<ThornSlashEffect> Pool = new();
        private SpriteRenderer image;
        private float remaining;
        private float angle;
        public static void Show(StarterWeaponProfile owner, Vector2 direction)
        {
            Pool.RemoveAll(item => item == null);
            var effect = Pool.Find(item => !item.gameObject.activeSelf);
            if (effect == null)
            {
                if (Pool.Count >= 8) return;
                var go = new GameObject("Rowan_ThornSlash", typeof(SpriteRenderer));
                effect = go.AddComponent<ThornSlashEffect>();
                effect.image = go.GetComponent<SpriteRenderer>();
                Pool.Add(effect);
            }
            var hero = owner.GetComponent<SpriteRenderer>();
            effect.image.sprite = owner.Effect;
            effect.image.color = Color.white;
            effect.image.flipX = true;
            effect.image.sortingLayerID = hero.sortingLayerID;
            effect.image.sortingOrder = hero.sortingOrder - 5;
            effect.transform.position = owner.transform.position + (Vector3)(direction * owner.Range * .5f) + Vector3.up * .25f;
            effect.transform.localScale = Vector3.one * (owner.Range * 1.15f);
            effect.angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            effect.remaining = .18f;
            effect.gameObject.SetActive(true);
        }
        private void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0) { gameObject.SetActive(false); return; }
            float progress = 1f - remaining / .18f;
            transform.rotation = Quaternion.Euler(0, 0, angle + Mathf.Lerp(-25, 25, progress));
            image.color = new Color(1, 1, 1, Mathf.Min(1, remaining / .06f));
        }
    }
}
