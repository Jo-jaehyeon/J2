using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DigitalArena;
using UnityEditor;
using UnityEngine;

public sealed class ArenaBalanceEditor : EditorWindow
{

    ArenaBalance	data;

    const string	DataPath = "Assets/Resources/ArenaBalance.json";
    string			status;

    Vector2	scroll;

    [MenuItem("Digital Arena/Balance Table")]
    static void Open()
    {
        GetWindow<ArenaBalanceEditor>("기물 데이터 테이블").minSize = new Vector2(800, 500);
    }

    void OnEnable()
    {
        Reload();
    }

    void Reload()
    {
        data = File.Exists(DataPath) ? JsonUtility.FromJson<ArenaBalance>(File.ReadAllText(DataPath)) : new ArenaBalance();
        status = "변경 사항은 저장 후 새 게임부터 적용됩니다.";
    }

    void OnGUI()
    {
        if (GUILayout.Button("디지몬 스탯 · 타입 · 속성 테이블 열기"))
        {
            DigimonTableEditor.Open();
        }

        EditorGUILayout.LabelField("레벨별 등장 확률 · 필요 경험치 · 코스트별 공유 재고", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("필요 XP는 해당 레벨에서 다음 레벨까지 필요한 경험치입니다. 예: 레벨 1의 6 → 1에서 2까지 6XP. 최대 레벨 32에는 적용하지 않습니다. 저장 후 새 게임부터 적용됩니다.", MessageType.Info);
        EditorGUILayout.HelpBox("상점 후보는 재고를 임시 예약하며 미구매 후보는 다음 라운드에 반환됩니다. 판매 시 별에 따라 원본 1/3/9개를 반환합니다. 별 합성은 재고를 변경하지 않습니다.", MessageType.Info);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(76);

        for (int tier = 0; tier < 5; tier++)
        {
            GUILayout.Label((tier + 1) + "코스트", GUILayout.Width(106));
        }

        GUILayout.Label("다음 레벨 필요 XP", GUILayout.Width(130));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("총 수량", GUILayout.Width(70));

        for (int tier = 0; tier < 5; tier++)
        {
            data.poolCounts[tier] = EditorGUILayout.IntField(data.poolCounts[tier], GUILayout.Width(106));
        }

        EditorGUILayout.EndHorizontal();
        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var row in data.levels)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("레벨 " + row.level, GUILayout.Width(70));

            for (int tier = 0; tier < 5; tier++)
            {
                row.weights[tier] = EditorGUILayout.IntField(row.weights[tier], GUILayout.Width(106));
            }

            if (row.level == data.levels.Length)
            {
                GUILayout.Label("MAX", GUILayout.Width(130));
            }
            else
            {
                row.requiredXp = EditorGUILayout.IntField(row.requiredXp, GUILayout.Width(130));
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        string error = data.Validate();

        EditorGUILayout.HelpBox(error ?? status, error == null ? MessageType.Info : MessageType.Error);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("저장", GUILayout.Height(32)))
        {
            if (error == null)
            {
                File.WriteAllText(DataPath, JsonUtility.ToJson(data, true));
                AssetDatabase.ImportAsset(DataPath);
                status = "저장 완료. 새 게임부터 적용됩니다.";
            }
        }

        if (GUILayout.Button("다시 불러오기", GUILayout.Height(32)))
        {
            Reload();
        }

        EditorGUILayout.EndHorizontal();
    }
}
