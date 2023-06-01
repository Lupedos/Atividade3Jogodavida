using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class GameofLife3d : MonoBehaviour
{
    struct Cube
    {
        public Vector3 position;
        public Color color;
        public int isAlive;
    }

    public ComputeShader computeShader;
    public int ncubes = 100;
    Cube[] data;
    public GameObject cubePrefab;
    GameObject[] gameObjects;

    public bool foi = false;
    public bool isRunning = false;
    public bool useGPU = false;

    private int kernelIndex;

    private void Start()
    {
        kernelIndex = computeShader.FindKernel("ProcessGrid3");
    }

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

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;
                int index = System.Array.IndexOf(gameObjects, clickedObject);
                if (index != -1)
                {
                    data[index].isAlive = 1 - data[index].isAlive;
                    UpdateCubeColor(index);
                }
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
        if (!foi)
            CreateCube();
    }

    private void EndGame()
    {
        isRunning = false;
    }

    private void CreateCube()
    {
        foi = true;
        data = new Cube[ncubes * ncubes * ncubes];
        gameObjects = new GameObject[ncubes * ncubes * ncubes];

        for (int i = 0; i < ncubes; i++)
        {
            float offsetX = (-ncubes / 2 + i);

            for (int j = 0; j < ncubes; j++)
            {
                float offsetY = (-ncubes / 2 + j);

                for (int k = 0; k < ncubes; k++)
                {
                    float offsetZ = (-ncubes / 2 + k);

                    GameObject go = GameObject.Instantiate(cubePrefab, new Vector3(offsetX * 1.1f, offsetY * 1.1f, offsetZ * 1.1f), Quaternion.identity);

                    data[i * ncubes * ncubes + j * ncubes + k] = new Cube();
                    data[i * ncubes * ncubes + j * ncubes + k].position = go.transform.position;
                    data[i * ncubes * ncubes + j * ncubes + k].isAlive = 0;
                    data[i * ncubes * ncubes + j * ncubes + k].color = Color.black;

                    gameObjects[i * ncubes * ncubes + j * ncubes + k] = go;
                    int index = System.Array.IndexOf(gameObjects, go);
                    UpdateCubeColor(index);
                }
            }
        }
    }

    private void ProcessCPU()
    {
        Cube[] newData = new Cube[data.Length];

        for (int i = 0; i < ncubes; i++)
        {
            for (int j = 0; j < ncubes; j++)
            {
                for (int k = 0; k < ncubes; k++)
                {
                    int index = i * ncubes * ncubes + j * ncubes + k;
                    int isAlive = data[index].isAlive;

                    int aliveNeighbors = CountAliveNeighbors(i, j, k);

                    if (isAlive == 1 && (aliveNeighbors < 2 || aliveNeighbors > 3))
                    {
                        isAlive = 0;
                        UpdateCubeColor(index, Color.black);
                    }
                    else if (isAlive == 0 && aliveNeighbors == 3)
                    {
                        isAlive = 1;
                        UpdateCubeColor(index, Color.red);
                    }

                    newData[index] = new Cube();
                    newData[index].position = data[index].position;
                    newData[index].color = data[index].color;
                    newData[index].isAlive = isAlive;
                }
            }
        }

        data = newData;
    }

    private int CountAliveNeighbors(int x, int y, int z)
    {
        int count = 0;

        for (int i = x - 1; i <= x + 1; i++)
        {
            for (int j = y - 1; j <= y + 1; j++)
            {
                for (int k = z - 1; k <= z + 1; k++)
                {
                    if (i >= 0 && i < ncubes && j >= 0 && j < ncubes && k >= 0 && k < ncubes && !(i == x && j == y && k == z) && data[i * ncubes * ncubes + j * ncubes + k].isAlive == 1)
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    private void ProcessGPU()
    {
       
        int cubeSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Cube)); // Size of the Cube struct

        int bufferSize = cubeSize * data.Length;

        ComputeBuffer cb = new ComputeBuffer(data.Length, cubeSize);
        cb.SetData(data);
        computeShader.SetBuffer(kernelIndex, "cubes", cb);

        int numGroups = Mathf.CeilToInt((float)data.Length / 16f);
        computeShader.Dispatch(kernelIndex, numGroups, 1, 1);

        cb.GetData(data);
        cb.Release();
        
        
    }

    private void UpdateCubeColor(int index, Color color)
    {
        gameObjects[index].GetComponent<Renderer>().material.SetColor("_Color", color);
        data[index].color = color;
    }

    private void UpdateCubeColor(int index)
    {
        Color color = data[index].isAlive == 1 ? Color.red : Color.black;
        gameObjects[index].GetComponent<Renderer>().material.SetColor("_Color", color);
        data[index].color = color;
    }
}
