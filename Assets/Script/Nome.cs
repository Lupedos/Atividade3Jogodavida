using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nome : MonoBehaviour
{
    struct Cube
    {
        public Vector2 position;
        public Color color;
        public bool isAlive;
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

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            //Debug.Log("1");
            if (hit.collider != null)
            {
                //Debug.Log("2");
                GameObject clickedObject = hit.collider.gameObject;
                int index = System.Array.IndexOf(gameObjects, clickedObject);
                if (index != -1)
                {
                    data[index].isAlive = !data[index].isAlive;
                    UpdateCubeColor(index);
                    //Debug.Log("aaaa");
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
                data[i * ncubes + j].isAlive = false;
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
                bool isAlive = data[index].isAlive;

                int aliveNeighbors = CountAliveNeighbors(i, j);

                if (isAlive && (aliveNeighbors < 2 || aliveNeighbors > 3))
                {
                    isAlive = false;
                    UpdateCubeColor(index, Color.black);
                }
                else if (!isAlive && aliveNeighbors == 3)
                {
                    isAlive = true;
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
                if (i >= 0 && i < ncubes && j >= 0 && j < ncubes && !(i == x && j == y) && data[i * ncubes + j].isAlive)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void ProcessGPU()
    {
        int totalBytes = sizeof(float) * 3 + sizeof(float) * 4 + sizeof(bool);

        ComputeBuffer cb = new ComputeBuffer(data.Length, totalBytes);
        cb.SetData(data);
        computeShader.SetBuffer(0, "cubes", cb);

        computeShader.Dispatch(0, data.Length / 10, 1, 1);

        cb.GetData(data);
        cb.Release();
    }

    private void UpdateCubeColor(int index, Color color)
    {
        gameObjects[index].GetComponent<SpriteRenderer>().material.SetColor("_Color", color);
        data[index].color = color;
    }

    private void UpdateCubeColor(int index)
    {
        Color color = data[index].isAlive ? Color.red : Color.black;
        gameObjects[index].GetComponent<SpriteRenderer>().material.SetColor("_Color", color);
        data[index].color = color;
    }
}
