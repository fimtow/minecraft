using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEngine;
using System.Threading;

public class ChunkManager : MonoBehaviour
{
    public Transform player;
    public GameObject chunkPrefab;
    public GameObject treePrefab;
    public GameObject plantPrefab;
    public GameObject bigTreePrefab;

    public int distanceView;
    public int size;
    public NoiseSettings settings;

    public static float[,] map;
    public static Dictionary<Vector3, GameObject> loadedChunks = new Dictionary<Vector3, GameObject>();
    public static Queue<GameObject> chunkPool = new Queue<GameObject>();

    public Vector3 playerChunkCords;
    public Vector3 cordsDiff;
    int maxIntHeight;
    int maxIntSize;
    bool finishedLoading = true;

    public static Queue<chunkMesh> meshToDraw = new Queue<chunkMesh>();
    public static Queue<chunkData> meshToGenerate = new Queue<chunkData>();
    public static Queue<vegetation> vegetationToGenerate = new Queue<vegetation>();

    void Start()
    {
        Time.timeScale = 0;
        map = MapGenerator.GenerateFiniteMap(size, settings, Vector2.zero);
        loadFirstChunks();

        Thread myThread = new Thread(new ThreadStart(chunkMeshGenerationManager));
        myThread.Start();

        playerChunkCords = floatToIntCords(player.position);
        cordsDiff = new Vector3();
        maxIntHeight = Chunk.maxHeight / Chunk.chunkSize;
        maxIntSize = size / Chunk.chunkSize;

    }

    private void Update()
    {
        chunkMeshDrawManager();
        treeManager();
        cordsDiff = playerChunkCords - floatToIntCords(player.position);
        if (!cordsDiff.Equals(Vector3.zero))
        {
            if(cordsDiff.x != 0)
            {
                Vector3 tempCordsDiff = new Vector3(cordsDiff.x, 0, 0);
                loadChunks(tempCordsDiff);
                playerChunkCords -= tempCordsDiff;
            }
            if(cordsDiff.y != 0)
            {
                Vector3 tempCordsDiff = new Vector3(0, cordsDiff.y, 0);
                loadChunks(tempCordsDiff);
                playerChunkCords -= tempCordsDiff;
            }
            if(cordsDiff.z != 0)
            {
                Vector3 tempCordsDiff = new Vector3(0, 0, cordsDiff.z);
                loadChunks(tempCordsDiff);
                playerChunkCords -= tempCordsDiff;
            }
        }
    }

    public void loadFirstChunks()
    {
        for (int x = -distanceView; x <= distanceView; x++)
        {
            for (int z = -distanceView; z <= distanceView; z++)
            {
                for (int y = -distanceView; y <= distanceView; y++)
                {
                    Vector3 chunkCords = floatToIntCords(player.position) + new Vector3(x, y, z);
                    GameObject chunk = Instantiate(chunkPrefab, intToFloatCords(chunkCords), Quaternion.identity);
                    loadedChunks.Add(chunkCords, chunk);
                }
            }
        }
    }
    public void loadChunks(Vector3 cordsDiff)
    {
        Vector3 toDelete = distanceView*cordsDiff + playerChunkCords;
        Vector3 toAdd = -(distanceView+1) * cordsDiff + playerChunkCords;
        Vector3[] baseVects = baseVectors(cordsDiff);
        for(int i=-distanceView; i<= distanceView; i++)
        {
            for(int j=-distanceView; j <= distanceView; j++)
            {
                // unload old chunks
                Vector3 key = toDelete + i * baseVects[0] + j * baseVects[1];
                if(0<=key.x && key.x<=maxIntSize && key.y>=0 && key.y<=maxIntHeight && 0<=key.z && key.z <= maxIntSize)
                {
                    Destroy(loadedChunks[key]);
                    //chunkPool.Enqueue(loadedChunks[key]);
                    loadedChunks.Remove(key);
                }
                // load new chunks
                Vector3 chunkCords = toAdd + i * baseVects[0] + j * baseVects[1];
                if(0<=chunkCords.x && chunkCords.x <= maxIntSize && chunkCords.y>=0 && chunkCords.y <= maxIntHeight && 0 <= chunkCords.z && chunkCords.z <= maxIntSize)
                {
                    GameObject chunk;
                    //GameObject chunk = Instantiate(chunkPrefab, intToFloatCords(chunkCords), Quaternion.identity);
                    if (chunkPool.Count != 0)
                    {
                        chunk = chunkPool.Dequeue();
                        chunk.GetComponent<Chunk>().initializeChunk(intToFloatCords(chunkCords));
                    }
                    else
                        chunk = Instantiate(chunkPrefab, intToFloatCords(chunkCords), Quaternion.identity);
                    loadedChunks.Add(chunkCords, chunk);
                }
            }
        }
    }

