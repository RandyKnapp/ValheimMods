using System;
using System.Collections.Generic;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace EpicLoot.ShardStones {
    // Dynamically builds a sprite icon for any socketed item prefab
    public static class ShardTooltipSprites {
        private const string AssetNamePrefix = "EpicLoot_Socket_";

        private static bool _initialized;
        private static Shader _shader;
        private static readonly Dictionary<string, string> _tagByPrefab = new Dictionary<string, string>();

        // Keep strong references so the runtime-created ScriptableObjects/Materials aren't collected.
        private static readonly List<UnityEngine.Object> _keepAlive = new List<UnityEngine.Object>();

        // Inline sprite tag for the icon of the socketed item identified by sourcePrefab, or "" when
        // unavailable (empty prefab, prefab/icon not found, no shader, or build failed). The "" result is
        // cached too so a missing prefab isn't retried every tooltip frame.
        public static string GetSpriteTag(string sourcePrefab) {
            if (string.IsNullOrEmpty(sourcePrefab)) {
                return string.Empty;
            }

            if (_tagByPrefab.TryGetValue(sourcePrefab, out var cached)) {
                return cached;
            }

            var tag = BuildTag(sourcePrefab);
            _tagByPrefab[sourcePrefab] = tag;
            return tag;
        }

        private static string BuildTag(string sourcePrefab) {
            EnsureInitialized();
            if (_shader == null) {
                return string.Empty;
            }

            try {
                var sprite = PrefabManager.Instance.GetPrefab(sourcePrefab)
                    ?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
                if (sprite == null) {
                    return string.Empty;
                }

                return BuildSpriteAsset(sourcePrefab, sprite, _shader)
                    ? $"<sprite=\"{AssetNamePrefix}{sourcePrefab}\" index=0>"
                    : string.Empty;
            } catch (Exception e) {
                EpicLoot.LogWarning($"Failed to build socket tooltip icon for '{sourcePrefab}': {e.Message}");
                return string.Empty;
            }
        }

        private static void EnsureInitialized() {
            if (_initialized) {
                return;
            }
            _initialized = true;

            _shader = ResolveSpriteShader();
            if (_shader == null) {
                EpicLoot.LogWarning("Socket tooltip icons disabled: TextMeshPro/Sprite shader not found.");
            }
        }

        private static Shader ResolveSpriteShader() {
            var shader = Shader.Find("TextMeshPro/Sprite");
            if (shader != null) {
                return shader;
            }

            // Fall back to whatever the default sprite asset uses, if the named shader isn't in the build.
            var defaultSprite = TMP_Settings.defaultSpriteAsset;
            return defaultSprite != null && defaultSprite.material != null ? defaultSprite.material.shader : null;
        }

        // Constructs and registers a single-sprite TMP_SpriteAsset for one source prefab's icon, keyed by
        // that prefab name. Returns true on success. Referencing the sprite's own texture as the sprite
        // sheet (with the sprite's textureRect as the glyph rect) keeps the icon on the GPU -- no pixels
        // are ever read back.
        private static bool BuildSpriteAsset(string key, Sprite sprite, Shader shader) {
            var texture = sprite.texture;
            if (texture == null) {
                return false;
            }

            var rect = sprite.textureRect;
            int w = Mathf.RoundToInt(rect.width);
            int h = Mathf.RoundToInt(rect.height);
            if (w <= 0 || h <= 0) {
                return false;
            }

            var assetName = AssetNamePrefix + key;

            var material = new Material(shader) { name = assetName + "_Mat", mainTexture = texture };

            var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            spriteAsset.name = assetName;
            // Register under the SAME hash the rich-text parser uses to resolve <sprite="assetName" ...>.
            // ValidateHtmlTag hashes the name attribute case-insensitively (ToUpperFast per char), which is
            // GetHashCode -- NOT the case-sensitive GetSimpleHashCode. Using the wrong one makes the lookup
            // miss and TMP prints the tag verbatim instead of drawing the sprite.
            spriteAsset.hashCode = TMP_TextUtilities.GetHashCode(assetName);
            spriteAsset.spriteSheet = texture;
            spriteAsset.material = material;
            spriteAsset.materialHashCode = TMP_TextUtilities.GetSimpleHashCode(material.name);
            // A minimal, self-consistent face: pointSize == glyph height keeps the sprite rendering at
            // roughly the surrounding font size, sitting on the text baseline.
            spriteAsset.faceInfo = new FaceInfo {
                familyName = assetName,
                pointSize = h,
                scale = 1f,
                lineHeight = h,
                ascentLine = h,
                capLine = h,
                meanLine = h * 0.5f,
                baseline = 0f,
                descentLine = 0f
            };

            // A runtime-created sprite asset leaves the legacy spriteInfoList null (it is a plain public
            // field with no [SerializeField], unlike the glyph/character tables), and TMP's one-time
            // UpgradeSpriteAsset() dereferences spriteInfoList.Count -- so initialize it first or the call
            // below throws a NullReferenceException.
            if (spriteAsset.spriteInfoList == null) {
                spriteAsset.spriteInfoList = new List<TMP_Sprite>();
            }

            // Initialize while the tables are still empty. A freshly created sprite asset has an empty
            // version string, so this first UpdateLookupTables() runs TMP's one-time UpgradeSpriteAsset()
            // (harmless on the empty tables/spriteInfoList) and, crucially, stamps the version. If we
            // populated the tables first, that upgrade would CLEAR them (rebuilding from the empty legacy
            // spriteInfoList) and the <sprite> would render nothing. With the version now set, the second
            // UpdateLookupTables() below skips the upgrade and simply builds the lookups from the
            // glyph/character we add. This keeps us on public TMP API only -- no reflection.
            spriteAsset.UpdateLookupTables();

            // The glyph/character tables have internal setters, but their getters return the (initialized)
            // backing lists, so we just add to them -- no reflection needed.
            var glyphTable = spriteAsset.spriteGlyphTable;
            var characterTable = spriteAsset.spriteCharacterTable;
            if (glyphTable == null || characterTable == null) {
                return false;
            }

            var glyph = new TMP_SpriteGlyph(
                0,
                new GlyphMetrics(w, h, 0f, h, w),
                new GlyphRect(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), w, h),
                1f,
                0) {
                sprite = sprite
            };
            glyphTable.Add(glyph);

            characterTable.Add(new TMP_SpriteCharacter(0xE000, spriteAsset, glyph) {
                name = key,
                glyphIndex = 0,
                scale = 1f
            });

            // Second pass: version is already stamped, so this only builds the glyph/character lookups from
            // the entries we just added (no destructive upgrade).
            spriteAsset.UpdateLookupTables();

            // Register so <sprite="assetName"> resolves on any TMP tooltip surface without touching each
            // component. Keyed by the asset's name hash, matching TMP's rich-text name lookup.
            MaterialReferenceManager.AddSpriteAsset(spriteAsset);

            _keepAlive.Add(material);
            _keepAlive.Add(spriteAsset);
            return true;
        }
    }
}
