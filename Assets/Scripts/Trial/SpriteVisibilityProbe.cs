using System;
using System.Collections.Generic;
using UnityEngine;

namespace DATN.Trial
{
    // Opt-in QA only: sample real rendered alpha, including non-null transparent sprites.
    internal sealed class SpriteVisibilityProbe : IDisposable
    {
        private readonly GameObject _root;
        private readonly Camera _camera;
        private readonly SpriteRenderer _sprite;
        private readonly RenderTexture _target;
        private readonly Texture2D _readback;
        private readonly Dictionary<Sprite, int> _coverage = new();
        private bool _disposed;

        public SpriteVisibilityProbe()
        {
            _root = new GameObject("QA_VisibilityProbe");
            _root.transform.position = new Vector3(10000, 10000, 0);
            var cameraObject = new GameObject("QA_AlphaCamera", typeof(Camera));
            cameraObject.transform.SetParent(_root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 0, -10);
            _camera = cameraObject.GetComponent<Camera>();
            _camera.enabled = false;
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.cullingMask = 1 << 31;
            _target = RenderTexture.GetTemporary(64, 64, 16, RenderTextureFormat.ARGB32);
            _camera.targetTexture = _target;
            _readback = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var image = new GameObject("QA_IsolatedSprite", typeof(SpriteRenderer));
            image.layer = 31;
            image.transform.SetParent(_root.transform, false);
            _sprite = image.GetComponent<SpriteRenderer>();
        }

        public int VisiblePixels(SpriteRenderer source)
        {
            if (source.sprite == null) return 0;
            if (_coverage.TryGetValue(source.sprite, out int count)) return count;
            _sprite.sprite = source.sprite;
            _sprite.sharedMaterial = source.sharedMaterial;
            _sprite.color = Color.white;
            var bounds = source.sprite.bounds;
            _sprite.transform.localPosition = -bounds.center;
            _camera.orthographicSize = Mathf.Max(0.1f, Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.1f);
            var previous = RenderTexture.active;
            try
            {
                _camera.Render();
                RenderTexture.active = _target;
                _readback.ReadPixels(new Rect(0, 0, 64, 64), 0, 0, false);
                _readback.Apply(false);
                count = 0;
                foreach (var pixel in _readback.GetPixels32()) if (pixel.a > 8) count++;
                _coverage.Add(source.sprite, count);
                return count;
            }
            finally { RenderTexture.active = previous; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) RenderTexture.ReleaseTemporary(_target);
            if (_readback != null) UnityEngine.Object.Destroy(_readback);
            if (_root != null) UnityEngine.Object.Destroy(_root);
        }
    }
}
