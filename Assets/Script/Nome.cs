using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
public class Nome : MonoBehaviour
{
    struct Cube
    {
        public Vector2 position;
        public Color color;
        public int isAlive;
        public int aliveNeighbors;
        
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
        kernelIndex = computeShader.FindKernel("ProcessGrid");
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
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
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
        data = new Cube[ncubes * ncubes];
        gameObjects = new GameObject[ncubes * ncubes];

        for (int i = 0; i < ncubes; i++)
        {
            float offsetX = (-ncubes / 2 + i);

            for (int j = 0; j < ncubes; j++)
            {
                float offsetY = (-ncubes / 2 + j);

                GameObject go = GameObject.Instantiate(cubePrefab, new Vector2(offsetX * 1.1f, offsetY * 1.1f), Quaternion.identity);

                data[i * ncubes + j] = new Cube();
                data[i * ncubes + j].position = go.transform.position;
                data[i * ncubes + j].isAlive = 0;
                data[i * ncubes + j].color = Color.black;
                

                gameObjects[i * ncubes + j] = go;
                int index = System.Array.IndexOf(gameObjects, go);
                UpdateCubeColor(index);
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
                int index = i * ncubes + j;
                int isAlive = data[index].isAlive;

                int aliveNeighbors = CountAliveNeighbors(i, j);

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

        data = newData;
    }

    private int CountAliveNeighbors(int x, int y)
    {
        int count = 0;

        for (int i = x - 1; i <= x + 1; i++)
        {
            for (int j = y - 1; j <= y + 1; j++)
            {
                if (i >= 0 && i < ncubes && j >= 0 && j < ncubes && !(i == x && j == y) && data[i * ncubes + j].isAlive == 1)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void ProcessGPU()
    {
        try
        {
            int cubeSize = Marshal.SizeOf(new Cube());   // Tamanho da estrutura Cube
            // Calcula o tamanho do buffer com base no tamanho da estrutura Cube
            int bufferSize = Mathf.CeilToInt((float)data.Length * cubeSize);
            
            ComputeBuffer cb = new ComputeBuffer(ncubes * ncubes, cubeSize);

            for (int i = 0; i < ncubes; i++)
            {
                for (int j = 0; j < ncubes; j++)
                {
                    int index = i * ncubes + j;
                    int aliveNeighbors = CountAliveNeighbors(i, j);
                    data[index].aliveNeighbors = aliveNeighbors;
                }
            }

            cb.SetData(data);
            
            computeShader.SetBuffer(kernelIndex, "cubes", cb);
            
            int numGroups = Mathf.CeilToInt((float)data.Length / 16f);
      
            computeShader.Dispatch(kernelIndex, numGroups, 1, 1);            
            cb.GetData(data);
            for (int i = 0; i < bufferSize; i++)
            {
                gameObjects[i].GetComponent<SpriteRenderer>().material.SetColor("_Color", data[i].color);
                //// Update the corresponding object in the scene with the modified color
                //GameObject obj = data[i]; /* Get the GameObject corresponding to the objectDataArray[i] */;
                //Renderer renderer = obj.GetComponent<Renderer>();
                //renderer.material.color = data[i].color;
            }
            cb.Release();
        }
        catch (Exception ex )
        {
            Debug.Log("Erro do metodo ProcessGPU: " + ex.Message);
        }
    }

    private void UpdateCubeColor(int index, Color color)
    {
        gameObjects[index].GetComponent<SpriteRenderer>().material.SetColor("_Color", color);
        data[index].color = color;
    }

    private void UpdateCubeColor(int index)
    {
        Color color = data[index].isAlive == 1 ? Color.red : Color.black;
        gameObjects[index].GetComponent<SpriteRenderer>().material.SetColor("_Color", color);
        data[index].color = color;
    }
}
