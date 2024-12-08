using UnityEngine;
using System.IO;

public class EndmillMover : MonoBehaviour
{
    // Start is called once before the first execution of Update
    // after the MonoBehaviour is created
    void Start()
    {
        transform.position = new Vector3(0, 0, 0);
        Debug.Log("Hello World");

        // Read the gcode/test.cnc file line by line and log each line
        string path = "Assets/gcode/test.cnc";
        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                Debug.Log(line);
            }
        }
        else
        {
            Debug.LogError("File not found: " + path);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
