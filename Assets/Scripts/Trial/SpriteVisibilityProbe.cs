using System;
using System.Collections.Generic;
using UnityEngine;

namespace DATN.Trial
{
    // Opt-in QA only: sample real rendered alpha, including non-null transparent sprites.
    internal sealed class SpriteVisibilityProbe : IDisposable
    {
        private const int SignatureResolution = 128;
        private readonly GameObject _root;
        private readonly Camera _camera;
        private readonly SpriteRenderer _sprite;
        private readonly RenderTexture _target;
        private readonly Texture2D _readback;
        private readonly RenderTexture _signatureTarget;
        private readonly Texture2D _signatureReadback;
        private readonly Dictionary<Sprite, int> _coverage = new();
        private readonly MaterialPropertyBlock _propertyBlock = new();
        private bool _disposed;
        internal Color32[] LastRenderedPixels()=>_signatureReadback.GetPixels32();

        internal readonly struct RenderSignature
        {
            public readonly int OpaquePixels;
            public readonly ulong RgbaHash;
            public readonly ulong AlphaHash;

            public RenderSignature(int opaquePixels, ulong rgbaHash, ulong alphaHash)
            {
                OpaquePixels = opaquePixels;
                RgbaHash = rgbaHash;
                AlphaHash = alphaHash;
            }
        }

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
            _signatureTarget = RenderTexture.GetTemporary(SignatureResolution, SignatureResolution, 16, RenderTextureFormat.ARGB32);
            _signatureReadback = new Texture2D(SignatureResolution, SignatureResolution, TextureFormat.RGBA32, false);
            var image = new GameObject("QA_IsolatedSprite", typeof(SpriteRenderer));
            image.layer = 31;
            image.transform.SetParent(_root.transform, false);
            _sprite = image.GetComponent<SpriteRenderer>();
        }

        public int VisiblePixels(SpriteRenderer source)
        {
            if (source == null) return 0;
            return VisiblePixels(source.sprite, source.sharedMaterial);
        }

        public int VisiblePixels(Sprite sprite, Material material)
        {
            if (sprite == null) return 0;
            if (_coverage.TryGetValue(sprite, out int count)) return count;
            _sprite.SetPropertyBlock(null);
            _sprite.sprite = sprite;
            _sprite.sharedMaterial = material;
            _sprite.color = Color.white;
            _sprite.flipX = false;
            _sprite.flipY = false;
            _sprite.drawMode = SpriteDrawMode.Simple;
            _sprite.transform.localScale = Vector3.one;
            _sprite.transform.localRotation = Quaternion.identity;
            var bounds = sprite.bounds;
            _sprite.transform.localPosition = -bounds.center;
            _camera.orthographicSize = Mathf.Max(0.1f, Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.1f);
            var previous = RenderTexture.active;
            try
            {
                _camera.targetTexture = _target;
                _camera.Render();
                RenderTexture.active = _target;
                _readback.ReadPixels(new Rect(0, 0, 64, 64), 0, 0, false);
                _readback.Apply(false);
                count = 0;
                foreach (var pixel in _readback.GetPixels32()) if (pixel.a > 8) count++;
                _coverage.Add(sprite, count);
                return count;
            }
            finally { RenderTexture.active = previous; }
        }

        // Unlike VisiblePixels, this is deliberately uncached: animated rigs need a fresh
        // GPU readback each sample so a changed shader pose cannot pass on sprite identity.
        public RenderSignature RenderedSignature(SpriteRenderer source)
        {
            if (source == null || source.sprite == null) return new RenderSignature(0, 0, 0);

            _sprite.sprite = source.sprite;
            _sprite.sharedMaterial = source.sharedMaterial;
            _sprite.color = source.color;
            _sprite.flipX = source.flipX;
            _sprite.flipY = source.flipY;
            _sprite.drawMode = source.drawMode;
            _sprite.size = source.size;
            _sprite.transform.localScale = source.transform.lossyScale;
            _sprite.transform.localRotation = Quaternion.Inverse(_root.transform.rotation) * source.transform.rotation;
            _sprite.transform.localPosition = -source.sprite.bounds.center;
            source.GetPropertyBlock(_propertyBlock);
            _sprite.SetPropertyBlock(_propertyBlock);

            var bounds = source.sprite.bounds;
            var scale = source.transform.lossyScale;
            _camera.orthographicSize = Mathf.Max(0.1f,
                Mathf.Max(bounds.extents.x * Mathf.Abs(scale.x), bounds.extents.y * Mathf.Abs(scale.y)) * 1.1f);

            var previous = RenderTexture.active;
            try
            {
                _camera.targetTexture = _signatureTarget;
                _camera.Render();
                RenderTexture.active = _signatureTarget;
                _signatureReadback.ReadPixels(new Rect(0, 0, SignatureResolution, SignatureResolution), 0, 0, false);
                _signatureReadback.Apply(false);

                int opaque = 0;
                ulong hash = 14695981039346656037UL;
                ulong alphaHash = 14695981039346656037UL;
                foreach (var pixel in _signatureReadback.GetPixels32())
                {
                    if (pixel.a > 8) opaque++;
                    alphaHash = (alphaHash ^ pixel.a) * 1099511628211UL;
                    hash = (hash ^ pixel.r) * 1099511628211UL;
                    hash = (hash ^ pixel.g) * 1099511628211UL;
                    hash = (hash ^ pixel.b) * 1099511628211UL;
                    hash = (hash ^ pixel.a) * 1099511628211UL;
                }
                return new RenderSignature(opaque, hash, alphaHash);
            }
            finally
            {
                _camera.targetTexture = _target;
                RenderTexture.active = previous;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) RenderTexture.ReleaseTemporary(_target);
            if (_readback != null) UnityEngine.Object.Destroy(_readback);
            if (_signatureTarget != null) RenderTexture.ReleaseTemporary(_signatureTarget);
            if (_signatureReadback != null) UnityEngine.Object.Destroy(_signatureReadback);
            if (_root != null) UnityEngine.Object.Destroy(_root);
        }
    }
}
