using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DDoSAttacker))]
public class DDoSAttackerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DDoSAttacker attacker = (DDoSAttacker)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Drone Target Management", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Find All Drones with PacketReceiver"))
        {
            attacker.PopulateTargetDrones();
            EditorUtility.SetDirty(attacker);
        }

        EditorGUILayout.HelpBox(
            "This will find all GameObjects with PacketReceiver components (e.g., BlueInvisDrone, RedInvisDrone, GreenInvisDrone) and add them to the Target Drones list.",
            MessageType.Info
        );
    }

    private void OnSceneGUI()
    {
        DDoSAttacker attacker = (DDoSAttacker)target;

        if (attacker == null) return;

        Color rangeColor = attacker.attackActive ? new Color(1f, 0f, 0f, 0.2f) : new Color(1f, 1f, 0f, 0.2f);
        Color wireColor = attacker.attackActive ? new Color(1f, 0f, 0f, 0.4f) : new Color(1f, 1f, 0f, 0.4f);

        Handles.color = rangeColor;
        Handles.SphereHandleCap(0, attacker.transform.position, Quaternion.identity, attacker.attackRange * 2f, EventType.Repaint);

        Handles.color = wireColor;
        Handles.DrawWireDisc(attacker.transform.position, Vector3.up, attacker.attackRange);
        Handles.DrawWireDisc(attacker.transform.position, Vector3.right, attacker.attackRange);
        Handles.DrawWireDisc(attacker.transform.position, Vector3.forward, attacker.attackRange);

        Handles.color = Color.white;
        Handles.Label(attacker.transform.position + Vector3.up * (attacker.attackRange + 2f), 
            $"Attack Range: {attacker.attackRange}m\nDrones in Range: {attacker.dronesInRange}");

        if (Application.isPlaying)
        {
            Handles.color = new Color(1f, 0f, 0f, 0.5f);
            foreach (GameObject drone in attacker.targetDrones)
            {
                if (drone != null)
                {
                    float distance = Vector3.Distance(attacker.transform.position, drone.transform.position);
                    if (distance <= attacker.attackRange)
                    {
                        Handles.DrawDottedLine(attacker.transform.position, drone.transform.position, 2f);
                    }
                }
            }
        }

        EditorGUI.BeginChangeCheck();
        float newRange = Handles.RadiusHandle(Quaternion.identity, attacker.transform.position, attacker.attackRange);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(attacker, "Change Attack Range");
            attacker.attackRange = newRange;
        }
    }
}
