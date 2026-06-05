#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Scorewriter.Editor
{
    [InitializeOnLoad]
    public static class ScorewriterNoteSpriteImporter
    {
        private static readonly string[] NoteFolders =
        {
            "Assets/Scorewriter/Resources/Images/Notes",
            "Assets/Images/Notes"
        };

        static ScorewriterNoteSpriteImporter()
        {
            EditorApplication.delayCall += ConfigureNoteSprites;
        }

        [MenuItem("Scorewriter/刷新 Note SVG Sprite")]
        public static void ConfigureNoteSprites()
        {
            bool changed = false;
            foreach (string folder in NoteFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                string[] guids = AssetDatabase.FindAssets("", new[] { folder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    changed |= ConfigureSvgImporter(path);
                }
            }

            if (changed)
                AssetDatabase.SaveAssets();
        }

        private static bool ConfigureSvgImporter(string path)
        {
            // Use the strongly-typed SVGImporter API. The previous version set the
            // enum via SerializedProperty.enumValueIndex = 1, but for SVGType the
            // value index 1 maps to VectorSprite (textureless) — NOT TexturedSprite.
            // That silently reverted the notes to a mesh-only sprite that UnityEngine.UI.Image
            // cannot render, so the vector graphics disappeared on every domain reload.
            var importer = AssetImporter.GetAtPath(path) as Unity.VectorGraphics.Editor.SVGImporter;
            if (importer == null)
                return false;

            bool changed = false;

            if (importer.SvgType != Unity.VectorGraphics.Editor.SVGType.TexturedSprite)
            {
                importer.SvgType = Unity.VectorGraphics.Editor.SVGType.TexturedSprite;
                changed = true;
            }

            // Keep the rasterized texture crisp at the sizes we display notes at.
            if (importer.TextureSize != 256)
            {
                importer.TextureSize = 256;
                changed = true;
            }

            // Preserve aspect (no stable public setter across versions -> serialized field).
            SerializedObject serializedImporter = new SerializedObject(importer);
            SerializedProperty preserveAspect = serializedImporter.FindProperty("m_PreserveSVGImageAspect") ?? serializedImporter.FindProperty("preserveSVGImageAspect");
            if (preserveAspect != null && preserveAspect.propertyType == SerializedPropertyType.Boolean && !preserveAspect.boolValue)
            {
                preserveAspect.boolValue = true;
                serializedImporter.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            if (!changed)
                return false;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log($"[Scorewriter] 已刷新 SVG Sprite (TexturedSprite): {path}");
            return true;
        }
    }
}
#endif
