using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SelectionHistory : EditorWindow
{
    private static List<Object> selectionHistory = new List<Object>();
    private static int currentIndex = -1;
    private static bool suppressSelectionChange = false;
    private const int MaxHistoryCount = 20;

    [MenuItem("Tools/Selection History")]
    public static void ShowWindow()
    {
        GetWindow<SelectionHistory>("Selection History");
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDestroy()
    {
        Selection.selectionChanged -= OnSelectionChanged;
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
        if (suppressSelectionChange) return;

        if (Selection.activeObject != null)
        {
            // 重複を防ぐ: 現在の履歴が直前と同じであれば追加しない
            if (currentIndex >= 0 && selectionHistory[currentIndex] == Selection.activeObject)
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

    private static void GoBack()
    {
        if (currentIndex > 0)
        {
            suppressSelectionChange = true;
            currentIndex--;
            SelectInProjectView(selectionHistory[currentIndex]);
            suppressSelectionChange = false;
        }
    }

    private static void GoForward()
    {
        if (currentIndex < selectionHistory.Count - 1)
        {
            suppressSelectionChange = true;
            currentIndex++;
            SelectInProjectView(selectionHistory[currentIndex]);
            suppressSelectionChange = false;
        }
    }

    private static void GoToIndex(int index)
    {
        if (index >= 0 && index < selectionHistory.Count)
        {
            suppressSelectionChange = true;
            currentIndex = index;
            SelectInProjectView(selectionHistory[currentIndex]);
            suppressSelectionChange = false;
        }
    }

    /// <summary>
    /// プロジェクトビューで対象のオブジェクトを選択
    /// </summary>
    /// <param name="obj">選択するオブジェクト</param>
    private static void SelectInProjectView(Object obj)
    {
        if (obj != null)
        {
            Selection.activeObject = obj;

            // プロジェクトビューをフォーカス
            EditorApplication.ExecuteMenuItem("Window/General/Project");
        }
    }

    /// <summary>
    /// ウィンドウを取得するが、フォーカスを変更しない
    /// </summary>
    private static T GetWindowWithoutFocus<T>() where T : EditorWindow
    {
        var window = Resources.FindObjectsOfTypeAll<T>();
        return window.Length > 0 ? window[0] : null;
    }
}
