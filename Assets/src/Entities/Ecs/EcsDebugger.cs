using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Reflex.Attributes;

public class EcsDebugger : MonoBehaviour {
    [Inject] private Ecs _ecs;

    public float WindowWidth  = 1600f;
    public float WindowHeight = 900f;

    // UI state
    private bool       _visible        = true;
    private Vector2    _scrollPos      = Vector2.zero;
    private bool[]     _archetypeFolds;
    private int        _lastArchCount  = 0;

    // Cached display data, rebuilt each frame (cheap enough for a debugger)
    private readonly StringBuilder _sb = new();

    // Styles (initialized once in OnGUI because GUI styles require the skin to be ready)
    private GUIStyle _headerStyle;
    private GUIStyle _archetypeStyle;
    private GUIStyle _entityStyle;
    private bool     _stylesInitialized;

    private Rect _windowRect;


    private void Awake() {
        _windowRect = new Rect(10, 10, WindowWidth, WindowHeight);
    }

    private void InitStyles() {
        if (_stylesInitialized) return;

        _headerStyle = new GUIStyle(GUI.skin.label) {
            fontSize  = 24,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.9f, 0.85f, 0.4f) }
        };

        _archetypeStyle = new GUIStyle(GUI.skin.label) {
            fontSize  = 20,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.55f, 0.85f, 1f) }
        };

        _entityStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 20,
            normal   = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        _stylesInitialized = true;
    }

    private void OnGUI() {
        InitStyles();

        // Toggle button always visible
        if (GUI.Button(new Rect(10, 10, 120, 24), _visible ? "Hide ECS Debug" : "Show ECS Debug")) {
            _visible = !_visible;
        }

        if (!_visible || _ecs == null) return;

        _windowRect = GUI.Window(0, _windowRect, DrawWindow, "ECS Debugger");
    }

    private void DrawWindow(int id) {
        if (_ecs == null) {
            GUILayout.Label("Ecs not injected yet.");
            GUI.DragWindow();
            return;
        }

        var archetypes = _ecs.EntitiesByArchetype;

        // Resize fold array if archetype count changed
        if (_archetypeFolds == null || archetypes.Count != _lastArchCount) {
            _archetypeFolds = new bool[archetypes.Count];
            for (var i = 0; i < _archetypeFolds.Length; i++) _archetypeFolds[i] = true;
            _lastArchCount = archetypes.Count;
        }

        // ── Header ────────────────────────────────────────────────────────────
        GUILayout.Label(
            $"Components: {_ecs.ComponentsCount}   Archetypes: {archetypes.Count}",
            _headerStyle);

        GUILayout.Space(4);

        // ── Scrollable content ────────────────────────────────────────────────
        _scrollPos = GUILayout.BeginScrollView(_scrollPos,
            GUILayout.Width(WindowWidth - 12),
            GUILayout.Height(WindowHeight - 70));

        var archIndex = 0;
        foreach (var kvp in archetypes) {
            var archBitset  = kvp.Key;
            var entityList  = kvp.Value;
            var entityCount = entityList.Count;

            // Build archetype label: component names separated by " | "
            var archLabel = BuildArchetypeLabel(archBitset);

            // Foldout header row
            GUILayout.BeginHorizontal();
            var foldLabel = _archetypeFolds[archIndex] ? "▼" : "▶";
            if (GUILayout.Button($"{foldLabel} {archLabel}  [{entityCount}]",
                    _archetypeStyle,
                    GUILayout.ExpandWidth(true))) {
                _archetypeFolds[archIndex] = !_archetypeFolds[archIndex];
            }
            GUILayout.EndHorizontal();

            // Entity list
            if (_archetypeFolds[archIndex] && entityCount > 0) {
                GUILayout.BeginVertical(GUI.skin.box);

                _sb.Clear();
                for (var i = 0; i < entityCount; i++) {
                    _sb.Append(entityList[i]);
                    if (i < entityCount - 1) _sb.Append("  ");

                    // Wrap lines every 10 entities for readability
                    if ((i + 1) % 10 == 0 && i < entityCount - 1) {
                        GUILayout.Label(_sb.ToString(), _entityStyle);
                        _sb.Clear();
                    }
                }
                if (_sb.Length > 0) {
                    GUILayout.Label(_sb.ToString(), _entityStyle);
                }

                GUILayout.EndVertical();
            }

            GUILayout.Space(2);
            archIndex++;
        }

        GUILayout.EndScrollView();

        GUI.DragWindow(new Rect(0, 0, WindowWidth, 20));
    }

    private string BuildArchetypeLabel(BitSet archBitset) {
        _sb.Clear();

        var found = false;
        foreach (var kv in _ecs.BitByType) {
            var type = kv.Key;
            var bit  = kv.Value;

            if (archBitset.TestBit(bit)) {
                if (found) _sb.Append(" | ");
                _sb.Append(type.Name);
                found = true;
            }
        }

        if (!found) _sb.Append("<empty>");

        return _sb.ToString();
    }
}
