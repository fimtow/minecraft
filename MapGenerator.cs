using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapGenerator
{
	public enum NormalizeMode { Local, Global };

	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre)
	{
		float[,] noiseMap = new float[mapWidth, mapHeight];

		System.Random prng = new System.Random(settings.seed);
		Vector2[] octaveOffsets = new Vector2[settings.octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < settings.octaves; i++)
		{
			float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCentre.x;
			float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCentre.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= settings.persistance;
		}

		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;


		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
			{

				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < settings.octaves; i++)
				{
					float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
					float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= settings.persistance;
					frequency *= settings.lacunarity;
				}

				if (noiseHeight > maxLocalNoiseHeight)
				{
					maxLocalNoiseHeight = noiseHeight;
				}
				if (noiseHeight < minLocalNoiseHeight)
				{
					minLocalNoiseHeight = noiseHeight;
				}
				noiseMap[x, y] = noiseHeight;

				if (settings.normalizeMode == NormalizeMode.Global)
				{
					float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
					noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
				}
			}
		}

		if (settings.normalizeMode == NormalizeMode.Local)
		{
			for (int y = 0; y < mapHeight; y++)
			{
				for (int x = 0; x < mapWidth; x++)
				{
					noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
				}
			}
		}

		return noiseMap;
	}
	public static float[,] GenerateFalloffMap(int size)
	{
		float[,] map = new float[size, size];

		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float x = i / (float)size * 2 - 1;
				float y = j / (float)size * 2 - 1;

				float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
				map[i, j] = Evaluate(value);
			}
		}

		return map;
	}

	static float Evaluate(float value)
	{
		float a = 3;
		float b = 2.2f;

		return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
	}

	public static float[,] GenerateFiniteMap(int mapSize, NoiseSettings settings,Vector2 sampleCentre)
	{
		float[,] falloffMap = GenerateFalloffMap(mapSize);
		float[,] noiseMap = GenerateNoiseMap(mapSize, mapSize, settings, sampleCentre);
		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
			}
		}
		return noiseMap;
	}
	public static Texture2D generateTexture(Color[] colourMap, int size)
	{
		Texture2D texture = new Texture2D(size, size);
		texture.SetPixels(colourMap);
		texture.Apply();
		return texture;
	}
}
[System.Serializable]
public class NoiseSettings
{
	public MapGenerator.NormalizeMode normalizeMode;

	public float scale = 1000;

	public int octaves = 6;
	[Range(0, 1)]
	public float persistance = .6f;
	public float lacunarity = 2;

	public int seed = 26;
	public Vector2 offset;

	public void ValidateValues()
	{
		scale = Mathf.Max(scale, 0.01f);
		octaves = Mathf.Max(octaves, 1);
		lacunarity = Mathf.Max(lacunarity, 1);
		persistance = Mathf.Clamp01(persistance);
	}
}
