#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using gameStates;
using UnityEngine;
using UnityEngine.InputSystem;

namespace recording
{
    // Developer-only runtime viewer. It deliberately reads the in-memory sink and never affects
    // recording, command execution, or the persistent JSONL stream.
    public sealed class BehaviorRecorderOverlay : MonoBehaviour
    {
        private const int MaxVisibleRecords = 200;
        private const int WindowId = 0x42BEEF;
        private const float ReferenceScreenHeight = 1080f;
        private const float BaseWindowWidth = 920f;
        private const float BaseWindowHeight = 620f;
        private const float MinimumAutoScale = 0.75f;
        private const float MaximumAutoScale = 2.5f;
        private const float MinimumUserScale = 0.75f;
        private const float MaximumUserScale = 1.5f;
        private const float MinimumWindowWidth = 560f;
        private const float MinimumWindowHeight = 400f;
        private const float ResizeEdgeSize = 14f;
        private const float ResizeHandleSize = 24f;

        [Flags]
        private enum WindowResizeEdge
        {
            None = 0,
            Left = 1,
            Right = 2,
            Top = 4,
            Bottom = 8,
        }

        private StateManager _stateManager;
        private Rect _windowRect = new Rect(20f, 20f, BaseWindowWidth, BaseWindowHeight);
        private Vector2 _recordScroll;
        private Vector2 _detailScroll;
        private List<BehaviorRecord> _visibleRecords = new List<BehaviorRecord>();
        private BehaviorRecord _selectedRecord;
        private InMemoryBehaviorRecordSink _observedSink;
        private ulong _observedSinkVersion;
        private int _lastToggleFrame = -1;
        private float _userScale = 1f;
        private float _currentUiScale = 1f;
        private WindowResizeEdge _resizeEdges;
        private Rect _resizeStartWindowRect;
        private Vector2 _resizeStartMousePosition;
        private bool _visible;
        private bool _followLatest = true;
        private bool _expandRootCommandChain;
        private bool _showBattle = true;
        private bool _showCommand = true;
        private bool _showAction = true;
        private bool _showHook = true;
        private bool _showObserved = true;
        private bool _showSucceeded = true;
        private bool _showRejected = true;
        private bool _showInterrupted = true;
        private bool _onlyProblems;
        private string _rootCommandText = string.Empty;
        private string _rootCommandError;

        public void Initialize(StateManager stateManager)
        {
            _stateManager = stateManager;
            RefreshRecords();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f8Key.wasPressedThisFrame)
            {
                ToggleVisibility();
            }

            InMemoryBehaviorRecordSink sink = GetMemorySink();
            if (_visible && sink != null &&
                (sink != _observedSink || sink.Version != _observedSinkVersion))
            {
                RefreshRecords();
            }
        }

        private void OnGUI()
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            _currentUiScale = CalculateUiScale();
            GUI.matrix = Matrix4x4.Scale(new Vector3(_currentUiScale, _currentUiScale, 1f));
            try
            {
                GUI.depth = -1000;
                Event currentEvent = Event.current;
                if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.F8)
                {
                    ToggleVisibility();
                    currentEvent.Use();
                }

                if (!_visible)
                {
                    if (GUI.Button(new Rect(8f, 8f, 120f, 28f), "Records (F8)"))
                    {
                        ToggleVisibility();
                    }
                    return;
                }

