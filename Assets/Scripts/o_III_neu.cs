using UnityEngine;
using System.Collections.Generic;
using System;

public class o_III_neu : MonoBehaviour
{
    public TextAsset dataFile;
    public Material pointMaterial;

    public Vector3 center;

    private bool dunkel = true;

    private float rotationSpeed=10;
    private int scalefactor=45;

    private const int BinCount = 20;

    private Mesh[] sphereMeshes = new Mesh[BinCount];
    private List<Matrix4x4>[] matrices = new List<Matrix4x4>[BinCount];
    private List<Vector3>[] velocities = new List<Vector3>[BinCount];
    private List<Vector3>[] originalPositions = new List<Vector3>[BinCount];
    private MaterialPropertyBlock[] props = new MaterialPropertyBlock[BinCount];

    private float factor = 3086.0f;
    private bool isMoving = false;
    private bool isVisible = true;
    private bool isRotatingX = false;
    private bool isRotatingY = false;
    private bool isRotatingZ = false; // Neue Variable zur Steuerung der Rotation

    public Color linecolor;

    void Start()
    {
        Application.targetFrameRate = 144;
        string[] dataLines = dataFile.text.Split('\n');

        for (int i = 0; i < BinCount; i++)
        {
            props[i] = new MaterialPropertyBlock();
            float emissionFactor = (i + 1) *1f/BinCount;
            props[i].SetColor("_Color", linecolor);
            props[i].SetColor("_EmissionColor", linecolor * emissionFactor);

            sphereMeshes[i] = CreateSphereMesh();
            matrices[i] = new List<Matrix4x4>();
            velocities[i] = new List<Vector3>();
            originalPositions[i] = new List<Vector3>();
        }

        float minFlux = 150; // Kleinster nicht-null Flux-Wert, den deine Daten annehmen können
        float maxFlux = 4550; // Größter Flux-Wert, den deine Daten annehmen können

        // Bestimme die Grenzen für die logarithmischen Bins
        float logMin = Mathf.Log10(minFlux);
        float logMax = Mathf.Log10(maxFlux);
        float rangePerBin = (logMax - logMin) / BinCount;

        for (int i = 1; i < dataLines.Length - 1; i++)
        {
            string[] data = dataLines[i].Split(',');

            if (data.Length >= 7)
            {
                if (TryParseFloat(data[2], out float x) &&
                    TryParseFloat(data[3], out float y) &&
                    TryParseFloat(data[4], out float z) &&
                    TryParseFloat(data[5], out float v) &&
                    TryParseFloat(data[6], out float flux))
                {
                    float logFlux = Mathf.Log10(flux);
                    int index = (int)((logFlux - logMin) / rangePerBin);
                    index = Mathf.Clamp(index, 0, BinCount - 1);

                    Vector3 position = new Vector3(x, y, z) * scalefactor - center;
                    matrices[index].Add(Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * 0.05f));
                    velocities[index].Add((position + center).normalized * v / factor);
                    originalPositions[index].Add(position); // Speichern der ursprünglichen Position
                }
            }
            else
            {
                Debug.LogWarning($"Skipping incomplete data line at index {i + 1}.");
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isMoving = !isMoving;
            if (isMoving)
            {
                ResetPositions();
                isRotatingX = false;
                isRotatingY = false;
                isRotatingZ = false;

            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPositions();
            isRotatingX = false;
            isRotatingY = false;
            isRotatingZ = false;
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            isVisible = !isVisible;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            dunkel = !dunkel;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            isRotatingX = !isRotatingX;
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            isRotatingY = !isRotatingY;
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            isRotatingZ = !isRotatingZ;
        }

        if (isMoving)
        {
            MovePositions();
        }

        if (isRotatingX)
        {
            RotatePositions(new Vector3(0,0,1));
        }
        if (isRotatingY)
        {
            RotatePositions(new Vector3(0,1,0));
        }
        if (isRotatingZ)
        {
            RotatePositions(new Vector3(1,0,0));
        }

        DrawMeshes();
    }

    private void ResetPositions()
    {
        for (int i = 0; i < BinCount; i++)
        {
            for (int j = 0; j < originalPositions[i].Count; j++)
            {
                Vector3 resetPosition = originalPositions[i][j];
                matrices[i][j] = Matrix4x4.TRS(resetPosition, Quaternion.identity, Vector3.one * 0.05f);
            }
        }
    }

    private void MovePositions()
    {
        for (int i = 0; i < BinCount; i++)
        {
            for (int j = 0; j < matrices[i].Count; j++)
            {
                Matrix4x4 matrix = matrices[i][j];
                Vector3 position = matrix.GetColumn(3); // Extrahiert die Position als Vector3
                position += (velocities[i][j] * Time.deltaTime); // Korrekte Addition von Vector3 zu Vector3
                matrices[i][j] = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * 0.05f); // Erzeugt eine neue Transformationsmatrix
            }
        }
    }

    private void RotatePositions(Vector3 axis)
    {
        float angle = rotationSpeed * Time.deltaTime; // Berechne den Winkel basierend auf der Rotationgeschwindigkeit und der Zeit
        Quaternion rotation = Quaternion.AngleAxis(angle, axis); // Erzeuge eine Rotation um die gegebene Achse

        for (int i = 0; i < BinCount; i++)
        {
            for (int j = 0; j < matrices[i].Count; j++)
            {
                Matrix4x4 matrix = matrices[i][j];
                Vector3 position = matrix.GetColumn(3); // Extrahiere die Position als Vector3
                position = rotation * position; // Drehe die Position um die gegebene Achse
                matrices[i][j] = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * 0.05f); // Erzeuge eine neue Transformationsmatrix
            }
        }
    }

    private void DrawMeshes()
    {
        for (int i = 0; i < BinCount; i++)
        {
            if (matrices[i].Count > 0 && isVisible && (dunkel || i > 2))
            {
                Graphics.DrawMeshInstanced(sphereMeshes[i], 0, pointMaterial, matrices[i], props[i]);
            }
        }
    }

    private Mesh CreateSphereMesh()
    {
        // Erzeuge ein primitives Sphären-GameObject
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        // Extrahiere das Mesh
        Mesh mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
        // Zerstöre das GameObject sofort, um das Mesh beizubehalten, aber das GameObject selbst zu entfernen
        Destroy(sphere);
        return mesh;
    }

    bool TryParseFloat(string str, out float result)
    {
        return float.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result) && !float.IsNaN(result);
    }
}
