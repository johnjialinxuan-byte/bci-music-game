using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Scorewriter
{
    [RequireComponent(typeof(ScorewriterCriAudioPlayer))]
    public sealed class ScorewriterApp : MonoBehaviour
    {
        private const float TimelinePixelsPerSecond = 92f;
        private const float LaneWidth = 128f;
        private const float MarkerSize = 34f;
        private const float HoldLineWidth = 12f;

        private readonly List<ScorewriterSong> songs = new List<ScorewriterSong>();
        private readonly int[] snapDivisions = { 4, 6, 8, 16, 0 };

        private ScorewriterCriAudioPlayer audioPlayer;
        private ScorewriterChart chart;
        private ScorewriterNote selectedNote;
        private ScorewriterNoteKind placementKind = ScorewriterNoteKind.Hold;
        private ScorewriterLane placementStartLane = ScorewriterLane.TopLeft;
        private ScorewriterLane placementEndLane = ScorewriterLane.TopLeft;
        private int songIndex;
        private int snapIndex = 3;
        private float currentTime;
        private bool followPlayback = true;
        private bool suppressSliderEvent;
        private bool autoPlayOnStart = true;

        private Font font;
        private Sprite whiteSprite;
        private Sprite circleSprite;

        private RectTransform timelineViewport;
        private RectTransform timelineContent;
        private RectTransform timelineGridLayer;
        private RectTransform timelineNoteLayer;
        private RectTransform playhead;
        private ScrollRect timelineScroll;
        private RectTransform previewSurface;
        private RectTransform previewNoteLayer;
        private Slider timeSlider;

        private Text songTitleText;
        private Text timeText;
        private Text statusText;
        private Text typeButtonText;
        private Text startLaneButtonText;
        private Text endLaneButtonText;
        private Text snapButtonText;
        private Text playButtonText;
        private Text followButtonText;
        private Text selectedText;
        private InputField bpmInput;
        private InputField lengthInput;
        private InputField offsetInput;
        private InputField durationInput;
        private InputField thresholdInput;
        private Toggle tailSlideToggle;
        private Toggle quarterGridToggle;
        private Toggle sixthGridToggle;
        private Toggle eighthGridToggle;
        private Toggle sixteenthGridToggle;

        private ScorewriterSong CurrentSong => songs[Mathf.Clamp(songIndex, 0, songs.Count - 1)];
        private float SongLength => Mathf.Max(8f, chart?.songLength ?? CurrentSong.songLength);
        private float ChartOffsetSeconds => (chart?.timingOffsetMs ?? 0f) / 1000f;

        private void Awake()
        {
            audioPlayer = GetComponent<ScorewriterCriAudioPlayer>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            whiteSprite = CreateSolidSprite();
            circleSprite = CreateCircleSprite(64);

            BuildSongList();
            BuildInterface();
            LoadChartForCurrentSong(false);
            ApplySongToControls();
            RebuildTimeline();
            UpdatePlacementButtonLabels();
            SetStatus("已就绪。");
        }

        private void Start()
        {
            audioPlayer.EnsureInitialized(CurrentSong);
            if (autoPlayOnStart)
                StartPlaybackFromCurrentTime();
        }

        private void Update()
        {
            if (audioPlayer.IsPlaying || audioPlayer.IsPaused)
                currentTime = Mathf.Clamp(audioPlayer.CurrentTime + ChartOffsetSeconds, 0f, SongLength);

            if (audioPlayer.IsPlaying && currentTime >= SongLength - 0.02f)
            {
                audioPlayer.Stop();
                currentTime = 0f;
            }

            UpdateTransportUi();
            UpdatePlayhead();
            RenderPreview();

            if (followPlayback && audioPlayer.IsPlaying)
                CenterTimelineOnTime(currentTime);
        }

        public void HandleTimelineClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!TryTimelinePoint(eventData, out Vector2 localPoint))
                return;

            ScorewriterLane lane = LaneFromX(localPoint.x);
            float time = SnapTime(localPoint.y / TimelinePixelsPerSecond);
            AddNote(time, lane);
        }

        public void SelectNote(ScorewriterNote note)
        {
            selectedNote = note;
            if (selectedNote != null)
            {
                placementKind = selectedNote.kind;
                placementStartLane = selectedNote.startLane;
                placementEndLane = selectedNote.endLane;
                durationInput.SetTextWithoutNotify(FormatFloat(selectedNote.duration));
                thresholdInput.SetTextWithoutNotify(selectedNote.threshold.ToString(CultureInfo.InvariantCulture));
                tailSlideToggle.SetIsOnWithoutNotify(selectedNote.hasTailSlide);
            }

            UpdatePlacementButtonLabels();
            RefreshNotes();
            UpdateSelectedLabel();
        }

        public void HandleNoteDrag(ScorewriterNote note, bool editsTail, PointerEventData eventData)
        {
            if (note == null || !TryTimelinePoint(eventData, out Vector2 localPoint))
                return;

            ScorewriterLane lane = LaneFromX(localPoint.x);
            float time = Mathf.Clamp(SnapTime(localPoint.y / TimelinePixelsPerSecond), 0f, SongLength);

            if (editsTail)
            {
                note.endLane = lane;
                note.duration = Mathf.Max(GetSnapStep(), time - note.time);
                placementEndLane = note.endLane;
            }
            else
            {
                float endTime = note.EndTime;
                note.time = time;
                note.startLane = lane;
                if (note.kind == ScorewriterNoteKind.Hold)
                    note.duration = Mathf.Max(GetSnapStep(), endTime - note.time);
                placementStartLane = note.startLane;
            }

            selectedNote = note;
            durationInput.SetTextWithoutNotify(FormatFloat(note.duration));
            UpdatePlacementButtonLabels();
            UpdateSelectedLabel();
            RefreshNotes();
        }

        public void EndNoteDrag()
        {
            SortNotes();
            RefreshNotes();
        }

        private void BuildSongList()
        {
            songs.Clear();
            songs.Add(new ScorewriterSong
            {
                songId = "0_oyasumi",
                title = "Oyasumi, Mother Goose",
                artist = "Karasu Producer",
                bpm = 120f,
                songLength = 150f,
                cueSheetName = "oyasumi",
                cueName = "",
                acbAssetPath = "CRI/Public/WorkUnit_0/oyasumi.acb"
            });
            songs.Add(new ScorewriterSong
            {
                songId = "song1",
                title = "A Forever Friend",
                artist = "Unknown",
                bpm = 120f,
                songLength = 150f,
                cueSheetName = "song1",
                cueName = "A Forever Friend",
                acbAssetPath = "CRI/Public/WorkUnit_0/song1.acb"
            });
            songs.Add(new ScorewriterSong
            {
                songId = "2077",
                title = "City of Night",
                artist = "Unknown",
                bpm = 120f,
                songLength = 150f,
                cueSheetName = "song2",
                cueName = "City of Night",
                acbAssetPath = "CRI/Public/WorkUnit_0/song2.acb"
            });
            songs.Add(new ScorewriterSong
            {
                songId = "jumping",
                title = "Cute Jump",
                artist = "Unknown",
                bpm = 120f,
                songLength = 150f,
                cueSheetName = "song3",
                cueName = "Cute Jump",
                acbAssetPath = "CRI/Public/WorkUnit_0/song3.acb"
            });
            songs.Add(new ScorewriterSong
            {
                songId = "song4",
                title = "Lost in the Phantom Night",
                artist = "Unknown",
                bpm = 120f,
                songLength = 150f,
                cueSheetName = "song4",
                cueName = "Lost in the Phantom Night(1)",
                acbAssetPath = "CRI/Public/WorkUnit_0/song4.acb"
            });
            songs.Add(new ScorewriterSong
            {
                songId = "song5",
                title = "song5",
                artist = "Unknown",
                bpm = 120f,
                songLength = 150f,
                cueSheetName = "song5",
                cueName = "",
                acbAssetPath = "CRI/Public/WorkUnit_0/song5.acb"
            });
            songs.Add(new ScorewriterSong
            {
                songId = "kite",
                title = "Kite",
                artist = "Unknown",
                bpm = 120f,
                songLength = 150f,
                cueSheetName = "song6",
                cueName = "F",
                acbAssetPath = "CRI/Public/WorkUnit_0/song6.acb"
            });
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject("ScorewriterCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main ?? FindAnyObjectByType<Camera>();
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            RectTransform root = CreateRect("Root", canvasRect);
            Stretch(root);
            AddImage(root.gameObject, new Color(0.055f, 0.062f, 0.075f, 1f));
            VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 10f;
            rootLayout.padding = new RectOffset(14, 14, 12, 12);
            rootLayout.childControlHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childForceExpandWidth = true;

            BuildHeader(root);
            BuildBody(root);
            BuildTransport(root);
        }

        private void BuildHeader(RectTransform root)
        {
            RectTransform header = CreateRect("Header", root);
            AddImage(header.gameObject, new Color(0.09f, 0.105f, 0.125f, 1f));
            SetLayout(header, -1f, 66f, 0f, 0f);
            HorizontalLayoutGroup layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = false;

            CreateLabel(header, "制谱器", 24, TextAnchor.MiddleLeft, 110f, 42f, new Color(0.86f, 0.91f, 1f, 1f));
            CreateButton(header, "<", 42f, 42f, () => SwitchSong(-1));
            songTitleText = CreateLabel(header, "", 18, TextAnchor.MiddleLeft, 270f, 42f, Color.white);
            CreateButton(header, ">", 42f, 42f, () => SwitchSong(1));

            CreateLabel(header, "BPM", 14, TextAnchor.MiddleRight, 46f, 42f, new Color(0.72f, 0.77f, 0.84f, 1f));
            bpmInput = CreateInput(header, "120", 78f, 42f, OnBpmEdited);
            CreateLabel(header, "长度", 14, TextAnchor.MiddleRight, 46f, 42f, new Color(0.72f, 0.77f, 0.84f, 1f));
            lengthInput = CreateInput(header, "150", 82f, 42f, OnLengthEdited);
            CreateLabel(header, "Offset(ms)", 14, TextAnchor.MiddleRight, 82f, 42f, new Color(0.72f, 0.77f, 0.84f, 1f));
            offsetInput = CreateInput(header, "0", 86f, 42f, OnOffsetEdited);
            CreateButton(header, "加载", 70f, 42f, () => LoadChartForCurrentSong(true));
            CreateButton(header, "保存", 70f, 42f, SaveEditorChart);
            CreateButton(header, "导出JSON", 112f, 42f, ExportGameJson);
        }

        private void BuildBody(RectTransform root)
        {
            RectTransform body = CreateRect("Body", root);
            SetLayout(body, -1f, -1f, 1f, 1f);
            HorizontalLayoutGroup bodyLayout = body.gameObject.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 12f;
            bodyLayout.childControlHeight = true;
            bodyLayout.childControlWidth = true;
            bodyLayout.childForceExpandHeight = true;
            bodyLayout.childForceExpandWidth = false;

            BuildTimelinePanel(body);
            BuildPreviewPanel(body);
        }

        private void BuildTimelinePanel(RectTransform body)
        {
            RectTransform panel = CreateRect("TimelinePanel", body);
            AddImage(panel.gameObject, new Color(0.078f, 0.086f, 0.103f, 1f));
            SetLayout(panel, 690f, -1f, 0f, 1f);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            RectTransform tools = CreateRect("TimelineTools", panel);
            SetLayout(tools, -1f, 48f, 0f, 0f);
            HorizontalLayoutGroup toolsLayout = tools.gameObject.AddComponent<HorizontalLayoutGroup>();
            toolsLayout.spacing = 8f;
            toolsLayout.childAlignment = TextAnchor.MiddleLeft;
            toolsLayout.childControlHeight = true;
            toolsLayout.childControlWidth = false;

            Button typeButton = CreateButton(tools, "", 116f, 42f, CyclePlacementKind);
            typeButtonText = typeButton.GetComponentInChildren<Text>();
            Button startButton = CreateButton(tools, "", 94f, 42f, CycleStartLane);
            startLaneButtonText = startButton.GetComponentInChildren<Text>();
            Button endButton = CreateButton(tools, "", 94f, 42f, CycleEndLane);
            endLaneButtonText = endButton.GetComponentInChildren<Text>();
            Button snapButton = CreateButton(tools, "", 104f, 42f, CycleSnap);
            snapButtonText = snapButton.GetComponentInChildren<Text>();
            CreateButton(tools, "添加", 68f, 42f, AddNoteAtCurrentTime);
            CreateButton(tools, "删除", 82f, 42f, DeleteSelectedNote);

            RectTransform fields = CreateRect("NoteFields", panel);
            SetLayout(fields, -1f, 44f, 0f, 0f);
            HorizontalLayoutGroup fieldsLayout = fields.gameObject.AddComponent<HorizontalLayoutGroup>();
            fieldsLayout.spacing = 8f;
            fieldsLayout.childControlHeight = true;
            fieldsLayout.childControlWidth = false;

            CreateLabel(fields, "时长", 13, TextAnchor.MiddleRight, 48f, 36f, new Color(0.75f, 0.80f, 0.86f, 1f));
            durationInput = CreateInput(fields, "1.0", 76f, 36f, OnDurationEdited);
            CreateLabel(fields, "阈值", 13, TextAnchor.MiddleRight, 48f, 36f, new Color(0.75f, 0.80f, 0.86f, 1f));
            thresholdInput = CreateInput(fields, "10", 64f, 36f, OnThresholdEdited);
            tailSlideToggle = CreateToggle(fields, "尾部滑动", false, OnTailSlideChanged, 118f, 36f);
            selectedText = CreateLabel(fields, "未选择音符", 13, TextAnchor.MiddleLeft, 260f, 36f, new Color(0.73f, 0.82f, 0.95f, 1f));

            RectTransform gridFields = CreateRect("GridFields", panel);
            SetLayout(gridFields, -1f, 38f, 0f, 0f);
            HorizontalLayoutGroup gridLayout = gridFields.gameObject.AddComponent<HorizontalLayoutGroup>();
            gridLayout.spacing = 8f;
            gridLayout.childControlHeight = true;
            gridLayout.childControlWidth = false;
            CreateLabel(gridFields, "网格显示", 13, TextAnchor.MiddleRight, 72f, 34f, new Color(0.75f, 0.80f, 0.86f, 1f));
            quarterGridToggle = CreateToggle(gridFields, "1/4", true, value => SetGridVisibility(4, value), 80f, 34f);
            sixthGridToggle = CreateToggle(gridFields, "1/6", true, value => SetGridVisibility(6, value), 80f, 34f);
            eighthGridToggle = CreateToggle(gridFields, "1/8", true, value => SetGridVisibility(8, value), 80f, 34f);
            sixteenthGridToggle = CreateToggle(gridFields, "1/16", true, value => SetGridVisibility(16, value), 90f, 34f);

            BuildTimelineScroll(panel);
        }

        private void BuildTimelineScroll(RectTransform panel)
        {
            RectTransform scrollRoot = CreateRect("TimelineScroll", panel);
            SetLayout(scrollRoot, -1f, -1f, 1f, 1f);
            AddImage(scrollRoot.gameObject, new Color(0.035f, 0.039f, 0.047f, 1f));
            timelineScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            timelineScroll.horizontal = false;
            timelineScroll.vertical = true;
            timelineScroll.movementType = ScrollRect.MovementType.Clamped;
            timelineScroll.scrollSensitivity = 34f;

            timelineViewport = CreateRect("Viewport", scrollRoot);
            Stretch(timelineViewport);
            Image viewportImage = AddImage(timelineViewport.gameObject, new Color(0.025f, 0.028f, 0.034f, 1f));
            viewportImage.raycastTarget = true;
            Mask mask = timelineViewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            timelineScroll.viewport = timelineViewport;

            timelineContent = CreateRect("Content", timelineViewport);
            timelineContent.anchorMin = new Vector2(0f, 0f);
            timelineContent.anchorMax = new Vector2(0f, 0f);
            timelineContent.pivot = new Vector2(0f, 0f);
            timelineContent.anchoredPosition = Vector2.zero;
            timelineContent.gameObject.AddComponent<CanvasGroup>();
            Image contentImage = AddImage(timelineContent.gameObject, new Color(0f, 0f, 0f, 0f));
            contentImage.raycastTarget = true;
            timelineContent.gameObject.AddComponent<ScorewriterTimelineInput>().Bind(this);
            timelineScroll.content = timelineContent;

            timelineGridLayer = CreateLayer("Grid", timelineContent);
            timelineNoteLayer = CreateLayer("Notes", timelineContent);
            playhead = CreateRect("Playhead", timelineContent);
            playhead.anchorMin = new Vector2(0f, 0f);
            playhead.anchorMax = new Vector2(0f, 0f);
            playhead.pivot = new Vector2(0f, 0.5f);
            AddImage(playhead.gameObject, new Color(1f, 0.84f, 0.28f, 1f));
        }

        private void BuildPreviewPanel(RectTransform body)
        {
            RectTransform panel = CreateRect("PreviewPanel", body);
            AddImage(panel.gameObject, new Color(0.078f, 0.086f, 0.103f, 1f));
            SetLayout(panel, -1f, -1f, 1f, 1f);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            CreateLabel(panel, "预览", 22, TextAnchor.MiddleLeft, -1f, 34f, Color.white);
            previewSurface = CreateRect("PreviewSurface", panel);
            AddImage(previewSurface.gameObject, new Color(0.028f, 0.033f, 0.042f, 1f));
            SetLayout(previewSurface, -1f, -1f, 1f, 1f);
            BuildPreviewBase();
            previewNoteLayer = CreateRect("PreviewNotes", previewSurface);
            Stretch(previewNoteLayer);

            statusText = CreateLabel(panel, "", 14, TextAnchor.MiddleLeft, -1f, 34f, new Color(0.76f, 0.84f, 0.94f, 1f));
        }

        private void BuildPreviewBase()
        {
            CreatePreviewQuadrant("左上", 0f, 0.5f, 0.5f, 1f, new Color(0.19f, 0.21f, 0.24f, 1f));
            CreatePreviewQuadrant("右上", 0.5f, 0.5f, 1f, 1f, new Color(0.13f, 0.23f, 0.25f, 1f));
            CreatePreviewQuadrant("左下", 0f, 0f, 0.5f, 0.5f, new Color(0.12f, 0.16f, 0.28f, 1f));
            CreatePreviewQuadrant("右下", 0.5f, 0f, 1f, 0.5f, new Color(0.25f, 0.13f, 0.15f, 1f));
        }

        private void BuildTransport(RectTransform root)
        {
            RectTransform transport = CreateRect("Transport", root);
            AddImage(transport.gameObject, new Color(0.09f, 0.105f, 0.125f, 1f));
            SetLayout(transport, -1f, 76f, 0f, 0f);
            HorizontalLayoutGroup layout = transport.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = false;

            Button playButton = CreateButton(transport, "播放", 86f, 46f, TogglePlayback);
            playButtonText = playButton.GetComponentInChildren<Text>();
            CreateButton(transport, "停止", 72f, 46f, StopPlayback);
            CreateButton(transport, "-1s", 58f, 46f, () => SeekRelative(-1f));
            CreateButton(transport, "+1s", 58f, 46f, () => SeekRelative(1f));
            CreateButton(transport, "Offset -10", 92f, 46f, () => AdjustOffset(-10f));
            CreateButton(transport, "Offset +10", 92f, 46f, () => AdjustOffset(10f));
            Button followButton = CreateButton(transport, followPlayback ? "跟随开" : "跟随关", 104f, 46f, ToggleFollowPlayback);
            followButtonText = followButton.GetComponentInChildren<Text>();

            timeSlider = CreateSlider(transport, 500f, 46f);
            timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            timeText = CreateLabel(transport, "0.00 / 0.00", 16, TextAnchor.MiddleRight, 160f, 46f, Color.white);
        }

        private void RebuildTimeline()
        {
            if (timelineContent == null)
                return;

            float width = LaneWidth * 4f;
            float height = SongLength * TimelinePixelsPerSecond;
            timelineContent.sizeDelta = new Vector2(width, height);
            SetLayerSize(timelineGridLayer, width, height);
            SetLayerSize(timelineNoteLayer, width, height);
            playhead.sizeDelta = new Vector2(width, 3f);

            BuildGrid(width, height);
            RefreshNotes();
            UpdatePlayhead();
            Canvas.ForceUpdateCanvases();
            timelineScroll.verticalNormalizedPosition = Mathf.Clamp01(currentTime / SongLength);
        }

        private void BuildGrid(float width, float height)
        {
            ClearChildren(timelineGridLayer);
            Color[] laneColors =
            {
                new Color(1f, 1f, 1f, 0.035f),
                new Color(0.2f, 0.92f, 1f, 0.04f),
                new Color(0.24f, 0.43f, 1f, 0.04f),
                new Color(1f, 0.2f, 0.24f, 0.04f)
            };

            for (int i = 0; i < 4; i++)
            {
                RectTransform lane = CreateRect($"Lane_{i}", timelineGridLayer);
                lane.anchorMin = new Vector2(0f, 0f);
                lane.anchorMax = new Vector2(0f, 0f);
                lane.pivot = new Vector2(0f, 0f);
                lane.anchoredPosition = new Vector2(i * LaneWidth, 0f);
                lane.sizeDelta = new Vector2(LaneWidth, height);
                AddImage(lane.gameObject, laneColors[i]);

                Text label = CreateLabel(lane, ScorewriterLaneUtility.GetShortName((ScorewriterLane)i), 18, TextAnchor.LowerCenter, LaneWidth, 34f, new Color(1f, 1f, 1f, 0.72f));
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(0f, 0f);
                labelRect.pivot = new Vector2(0f, 0f);
                labelRect.anchoredPosition = new Vector2(0f, 34f);
            }

            for (int i = 0; i <= 4; i++)
                CreateGridLine(i * LaneWidth, true, new Color(1f, 1f, 1f, 0.16f), width, height, 2f, true);

            float beat = 60f / Mathf.Max(1f, chart.bpm);
            if (chart.showSixteenthGrid)
                DrawSubdivisionLines(16, beat, width, height, new Color(0.45f, 0.52f, 0.62f, 0.12f), 1f);
            if (chart.showEighthGrid)
                DrawSubdivisionLines(8, beat, width, height, new Color(0.56f, 0.66f, 0.82f, 0.16f), 1f);
            if (chart.showSixthGrid)
                DrawSubdivisionLines(6, beat, width, height, new Color(0.95f, 0.55f, 0.20f, 0.16f), 1f);
            if (chart.showQuarterGrid)
                DrawSubdivisionLines(4, beat, width, height, new Color(0.85f, 0.92f, 1f, 0.28f), 2f);

            float measure = beat * 4f;
            for (float t = 0f; t <= SongLength + 0.001f; t += measure)
            {
                float y = t * TimelinePixelsPerSecond;
                CreateHorizontalLine(y, width, 3f, new Color(1f, 1f, 1f, 0.38f), "Measure");
                Text label = CreateLabel(timelineGridLayer, FormatTime(t), 12, TextAnchor.MiddleLeft, 72f, 22f, new Color(1f, 1f, 1f, 0.62f));
                RectTransform rect = label.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(6f, y + 12f);
            }
        }

        private void DrawSubdivisionLines(int division, float beat, float width, float height, Color color, float thickness)
        {
            float step = beat * 4f / division;
            if (step <= 0.001f)
                return;

            for (float t = 0f; t <= SongLength + 0.001f; t += step)
            {
                float y = t * TimelinePixelsPerSecond;
                CreateHorizontalLine(y, width, thickness, color, $"{division}th");
            }
        }

        private void RefreshNotes()
        {
            if (timelineNoteLayer == null || chart == null)
                return;

            ClearChildren(timelineNoteLayer);
            foreach (ScorewriterNote note in chart.notes)
                CreateTimelineNote(note);
        }

        private void CreateTimelineNote(ScorewriterNote note)
        {
            Vector2 start = TimelinePosition(note.startLane, note.time);
            Color color = GetLaneColor(note.startLane);
            bool isSelected = selectedNote == note;

            if (note.kind == ScorewriterNoteKind.Hold)
            {
                Vector2 end = TimelinePosition(note.endLane, note.EndTime);
                CreateConnector(timelineNoteLayer, start, end, isSelected ? new Color(1f, 0.9f, 0.2f, 0.78f) : new Color(color.r, color.g, color.b, 0.45f), HoldLineWidth, note, false);
                CreateMarker(timelineNoteLayer, note, start, "click", color, isSelected, false);
                CreateMarker(timelineNoteLayer, note, end, "slide", GetLaneColor(note.endLane), isSelected, true);
                return;
            }

            string shape = note.kind == ScorewriterNoteKind.Slide ? "slide" : "round";
            CreateMarker(timelineNoteLayer, note, start, shape, color, isSelected, false);
        }

        private void CreateMarker(RectTransform parent, ScorewriterNote note, Vector2 position, string shape, Color color, bool isSelected, bool tail)
        {
            RectTransform marker = CreateRect(tail ? "TailHandle" : "NoteHandle", parent);
            marker.anchorMin = new Vector2(0f, 0f);
            marker.anchorMax = new Vector2(0f, 0f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = position;
            float size = isSelected ? MarkerSize + 8f : MarkerSize;
            marker.sizeDelta = new Vector2(size, size);

            Image image = AddImage(marker.gameObject, Color.white);
            image.sprite = LoadNoteSprite(note, shape);
            image.color = isSelected ? Color.white : color;
            image.raycastTarget = true;

            if (shape == "slide" && image.sprite == circleSprite)
                marker.localEulerAngles = new Vector3(0f, 0f, 45f);

            marker.gameObject.AddComponent<ScorewriterNoteHandle>().Bind(this, note, tail);
        }

        private void CreateConnector(RectTransform parent, Vector2 start, Vector2 end, Color color, float thickness, ScorewriterNote note, bool preview)
        {
            Vector2 delta = end - start;
            float distance = Mathf.Max(1f, delta.magnitude);
            RectTransform line = CreateRect(preview ? "PreviewHoldLine" : "HoldLine", parent);
            line.anchorMin = new Vector2(0f, 0f);
            line.anchorMax = new Vector2(0f, 0f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = start + delta * 0.5f;
            line.sizeDelta = new Vector2(thickness, distance);
            line.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
            Image image = AddImage(line.gameObject, color);
            image.raycastTarget = !preview;
            if (!preview)
                line.gameObject.AddComponent<ScorewriterNoteHandle>().Bind(this, note, false);
        }

        private void RenderPreview()
        {
            if (previewNoteLayer == null || chart == null)
                return;

            ClearChildren(previewNoteLayer);
            foreach (ScorewriterNote note in chart.notes)
            {
                if (note.kind == ScorewriterNoteKind.Hold)
                {
                    bool visible = note == selectedNote || (currentTime >= note.time - 1.5f && currentTime <= note.EndTime + 0.45f);
                    if (!visible)
                        continue;

                    Vector2 start = PreviewPosition(note.startLane);
                    Vector2 end = PreviewPosition(note.endLane);
                    Color lineColor = GetLaneColor(note.startLane);
                    float alpha = note == selectedNote ? 0.94f : currentTime >= note.time && currentTime <= note.EndTime ? 0.86f : 0.38f;
                    lineColor.a = alpha;
                    CreateConnector(previewNoteLayer, start, end, lineColor, 10f, note, true);

                    if (currentTime >= note.time && currentTime <= note.EndTime)
                    {
                        float normalized = Mathf.InverseLerp(note.time, note.EndTime, currentTime);
                        CreatePreviewMarker(Vector2.Lerp(start, end, normalized), note, "round", Color.white, 1.18f);
                    }
                    else
                    {
                        CreatePreviewMarker(start, note, "click", GetLaneColor(note.startLane), 0.96f);
                    }
                    continue;
                }

                float delta = note.time - currentTime;
                if (note != selectedNote && (delta < -0.35f || delta > 1.5f))
                    continue;

                float scale = note == selectedNote ? 1.28f : Mathf.Lerp(1.2f, 0.72f, Mathf.Clamp01(delta / 1.5f));
                string shape = note.kind == ScorewriterNoteKind.Slide ? "slide" : "round";
                CreatePreviewMarker(PreviewPosition(note.startLane), note, shape, GetLaneColor(note.startLane), scale);
            }
        }

        private void CreatePreviewMarker(Vector2 position, ScorewriterNote note, string shape, Color color, float scale)
        {
            RectTransform marker = CreateRect("PreviewNote", previewNoteLayer);
            marker.anchorMin = new Vector2(0.5f, 0.5f);
            marker.anchorMax = new Vector2(0.5f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = position;
            marker.sizeDelta = Vector2.one * 58f * scale;
            Image image = AddImage(marker.gameObject, color);
            image.sprite = LoadNoteSprite(note, shape);
            image.raycastTarget = false;
        }

        private void AddNoteAtCurrentTime()
        {
            AddNote(SnapTime(currentTime), placementStartLane);
        }

        private void AddNote(float time, ScorewriterLane lane)
        {
            ScorewriterNote note = new ScorewriterNote
            {
                id = Guid.NewGuid().ToString("N"),
                time = Mathf.Clamp(time, 0f, SongLength),
                kind = placementKind,
                startLane = lane,
                endLane = placementEndLane,
                duration = placementKind == ScorewriterNoteKind.Hold ? Mathf.Max(GetSnapStep(), ParseFloat(durationInput.text, GetSnapStep() * 4f)) : 0f,
                threshold = Mathf.RoundToInt(ParseFloat(thresholdInput.text, 10f)),
                hasTailSlide = tailSlideToggle != null && tailSlideToggle.isOn
            };

            if (note.kind != ScorewriterNoteKind.Hold)
                note.endLane = note.startLane;

            chart.notes.Add(note);
            SortNotes();
            currentTime = note.time;
            audioPlayer.Seek(CurrentSong, ChartTimeToAudioTime(currentTime));
            SelectNote(note);
            CenterTimelineOnTime(currentTime);
            SetStatus($"已添加 {KindLabel(note.kind)}，时间 {FormatTime(note.time)}。");
        }

        private void DeleteSelectedNote()
        {
            if (selectedNote == null)
                return;

            chart.notes.Remove(selectedNote);
            selectedNote = null;
            RefreshNotes();
            UpdateSelectedLabel();
            SetStatus("已删除选中的音符。");
        }

        private void CyclePlacementKind()
        {
            placementKind = (ScorewriterNoteKind)(((int)placementKind + 1) % Enum.GetValues(typeof(ScorewriterNoteKind)).Length);
            if (selectedNote != null)
            {
                selectedNote.kind = placementKind;
                if (selectedNote.kind != ScorewriterNoteKind.Hold)
                {
                    selectedNote.duration = 0f;
                    selectedNote.endLane = selectedNote.startLane;
                }
                else
                {
                    selectedNote.duration = Mathf.Max(selectedNote.duration, GetSnapStep());
                    selectedNote.endLane = placementEndLane;
                }
                durationInput.SetTextWithoutNotify(FormatFloat(selectedNote.duration));
                RefreshNotes();
            }
            UpdatePlacementButtonLabels();
            UpdateSelectedLabel();
        }

        private void CycleStartLane()
        {
            placementStartLane = (ScorewriterLane)(((int)placementStartLane + 1) % 4);
            if (selectedNote != null)
            {
                selectedNote.startLane = placementStartLane;
                RefreshNotes();
            }
            UpdatePlacementButtonLabels();
            UpdateSelectedLabel();
        }

        private void CycleEndLane()
        {
            placementEndLane = (ScorewriterLane)(((int)placementEndLane + 1) % 4);
            if (selectedNote != null)
            {
                selectedNote.endLane = placementEndLane;
                if (selectedNote.kind == ScorewriterNoteKind.Hold)
                    RefreshNotes();
            }
            UpdatePlacementButtonLabels();
            UpdateSelectedLabel();
        }

        private void CycleSnap()
        {
            snapIndex = (snapIndex + 1) % snapDivisions.Length;
            UpdatePlacementButtonLabels();
            SetStatus($"吸附：{SnapLabel()}。");
        }

        private void ToggleFollowPlayback()
        {
            followPlayback = !followPlayback;
            UpdateTransportUi();
            SetStatus(followPlayback ? "已开启播放跟随。" : "已关闭播放跟随。");
        }

        private void OnDurationEdited(string value)
        {
            float duration = Mathf.Max(0f, ParseFloat(value, GetSnapStep()));
            durationInput.SetTextWithoutNotify(FormatFloat(duration));
            if (selectedNote == null)
                return;

            selectedNote.duration = selectedNote.kind == ScorewriterNoteKind.Hold ? Mathf.Max(GetSnapStep(), duration) : 0f;
            RefreshNotes();
            UpdateSelectedLabel();
        }

        private void OnThresholdEdited(string value)
        {
            int threshold = Mathf.Max(0, Mathf.RoundToInt(ParseFloat(value, 10f)));
            thresholdInput.SetTextWithoutNotify(threshold.ToString(CultureInfo.InvariantCulture));
            if (selectedNote != null)
                selectedNote.threshold = threshold;
            UpdateSelectedLabel();
        }

        private void OnTailSlideChanged(bool value)
        {
            if (selectedNote != null)
                selectedNote.hasTailSlide = value;
            UpdateSelectedLabel();
        }

        private void OnBpmEdited(string value)
        {
            float bpm = Mathf.Clamp(ParseFloat(value, chart.bpm), 20f, 320f);
            chart.bpm = bpm;
            CurrentSong.bpm = bpm;
            bpmInput.SetTextWithoutNotify(FormatFloat(bpm));
            RebuildTimeline();
        }

        private void OnLengthEdited(string value)
        {
            float length = Mathf.Clamp(ParseFloat(value, chart.songLength), 8f, 600f);
            chart.songLength = length;
            CurrentSong.songLength = length;
            lengthInput.SetTextWithoutNotify(FormatFloat(length));
            currentTime = Mathf.Clamp(currentTime, 0f, SongLength);
            RebuildTimeline();
        }

        private void OnOffsetEdited(string value)
        {
            chart.timingOffsetMs = Mathf.Clamp(ParseFloat(value, chart.timingOffsetMs), -10000f, 10000f);
            offsetInput.SetTextWithoutNotify(FormatFloat(chart.timingOffsetMs));
            audioPlayer.Seek(CurrentSong, ChartTimeToAudioTime(currentTime));
            SetStatus($"Offset 已设为 {FormatFloat(chart.timingOffsetMs)} ms。");
        }

        private void AdjustOffset(float deltaMs)
        {
            chart.timingOffsetMs = Mathf.Clamp(chart.timingOffsetMs + deltaMs, -10000f, 10000f);
            if (offsetInput != null)
                offsetInput.SetTextWithoutNotify(FormatFloat(chart.timingOffsetMs));
            audioPlayer.Seek(CurrentSong, ChartTimeToAudioTime(currentTime));
            SetStatus($"Offset：{FormatFloat(chart.timingOffsetMs)} ms。");
        }

        private void SetGridVisibility(int division, bool visible)
        {
            if (chart == null)
                return;

            switch (division)
            {
                case 4:
                    chart.showQuarterGrid = visible;
                    break;
                case 6:
                    chart.showSixthGrid = visible;
                    break;
                case 8:
                    chart.showEighthGrid = visible;
                    break;
                case 16:
                    chart.showSixteenthGrid = visible;
                    break;
            }

            RebuildTimeline();
        }

        private void TogglePlayback()
        {
            if (audioPlayer.IsPlaying)
            {
                audioPlayer.Pause();
                return;
            }

            if (audioPlayer.IsPaused)
            {
                audioPlayer.Resume();
                return;
            }

            StartPlaybackFromCurrentTime();
        }

        private void StopPlayback()
        {
            audioPlayer.Stop();
            currentTime = 0f;
            audioPlayer.SetManualTime(currentTime);
            UpdateTransportUi();
            UpdatePlayhead();
        }

        private void SeekRelative(float delta)
        {
            currentTime = Mathf.Clamp(currentTime + delta, 0f, SongLength);
            audioPlayer.Seek(CurrentSong, ChartTimeToAudioTime(currentTime));
            UpdateTransportUi();
            UpdatePlayhead();
        }

        private void OnTimeSliderChanged(float value)
        {
            if (suppressSliderEvent || audioPlayer.IsPlaying)
                return;

            currentTime = Mathf.Clamp(value * SongLength, 0f, SongLength);
            audioPlayer.SetManualTime(ChartTimeToAudioTime(currentTime));
            UpdatePlayhead();
        }

        private void StartPlaybackFromCurrentTime()
        {
            audioPlayer.Play(CurrentSong, ChartTimeToAudioTime(currentTime));
            SetStatus($"播放：{CurrentSong.DisplayName}");
        }

        private float ChartTimeToAudioTime(float chartTime)
        {
            return Mathf.Max(0f, chartTime - ChartOffsetSeconds);
        }

        private void SwitchSong(int delta)
        {
            SaveEditorChart();
            audioPlayer.Stop();
            currentTime = 0f;
            songIndex = (songIndex + delta + songs.Count) % songs.Count;
            LoadChartForCurrentSong(false);
            ApplySongToControls();
            RebuildTimeline();
            SetStatus($"已选择：{CurrentSong.DisplayName}。");
            StartPlaybackFromCurrentTime();
        }

        private void LoadChartForCurrentSong(bool reportMissing)
        {
            string path = EditorChartPath(CurrentSong);
            if (File.Exists(path))
            {
                chart = JsonUtility.FromJson<ScorewriterChart>(File.ReadAllText(path));
                if (chart.notes == null)
                    chart.notes = new List<ScorewriterNote>();
                CurrentSong.bpm = chart.bpm;
                CurrentSong.songLength = chart.songLength;
                SortNotes();
                selectedNote = null;
                SetStatus($"已加载：{Path.GetFileName(path)}。");
            }
            else
            {
                chart = new ScorewriterChart
                {
                    songId = CurrentSong.songId,
                    title = CurrentSong.title,
                    bpm = CurrentSong.bpm,
                    songLength = CurrentSong.songLength,
                    difficulty = 0,
                    level = 1
                };
                selectedNote = null;
                if (reportMissing)
                    SetStatus("还没有保存过谱面，已创建空谱面。");
            }

            ApplySongToControls();
            RefreshNotes();
            UpdateSelectedLabel();
        }

        private void SaveEditorChart()
        {
            if (chart == null)
                return;

            chart.songId = CurrentSong.songId;
            chart.title = CurrentSong.title;
            chart.bpm = CurrentSong.bpm;
            chart.songLength = CurrentSong.songLength;
            Directory.CreateDirectory(ChartsDirectory());
            string path = EditorChartPath(CurrentSong);
            File.WriteAllText(path, JsonUtility.ToJson(chart, true));
            RefreshAssetDatabase();
            SetStatus($"已保存编辑器谱面：{RelativeProjectPath(path)}");
        }

        private void ExportGameJson()
        {
            if (chart == null)
                return;

            SortNotes();
            GameChartExport export = new GameChartExport
            {
                difficulty = chart.difficulty,
                level = chart.level
            };

            foreach (ScorewriterNote note in chart.notes)
                export.notes.Add(ToGameNote(note));

            string defaultName = $"{CurrentSong.songId}_game_chart.json";
            string path = ChooseJsonSavePath(defaultName);
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("已取消导出。");
                return;
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(export, true));
            RefreshAssetDatabase();
            SetStatus($"已导出游戏 JSON：{RelativeProjectPath(path)}");
        }

        private GameNoteExport ToGameNote(ScorewriterNote note)
        {
            Vector3 start = ScorewriterLaneUtility.ToWorldPosition(note.startLane);
            Vector3 end = ScorewriterLaneUtility.ToWorldPosition(note.endLane);
            bool isSlide = note.kind == ScorewriterNoteKind.Slide;
            bool isRound = note.kind == ScorewriterNoteKind.Round;
            float exportedDuration = note.kind == ScorewriterNoteKind.Hold
                ? Mathf.Max(GetSnapStep(), note.duration)
                : isRound ? Mathf.Max(0.08f, GetSnapStep() * 0.5f) : 0f;

            GameNoteExport data = new GameNoteExport
            {
                time = ChartTimeToAudioTime(note.time),
                x = start.x,
                y = start.y,
                z = start.z,
                noteType = isSlide ? 1 : 0,
                duration = exportedDuration,
                threshold = Mathf.Max(0, note.threshold),
                hasTailFlick = note.kind == ScorewriterNoteKind.Hold && note.hasTailSlide,
                flickDirection = ScorewriterLaneUtility.DirectionFromLanes(note.startLane, note.endLane),
                approachTime = 2f,
                useCustomEndPoint = note.kind == ScorewriterNoteKind.Hold && note.endLane != note.startLane,
                endX = end.x,
                endY = end.y,
                endZ = end.z,
                editorKind = note.kind.ToString(),
                startLane = (int)note.startLane,
                endLane = (int)note.endLane
            };

            return data;
        }

        private void ApplySongToControls()
        {
            if (songTitleText != null)
                songTitleText.text = $"{CurrentSong.DisplayName}  [{CurrentSong.cueSheetName}]";
            if (bpmInput != null)
                bpmInput.SetTextWithoutNotify(FormatFloat(CurrentSong.bpm));
            if (lengthInput != null)
                lengthInput.SetTextWithoutNotify(FormatFloat(CurrentSong.songLength));
            if (offsetInput != null && chart != null)
                offsetInput.SetTextWithoutNotify(FormatFloat(chart.timingOffsetMs));
            if (quarterGridToggle != null && chart != null)
                quarterGridToggle.SetIsOnWithoutNotify(chart.showQuarterGrid);
            if (sixthGridToggle != null && chart != null)
                sixthGridToggle.SetIsOnWithoutNotify(chart.showSixthGrid);
            if (eighthGridToggle != null && chart != null)
                eighthGridToggle.SetIsOnWithoutNotify(chart.showEighthGrid);
            if (sixteenthGridToggle != null && chart != null)
                sixteenthGridToggle.SetIsOnWithoutNotify(chart.showSixteenthGrid);
        }

        private void UpdateTransportUi()
        {
            if (timeSlider != null)
            {
                suppressSliderEvent = true;
                timeSlider.value = SongLength <= 0f ? 0f : currentTime / SongLength;
                suppressSliderEvent = false;
            }

            if (timeText != null)
                timeText.text = $"{FormatTime(currentTime)} / {FormatTime(SongLength)}";

            if (playButtonText != null)
                playButtonText.text = audioPlayer.IsPlaying ? "暂停" : audioPlayer.IsPaused ? "继续" : "播放";
            if (followButtonText != null)
                followButtonText.text = followPlayback ? "跟随开" : "跟随关";
        }

        private void UpdatePlayhead()
        {
            if (playhead == null)
                return;

            playhead.anchoredPosition = new Vector2(0f, currentTime * TimelinePixelsPerSecond);
        }

        private void CenterTimelineOnTime(float time)
        {
            if (timelineScroll == null || timelineViewport == null || timelineContent == null)
                return;

            float contentHeight = timelineContent.rect.height;
            float viewportHeight = timelineViewport.rect.height;
            if (contentHeight <= viewportHeight)
                return;

            float target = time * TimelinePixelsPerSecond - viewportHeight * 0.45f;
            timelineScroll.verticalNormalizedPosition = Mathf.Clamp01(target / (contentHeight - viewportHeight));
        }

        private void UpdatePlacementButtonLabels()
        {
            if (typeButtonText != null)
                typeButtonText.text = $"类型 {KindLabel(placementKind)}";
            if (startLaneButtonText != null)
                startLaneButtonText.text = $"起点 {ScorewriterLaneUtility.GetShortName(placementStartLane)}";
            if (endLaneButtonText != null)
                endLaneButtonText.text = $"终点 {ScorewriterLaneUtility.GetShortName(placementEndLane)}";
            if (snapButtonText != null)
                snapButtonText.text = $"吸附 {SnapLabel()}";
        }

        private void UpdateSelectedLabel()
        {
            if (selectedText == null)
                return;

            if (selectedNote == null)
            {
                selectedText.text = "未选择音符";
                return;
            }

            selectedText.text = $"{KindLabel(selectedNote.kind)} {FormatTime(selectedNote.time)} {ScorewriterLaneUtility.GetShortName(selectedNote.startLane)}->{ScorewriterLaneUtility.GetShortName(selectedNote.endLane)}";
        }

        private bool TryTimelinePoint(PointerEventData eventData, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;
            if (timelineContent == null)
                return false;

            bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(timelineContent, eventData.position, eventData.pressEventCamera, out localPoint);
            if (!ok)
                return false;

            localPoint.x = Mathf.Clamp(localPoint.x, 0f, LaneWidth * 4f - 0.01f);
            localPoint.y = Mathf.Clamp(localPoint.y, 0f, SongLength * TimelinePixelsPerSecond);
            return true;
        }

        private ScorewriterLane LaneFromX(float x)
        {
            return (ScorewriterLane)Mathf.Clamp(Mathf.FloorToInt(x / LaneWidth), 0, 3);
        }

        private Vector2 TimelinePosition(ScorewriterLane lane, float time)
        {
            return new Vector2((int)lane * LaneWidth + LaneWidth * 0.5f, Mathf.Clamp(time, 0f, SongLength) * TimelinePixelsPerSecond);
        }

        private Vector2 PreviewPosition(ScorewriterLane lane)
        {
            Rect rect = previewSurface.rect;
            float x = lane == ScorewriterLane.TopLeft || lane == ScorewriterLane.BottomLeft ? -rect.width * 0.25f : rect.width * 0.25f;
            float y = lane == ScorewriterLane.TopLeft || lane == ScorewriterLane.TopRight ? rect.height * 0.25f : -rect.height * 0.25f;
            return new Vector2(x, y);
        }

        private float SnapTime(float time)
        {
            float step = GetSnapStep();
            if (step <= 0f)
                return Mathf.Clamp(time, 0f, SongLength);

            return Mathf.Clamp(Mathf.Round(time / step) * step, 0f, SongLength);
        }

        private float GetSnapStep()
        {
            int division = snapDivisions[Mathf.Clamp(snapIndex, 0, snapDivisions.Length - 1)];
            if (division <= 0)
                return 0f;

            float beat = 60f / Mathf.Max(1f, chart?.bpm ?? 120f);
            return beat * 4f / division;
        }

        private string SnapLabel()
        {
            int division = snapDivisions[Mathf.Clamp(snapIndex, 0, snapDivisions.Length - 1)];
            return division <= 0 ? "关闭" : $"1/{division}";
        }

        private static string KindLabel(ScorewriterNoteKind kind)
        {
            switch (kind)
            {
                case ScorewriterNoteKind.Hold:
                    return "长按";
                case ScorewriterNoteKind.Slide:
                    return "滑动";
                case ScorewriterNoteKind.Round:
                    return "圆点";
                default:
                    return kind.ToString();
            }
        }

        private Sprite LoadNoteSprite(ScorewriterNote note, string shape)
        {
            int direction = ScorewriterLaneUtility.DirectionFromLanes(note.startLane, note.endLane);
            string color = DirectionColorName(direction);
            Sprite sprite = Resources.Load<Sprite>($"Images/Notes/{color}_{shape}");
            if (sprite != null)
                return sprite;

            return shape == "slide" ? whiteSprite : circleSprite;
        }

        private static string DirectionColorName(int direction)
        {
            switch (direction)
            {
                case 1:
                    return "miku";
                case 2:
                    return "red";
                case 3:
                    return "blue";
                default:
                    return "white";
            }
        }

        private static Color GetLaneColor(ScorewriterLane lane)
        {
            switch (lane)
            {
                case ScorewriterLane.TopLeft:
                    return new Color(0.92f, 0.95f, 1f, 1f);
                case ScorewriterLane.TopRight:
                    return new Color(0.18f, 0.92f, 1f, 1f);
                case ScorewriterLane.BottomLeft:
                    return new Color(0.24f, 0.45f, 1f, 1f);
                case ScorewriterLane.BottomRight:
                    return new Color(1f, 0.2f, 0.24f, 1f);
                default:
                    return Color.white;
            }
        }

        private void SortNotes()
        {
            chart.notes.Sort((a, b) => a.time.CompareTo(b.time));
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
            Debug.Log($"[Scorewriter] {message}");
        }

        private string EditorChartPath(ScorewriterSong song)
        {
            return Path.Combine(ChartsDirectory(), $"{song.songId}_scorewriter.json");
        }

        private static string ChartsDirectory()
        {
            return Path.Combine(Application.dataPath, "Scorewriter/Charts");
        }

        private static string ExportsDirectory()
        {
            return Path.Combine(Application.dataPath, "Scorewriter/Exports");
        }

        private static string ChooseJsonSavePath(string defaultName)
        {
#if UNITY_EDITOR
            Directory.CreateDirectory(ExportsDirectory());
            return EditorUtility.SaveFilePanel("保存游戏谱面 JSON", ExportsDirectory(), defaultName, "json");
#else
            Directory.CreateDirectory(ExportsDirectory());
            return Path.Combine(ExportsDirectory(), defaultName);
#endif
        }

        private static string RelativeProjectPath(string absolutePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            if (absolutePath.StartsWith(projectRoot, StringComparison.Ordinal))
                return absolutePath.Substring(projectRoot.Length + 1);
            return absolutePath;
        }

        private static void RefreshAssetDatabase()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        private static float ParseFloat(string value, float fallback)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float invariant))
                return invariant;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out float current))
                return current;
            return fallback;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            float seconds = time - minutes * 60f;
            return $"{minutes:00}:{seconds:00.00}";
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private RectTransform CreateLayer(string name, RectTransform parent)
        {
            RectTransform layer = CreateRect(name, parent);
            layer.anchorMin = new Vector2(0f, 0f);
            layer.anchorMax = new Vector2(0f, 0f);
            layer.pivot = new Vector2(0f, 0f);
            layer.anchoredPosition = Vector2.zero;
            return layer;
        }

        private static void SetLayerSize(RectTransform layer, float width, float height)
        {
            layer.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetLayout(RectTransform rect, float preferredWidth, float preferredHeight, float flexibleWidth, float flexibleHeight)
        {
            LayoutElement element = rect.gameObject.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth >= 0f)
                element.preferredWidth = preferredWidth;
            if (preferredHeight >= 0f)
                element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
        }

        private Image AddImage(GameObject obj, Color color)
        {
            Image image = obj.GetComponent<Image>() ?? obj.AddComponent<Image>();
            image.sprite = whiteSprite;
            image.color = color;
            return image;
        }

        private Text CreateLabel(Transform parent, string text, int size, TextAnchor alignment, float width, float height, Color color)
        {
            RectTransform rect = CreateRect("Text", parent);
            if (width >= 0f || height >= 0f)
                SetLayout(rect, width, height, 0f, 0f);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Button CreateButton(Transform parent, string label, float width, float height, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = CreateRect("Button", parent);
            SetLayout(rect, width, height, 0f, 0f);
            Image image = AddImage(rect.gameObject, new Color(0.16f, 0.19f, 0.23f, 1f));
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.19f, 0.23f, 1f);
            colors.highlightedColor = new Color(0.24f, 0.30f, 0.38f, 1f);
            colors.pressedColor = new Color(0.12f, 0.44f, 0.48f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(action);

            Text text = CreateLabel(rect, label, 14, TextAnchor.MiddleCenter, -1f, -1f, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        private InputField CreateInput(Transform parent, string value, float width, float height, UnityEngine.Events.UnityAction<string> onEndEdit)
        {
            RectTransform rect = CreateRect("Input", parent);
            SetLayout(rect, width, height, 0f, 0f);
            Image image = AddImage(rect.gameObject, new Color(0.035f, 0.041f, 0.052f, 1f));
            InputField input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.text = value;
            input.contentType = InputField.ContentType.Standard;

            Text text = CreateLabel(rect, value, 14, TextAnchor.MiddleCenter, -1f, -1f, Color.white);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(8f, 0f);
            text.rectTransform.offsetMax = new Vector2(-8f, 0f);
            input.textComponent = text;
            input.onEndEdit.AddListener(onEndEdit);
            return input;
        }

        private Toggle CreateToggle(Transform parent, string label, bool value, UnityEngine.Events.UnityAction<bool> onChanged, float width, float height)
        {
            RectTransform rect = CreateRect("Toggle", parent);
            SetLayout(rect, width, height, 0f, 0f);
            Toggle toggle = rect.gameObject.AddComponent<Toggle>();

            RectTransform box = CreateRect("Box", rect);
            box.anchorMin = new Vector2(0f, 0.5f);
            box.anchorMax = new Vector2(0f, 0.5f);
            box.pivot = new Vector2(0f, 0.5f);
            box.anchoredPosition = new Vector2(6f, 0f);
            box.sizeDelta = new Vector2(22f, 22f);
            Image boxImage = AddImage(box.gameObject, new Color(0.035f, 0.041f, 0.052f, 1f));

            RectTransform check = CreateRect("Checkmark", box);
            Stretch(check);
            check.offsetMin = new Vector2(5f, 5f);
            check.offsetMax = new Vector2(-5f, -5f);
            Image checkImage = AddImage(check.gameObject, new Color(1f, 0.82f, 0.25f, 1f));

            Text text = CreateLabel(rect, label, 13, TextAnchor.MiddleLeft, -1f, -1f, new Color(0.85f, 0.9f, 0.98f, 1f));
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(34f, 0f);

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(onChanged);
            return toggle;
        }

        private Slider CreateSlider(Transform parent, float width, float height)
        {
            RectTransform root = CreateRect("TimeSlider", parent);
            SetLayout(root, width, height, 1f, 0f);
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            RectTransform background = CreateRect("Background", root);
            Stretch(background);
            background.offsetMin = new Vector2(0f, 17f);
            background.offsetMax = new Vector2(0f, -17f);
            AddImage(background.gameObject, new Color(0.03f, 0.035f, 0.043f, 1f));

            RectTransform fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(4f, 17f);
            fillArea.offsetMax = new Vector2(-4f, -17f);

            RectTransform fill = CreateRect("Fill", fillArea);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = new Vector2(0f, 0f);
            AddImage(fill.gameObject, new Color(0.17f, 0.78f, 0.83f, 1f));

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);

            RectTransform handle = CreateRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(18f, 30f);
            Image handleImage = AddImage(handle.gameObject, new Color(1f, 0.84f, 0.24f, 1f));

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private void CreateGridLine(float position, bool vertical, Color color, float width, float height, float thickness, bool laneLine)
        {
            RectTransform line = CreateRect(laneLine ? "LaneLine" : "GridLine", timelineGridLayer);
            line.anchorMin = new Vector2(0f, 0f);
            line.anchorMax = new Vector2(0f, 0f);
            line.pivot = vertical ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
            line.anchoredPosition = vertical ? new Vector2(position, 0f) : new Vector2(0f, position);
            line.sizeDelta = vertical ? new Vector2(thickness, height) : new Vector2(width, thickness);
            AddImage(line.gameObject, color).raycastTarget = false;
        }

        private void CreateHorizontalLine(float y, float width, float thickness, Color color, string name)
        {
            RectTransform line = CreateRect(name, timelineGridLayer);
            line.anchorMin = new Vector2(0f, 0f);
            line.anchorMax = new Vector2(0f, 0f);
            line.pivot = new Vector2(0f, 0.5f);
            line.anchoredPosition = new Vector2(0f, y);
            line.sizeDelta = new Vector2(width, thickness);
            AddImage(line.gameObject, color).raycastTarget = false;
        }

        private void CreatePreviewQuadrant(string label, float minX, float minY, float maxX, float maxY, Color color)
        {
            RectTransform rect = CreateRect($"Preview_{label}", previewSurface);
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            AddImage(rect.gameObject, color).raycastTarget = false;
            Text text = CreateLabel(rect, label, 22, TextAnchor.MiddleCenter, -1f, -1f, new Color(1f, 1f, 1f, 0.78f));
            Stretch(text.rectTransform);
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Sprite CreateSolidSprite()
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
