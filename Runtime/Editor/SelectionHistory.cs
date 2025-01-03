/*
 * SelectionHistory.cs
 * 
 * This script defines an EditorWindow in Unity that keeps track of the selection history of objects in the editor.
 * It allows users to navigate back and forth through their selection history using "Back" and "Forward" buttons.
 * The selection history is displayed in a scrollable list, with the most recent selections at the top.
 * 
 * Features:
 * - Tracks up to 20 selections.
 * - Provides "Back" and "Forward" buttons to navigate through the selection history.
 * - Displays the selection history in a scrollable list.
 * 
 * Usage:
 * - Open the window from the Unity menu: Tools -> Selection History.
 * - Use the "Back" and "Forward" buttons to navigate through the selection history.
 * - Click on an item in the history list to reselect it.
 * 
 * Note:
 * - The selection history is cleared when the window is closed.
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class SelectionHistory : EditorWindow
{
    private static List<Object> selectionHistory = new List<Object>();
    private static int currentIndex = -1;
    private const int MaxHistoryCount = 20;

    private const string HistoryKey = "SelectionHistory_Data";
    private const string IndexKey = "SelectionHistory_Index";

    private static bool isInitialized = false;

    [MenuItem("Tools/Selection History")]
    public static void ShowWindow()
    {
        GetWindow<SelectionHistory>("Selection History");
        Initialize();
    }

    private void OnEnable()
    {
        // イベントの登録
        Selection.selectionChanged += OnSelectionChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // 履歴を復元
        if (!isInitialized)
        {
            LoadHistory();
            isInitialized = true;
        }
    }

    private void OnDisable()
    {
        // イベントの解除
        Selection.selectionChanged -= OnSelectionChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        // 履歴を保存
        SaveHistory();
    }

    private void OnDestroy()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        SaveHistory();
    }

    private static void Initialize()
    {
        // `Initialize` メソッドは `OnEnable` で処理するため不要
        /*
        if (isInitialized) return;

        Selection.selectionChanged += OnSelectionChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        LoadHistory();
        isInitialized = true;
        */
    }

    private Vector2 scrollPosition;

    private void OnGUI()
    {
        GUILayout.Label("選択履歴管理", EditorStyles.boldLabel);

        // ボタン群をウィンドウ上部に配置
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = currentIndex > 0;
        if (GUILayout.Button("戻る", GUILayout.Height(30)))
        {
            GoBack();
        }
        GUI.enabled = currentIndex < selectionHistory.Count - 1;
        if (GUILayout.Button("進む", GUILayout.Height(30)))
        {
            GoForward();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);

        // 履歴リスト表示（スクロール可能、上から新しい順）
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        for (int i = selectionHistory.Count - 1; i >= 0; i--)
        {
            if (selectionHistory[i] == null)
            {
                selectionHistory.RemoveAt(i);
                if (i <= currentIndex)
                {
                    currentIndex--;
                }
                continue;
            }

            if (i == currentIndex)
            {
                GUILayout.Label($"-> {selectionHistory[i]?.name ?? "null"}", EditorStyles.boldLabel);
            }
            else
            {
                if (GUILayout.Button(selectionHistory[i]?.name ?? "null"))
                {
                    GoToIndex(i);
                }
            }
        }

        GUILayout.EndScrollView();
    }

    private static void OnSelectionChanged()
    {
        if (Selection.activeObject != null)
        {
            // 履歴内の無効なオブジェクトを削除
            CleanInvalidObjects();

            // 重複を防ぐ: 現在の履歴が直前と同じであれば追加しない
            if (currentIndex >= 0 && currentIndex < selectionHistory.Count && selectionHistory[currentIndex] == Selection.activeObject)
            {
                return;
            }

            if (currentIndex < selectionHistory.Count - 1)
            {
                // 戻る操作後に新しい選択が発生した場合、「進む」履歴を削除
                selectionHistory.RemoveRange(currentIndex + 1, selectionHistory.Count - currentIndex - 1);
            }

            // 新しい選択を履歴に追加
            selectionHistory.Add(Selection.activeObject);

            // 履歴の最大数を超えたら古いものを削除
            if (selectionHistory.Count > MaxHistoryCount)
            {
                selectionHistory.RemoveAt(0);
            }

            currentIndex = selectionHistory.Count - 1;

            // ウィンドウの再描画をリクエスト
            var window = GetWindowWithoutFocus<SelectionHistory>();
            window?.Repaint();
        }
    }
    private static void CleanInvalidObjects()
    {
        // 履歴内の無効なオブジェクトを削除
        for (int i = selectionHistory.Count - 1; i >= 0; i--)
        {
            if (selectionHistory[i] == null)
            {
                selectionHistory.RemoveAt(i);
                if (i <= currentIndex)
                {
                    currentIndex--; // カレントインデックスを調整
                }
            }
        }

        // currentIndex を範囲内に調整
        if (currentIndex >= selectionHistory.Count)
        {
            currentIndex = selectionHistory.Count - 1;
        }

        if (currentIndex < 0 && selectionHistory.Count > 0)
        {
            currentIndex = 0;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearHistory();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        ClearHistory();
    }

    private static void ClearHistory()
    {
        selectionHistory.Clear();
        currentIndex = -1;

        var window = GetWindowWithoutFocus<SelectionHistory>();
        window?.Repaint();
    }

    private static void GoBack()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            SelectInProjectView(selectionHistory[currentIndex]);
        }
    }

    private static void GoForward()
    {
        if (currentIndex < selectionHistory.Count - 1)
        {
            currentIndex++;
            SelectInProjectView(selectionHistory[currentIndex]);
        }
    }

    private static void GoToIndex(int index)
    {
        if (index >= 0 && index < selectionHistory.Count)
        {
            currentIndex = index;
            SelectInProjectView(selectionHistory[currentIndex]);
        }
    }

    private static void SaveHistory()
    {
        var validHistory = selectionHistory.Where(o => o != null).Select(o => AssetDatabase.GetAssetPath(o)).ToArray();
        EditorPrefs.SetString(HistoryKey, string.Join(";", validHistory));
        EditorPrefs.SetInt(IndexKey, currentIndex);
    }

    private static void LoadHistory()
    {
        selectionHistory.Clear();

        var savedHistory = EditorPrefs.GetString(HistoryKey, "");
        if (!string.IsNullOrEmpty(savedHistory))
        {
            var paths = savedHistory.Split(';');
            foreach (var path in paths)
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj != null)
                {
                    selectionHistory.Add(obj);
                }
            }
        }

        currentIndex = EditorPrefs.GetInt(IndexKey, -1);
    }

    private static void SelectInProjectView(Object obj)
    {
        if (obj != null)
        {
            Selection.activeObject = obj;
            EditorApplication.ExecuteMenuItem("Window/General/Project");
        }
    }

    private static T GetWindowWithoutFocus<T>() where T : EditorWindow
    {
        var window = Resources.FindObjectsOfTypeAll<T>();
        return window.Length > 0 ? window[0] : null;
    }
}