                ConstrainWindowToScreen();
                HandleScreenSpaceWindowResize();
                _windowRect = GUI.Window(
                    WindowId,
                    _windowRect,
                    DrawWindow,
                    "Behavior Recorder — F8");
                ConstrainWindowToScreen();
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
        }

        private void ToggleVisibility()
        {
            if (_lastToggleFrame == Time.frameCount)
            {
                return;
            }

            _lastToggleFrame = Time.frameCount;
            _visible = !_visible;
            if (_visible)
            {
                RefreshRecords();
            }
        }

        private void DrawWindow(int windowId)
        {
            DrawStatus();
            bool filterChanged = DrawFilters();
            if (filterChanged)
            {
                RefreshRecords();
            }

            GUILayout.Space(4f);
            GUILayout.Label($"Records: {_visibleRecords.Count} (showing at most {MaxVisibleRecords})");

            float listHeight = Mathf.Max(180f, _windowRect.height * 0.48f);
            _recordScroll = GUILayout.BeginScrollView(_recordScroll, GUILayout.Height(listHeight));
            for (int i = 0; i < _visibleRecords.Count; i++)
            {
                BehaviorRecord record = _visibleRecords[i];
                Color previousBackgroundColor = GUI.backgroundColor;
                GUI.backgroundColor = GetRowColor(record);
                string row = ReferenceEquals(record, _selectedRecord)
                    ? $"    > {BuildRow(record)}"
                    : $"      {BuildRow(record)}";
                if (GUILayout.Button(row, GUILayout.ExpandWidth(true)))
                {
                    _selectedRecord = record;
                }
                GUI.backgroundColor = previousBackgroundColor;
                DrawRowMarkers(GUILayoutUtility.GetLastRect(), record);
            }
            GUILayout.EndScrollView();

            if (_followLatest && Event.current.type == EventType.Repaint)
            {
                _recordScroll.y = float.MaxValue;
            }

            GUILayout.Label("Selected record");
            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandHeight(true));
            GUILayout.TextArea(_selectedRecord == null
                ? "Select a record to inspect it."
                : BuildDetails(_selectedRecord), GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            DrawResizeChrome();
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 24f));
        }

        private void HandleScreenSpaceWindowResize()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                _resizeEdges = WindowResizeEdge.None;
                return;
            }

            Vector2 screenMousePosition = mouse.position.ReadValue();
            Vector2 logicalMousePosition = new Vector2(
                screenMousePosition.x / _currentUiScale,
                (Screen.height - screenMousePosition.y) / _currentUiScale);
            bool wasResizing = _resizeEdges != WindowResizeEdge.None;

            if (!wasResizing && mouse.leftButton.wasPressedThisFrame)
            {
                WindowResizeEdge hitEdges = GetResizeEdges(logicalMousePosition);
                if (hitEdges != WindowResizeEdge.None)
                {
                    _resizeEdges = hitEdges;
                    _resizeStartWindowRect = _windowRect;
                    _resizeStartMousePosition = logicalMousePosition;
                }
            }

            if (_resizeEdges != WindowResizeEdge.None)
            {
                if (mouse.leftButton.isPressed)
                {
                    Vector2 delta = logicalMousePosition - _resizeStartMousePosition;
                    _windowRect = ResizeWindow(_resizeStartWindowRect, delta, _resizeEdges);
                    ConstrainWindowToScreen();
                }
                else
                {
                    _resizeEdges = WindowResizeEdge.None;
                }
            }

            if ((wasResizing || _resizeEdges != WindowResizeEdge.None) && Event.current.isMouse)
            {
                Event.current.Use();
            }
        }

        private void DrawResizeChrome()
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0.75f, 0.85f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(0f, 0f, _windowRect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, _windowRect.height - 2f, _windowRect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, 0f, 2f, _windowRect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(_windowRect.width - 2f, 0f, 2f, _windowRect.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUI.Box(
                new Rect(
                    _windowRect.width - ResizeHandleSize - 2f,
                    _windowRect.height - ResizeHandleSize - 2f,
                    ResizeHandleSize,
                    ResizeHandleSize),
                "↘");
        }

        private WindowResizeEdge GetResizeEdges(Vector2 mouse)
        {
            WindowResizeEdge edges = WindowResizeEdge.None;
            bool withinVerticalRange =
                mouse.y >= _windowRect.y - ResizeEdgeSize &&
                mouse.y <= _windowRect.yMax + ResizeEdgeSize;
            bool withinHorizontalRange =
                mouse.x >= _windowRect.x - ResizeEdgeSize &&
                mouse.x <= _windowRect.xMax + ResizeEdgeSize;

            if (withinVerticalRange && Mathf.Abs(mouse.x - _windowRect.x) <= ResizeEdgeSize)
            {
                edges |= WindowResizeEdge.Left;
            }
            if (withinVerticalRange && Mathf.Abs(mouse.x - _windowRect.xMax) <= ResizeEdgeSize)
            {
                edges |= WindowResizeEdge.Right;
            }
            if (withinHorizontalRange && Mathf.Abs(mouse.y - _windowRect.y) <= ResizeEdgeSize)
            {
                edges |= WindowResizeEdge.Top;
            }
            if (withinHorizontalRange && Mathf.Abs(mouse.y - _windowRect.yMax) <= ResizeEdgeSize)
            {
                edges |= WindowResizeEdge.Bottom;
            }
            return edges;
        }

        private static Rect ResizeWindow(Rect startWindow, Vector2 delta, WindowResizeEdge edges)
        {
            Rect window = startWindow;
            if ((edges & WindowResizeEdge.Left) != 0)
            {
                float nextWidth = Mathf.Max(MinimumWindowWidth, startWindow.width - delta.x);
                window.x = startWindow.xMax - nextWidth;
                window.width = nextWidth;
            }
            if ((edges & WindowResizeEdge.Right) != 0)
            {
                window.width = Mathf.Max(MinimumWindowWidth, startWindow.width + delta.x);
            }
            if ((edges & WindowResizeEdge.Top) != 0)
            {
                float nextHeight = Mathf.Max(MinimumWindowHeight, startWindow.height - delta.y);
                window.y = startWindow.yMax - nextHeight;
                window.height = nextHeight;
            }
            if ((edges & WindowResizeEdge.Bottom) != 0)
            {
                window.height = Mathf.Max(MinimumWindowHeight, startWindow.height + delta.y);
            }
            return window;
        }

        private static Color GetRowColor(BehaviorRecord record)
        {
            switch (record.Category)
            {
                case BehaviorRecordCategory.Battle: return new Color(1f, 0.55f, 0.35f);
                case BehaviorRecordCategory.Command: return new Color(0.4f, 0.7f, 1f);
                case BehaviorRecordCategory.Action: return new Color(0.45f, 0.85f, 0.55f);
                case BehaviorRecordCategory.Hook: return new Color(0.75f, 0.55f, 1f);
                default: return Color.white;
            }
        }

        private static void DrawRowMarkers(Rect rowRect, BehaviorRecord record)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = GetRootCommandColor(record.RootCommandId);
            GUI.DrawTexture(
                new Rect(rowRect.x + 3f, rowRect.y + 2f, 7f, Mathf.Max(1f, rowRect.height - 4f)),
                Texture2D.whiteTexture);

            if (RecordQuery.IsProblem(record))
            {
                GUI.color = new Color(1f, 0.2f, 0.2f);
                GUI.DrawTexture(
                    new Rect(rowRect.xMax - 7f, rowRect.y + 2f, 4f, Mathf.Max(1f, rowRect.height - 4f)),
                    Texture2D.whiteTexture);
            }

            GUI.color = previousColor;
        }

        private static Color GetRootCommandColor(ulong? rootCommandId)
        {
            if (!rootCommandId.HasValue)
            {
                return new Color(0.5f, 0.5f, 0.5f);
            }

            const double goldenRatioConjugate = 0.618033988749895d;
            float hue = (float)((rootCommandId.Value * goldenRatioConjugate) % 1d);
            return Color.HSVToRGB(hue, 0.72f, 0.95f);
        }

        private void DrawStatus()
        {
            var gameState = _stateManager?.GameState;
            if (gameState == null)
            {
                GUILayout.Label("Recorder is not initialized.");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Session: {gameState.BehaviorRecorder?.SessionId ?? "-"}");
            GUILayout.Label(gameState.PersistentBehaviorLogAvailable ? "JSONL: enabled" : "JSONL: unavailable");
            if (GUILayout.Button("Copy log path", GUILayout.Width(110f)))
            {
                GUIUtility.systemCopyBuffer = gameState.BehaviorRecordFilePath ?? string.Empty;
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(75f)))
            {
                RefreshRecords();
            }
            if (GUILayout.Button("A-", GUILayout.Width(36f)))
            {
                _userScale = Mathf.Max(MinimumUserScale, _userScale - 0.1f);
            }
            if (GUILayout.Button($"UI {Mathf.RoundToInt(_currentUiScale * 100f)}%", GUILayout.Width(72f)))
            {
                _userScale = 1f;
            }
            if (GUILayout.Button("A+", GUILayout.Width(36f)))
            {
                _userScale = Mathf.Min(MaximumUserScale, _userScale + 0.1f);
            }
            GUILayout.EndHorizontal();
        }

        private float CalculateUiScale()
        {
            float automaticScale = Mathf.Clamp(
                Screen.height / ReferenceScreenHeight,
                MinimumAutoScale,
                MaximumAutoScale);
            return automaticScale * _userScale;
        }

        private void ConstrainWindowToScreen()
        {
            const float margin = 12f;
            float logicalScreenWidth = Screen.width / _currentUiScale;
            float logicalScreenHeight = Screen.height / _currentUiScale;

            float maximumWidth = Mathf.Max(1f, logicalScreenWidth - margin * 2f);
            float maximumHeight = Mathf.Max(1f, logicalScreenHeight - margin * 2f);
            float minimumWidth = Mathf.Min(MinimumWindowWidth, maximumWidth);
            float minimumHeight = Mathf.Min(MinimumWindowHeight, maximumHeight);
            _windowRect.width = Mathf.Clamp(_windowRect.width, minimumWidth, maximumWidth);
            _windowRect.height = Mathf.Clamp(_windowRect.height, minimumHeight, maximumHeight);
            _windowRect.x = Mathf.Clamp(
                _windowRect.x,
                margin,
                Mathf.Max(margin, logicalScreenWidth - _windowRect.width - margin));
            _windowRect.y = Mathf.Clamp(
                _windowRect.y,
                margin,
                Mathf.Max(margin, logicalScreenHeight - _windowRect.height - margin));
        }

        private bool DrawFilters()
        {
            bool changed = false;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Domain", GUILayout.Width(55f));
            changed |= DrawToggle("Battle", ref _showBattle);
            changed |= DrawToggle("Command", ref _showCommand);
            changed |= DrawToggle("Action", ref _showAction);
            changed |= DrawToggle("Hook", ref _showHook);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Outcome", GUILayout.Width(55f));
            changed |= DrawColoredToggle(
                "Observed",
                ref _showObserved,
                new Color(0.78f, 0.78f, 0.78f));
            changed |= DrawColoredToggle(
                "Succeeded",
                ref _showSucceeded,
                new Color(0.45f, 1f, 0.55f));
            changed |= DrawColoredToggle(
                "Rejected",
                ref _showRejected,
                new Color(1f, 0.62f, 0.25f));
            changed |= DrawColoredToggle(
                "Interrupted",
                ref _showInterrupted,
                new Color(1f, 0.35f, 0.35f));
            Color previousContentColor = GUI.contentColor;
            GUI.contentColor = new Color(1f, 0.35f, 0.35f);
            bool nextOnlyProblems = GUILayout.Toggle(_onlyProblems, "Only Problems", GUILayout.Width(105f));
            GUI.contentColor = previousContentColor;
            if (nextOnlyProblems != _onlyProblems)
            {
                _onlyProblems = nextOnlyProblems;
                changed = true;
                if (_onlyProblems)
                {
                    _showObserved = true;
                    _showSucceeded = true;
                    _showRejected = true;
                    _showInterrupted = true;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Root ID", GUILayout.Width(55f));
            string nextRootCommandText = GUILayout.TextField(_rootCommandText, GUILayout.Width(130f));
            if (!string.Equals(nextRootCommandText, _rootCommandText, StringComparison.Ordinal))
            {
                _rootCommandText = nextRootCommandText;
                changed = true;
            }
            changed |= DrawToggle("Expand chain", ref _expandRootCommandChain);
            changed |= DrawToggle("Follow latest", ref _followLatest);
            if (GUILayout.Button("Clear filters", GUILayout.Width(100f)))
            {
                ResetFilters();
                changed = true;
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_rootCommandError))
            {
                Color previousColor = GUI.color;
                GUI.color = new Color(1f, 0.45f, 0.45f);
                GUILayout.Label(_rootCommandError);
                GUI.color = previousColor;
            }

            return changed;
        }

        private static bool DrawToggle(string label, ref bool value)
        {
            bool nextValue = GUILayout.Toggle(value, label, GUILayout.Width(105f));
            if (nextValue == value)
            {
                return false;
            }

            value = nextValue;
            return true;
        }

        private static bool DrawColoredToggle(string label, ref bool value, Color textColor)
        {
            Color previousContentColor = GUI.contentColor;
            GUI.contentColor = textColor;
            bool changed = DrawToggle(label, ref value);
            GUI.contentColor = previousContentColor;
            return changed;
        }

        private void ResetFilters()
        {
            _showBattle = true;
            _showCommand = true;
            _showAction = true;
            _showHook = true;
            _showObserved = true;
            _showSucceeded = true;
            _showRejected = true;
            _showInterrupted = true;
            _onlyProblems = false;
            _rootCommandText = string.Empty;
            _expandRootCommandChain = false;
            _rootCommandError = null;
        }

        private void RefreshRecords()
        {
            InMemoryBehaviorRecordSink sink = GetMemorySink();
            if (sink == null)
            {
                _observedSink = null;
                _observedSinkVersion = 0;
                _visibleRecords.Clear();
                _selectedRecord = null;
                return;
            }

            _observedSink = sink;
            _observedSinkVersion = sink.Version;
            RecordFilter filter = BuildFilter();
            if (filter == null)
            {
                _visibleRecords.Clear();
                _selectedRecord = null;
                return;
            }

            _visibleRecords = RecordQuery.Apply(sink.Records, filter);
            if (_visibleRecords.Count > MaxVisibleRecords)
            {
                _visibleRecords.RemoveRange(0, _visibleRecords.Count - MaxVisibleRecords);
            }

            if (_selectedRecord != null && !_visibleRecords.Contains(_selectedRecord))
            {
                _selectedRecord = null;
            }
        }

        private RecordFilter BuildFilter()
        {
            _rootCommandError = null;
            if ((!_showBattle && !_showCommand && !_showAction && !_showHook) ||
                (!_showObserved && !_showSucceeded && !_showRejected && !_showInterrupted))
            {
                return null;
            }

            RecordFilter filter = new RecordFilter
            {
                ExpandRootCommandChain = _expandRootCommandChain,
                OnlyProblems = _onlyProblems,
            };

            if (!_showBattle || !_showCommand || !_showAction || !_showHook)
            {
                if (_showBattle) filter.Categories.Add(BehaviorRecordCategory.Battle);
                if (_showCommand) filter.Categories.Add(BehaviorRecordCategory.Command);
                if (_showAction) filter.Categories.Add(BehaviorRecordCategory.Action);
                if (_showHook) filter.Categories.Add(BehaviorRecordCategory.Hook);
            }

            if (!_showObserved || !_showSucceeded || !_showRejected || !_showInterrupted)
            {
                if (_showObserved) filter.Outcomes.Add(EnumBehaviorRecordOutcome.Observed);
                if (_showSucceeded) filter.Outcomes.Add(EnumBehaviorRecordOutcome.Succeeded);
                if (_showRejected) filter.Outcomes.Add(EnumBehaviorRecordOutcome.Rejected);
                if (_showInterrupted) filter.Outcomes.Add(EnumBehaviorRecordOutcome.Interrupted);
            }

            if (!string.IsNullOrWhiteSpace(_rootCommandText))
            {
                if (!ulong.TryParse(_rootCommandText, out ulong rootCommandId) || rootCommandId == 0)
                {
                    _rootCommandError = "Root ID must be a positive integer.";
                    return null;
                }
                filter.RootCommandIds.Add(rootCommandId);
            }

            return filter;
        }

        private InMemoryBehaviorRecordSink GetMemorySink()
        {
            return _stateManager?.GameState?.BehaviorRecordMemory;
        }

        private static string BuildRow(BehaviorRecord record)
        {
            string root = record.RootCommandId?.ToString() ?? "-";
            return $"#{record.Sequence,-6} {record.UtcTime:HH:mm:ss.fff}  {record.Category,-7}  " +
                   $"{GetKind(record),-20} root={root,-6} {BuildSummary(record)}";
        }

        private static string BuildDetails(BehaviorRecord record)
        {
            return
                $"Session: {record.SessionId}\n" +
                $"Sequence: {record.Sequence}\n" +
                $"UTC: {record.UtcTime:O}\n" +
                $"Battle: {record.BattleId}\n" +
                $"Turn: {record.Turn}\n" +
                $"Root command: {record.RootCommandId?.ToString() ?? "-"}\n" +
                $"Category: {record.Category}\n" +
                $"Kind: {GetKind(record)}\n" +
                $"Outcome: {record.Outcome}\n" +
                $"Severity: {record.Severity}\n" +
                BuildSummary(record);
        }

        private static string GetKind(BehaviorRecord record)
        {
            if (record is BattleRecord battleRecord) return battleRecord.Kind.ToString();
            if (record is CommandRecord commandRecord) return commandRecord.Kind.ToString();
            if (record is ActionRecord actionRecord) return actionRecord.Kind.ToString();
            if (record is HookRecord hookRecord) return hookRecord.Kind.ToString();
            return record.GetType().Name;
        }

        private static string BuildSummary(BehaviorRecord record)
        {
            if (record is CommandRecord commandRecord)
            {
                return $"type={commandRecord.CommandType?.ToString() ?? "-"} " +
                       $"result={commandRecord.CommandResult?.ToString() ?? "-"} " +
                       $"source={commandRecord.Source?.ToString() ?? "-"} " +
                       $"target={commandRecord.Target?.ToString() ?? "-"} " +
                       $"card={commandRecord.Card?.ToString() ?? "-"}";
            }
            if (record is ActionRecord actionRecord)
            {
                return $"action={actionRecord.ActionId} type={actionRecord.ActionType}";
            }
            if (record is HookRecord hookRecord)
            {
                return $"hook={hookRecord.HookType} source={hookRecord.Source} target={hookRecord.Target} " +
                       $"value={hookRecord.Value} listener={hookRecord.ListenerOwner?.ToString() ?? "-"} " +
                       $"filter={hookRecord.ListenerFilter?.ToString() ?? "-"}";
            }
            return string.Empty;
        }
    }
}
#endif
