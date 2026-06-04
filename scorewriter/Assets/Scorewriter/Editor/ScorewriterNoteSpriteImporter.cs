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
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer == null)
                return false;

            SerializedObject serializedImporter = new SerializedObject(importer);
            SerializedProperty svgType = serializedImporter.FindProperty("m_SvgType") ?? serializedImporter.FindProperty("svgType");
            bool changed = SetPropertyValue(svgType, 1);

            SerializedProperty preserveAspect = serializedImporter.FindProperty("m_PreserveSVGImageAspect") ?? serializedImporter.FindProperty("preserveSVGImageAspect");
            changed |= SetPropertyValue(preserveAspect, 1);

            if (!changed)
                return false;

            serializedImporter.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
            Debug.Log($"[Scorewriter] 已刷新 SVG Sprite: {path}");
            return true;
        }

        private static bool SetPropertyValue(SerializedProperty property, int value)
        {
            if (property == null)
                return false;

            if (property.propertyType == SerializedPropertyType.Enum)
            {
                if (property.enumValueIndex == value)
                    return false;

                property.enumValueIndex = value;
                return true;
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                if (property.intValue == value)
                    return false;

                property.intValue = value;
                return true;
            }

            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                bool boolValue = value != 0;
                if (property.boolValue == boolValue)
                    return false;

                property.boolValue = boolValue;
                return true;
            }

            return false;
        }
    }
}
#endif
