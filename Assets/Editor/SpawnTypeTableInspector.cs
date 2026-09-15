using System;
using System.Collections;
using System.Collections.Generic;
using J2.Spawning;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(SpawnTypeTable))]
public sealed class SpawnTypeTableInspector : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();

        var status = new HelpBox("ID는 서버 ObjectId와 동일합니다. 행 순서를 바꿔도 ID는 유지됩니다. 변경 시 서버 공용 JSON·헤더를 갱신합니다.", HelpBoxMessageType.Info);

        root.Add(status);
        root.Add(new Button(() =>
        {
            SpawnTypeTableEditorData.ImportMissing((SpawnTypeTable)target);
            SpawnTypeTableEditorData.ScheduleExport();
        }) { text = "기존 기물 중 미등록 항목 추가" });
        root.Add(new Button(() =>
        {
            SpawnTypeTableEditorData.Export((SpawnTypeTable)target);
            status.text = SpawnTypeTableEditorData.LastStatus;
        }) { text = "검증 / 서버로 내보내기" });
        root.Add(new PropertyField(serializedObject.FindProperty("entries"), "스폰 종류 목록"));
        root.TrackSerializedObjectValue(serializedObject, _ => SpawnTypeTableEditorData.ScheduleExport());
        root.schedule.Execute(() =>
        {
            if (!string.IsNullOrEmpty(SpawnTypeTableEditorData.LastStatus))
            {
                status.text = SpawnTypeTableEditorData.LastStatus;
            }
        }).Every(500);

        return root;
    }
}