    public void chunkMeshGenerationManager()
    {
        while(true)
        {
            if(meshToGenerate.Count != 0)
            {
                chunkData thisChunkData;
                lock (meshToDraw)
                {
                    thisChunkData = meshToGenerate.Dequeue();
                }
                Chunk.generateGreedyMeshData(thisChunkData.chunk, thisChunkData.chunkCords);
            }
        }
    }

    public void chunkMeshDrawManager()
    {
        if (meshToDraw.Count != 0)
        {
            chunkMesh theChunkMesh;
            do
            {
                lock (meshToDraw)
                {
                    theChunkMesh = meshToDraw.Dequeue();
                }
            }
            while (!loadedChunks.ContainsKey(theChunkMesh.chunkCords) && meshToDraw.Count != 0);
            if(loadedChunks.ContainsKey(theChunkMesh.chunkCords))
                loadedChunks[theChunkMesh.chunkCords].GetComponent<Chunk>().drawMesh(theChunkMesh.verts, theChunkMesh.tris, theChunkMesh.uvs);
            finishedLoading = false;
        }
        else
        {
            if (!finishedLoading)
            {
                Debug.Log("finished loading");
                Time.timeScale = 1;
                finishedLoading = true;
            }
        }
    }

    public void treeManager()
    {
        if(vegetationToGenerate.Count != 0)
        {
            vegetation thisVegetation = vegetationToGenerate.Dequeue();
            Vector3 vegetationChunkKey = floatToIntCords(thisVegetation.position);
            if(loadedChunks.ContainsKey(vegetationChunkKey))
            {
                GameObject myVegetationPrefab = treePrefab;
                switch (thisVegetation.id)
                {
                    case 1:
                        myVegetationPrefab = plantPrefab;
                        break;
                    case 2:
                        myVegetationPrefab = bigTreePrefab;
                        break;
                }
                GameObject myVegetation = Instantiate(myVegetationPrefab, thisVegetation.position, Quaternion.identity);
                myVegetation.transform.SetParent(loadedChunks[vegetationChunkKey].transform, true);
            }

        }
    }

    public static Vector3[] baseVectors(Vector3 cordsDiff)
    {
        Vector3 vect1 = new Vector3();
        Vector3 vect2 = new Vector3();
        if(cordsDiff.x != 0)
        {
            vect1.y = 1;
            vect2.z = 1;
        }
        else if(cordsDiff.y != 0)
        {
            vect1.x = 1;
            vect2.z = 1;
        }
        else
        {
            vect1.y = 1;
            vect2.x = 1;
        }
        Vector3[] baseVectors = new Vector3[] { vect1, vect2 };
        return baseVectors;
    }

    public static Vector3 floatToIntCords(Vector3 floatCords)
    {
        Vector3 intCords = new Vector3();
        intCords.x = (int)floatCords.x / Chunk.chunkSize;
        intCords.y = (int)floatCords.y / Chunk.chunkSize;
        intCords.z = (int)floatCords.z / Chunk.chunkSize;
        return intCords;
    }

    public static Vector3 intToFloatCords(Vector3 intCords)
    {
        Vector3 floatCords = new Vector3();
        floatCords.x = intCords.x * Chunk.chunkSize;
        floatCords.y = intCords.y * Chunk.chunkSize;
        floatCords.z = intCords.z * Chunk.chunkSize;
        return floatCords;
    }
}
