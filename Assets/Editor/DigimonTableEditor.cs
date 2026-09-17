using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using DigitalArena;
using UnityEditor;
using UnityEngine;

public sealed class DigimonTableEditor : EditorWindow
{

    DigimonCatalog	data;

    const string	Path = "Assets/Resources/DigimonCatalog.json";
    int				side, selected;
    string			status = "저장 후 새 게임부터 적용됩니다.";

    Vector2	scroll;

    [MenuItem("Digital Arena/Digimon Table")]
    public static void Open()
    {
        GetWindow<DigimonTableEditor>("디지몬 데이터 테이블").minSize = new Vector2(760, 670);
    }

    void OnEnable()
    {
        Reload();
    }

    void Reload()
    {
        data = File.Exists(Path) ? JsonUtility.FromJson<DigimonCatalog>(File.ReadAllText(Path)) : new DigimonCatalog();
        selected = 0;
    }

    DigimonData[] Rows => side == 0 ? data.allies : data.enemies;

    void SetRows(DigimonData[] rows)
    {
        if (side == 0)
        {
            data.allies = rows;
        }
        else
        {
            data.enemies = rows;
        }
    }

    void Add(bool duplicate)
    {
        var row = duplicate && Rows.Length > 0 ? Rows[selected].Copy() : new DigimonData();

        row.id = "digimon_" + Guid.NewGuid().ToString("N");
        row.name = duplicate ? row.name + " 복사" : "새 디지몬";

        if (!duplicate)
        {
            row.cost = side == 0 ? 1 : 0;
            row.startStage = 1;
        }

        SetRows(Rows.Concat(new[] { row }).ToArray());
        selected = Rows.Length - 1;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("디지몬 등록 · 아군 " + data.allies.Length + "종 / 크립 " + data.enemies.Length + "종", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("추가 → 설정 → 저장 → 게임 재시작. 같은 코스트의 기물은 균등 추첨하며 코스트별 재고를 공유합니다. 동일 기물·동일 별 3개로 별을 강화합니다.", MessageType.Info);

        int newSide = GUILayout.Toolbar(side, new[] { "플레이어", "크립" });

        if (newSide != side)
        {
            side = newSide;
            selected = 0;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("새 기물 추가"))
        {
            Add(false);
        }

        if (GUILayout.Button("선택 기물 복제"))
        {
            Add(true);
        }

        EditorGUILayout.EndHorizontal();
        selected = Mathf.Clamp(selected, 0, Rows.Length - 1);
        selected = EditorGUILayout.Popup("기물 선택", selected, Rows.Select(d => d.name + " [" + d.id + "]").ToArray());

        var row = Rows[selected];

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.SelectableLabel("ID: " + row.id, GUILayout.Height(20));
        row.name = EditorGUILayout.TextField("이름", row.name);

        if (side == 0)
        {
            row.cost = EditorGUILayout.Popup("코스트", row.cost - 1, new[]{"1코스트","2코스트","3코스트","4코스트","5코스트"}) + 1;

            var targets = data.allies.Where(d => d.id != row.id).ToArray();

            int target = Array.FindIndex(targets, d => d.id == row.evolvesTo) + 1;

            int next = EditorGUILayout.Popup("진화 경로 (증강·아이템용 보존)", target, new[] { "없음" }.Concat(targets.Select(d => d.name + " [" + d.id + "]")).ToArray());

            if (next != target)
            {
                row.evolvesTo = next == 0 ? "" : targets[next - 1].id;
            }
        }
        else
        {
            row.startStage = EditorGUILayout.IntField("등장 시작 스테이지", row.startStage);
        }

        EditorGUILayout.HelpBox("크립은 현재 스테이지 이하에서 가장 높은 등장 시작 스테이지의 기물을 사용합니다. 같은 스테이지로 등록한 크립은 순서대로 섞여 등장합니다.", MessageType.None);
        row.modelTier = EditorGUILayout.IntSlider("기본 도형 외형 번호", row.modelTier, 0, 6);
        row.placeholder = (PlaceholderModel)EditorGUILayout.Popup("임시 도형", (int)row.placeholder, new[] { "기존 외형", "파랑 정육면체", "초록 구", "보라 캡슐" });

        var prefab = string.IsNullOrEmpty(row.prefabPath) ? null : Resources.Load<GameObject>(row.prefabPath);

        var chosen = (GameObject)EditorGUILayout.ObjectField("외형 프리팹 (선택)", prefab, typeof(GameObject), false);

        if (chosen != prefab)
        {
            string path = chosen == null ? "" : AssetDatabase.GetAssetPath(chosen).Replace('\\', '/');

            int marker = path.IndexOf("/Resources/", StringComparison.Ordinal);

            if (chosen == null)
            {
                row.prefabPath = "";
            }
            else if (marker < 0 || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                status = "프리팹은 Assets 아래 Resources 폴더 안에 저장하세요.";
            }
            else
            {
                row.prefabPath = path.Substring(marker + 11, path.Length - marker - 11 - 7);
            }
        }

        EditorGUILayout.LabelField("프리팹 경로", string.IsNullOrEmpty(row.prefabPath) ? "없음 · 기본 도형 사용" : row.prefabPath);

        if (!string.IsNullOrEmpty(row.prefabPath) && prefab == null && GUILayout.Button("누락된 프리팹 연결 해제"))
        {
            row.prefabPath = "";
        }

        row.HP = EditorGUILayout.FloatField("HP", row.HP);
        row.SP = EditorGUILayout.FloatField("최대 SP", row.SP);
        row.ATK = EditorGUILayout.FloatField("ATK", row.ATK);
        row.DEF = EditorGUILayout.FloatField("DEF", row.DEF);
        row.INT = EditorGUILayout.FloatField("INT", row.INT);
        row.SPD = EditorGUILayout.FloatField("SPD (칸/초)", row.SPD);
        row.range = EditorGUILayout.IntSlider("기본 공격 사거리 (칸)", row.range, 1, 4);
        EditorGUILayout.BeginHorizontal();
        row.attack = (AttackKind)EditorGUILayout.Popup("공격 구분", (int)row.attack, new[] { "물리형", "특수형" });
        row.role = (DigimonRole)(EditorGUILayout.Popup("역할군", (int)row.role + 1, DigimonCatalog.RoleNames) - 1);
        EditorGUILayout.EndHorizontal();
        row.type = (DigimonType)EditorGUILayout.Popup("타입", (int)row.type, DigimonCatalog.TypeNames);
        row.element = (DigimonElement)EditorGUILayout.Popup("속성", (int)row.element, DigimonCatalog.ElementNames);
        row.spPerAttack = EditorGUILayout.FloatField("기본 공격당 SP", row.spPerAttack);
        row.skillPower = EditorGUILayout.FloatField("특수 공격 배율", row.skillPower);

        bool referenced = data.allies.Any(d => d.evolvesTo == row.id);

        using (new EditorGUI.DisabledScope(Rows.Length <= 1 || referenced))
        {
            if (GUILayout.Button("선택 기물 삭제"))
            {
                SetRows(Rows.Where(d => d != row).ToArray());
                selected = 0;
            }
        }

        if (referenced)
        {
            EditorGUILayout.HelpBox("진화 대상으로 사용 중입니다. 해당 연결을 먼저 변경하면 삭제할 수 있습니다.", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();

        string error = data.Validate();

        if (error == null && data.allies.Concat(data.enemies).Any(d => !string.IsNullOrEmpty(d.prefabPath) && Resources.Load<GameObject>(d.prefabPath) == null))
        {
            error = "누락된 프리팹 연결을 수정하세요.";
        }

        EditorGUILayout.HelpBox(error ?? status, error == null ? MessageType.Info : MessageType.Error);
        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("저장", GUILayout.Height(32)))
            {
                File.WriteAllText(Path, JsonUtility.ToJson(data, true));
                AssetDatabase.ImportAsset(Path);
                status = "저장 완료. 게임 재시작으로 적용하세요.";
            }
        }

        if (GUILayout.Button("다시 불러오기", GUILayout.Height(32)))
        {
            Reload();
        }

        if (GUILayout.Button("확률 / 재고 테이블", GUILayout.Height(32)))
        {
            EditorApplication.ExecuteMenuItem("Digital Arena/Balance Table");
        }

        EditorGUILayout.EndHorizontal();
    }
}
