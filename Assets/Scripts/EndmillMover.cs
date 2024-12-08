using UnityEngine;
using System.IO;
//using System.Collections.Generic;
using gs;

public class EndmillMover : MonoBehaviour
{
    public string gcodeFilePath = "Assets/gcode/test.cnc";
    private GenericGCodeParser gcodeParser;

    void Start()
    {
        gcodeParser = new GenericGCodeParser();
        string gcodeText = File.ReadAllText(gcodeFilePath);

        using (StringReader reader = new StringReader(gcodeText))
        {
            GCodeFile gcodeFile = gcodeParser.Parse(reader);

            foreach (var line in gcodeFile.AllLines())
            {
                Debug.Log($"Processing GCode Line: {line.parameters}");
                Debug.Log($"{line.orig_string}");
                Debug.Log($"linenumber: {line.lineNumber}");
                Debug.Log($"N: {line.N}");
                Debug.Log($"G: {line.code}");

                //List<int> gCodesInLine = new List<int>();

                if (line.parameters != null)
                {
                    foreach (var param in line.parameters)
                    {
                        Debug.Log($"Param ID: {param.identifier}");
                        Debug.Log($"Param Value: {param.doubleValue}");
                        //if (param.identifier == "G")
                        //{
                        //    int gcodeValue = param.intValue;
                        //    gCodesInLine.Add(gcodeValue);
                        //}
                    }
                }
            }

        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
