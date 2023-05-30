using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nome : MonoBehaviour
{
    struct Cube
    {
        public Vector2 position;
        public Color color;
    }

    public ComputeShader computeShader;
    public int ncubes = 100;
    Cube[] data;
    public GameObject cubePrefab;
    GameObject[] gameObjects;

    public bool foi = false;
    public bool isRunning = false; // Indica se o jogo está em execução
    public bool useGPU = false; // Indica se a execução é na GPU

    private void Update()
    {
        if (isRunning)
        {
            if (useGPU)
            {
                ProcessGPU();
            }
            else
            {
                ProcessCPU();
            }
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 100, 50), "Iniciar"))
        {
            StartGame();
        }

        if (GUI.Button(new Rect(110, 0, 100, 50), "Finalizar"))
        {
            EndGame();
        }

        if (GUI.Button(new Rect(220, 0, 100, 50), "Alternar"))
        {
            useGPU = !useGPU;
        }
    }

    private void StartGame()
    {
        isRunning = true;
        if(!foi)
        CreateCube();
    }

    private void EndGame()
    {
        isRunning = false;
    }

    private void CreateCube()
    {
        foi = true;
        data = new Cube[ncubes * ncubes];
        gameObjects = new GameObject[ncubes * ncubes];

        for (int i = 0; i < ncubes; i++)
        {
            float offsetX = (-ncubes / 2 + i);

            for (int j = 0; j < ncubes; j++)
            {
                float offsetY = (-ncubes / 2 + j);

                GameObject go = GameObject.Instantiate(cubePrefab, new Vector2(offsetX * 1.1f, offsetY * 1.1f), Quaternion.identity);
                gameObjects[i * ncubes + j] = go;

                data[i * ncubes + j] = new Cube();
                data[i * ncubes + j].position = go.transform.position;
            }
        }
    }

    private void ProcessCPU()
    {
        for (int i = 0; i < ncubes; i++)
        {
            for (int j = 0; j < ncubes; j++)
            {
                gameObjects[i * ncubes + j].GetComponent<SpriteRenderer>().material.SetColor("_Color", Random.ColorHSV());
            }
        }
    }

    private void ProcessGPU()
    {
        int totalBytes = sizeof(float) * 3 + sizeof(float) * 4;

        ComputeBuffer cb = new ComputeBuffer(data.Length, totalBytes);
        cb.SetData(data);
        computeShader.SetBuffer(0, "cubes", cb);

        computeShader.Dispatch(0, data.Length / 10, 1, 1);
    }
}
