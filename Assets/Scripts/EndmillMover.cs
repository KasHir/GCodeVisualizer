using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using gs;

public class EndmillMover : MonoBehaviour
{
    public string gcodeFilePath = "Assets/gcode/test.cnc";
    private GenericGCodeParser gcodeParser;
    private IEnumerator gcodeRoutine;
    private float SCALE = 0.5f; // Coordinate scaling factor

    // G-code modal state
    private bool isAbsoluteMode = true;
    private string currentPlane = "XY";

    // G-code position and Speed state
    private Vector3 currentGCodePosition;
    private float defaultFeedRate = 400f;

    private Vector3 ConvertToUnityCoordinates(Vector3 gcodePosition)
    {
        Vector3 position = new Vector3(gcodePosition.x, gcodePosition.z, gcodePosition.y);
        return position * SCALE;
    }

    private Vector3 ConvertToGCodeCoordinates(Vector3 unityPosition)
    {
        Vector3 position = new Vector3(unityPosition.x, unityPosition.z, unityPosition.y);
        return position / SCALE;
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition, float moveTime)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveTime)
        {
            transform.position = Vector3.Lerp(
                startPosition, targetPosition, elapsedTime / moveTime);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        transform.position = targetPosition;
    }

    void Start()
    {
        gcodeParser = new GenericGCodeParser();
        string gcodeText = File.ReadAllText(gcodeFilePath);

        // Unityでの初期位置をG-code座標系に変換
        currentGCodePosition = ConvertToGCodeCoordinates(transform.position);

        using (StringReader reader = new StringReader(gcodeText))
        {
            GCodeFile gcodeFile = gcodeParser.Parse(reader);

            gcodeRoutine = MoveCylinderAlongGCode(gcodeFile);
            StartCoroutine(gcodeRoutine);

        }
    }

    private IEnumerator MoveCylinderAlongGCode(GCodeFile gcodeFile)
    {
        int previousMotionCommand = -1; // 動作モード (G0, G1, G2, G3)
        int previousPlaneCommand = 17;  // 平面選択 (G17, G18, G19)
        int previousDistanceCommand = 90; // 距離モード (G90, G91)
        int previousFeedrateCommand = 94; // 送り速度モード (G93, G94)

        foreach (var line in gcodeFile.AllLines())
        {

            Debug.Log($"Processing GCode Line: {line.parameters}");
            Debug.Log($"{line.orig_string}");
            Debug.Log($"linenumber: {line.lineNumber}");
            Debug.Log($"N: {line.N}");
            Debug.Log($"G: {line.code}");

            // Gコマンドの抽出
            List<int> gCodesInLine = new List<int>();
            if (line.code != -1)
            {
                gCodesInLine.Add(line.code);
            }
            if (line.parameters != null)
            {
                foreach (var param in line.parameters)
                {
                    UnityEngine.Debug.Log($"Param: {param.identifier} = {param.doubleValue}");
                    if (param.identifier == "G")
                    {
                        int gcodeValue = param.intValue;
                        gCodesInLine.Add(gcodeValue);
                    }
                }
            }

            // モーダルコマンドを処理してモーダル状態を更新
            foreach (int gcode in gCodesInLine)
            {
                switch (gcode)
                {
                    // Group12
                    case 90:
                        isAbsoluteMode = true;
                        previousDistanceCommand = 90;
                        Debug.Log("Switched to Absolute Coordinate Mode.");
                        break;
                    case 91:
                        isAbsoluteMode = false;
                        previousDistanceCommand = 91;
                        Debug.Log("Switched to Relative Coordinate Mode.");
                        break;

                    // Gropup03
                    case 17:
                        currentPlane = "XY";
                        previousPlaneCommand = 17;
                        Debug.Log("Plane set to XY");
                        break;
                    case 18:
                        currentPlane = "XZ";
                        previousPlaneCommand = 18;
                        Debug.Log("Plane set to XZ");
                        break;
                    case 19:
                        currentPlane = "YZ";
                        previousPlaneCommand = 19;
                        Debug.Log("Plane set to YZ");
                        break;

                    // Group*
                    case 21:
                        Debug.Log("G21: Millimeter Units");
                        break;

                    // Group09
                    case 54:
                        Debug.Log("Work coordinate system set to G54");
                        break;
                    default:
                        break;
                }
            }

            // 動作コマンドを決定
            int motionCommand = -1;
            foreach (int gcode in gCodesInLine)
            {
                if (gcode == 0 || gcode == 1 || gcode == 2 || gcode == 3)
                {
                    motionCommand = gcode;
                    previousMotionCommand = motionCommand;
                    break;
                }
            }
            if (motionCommand == -1)
            {
                motionCommand = previousMotionCommand; // 前回の動作コマンドを使用
            }

            // 動作コマンドを実行
            switch (motionCommand)
            {
                case 0:
                case 1:
                    Debug.Log($"Linear Move: {line.parameters}");
                    yield return HandleLinearMove(line);
                    break;
                case 2:
                case 3:
                    //yield return HandleArcMove(line);
                    break;
                default:
                    Debug.Log($"Unsupported motion command: G{motionCommand}");
                    break;
            }

            yield return null;
        }
    }

    private IEnumerator HandleLinearMove(GCodeLine line)
    {
        Vector3 targetGCodePosition = GetTargetPosition(line);
        float distance = Vector3.Distance(currentGCodePosition, targetGCodePosition);
        currentGCodePosition = targetGCodePosition;

        Vector3 targetPosition = ConvertToUnityCoordinates(targetGCodePosition);
        float feedRate = GetFeedRate(line);
        float moveTime = distance / (feedRate / 60f);

        yield return StartCoroutine(MoveToPosition(targetPosition, moveTime));
    }

    private Vector3 GetTargetPosition(GCodeLine line)
    {
        float x = currentGCodePosition.x;
        float y = currentGCodePosition.y;
        float z = currentGCodePosition.z;

        Dictionary<string, double> parameters = CacheParameters(line);

        if (parameters.ContainsKey("X"))
            x = isAbsoluteMode ? (float)parameters["X"] : x + (float)parameters["X"];
        if (parameters.ContainsKey("Y"))
            y = isAbsoluteMode ? (float)parameters["Y"] : y + (float)parameters["Y"];
        if (parameters.ContainsKey("Z"))
            z = isAbsoluteMode ? (float)parameters["Z"] : z + (float)parameters["Z"];

        return new Vector3(x, y, z);
    }

    private float GetFeedRate(GCodeLine line)
    {
        float feedRate = defaultFeedRate;

        Dictionary<string, double> parameters = CacheParameters(line);

        if (parameters.ContainsKey("F"))
            feedRate = (float)parameters["F"];

        return feedRate;
    }

    private Dictionary<string, double> CacheParameters(GCodeLine line)
    {
        Dictionary<string, double> parameters = new Dictionary<string, double>();
        foreach (var param in line.parameters)
        {
            parameters[param.identifier] = param.doubleValue;
        }
        return parameters;
    }

}
