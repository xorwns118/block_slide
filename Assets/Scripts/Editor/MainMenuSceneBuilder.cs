#if UNITY_EDITOR
using System.Collections.Generic;
using GridShift.Core;
using GridShift.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GridShift.Editor
{
    public static class MainMenuSceneBuilder
    {
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string PlaySceneName = "PlayScene";
        private const string MenuRootName = "GridShiftMainMenu";

        [MenuItem("Tools/GridShift/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            GameObject menuRoot = null;

            try
            {
                GameObject existingRoot = GameObject.Find(MenuRootName);
                if (existingRoot != null)
                {
                    Object.DestroyImmediate(existingRoot);
                }

                EnsureEventSystem();

                menuRoot = new GameObject(MenuRootName);
                MainMenuController controller = menuRoot.AddComponent<MainMenuController>();

                Canvas canvas = CreateCanvas(menuRoot.transform);
                RectTransform panel = CreatePanel(canvas.transform);
                Text titleText = CreateText(panel, "TitleText", "GridShift", 72, TextAnchor.MiddleCenter);
                Text statusText = CreateText(panel, "StatusText", "Select a level.", 36, TextAnchor.MiddleCenter);
                RectTransform buttonRoot = CreateButtonRoot(panel);
                Button buttonTemplate = CreateButton(buttonRoot, "LevelButtonTemplate", "Level");
                Button playAllButton = CreateButton(panel, "PlayAllButton", "Play All Stages");

                buttonTemplate.gameObject.SetActive(false);

                PositionTitle(titleText.rectTransform);
                PositionStatus(statusText.rectTransform);
                PositionButtonRoot(buttonRoot);
                PositionPlayAll(playAllButton.GetComponent<RectTransform>());

                AssignController(controller, buttonRoot, buttonTemplate, playAllButton, statusText);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Selection.activeObject = menuRoot;
                Debug.Log("GridShift MainScene menu UI was built successfully.");
            }
            catch (System.Exception exception)
            {
                if (menuRoot != null)
                {
                    Object.DestroyImmediate(menuRoot);
                }

                Debug.LogError($"Failed to build GridShift MainScene menu UI: {exception}");
            }
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static RectTransform CreatePanel(Transform parent)
        {
            GameObject panelObject = new GameObject("MenuPanel", typeof(RectTransform), typeof(CanvasRenderer));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panelObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.11f, 1f);

            return rect;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            textObject.transform.SetParent(parent, false);

            Text textComponent = textObject.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = GetBuiltinFont();
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;

            return textComponent;
        }

        private static Font GetBuiltinFont()
        {
            Font font = TryLoadBuiltinFont("LegacyRuntime.ttf");
            if (font == null)
            {
                font = TryLoadBuiltinFont("Arial.ttf");
            }

            if (font == null)
            {
                Debug.LogWarning("No built-in UI font could be loaded. Menu text objects were created without an assigned font.");
            }

            return font;
        }

        private static Font TryLoadBuiltinFont(string path)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(path);
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

        private static RectTransform CreateButtonRoot(Transform parent)
        {
            GameObject rootObject = new GameObject("LevelButtonRoot", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);

            RectTransform rect = rootObject.GetComponent<RectTransform>();
            VerticalLayoutGroup layout = rootObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = rootObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rect;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420f, 64f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.24f, 0.32f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.25f, 0.35f, 0.45f, 1f);
            colors.pressedColor = new Color(0.12f, 0.18f, 0.26f, 1f);
            button.colors = colors;

            Text text = CreateText(buttonObject.transform, "Text", label, 40, TextAnchor.MiddleCenter);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private static void PositionTitle(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -96f);
            rect.sizeDelta = new Vector2(900f, 120f);
        }

        private static void PositionStatus(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -182f);
            rect.sizeDelta = new Vector2(900f, 72f);
        }

        private static void PositionButtonRoot(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -260f);
            rect.sizeDelta = new Vector2(420f, 520f);
        }

        private static void PositionPlayAll(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 96f);
            rect.sizeDelta = new Vector2(420f, 64f);
        }

        private static void AssignController(
            MainMenuController controller,
            Transform buttonRoot,
            Button buttonTemplate,
            Button playAllButton,
            Text statusText)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty levelsProperty = serializedController.FindProperty("availableLevels");
            List<LevelData> levels = LoadLevelAssets();
            levelsProperty.arraySize = levels.Count;
            for (int i = 0; i < levels.Count; i++)
            {
                levelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            }

            serializedController.FindProperty("playSceneName").stringValue = PlaySceneName;
            serializedController.FindProperty("levelButtonRoot").objectReferenceValue = buttonRoot;
            serializedController.FindProperty("levelButtonPrefab").objectReferenceValue = buttonTemplate;
            serializedController.FindProperty("playAllButton").objectReferenceValue = playAllButton;
            serializedController.FindProperty("statusText").objectReferenceValue = statusText;
            serializedController.ApplyModifiedProperties();
        }

        private static List<LevelData> LoadLevelAssets()
        {
            List<LevelData> levels = new List<LevelData>();
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/ScriptableObjects" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                {
                    levels.Add(level);
                }
            }

            levels.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return levels;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
#endif
