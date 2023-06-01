using UnityEngine;
using UnityEngine.UI;

public class GameOfLifeController : MonoBehaviour
{
    public ComputeShader gameOfLifeComputeShader;  // Referência ao Compute Shader para o jogo da vida
    public RawImage displayImage;  // Referência à imagem onde a simulação será exibida
    public Toggle cpuToggle;  // Referência ao Toggle para alternar entre CPU e GPU

    private int width = 100;  // Largura da grade do jogo da vida
    private int height = 100;  // Altura da grade do jogo da vida
    private int threadGroupSize = 8;  // Tamanho do grupo de threads para o Compute Shader
    private bool useGPU = true;  // Flag para indicar se a GPU será usada para a simulação

    private RenderTexture currentTexture;  // Textura atual da simulação
    private RenderTexture updatedTexture;  // Textura atualizada da simulação

    private void Start()
    {
        currentTexture = CreateTexture(width, height);  // Cria a textura atual
        updatedTexture = CreateTexture(width, height);  // Cria a textura atualizada

        displayImage.texture = currentTexture;  // Define a textura atual como a textura da imagem
    }

    private void Update()
    {
        if (useGPU)
        {
            // Configura os buffers de textura no Compute Shader
            gameOfLifeComputeShader.SetTexture(0, "Current", currentTexture);
            gameOfLifeComputeShader.SetTexture(0, "Result", updatedTexture);

            // Executa o Compute Shader
            int threadGroupsX = Mathf.CeilToInt(width / (float)threadGroupSize);
            int threadGroupsY = Mathf.CeilToInt(height / (float)threadGroupSize);
            gameOfLifeComputeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        }
        else
        {
            UpdateGameOfLifeCPU();  // Atualiza a simulação usando a CPU
        }

        SwapTextures(ref currentTexture, ref updatedTexture);  // Troca as texturas atual e atualizada

        displayImage.texture = currentTexture;  // Atualiza a textura da imagem com a textura atual
    }

    private void UpdateGameOfLifeCPU()
    {
        // Obtém a largura e altura da textura atual
        int width = currentTexture.width;
        int height = currentTexture.height;

        // Cria uma matriz de estados das células
        bool[,] cellStates = new bool[width, height];

        // Lê os estados das células da textura atual
        RenderTexture.active = currentTexture;
        Texture2D inputTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        inputTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        RenderTexture.active = null;

        // Converte os valores das células para a matriz de estados
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = inputTexture.GetPixel(x, y);
                bool isAlive = pixelColor.r > 0;
                cellStates[x, y] = isAlive;
            }
        }

        // Cria uma nova matriz para os novos estados das células
        bool[,] newCellStates = new bool[width, height];

        // Aplica as regras do jogo da vida para atualizar os estados das células
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int aliveNeighbors = CountAliveNeighbors(cellStates, x, y);

                if (!cellStates[x, y] && aliveNeighbors == 3)
                {
                    newCellStates[x, y] = true;  // Regra 1: célula morta com 3 vizinhos vivos se torna viva
                }
                else if (cellStates[x, y] && aliveNeighbors < 2)
                {
                    newCellStates[x, y] = false;  // Regra 2: célula viva com menos de 2 vizinhos vivos morre por isolamento
                }
                else if (cellStates[x, y] && aliveNeighbors > 3)
                {
                    newCellStates[x, y] = false;  // Regra 3: célula viva com mais de 3 vizinhos vivos morre por superpopulação
                }
                else if (cellStates[x, y] && (aliveNeighbors == 2 || aliveNeighbors == 3))
                {
                    newCellStates[x, y] = true;  // Regra 4: célula viva com 2 ou 3 vizinhos vivos permanece viva
                }
            }
        }

        // Atualiza a textura atualizada com os novos estados das células
        RenderTexture.active = updatedTexture;
        Texture2D outputTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isAlive = newCellStates[x, y];
                Color pixelColor = isAlive ? Color.white : Color.black;
                outputTexture.SetPixel(x, y, pixelColor);
            }
        }

        outputTexture.Apply();
        RenderTexture.active = null;
    }

    private int CountAliveNeighbors(bool[,] cellStates, int x, int y)
    {
        int count = 0;
        int width = cellStates.GetLength(0);
        int height = cellStates.GetLength(1);

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int neighborX = x + i;
                int neighborY = y + j;

                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                {
                    if (cellStates[neighborX, neighborY])
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    private RenderTexture CreateTexture(int width, int height)
    {
        // Cria uma nova textura renderizada
        RenderTexture texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        texture.enableRandomWrite = true;
        texture.Create();

        return texture;
    }

    private void SwapTextures(ref RenderTexture tex1, ref RenderTexture tex2)
    {
        // Troca as referências entre duas texturas
        RenderTexture temp = tex1;
        tex1 = tex2;
        tex2 = temp;
    }

    public void OnToggleGPU()
    {
        useGPU = cpuToggle.isOn;  // Atualiza a flag de uso da GPU com o valor do Toggle
    }
}
