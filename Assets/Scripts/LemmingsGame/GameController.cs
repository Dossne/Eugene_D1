using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hakaton.Lemmings
{
    public sealed class GameController : MonoBehaviour
    {
        private const int LemmingsPerLevel = 10;
        private const float SpawnDelay = 2f;
        private const float SpawnInterval = 1f;

        private readonly List<LemmingAgent> lemmings = new List<LemmingAgent>();

        private Camera mainCamera;
        private Canvas canvas;
        private Text savedText;
        private Text deadText;
        private Text introText;
        private Button pickaxeButton;
        private Image pickaxeButtonImage;
        private GameObject popupPanel;
        private Text popupMessageText;
        private Button popupButton;
        private Text popupButtonText;

        private GameObject levelRoot;
        private TerrainMap terrainMap;
        private ParsedLevel currentLevel;
        private Rect exitRect;
        private bool selectingPickaxeTarget;
        private bool levelResolved;
        private int currentLevelIndex;
        private int savedCount;
        private int deadCount;
        private int spawnedCount;
        private Coroutine spawnRoutine;
        private Coroutine introRoutine;

        private void Start()
        {
            mainCamera = Camera.main;
            BuildUi();
            LoadLevel(0);
        }

        private void Update()
        {
            if (currentLevel == null || levelResolved)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            for (int index = lemmings.Count - 1; index >= 0; index--)
            {
                LemmingAgent lemming = lemmings[index];
                if (lemming == null || !lemming.IsAlive)
                {
                    lemmings.RemoveAt(index);
                    continue;
                }

                lemming.Step(deltaTime);

                if (IsOnSpikes(lemming.BoundsRect))
                {
                    deadCount++;
                    lemming.Kill();
                    lemmings.RemoveAt(index);
                    RefreshCounters();
                    continue;
                }

                if (exitRect.Overlaps(lemming.BoundsRect))
                {
                    savedCount++;
                    lemming.Save();
                    lemmings.RemoveAt(index);
                    RefreshCounters();
                }
            }

            HandleSelectionInput();
            terrainMap.ApplyPendingVisuals();
            EvaluateLevelState();
        }

        private void LoadLevel(int levelIndex)
        {
            currentLevelIndex = levelIndex;
            currentLevel = LevelDatabase.GetLevel(levelIndex);
            levelResolved = false;
            selectingPickaxeTarget = false;
            savedCount = 0;
            deadCount = 0;
            spawnedCount = 0;
            RefreshCounters();
            HidePopup();

            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
            }

            if (introRoutine != null)
            {
                StopCoroutine(introRoutine);
            }

            if (levelRoot != null)
            {
                Destroy(levelRoot);
            }

            lemmings.Clear();

            levelRoot = new GameObject($"Level_{currentLevel.Number}");
            terrainMap = new TerrainMap(currentLevel, levelRoot.transform);
            BuildLevelDecorations(levelRoot.transform, currentLevel);
            ConfigureCamera(currentLevel);
            pickaxeButtonImage.color = new Color32(222, 176, 69, 255);

            spawnRoutine = StartCoroutine(SpawnLemmings());
            introRoutine = StartCoroutine(PlayLevelIntro(currentLevel.Number));
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("UI");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Font font = PixelArtFactory.GetUIFont();

            savedText = CreateText("SavedText", canvas.transform, font, 38, TextAnchor.UpperLeft);
            RectTransform savedRect = savedText.rectTransform;
            savedRect.anchorMin = new Vector2(0f, 1f);
            savedRect.anchorMax = new Vector2(0f, 1f);
            savedRect.pivot = new Vector2(0f, 1f);
            savedRect.anchoredPosition = new Vector2(30f, -30f);
            savedRect.sizeDelta = new Vector2(420f, 60f);

            deadText = CreateText("DeadText", canvas.transform, font, 38, TextAnchor.UpperLeft);
            RectTransform deadRect = deadText.rectTransform;
            deadRect.anchorMin = new Vector2(0f, 1f);
            deadRect.anchorMax = new Vector2(0f, 1f);
            deadRect.pivot = new Vector2(0f, 1f);
            deadRect.anchoredPosition = new Vector2(30f, -82f);
            deadRect.sizeDelta = new Vector2(420f, 60f);

            pickaxeButton = CreateButton("PickaxeButton", canvas.transform, new Color32(222, 176, 69, 255), font, string.Empty);
            RectTransform pickaxeRect = (RectTransform)pickaxeButton.transform;
            pickaxeRect.anchorMin = new Vector2(0.5f, 0f);
            pickaxeRect.anchorMax = new Vector2(0.5f, 0f);
            pickaxeRect.pivot = new Vector2(0.5f, 0f);
            pickaxeRect.anchoredPosition = new Vector2(0f, 34f);
            pickaxeRect.sizeDelta = new Vector2(180f, 120f);
            pickaxeButtonImage = pickaxeButton.GetComponent<Image>();
            pickaxeButton.onClick.AddListener(TogglePickaxeSelection);

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(pickaxeButton.transform, false);
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = PixelArtFactory.CreatePickaxeSprite();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(80f, 80f);

            introText = CreateText("IntroText", canvas.transform, font, 82, TextAnchor.MiddleCenter);
            introText.color = new Color(1f, 1f, 1f, 0f);
            RectTransform introRect = introText.rectTransform;
            introRect.anchorMin = new Vector2(0.5f, 0.5f);
            introRect.anchorMax = new Vector2(0.5f, 0.5f);
            introRect.pivot = new Vector2(0.5f, 0.5f);
            introRect.sizeDelta = new Vector2(760f, 200f);

            popupPanel = CreatePanel("PopupPanel", canvas.transform, new Color32(23, 29, 45, 238));
            RectTransform popupRect = (RectTransform)popupPanel.transform;
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(540f, 960f);

            popupMessageText = CreateText("PopupMessage", popupPanel.transform, font, 56, TextAnchor.MiddleCenter);
            RectTransform popupMessageRect = popupMessageText.rectTransform;
            popupMessageRect.anchorMin = new Vector2(0f, 0.5f);
            popupMessageRect.anchorMax = new Vector2(1f, 1f);
            popupMessageRect.offsetMin = new Vector2(24f, -24f);
            popupMessageRect.offsetMax = new Vector2(-24f, -24f);

            popupButton = CreateButton("PopupButton", popupPanel.transform, new Color32(68, 180, 92, 255), font, "Next");
            RectTransform popupButtonRect = (RectTransform)popupButton.transform;
            popupButtonRect.anchorMin = new Vector2(0.5f, 0f);
            popupButtonRect.anchorMax = new Vector2(0.5f, 0f);
            popupButtonRect.pivot = new Vector2(0.5f, 0f);
            popupButtonRect.anchoredPosition = new Vector2(0f, 60f);
            popupButtonRect.sizeDelta = new Vector2(260f, 110f);
            popupButtonText = popupButton.GetComponentInChildren<Text>();

            popupPanel.SetActive(false);
        }

        private void BuildLevelDecorations(Transform parent, ParsedLevel level)
        {
            GameObject spawnObject = new GameObject("Spawn");
            spawnObject.transform.SetParent(parent, false);
            SpriteRenderer spawnRenderer = spawnObject.AddComponent<SpriteRenderer>();
            spawnRenderer.sprite = PixelArtFactory.CreateSpawnSprite();
            spawnRenderer.sortingOrder = 3;
            spawnObject.transform.position = new Vector3(level.SpawnPoint.x, level.SpawnPoint.y, 0f);

            GameObject exitObject = new GameObject("Exit");
            exitObject.transform.SetParent(parent, false);
            SpriteRenderer exitRenderer = exitObject.AddComponent<SpriteRenderer>();
            exitRenderer.sprite = PixelArtFactory.CreateExitSprite();
            exitRenderer.sortingOrder = 3;
            exitObject.transform.position = new Vector3(level.ExitCell.x, level.ExitCell.y, 0f);
            exitRect = new Rect(level.ExitCell.x - 0.35f, level.ExitCell.y, 0.7f, 0.82f);

            for (int i = 0; i < level.Spikes.Count; i++)
            {
                SpikeDefinition spike = level.Spikes[i];
                GameObject spikeObject = new GameObject($"Spike_{i}");
                spikeObject.transform.SetParent(parent, false);
                SpriteRenderer renderer = spikeObject.AddComponent<SpriteRenderer>();
                renderer.sprite = PixelArtFactory.CreateSpikeSprite(spike.Orientation);
                renderer.sortingOrder = 4;
                spikeObject.transform.position = new Vector3(spike.Rect.xMin, spike.Rect.yMin, 0f);
                spikeObject.transform.localScale = new Vector3(spike.Rect.width, spike.Rect.height, 1f);
            }
        }

        private void ConfigureCamera(ParsedLevel level)
        {
            float aspect = (float)Screen.width / Mathf.Max(1f, Screen.height);
            float halfHeight = level.HeightCells * 0.5f + 0.7f;
            float halfWidth = level.WidthCells * 0.5f + 0.6f;
            mainCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.1f, aspect));
            mainCamera.transform.position = new Vector3(level.WidthCells * 0.5f, level.HeightCells * 0.5f, -10f);
        }

        private IEnumerator SpawnLemmings()
        {
            yield return new WaitForSeconds(SpawnDelay);
            while (spawnedCount < LemmingsPerLevel)
            {
                SpawnSingleLemming();
                spawnedCount++;
                if (spawnedCount < LemmingsPerLevel)
                {
                    yield return new WaitForSeconds(SpawnInterval);
                }
            }
        }

        private void SpawnSingleLemming()
        {
            GameObject lemmingObject = new GameObject($"Lemming_{spawnedCount + 1}");
            lemmingObject.transform.SetParent(levelRoot.transform, false);
            LemmingAgent lemming = lemmingObject.AddComponent<LemmingAgent>();
            lemming.Initialize(terrainMap, currentLevel.SpawnPoint);
            lemmings.Add(lemming);
            RefreshHighlights();
        }

        private IEnumerator PlayLevelIntro(int levelNumber)
        {
            introText.text = $"Level {levelNumber}";
            yield return FadeText(0f, 1f, 0.5f);
            yield return new WaitForSeconds(1.5f);
            yield return FadeText(1f, 0f, 0.5f);
        }

        private IEnumerator FadeText(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(from, to, elapsed / duration);
                introText.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            introText.color = new Color(1f, 1f, 1f, to);
        }

        private void TogglePickaxeSelection()
        {
            selectingPickaxeTarget = !selectingPickaxeTarget;
            pickaxeButtonImage.color = selectingPickaxeTarget ? new Color32(244, 205, 111, 255) : new Color32(222, 176, 69, 255);
            RefreshHighlights();
        }

        private void HandleSelectionInput()
        {
            if (!selectingPickaxeTarget)
            {
                return;
            }

            if (!TryGetPointerDown(out Vector2 screenPosition, out int fingerId))
            {
                return;
            }

            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
            if (TryAssignPickaxeAtWorldPoint(worldPoint))
            {
                return;
            }

            if (IsPointerOverUi(fingerId))
            {
                return;
            }
        }

        private bool TryAssignPickaxeAtWorldPoint(Vector2 worldPoint)
        {
            for (int i = lemmings.Count - 1; i >= 0; i--)
            {
                LemmingAgent lemming = lemmings[i];
                if (lemming == null || lemming.IsDigging || !lemming.IsAlive)
                {
                    continue;
                }

                if (!lemming.ContainsWorldPoint(worldPoint))
                {
                    continue;
                }

                lemming.AssignPickaxe();
                selectingPickaxeTarget = false;
                pickaxeButtonImage.color = new Color32(222, 176, 69, 255);
                RefreshHighlights();
                return true;
            }

            return false;
        }

        private void EvaluateLevelState()
        {
            if (savedCount >= LemmingsPerLevel)
            {
                levelResolved = true;
                ShowWinPopup();
                return;
            }

            if (spawnedCount >= LemmingsPerLevel && lemmings.Count == 0 && savedCount < LemmingsPerLevel)
            {
                levelResolved = true;
                ShowLosePopup();
            }
        }

        private bool IsOnSpikes(Rect bounds)
        {
            IReadOnlyList<SpikeDefinition> spikes = currentLevel.Spikes;
            for (int i = 0; i < spikes.Count; i++)
            {
                if (spikes[i].Rect.Overlaps(bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowWinPopup()
        {
            popupPanel.SetActive(true);
            if (currentLevelIndex < LevelDatabase.Count - 1)
            {
                popupMessageText.text = "You win!";
                popupButtonText.text = "Next Level";
                SetPopupButtonColor(new Color32(68, 180, 92, 255));
                popupButton.onClick.RemoveAllListeners();
                popupButton.onClick.AddListener(() => LoadLevel(currentLevelIndex + 1));
            }
            else
            {
                popupMessageText.text = "You win the game!";
                popupButtonText.text = "Restart";
                SetPopupButtonColor(new Color32(68, 180, 92, 255));
                popupButton.onClick.RemoveAllListeners();
                popupButton.onClick.AddListener(() => LoadLevel(0));
            }
        }

        private void ShowLosePopup()
        {
            popupPanel.SetActive(true);
            popupMessageText.text = "You Lost!";
            popupButtonText.text = "Try Again";
            SetPopupButtonColor(new Color32(191, 71, 71, 255));
            popupButton.onClick.RemoveAllListeners();
            popupButton.onClick.AddListener(() => LoadLevel(currentLevelIndex));
        }

        private void HidePopup()
        {
            popupPanel.SetActive(false);
        }

        private void SetPopupButtonColor(Color color)
        {
            Image buttonImage = popupButton.GetComponent<Image>();
            buttonImage.color = color;
        }

        private void RefreshCounters()
        {
            savedText.text = $"Saved lemmings: {savedCount}";
            deadText.text = $"Dead lemmings: {deadCount}";
        }

        private void RefreshHighlights()
        {
            for (int i = 0; i < lemmings.Count; i++)
            {
                LemmingAgent lemming = lemmings[i];
                if (lemming != null)
                {
                    lemming.SetHighlighted(selectingPickaxeTarget && !lemming.IsDigging);
                }
            }
        }

        private static Text CreateText(string name, Transform parent, Font font, int size, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Color32 color, Font font, string label)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            Button button = buttonObject.AddComponent<Button>();

            if (!string.IsNullOrEmpty(label))
            {
                Text buttonText = CreateText("Label", buttonObject.transform, font, 38, TextAnchor.MiddleCenter);
                buttonText.text = label;
                RectTransform buttonTextRect = buttonText.rectTransform;
                buttonTextRect.anchorMin = Vector2.zero;
                buttonTextRect.anchorMax = Vector2.one;
                buttonTextRect.offsetMin = Vector2.zero;
                buttonTextRect.offsetMax = Vector2.zero;
            }

            return button;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color32 color)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return panelObject;
        }

        private bool TryGetPointerDown(out Vector2 position, out int fingerId)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    position = touch.position;
                    fingerId = touch.fingerId;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                fingerId = -1;
                return true;
            }

            position = default;
            fingerId = -1;
            return false;
        }

        private static bool IsPointerOverUi(int fingerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return fingerId >= 0 ? EventSystem.current.IsPointerOverGameObject(fingerId) : EventSystem.current.IsPointerOverGameObject();
        }
    }
}
