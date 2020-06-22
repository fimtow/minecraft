using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewMap : MonoBehaviour
{
    public InputField mapSize;
    public InputField seed;
    public RawImage preview;
    int size;
    public TerrainType[] blockTypes;
    public NoiseSettings settings;
    public void generate()
    {
        settings.seed = System.Convert.ToInt32(seed.text);
        size = System.Convert.ToInt32(mapSize.text);
        float[,] previewMap = MapGenerator.GenerateFiniteMap(size, settings, Vector2.zero);
        Color[] colourMap = new Color[size * size];
        for(int x = 0; x < size; x++)
        {
            for(int y = 0; y < size; y++)
            {
                float currentHeight = previewMap[x, y];
                for(int i = 0; i < blockTypes.Length; i++)
                {
                    if(currentHeight <= blockTypes[i].height)
                    {
                        colourMap[y * size + x] = blockTypes[i].colour;
                        break;
                    }
                }
            }
        }
        Texture2D texture = MapGenerator.generateTexture(colourMap, size);
        preview.texture = texture;
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}