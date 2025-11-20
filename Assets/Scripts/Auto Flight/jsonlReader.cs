using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class jsonlReader : MonoBehaviour
{
    public string jsonlPath = "C:/MyData/instructions.jsonl";
    public float playbackDelay = 0.1f;  // time between instructions
    public GameObject targetObject;     // object that moves

    void Start()
    {
        StartCoroutine(ReadJsonl());
    }

    IEnumerator ReadJsonl()
    {
        using (StreamReader reader = new StreamReader(jsonlPath))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                Instruction msg = JsonUtility.FromJson<Instruction>(line);
                ApplyInstruction(msg);
                yield return new WaitForSeconds(playbackDelay);
            }
        }
    }

    void ApplyInstruction(Instruction msg)
    {
        if (msg.type == "PING" || msg.type == "MOVE")
        {
            Vector3 newPos = new Vector3(
                (float)msg.payload.x,
                (float)msg.payload.y,
                (float)msg.payload.z
            );

            targetObject.transform.position = newPos;
        }

        Debug.Log($"Applied {msg.type} → {msg.payload.x},{msg.payload.y},{msg.payload.z}");
    }

    [System.Serializable]
    public class Instruction
    {
        public string type;
        public int id;
        public Payload payload;
    }

    [System.Serializable]
    public class Payload
    {
        public double x;
        public double y;
        public double z;
        public string note;
    }
}
