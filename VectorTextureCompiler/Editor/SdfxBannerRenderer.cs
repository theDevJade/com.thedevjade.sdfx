using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Sprites;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxBannerRenderer
    {
        private const string BannerPath =
            "Packages/com.thedevjade.sdfx/VectorTextureCompiler/Assets/sdfx_banner_base.svg";

        private const string CornerPath =
            "Packages/com.thedevjade.sdfx/VectorTextureCompiler/Assets/sdfx_corner_bracket.svg";

        private const string VectorMaterialPath =
            "Packages/com.unity.vectorgraphics/Runtime/Materials/Unlit_Vector.mat";

        private const string VectorGradientMaterialPath =
            "Packages/com.unity.vectorgraphics/Runtime/Materials/Unlit_VectorGradient.mat";

        private const float BannerAspect = 530f / 200f;
        private const float BannerHeight = 80f;
        private const float CardPadding = 12f;
        private const float TopSpacing = 8f;
        private const float BottomSpacing = 8f;
        private const float CornerSize = 20f;
        private const float CornerInset = 5f;
        private const float CornerSlide = 6f;
        private const float AnimationTime = 0.16f;
        private const int RenderAntiAliasing = 2;

        private static readonly Color LayoutBackground = new Color(0.05f, 0.07f, 0.09f);

        private static Sprite _banner;
        private static Sprite _corner;
        private static bool _loaded;

        private static Material _vectorMaterial;
        private static Material _vectorGradientMaterial;

        private static Texture2D _bannerTexture;
        private static Vector2Int _bannerTextureSize;
        private static Texture2D _cornerTexture;
        private static Vector2Int _cornerTextureSize;

        private static MaterialEditor _materialEditor;
        private static EditorWindow _editorWindow;

        private static bool _hovered;
        private static bool _animating;
        private static float _progress;
        private static double _lastTime;

        public static void Draw(MaterialEditor editor)
        {
            _materialEditor = editor;
            _editorWindow = null;
            DrawInternal();
        }

        public static void Draw(EditorWindow window)
        {
            _materialEditor = null;
            _editorWindow = window;
            DrawInternal();
        }

        public static void Cleanup()
        {
            if (_animating)
            {
                _animating = false;
                EditorApplication.update -= TickAnimation;
            }

            _materialEditor = null;
            _editorWindow = null;
        }

        private static void DrawInternal()
        {
            EnsureLoaded();

            float totalHeight = BannerHeight + CardPadding * 2f + TopSpacing + BottomSpacing;
            Rect layoutRect = GUILayoutUtility.GetRect(
                0f,
                totalHeight,
                GUILayout.ExpandWidth(true));

            float aspect = GetBannerAspect();
            ComputeCardLayout(layoutRect, aspect, out Rect cardRect);

            HandleHover(cardRect);

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(cardRect, LayoutBackground);
            DrawBanner(cardRect);
            DrawCorners(cardRect);
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            _banner = AssetDatabase.LoadAssetAtPath<Sprite>(BannerPath);
            _corner = AssetDatabase.LoadAssetAtPath<Sprite>(CornerPath);
            _vectorMaterial = LoadMaterial(VectorMaterialPath);
            _vectorGradientMaterial = LoadMaterial(VectorGradientMaterialPath);

            _loaded = true;
        }

        private static Material LoadMaterial(string path)
        {
            var source = AssetDatabase.LoadMainAssetAtPath(path) as Material;
            return source != null ? new Material(source) : null;
        }

        private static float GetBannerAspect()
        {
            if (_banner == null || _banner.rect.height <= 0f)
                return BannerAspect;

            return _banner.rect.width / _banner.rect.height;
        }

        private static bool IsVectorSprite(Sprite sprite)
            => sprite != null && sprite.vertices != null && sprite.vertices.Length > 0;

        private static void HandleHover(Rect rect)
        {
            if (_materialEditor != null)
            {
                _hovered = false;
                _progress = 0f;
                return;
            }

            var eventType = Event.current.type;
            if (eventType != EventType.Repaint &&
                eventType != EventType.MouseMove &&
                eventType != EventType.MouseDrag)
                return;

            bool hovering = rect.Contains(Event.current.mousePosition);

            if (hovering != _hovered)
            {
                _hovered = hovering;
                BeginAnimation();
            }
        }

        private static void BeginAnimation()
        {
            if (!_animating)
            {
                _animating = true;
                _lastTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += TickAnimation;
            }
        }

        private static void TickAnimation()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastTime);
            _lastTime = now;

            float target = _hovered ? 1f : 0f;

            _progress = Mathf.MoveTowards(
                _progress,
                target,
                dt / AnimationTime);

            RepaintHost();

            if (Mathf.Approximately(_progress, target))
            {
                _animating = false;
                EditorApplication.update -= TickAnimation;
            }
        }

        private static void ComputeCardLayout(
            Rect layoutRect,
            float aspect,
            out Rect cardRect)
        {
            const float sideMargin = 4f;
            float maxCardWidth = Mathf.Max(1f, layoutRect.width - sideMargin * 2f);

            float contentHeight = BannerHeight;
            float contentWidth = contentHeight * aspect;
            float cardWidth = contentWidth + CardPadding * 2f;
            float cardHeight = contentHeight + CardPadding * 2f;

            if (cardWidth > maxCardWidth)
            {
                cardWidth = maxCardWidth;
                contentWidth = cardWidth - CardPadding * 2f;
                contentHeight = contentWidth / aspect;
                cardHeight = contentHeight + CardPadding * 2f;
            }

            float cardX = layoutRect.x + (layoutRect.width - cardWidth) * 0.5f;
            float cardY = layoutRect.y + (layoutRect.height - cardHeight) * 0.5f;
            cardRect = new Rect(cardX, cardY, cardWidth, cardHeight);
        }

        private static void DrawBanner(Rect cardRect)
        {
            if (_banner == null)
            {
                EditorGUI.DrawRect(cardRect, LayoutBackground);
                return;
            }

            Rect contentRect = new Rect(
                cardRect.x + CardPadding,
                cardRect.y + CardPadding,
                cardRect.width - CardPadding * 2f,
                cardRect.height - CardPadding * 2f);

            DrawSprite(contentRect, _banner, ref _bannerTexture, ref _bannerTextureSize);
        }

        private static void DrawCorners(Rect cardRect)
        {
            if (_corner == null)
                return;

            float slide = _progress * CornerSlide;

            DrawCorner(cardRect, slide, false, false);
            DrawCorner(cardRect, slide, true, false);
            DrawCorner(cardRect, slide, false, true);
            DrawCorner(cardRect, slide, true, true);
        }

        private static void DrawCorner(
            Rect cardRect,
            float slide,
            bool flipX,
            bool flipY)
        {
            float x = flipX
                ? cardRect.xMax - CornerInset - CornerSize + slide
                : cardRect.xMin + CornerInset - slide;

            float y = flipY
                ? cardRect.yMax - CornerInset - CornerSize + slide
                : cardRect.yMin + CornerInset - slide;

            Rect rect = new Rect(
                x,
                y,
                CornerSize,
                CornerSize);

            DrawSprite(rect, _corner, ref _cornerTexture, ref _cornerTextureSize, flipX, flipY);
        }

        private static void DrawSprite(
            Rect rect,
            Sprite sprite,
            ref Texture2D cache,
            ref Vector2Int cacheSize,
            bool flipX = false,
            bool flipY = false)
        {
            if (sprite == null)
                return;

            rect = SnapToPixels(rect);

            if (IsVectorSprite(sprite))
            {
                DrawVectorSprite(rect, sprite, ref cache, ref cacheSize, flipX, flipY);
                return;
            }

            DrawTexturedSprite(rect, sprite, flipX, flipY);
        }

        private static void DrawVectorSprite(
            Rect rect,
            Sprite sprite,
            ref Texture2D cache,
            ref Vector2Int cacheSize,
            bool flipX,
            bool flipY)
        {
            var material = sprite.texture != null ? _vectorGradientMaterial : _vectorMaterial;
            if (material == null)
            {
                DrawTexturedSprite(rect, sprite, flipX, flipY);
                return;
            }

            int width = ScalePixels(rect.width);
            int height = ScalePixels(rect.height);
            var targetSize = new Vector2Int(width, height);

            if (cache == null || cacheSize != targetSize)
            {
                if (cache != null)
                {
                    Object.DestroyImmediate(cache);
                }

                cache = VectorUtils.RenderSpriteToTexture2D(
                    sprite,
                    width,
                    height,
                    material,
                    RenderAntiAliasing,
                    expandEdges: true);
                cache.hideFlags = HideFlags.HideAndDontSave;
                cache.filterMode = FilterMode.Bilinear;
                cacheSize = targetSize;
            }

            if (flipX || flipY)
            {
                var uv = new Rect(0f, 0f, 1f, 1f);
                if (flipX)
                {
                    uv.x = 1f;
                    uv.width = -1f;
                }

                if (flipY)
                {
                    uv.y = 1f;
                    uv.height = -1f;
                }

                GUI.DrawTextureWithTexCoords(rect, cache, uv, alphaBlend: true);
                return;
            }

            GUI.DrawTexture(rect, cache, ScaleMode.ScaleToFit, true);
        }

        private static void DrawTexturedSprite(Rect rect, Sprite sprite, bool flipX, bool flipY)
        {
            if (sprite.texture == null)
                return;

            Rect uv = GetSpriteTexCoords(sprite);

            if (flipX)
            {
                uv.x += uv.width;
                uv.width = -uv.width;
            }

            if (flipY)
            {
                uv.y += uv.height;
                uv.height = -uv.height;
            }

            GUI.DrawTextureWithTexCoords(
                rect,
                sprite.texture,
                uv,
                alphaBlend: true);
        }

        private static int ScalePixels(float value)
            => Mathf.Max(1, Mathf.RoundToInt(value * EditorGUIUtility.pixelsPerPoint));

        private static Rect SnapToPixels(Rect rect)
        {
            return new Rect(
                Mathf.Round(rect.x),
                Mathf.Round(rect.y),
                Mathf.Round(rect.width),
                Mathf.Round(rect.height));
        }

        private static Rect GetSpriteTexCoords(Sprite sprite)
        {
            Vector4 uv = DataUtility.GetOuterUV(sprite);
            return new Rect(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y);
        }

        private static void RepaintHost()
        {
            if (_materialEditor != null)
            {
                _materialEditor.Repaint();
                return;
            }

            _editorWindow?.Repaint();
        }
    }
}
