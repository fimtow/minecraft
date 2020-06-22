using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public static int maxHeight = 304;
    public static int chunkSize = 16;

    public BlockType[] blockTypes;
    public int treeRarity = 500;
    public int plantsRarity = 300;
    public int bigTreeRarity = 300;

    public char[,,] chunk;

    int cx, cy, cz;
    public bool isEmpty;
    public bool isVisible;

    public void Start()
    {
        initializeChunk(this.transform.position);
    }

    public void initializeChunk(Vector3 chunkCords)
    {
        this.transform.position = chunkCords;
        cx = (int)chunkCords.x;
        cy = (int)chunkCords.y;
        cz = (int)chunkCords.z;
        chunk = new char[chunkSize, chunkSize, chunkSize];
        isEmpty = true;
        isVisible = false;
        GetComponent<MeshFilter>().mesh = null;
        GetComponent<MeshCollider>().sharedMesh = null;

        generateChunk();
        generateMesh();
    }
    public void generateChunk()
    {
        float[,] map = ChunkManager.map;
        for(int x=0; x < chunkSize; x++)
        {
            for(int z=0; z < chunkSize; z++)
            {
                float currentHeight = map[cx + x, cz + z];
                char block = (char)0;
                // set top block type
                for (int i = 0; i < blockTypes.Length; i++)
                {
                    if (currentHeight <= blockTypes[i].height)
                    {
                        block = blockTypes[i].id;
                        break;
                    }
                }
                // fill chunk array
                int currentIntHeight = (int)(currentHeight * maxHeight) - cy;
                for(int y=0; y < chunkSize && y < currentIntHeight; y++)
                {
                    if(y == currentIntHeight-1)
                        chunk[x, y, z] = block;
                    else
                        chunk[x, y, z] = blockTypes[3].id;
                    isEmpty = false;
                }
                // tree generation
                if (currentIntHeight < chunkSize && currentIntHeight >0)
                {
                    isVisible = true;
                    if(Random.Range(1,treeRarity) == treeRarity-1 && block == blockTypes[2].id)
                    {
                        vegetation tree;
                        tree.id = 0;
                        tree.position = new Vector3(cx + x + 1, cy + currentIntHeight, cz + z + 1);
                        ChunkManager.vegetationToGenerate.Enqueue(tree);
                        //chunk[x, currentIntHeight - 1, z] = blockTypes[4].id;
                    }
                    else if(Random.Range(1, plantsRarity) == plantsRarity - 1 && block == blockTypes[2].id)
                    {
                        vegetation plant;
                        plant.id = 1;
                        plant.position = new Vector3(cx + x + 0.5f, cy + currentIntHeight, cz + z + 0.5f);
                        ChunkManager.vegetationToGenerate.Enqueue(plant);
                        //chunk[x, currentIntHeight - 1, z] = blockTypes[4].id;
                    }
                    else if (Random.Range(1, bigTreeRarity) == bigTreeRarity - 1 && block == blockTypes[2].id)
                    {
                        int h = currentIntHeight + cy;
                        if((int)(map[cx+x-1,cz+z-1]*maxHeight) == h && (int)(map[cx+x-1, cz+z] * maxHeight) == h && (int)(map[cx+x-1, cz+z+1] * maxHeight) == h && (int)(map[cx+x, cz+z-1] * maxHeight) == h && (int)(map[cx+x,cz+ z+1] * maxHeight) == h && (int)(map[cx+x+1,cz+ z-1] * maxHeight) == h && (int)(map[cx+x+1, cz+z] * maxHeight) == h && (int)(map[cx+x+1, cz+z+1] * maxHeight) == h )
                        {
                            vegetation tree;
                            tree.id = 2;
                            tree.position = new Vector3(cx + x + 0.5f, cy + currentIntHeight, cz + z + 0.5f);
                            ChunkManager.vegetationToGenerate.Enqueue(tree);
                            //chunk[x, currentIntHeight - 1, z] = blockTypes[4].id;
                        }
                    }
                }
            }
        }
    }

    public void generateMesh()
    {
        if (!isEmpty)// && isVisible)
        {
            chunkData thisChunkData;
            thisChunkData.chunkCords = ChunkManager.floatToIntCords(new Vector3(cx, cy, cz));
            thisChunkData.chunk = chunk;
            
            lock (ChunkManager.meshToGenerate)
            {
                ChunkManager.meshToGenerate.Enqueue(thisChunkData);
            }
        }
    }

    public void loadChunk()
    {

    }

    public void saveChunk()
    {

    }

    public void drawMesh(List<Vector3> verts, List<int> tris, List<Vector2> uvs)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public void quickGenerateMesh()
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int x = 0; x < chunkSize; x++)
            for (int z = 0; z < chunkSize; z++)
                for (int y = 0; y < chunkSize; y++)
                {
                    if (chunk[x, y, z] != 0)
                    {
                        Vector3 blockPos = new Vector3(x, y, z);
                        int numFaces = 0;
                        //no land above, build top face
                        if (y == chunkSize - 1 || chunk[x, y + 1, z] == 0)
                        {
                            verts.Add(blockPos + new Vector3(0, 1, 0));
                            verts.Add(blockPos + new Vector3(0, 1, 1));
                            verts.Add(blockPos + new Vector3(1, 1, 1));
                            verts.Add(blockPos + new Vector3(1, 1, 0));
                            numFaces++;
                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }

                        //bottom
                        if (y == 0 || chunk[x, y - 1, z] == 0)
                        {
                            verts.Add(blockPos + new Vector3(0, 0, 0));
                            verts.Add(blockPos + new Vector3(1, 0, 0));
                            verts.Add(blockPos + new Vector3(1, 0, 1));
                            verts.Add(blockPos + new Vector3(0, 0, 1));
                            numFaces++;

                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }

                        //front
                        if (z == 0 || chunk[x, y, z - 1] == 0)
                        {
                            verts.Add(blockPos + new Vector3(0, 0, 0));
                            verts.Add(blockPos + new Vector3(0, 1, 0));
                            verts.Add(blockPos + new Vector3(1, 1, 0));
                            verts.Add(blockPos + new Vector3(1, 0, 0));
                            numFaces++;

                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }

                        //right
                        if (x == chunkSize - 1 || chunk[x + 1, y, z] == 0)
                        {
                            verts.Add(blockPos + new Vector3(1, 0, 0));
                            verts.Add(blockPos + new Vector3(1, 1, 0));
                            verts.Add(blockPos + new Vector3(1, 1, 1));
                            verts.Add(blockPos + new Vector3(1, 0, 1));
                            numFaces++;

                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }

                        //back
                        if (z == chunkSize - 1 || chunk[x, y, z + 1] == 0)
                        {
                            verts.Add(blockPos + new Vector3(1, 0, 1));
                            verts.Add(blockPos + new Vector3(1, 1, 1));
                            verts.Add(blockPos + new Vector3(0, 1, 1));
                            verts.Add(blockPos + new Vector3(0, 0, 1));
                            numFaces++;

                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }

                        //left
                        if (x == 0 || chunk[x - 1, y, z] == 0)
                        {
                            verts.Add(blockPos + new Vector3(0, 0, 1));
                            verts.Add(blockPos + new Vector3(0, 1, 1));
                            verts.Add(blockPos + new Vector3(0, 1, 0));
                            verts.Add(blockPos + new Vector3(0, 0, 0));
                            numFaces++;

                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }


                        int tl = verts.Count - 4 * numFaces;
                        for (int i = 0; i < numFaces; i++)
                        {
                            tris.AddRange(new int[] { tl + i * 4, tl + i * 4 + 1, tl + i * 4 + 2, tl + i * 4, tl + i * 4 + 2, tl + i * 4 + 3 });
                        }
                    }
                }

        this.drawMesh(verts, tris, uvs);

    }

    public static void generateGreedyMeshData(char[,,] chunk, Vector3 chunkCords)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        usedFaces[,,] blockUsedFaces = new usedFaces[chunkSize, chunkSize, chunkSize];

        for (int x = 0; x < chunkSize; x++)
            for (int z = 0; z < chunkSize; z++)
                for (int y = 0; y < chunkSize; y++)
                {
                    if (chunk[x, y, z] != 0)
                    {
                        Vector3 blockPos = new Vector3(x, y, z);
                        int numFaces = 0;
                        //no land above, build top face
                        if (blockUsedFaces[x, y, z].top == false && (y == chunkSize - 1 || chunk[x, y + 1, z] == 0))
                        {
                            int zend = chunkSize-1;
                            int xend = chunkSize-1;
                            for (int nz = z + 1; nz < chunkSize; nz++)
                            {
                                if (chunk[x, y, nz] == chunk[x, y, z] && (y == chunkSize - 1 || chunk[x, y + 1, nz] == 0) && blockUsedFaces[x, y, nz].top == false)
                                {
                                    blockUsedFaces[x, y, nz].top = true;
                                }
                                else
                                {
                                    zend = nz-1;
                                    break;
                                }
                            }
                            int failed = -1;
                            for (int nx = x + 1; nx < chunkSize; nx++)
                            {
                                for (int nz = z; nz <= zend; nz++)
                                {
                                    if (chunk[nx, y, nz] == chunk[x, y, z] && (y == chunkSize - 1 || chunk[nx, y + 1, nz] == 0) && blockUsedFaces[nx, y, nz].top == false)
                                    {
                                        blockUsedFaces[nx, y, nz].top = true;
                                    }
                                    else
                                    {
                                        failed = nz;
                                        xend = nx-1;
                                        break;
                                    }
                                }
                                if(failed != -1)
                                {
                                    for (int nz = z; nz <= failed; nz++)
                                    {
                                        blockUsedFaces[nx, y, nz].top = false;
                                    }
                                    break;
                                }
                            }

                            verts.Add(new Vector3(x, y+1, z));
                            verts.Add(new Vector3(x, y+1, zend+1));
                            verts.Add(new Vector3(xend+1, y+1, zend+1));
                            verts.Add(new Vector3(xend+1, y+1, z));
                            numFaces++;

                            blockUsedFaces[x, y, z].top = true;
                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }

                        //bottom
                        if (blockUsedFaces[x, y, z].bottom == false && (y == 0 || chunk[x, y - 1, z] == 0))
                        {
                            int zend = chunkSize - 1;
                            int xend = chunkSize - 1;
                            for (int nz = z + 1; nz < chunkSize; nz++)
                            {
                                if (chunk[x, y, nz] == chunk[x, y, z] && (y == 0 || chunk[x, y - 1, nz] == 0) && blockUsedFaces[x, y, nz].bottom == false)
                                {
                                    blockUsedFaces[x, y, nz].bottom = true;
                                }
                                else
                                {
                                    zend = nz - 1;
                                    break;
                                }
                            }
                            int failed = -1;
                            for (int nx = x + 1; nx < chunkSize; nx++)
                            {
                                for (int nz = z; nz <= zend; nz++)
                                {
                                    if (chunk[nx, y, nz] == chunk[x, y, z] && (y == 0 || chunk[nx, y - 1, nz] == 0) && blockUsedFaces[nx, y, nz].bottom == false)
                                    {
                                        blockUsedFaces[nx, y, nz].bottom = true;
                                    }
                                    else
                                    {
                                        failed = nz;
                                        xend = nx - 1;
                                        break;
                                    }
                                }
                                if (failed != -1)
                                {
                                    for (int nz = z; nz <= failed; nz++)
                                    {
                                        blockUsedFaces[nx, y, nz].bottom = false;
                                    }
                                    break;
                                }
                            }

                            verts.Add(new Vector3(x, y, z));
                            verts.Add(new Vector3(xend + 1, y, z));
                            verts.Add(new Vector3(xend + 1, y, zend + 1));
                            verts.Add(new Vector3(x, y, zend + 1));
                            numFaces++;

                            blockUsedFaces[x, y, z].bottom = true;
                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }

                        //front
                        if (blockUsedFaces[x, y, z].front == false && (z == 0 || chunk[x, y, z-1] == 0))
                        {
                            int yend = chunkSize - 1;
                            int xend = chunkSize - 1;
                            for (int ny = y + 1; ny < chunkSize; ny++)
                            {
                                if (chunk[x, ny, z] == chunk[x, y, z] && (z == 0 || chunk[x, ny, z - 1] == 0) && blockUsedFaces[x, ny, z].front == false)
                                {
                                    blockUsedFaces[x, ny, z].front = true;
                                }
                                else
                                {
                                    yend = ny - 1;
                                    break;
                                }
                            }
                            int failed = -1;
                            for (int nx = x + 1; nx < chunkSize; nx++)
                            {
                                for (int ny = y; ny <= yend; ny++)
                                {
                                    if (chunk[nx, ny, z] == chunk[x, y, z] && (z == 0 || chunk[nx, ny, z - 1] == 0) && blockUsedFaces[nx,ny, z].front == false)
                                    {
                                        blockUsedFaces[nx, ny, z].front = true;
                                    }
                                    else
                                    {
                                        failed = ny;
                                        xend = nx - 1;
                                        break;
                                    }
                                }
                                if (failed != -1)
                                {
                                    for (int ny = y; ny <= failed; ny++)
                                    {
                                        blockUsedFaces[nx, ny, z].front = false;
                                    }
                                    break;
                                }
                            }

                            verts.Add(new Vector3(x,y,z));
                            verts.Add(new Vector3(x,yend+1,z));
                            verts.Add(new Vector3(xend+1,yend+1,z));
                            verts.Add(new Vector3(xend+1,y,z));
                            numFaces++;

                            blockUsedFaces[x, y, z].front = true;
                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }

                        //right
                        if (blockUsedFaces[x, y, z].right == false && (x == chunkSize - 1 || chunk[x + 1, y, z] == 0))
                        {
                            int zend = chunkSize - 1;
                            int yend = chunkSize - 1;
                            for (int nz = z + 1; nz < chunkSize; nz++)
                            {
                                if (chunk[x, y, nz] == chunk[x, y, z] && (x == chunkSize - 1 || chunk[x + 1, y, nz] == 0) && blockUsedFaces[x, y, nz].right == false)
                                {
                                    blockUsedFaces[x, y, nz].right = true;
                                }
                                else
                                {
                                    zend = nz - 1;
                                    break;
                                }
                            }
                            int failed = -1;
                            for (int ny = y + 1; ny < chunkSize; ny++)
                            {
                                for (int nz = z; nz <= zend; nz++)
                                {
                                    if (chunk[x, ny, nz] == chunk[x, y, z] && (x == chunkSize - 1 || chunk[x + 1, ny, nz] == 0) && blockUsedFaces[x, ny, nz].right == false)
                                    {
                                        blockUsedFaces[x, ny, nz].right = true;
                                    }
                                    else
                                    {
                                        failed = nz;
                                        yend = ny - 1;
                                        break;
                                    }
                                }
                                if (failed != -1)
                                {
                                    for (int nz = z; nz <= failed; nz++)
                                    {
                                        blockUsedFaces[x, ny, nz].right = false;
                                    }
                                    break;
                                }
                            }
                            verts.Add(new Vector3(x+1, y, z));
                            verts.Add(new Vector3(x+1, yend + 1, z));
                            verts.Add(new Vector3(x+1, yend + 1, zend + 1));
                            verts.Add(new Vector3(x+1, y, zend + 1));
                            numFaces++;

                            blockUsedFaces[x, y, z].right = true;
                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }
                        //back
                        if (blockUsedFaces[x, y, z].back == false && (z == chunkSize - 1 || chunk[x, y, z + 1] == 0))
                        {
                            int yend = chunkSize - 1;
                            int xend = chunkSize - 1;
                            for (int ny = y + 1; ny < chunkSize; ny++)
                            {
                                if (chunk[x, ny, z] == chunk[x, y, z] && (z == chunkSize - 1 || chunk[x, ny, z + 1] == 0) && blockUsedFaces[x, ny, z].back == false)
                                {
                                    blockUsedFaces[x, ny, z].back = true;
                                }
                                else
                                {
                                    yend = ny - 1;
                                    break;
                                }
                            }
                            int failed = -1;
                            for (int nx = x + 1; nx < chunkSize; nx++)
                            {
                                for (int ny = y; ny <= yend; ny++)
                                {
                                    if (chunk[nx, ny, z] == chunk[x, y, z] && (z == chunkSize - 1 || chunk[nx, ny, z + 1] == 0) && blockUsedFaces[nx, ny, z].back == false)
                                    {
                                        blockUsedFaces[nx, ny, z].back = true;
                                    }
                                    else
                                    {
                                        failed = ny;
                                        xend = nx - 1;
                                        break;
                                    }
                                }
                                if (failed != -1)
                                {
                                    for (int ny = y; ny <= failed; ny++)
                                    {
                                        blockUsedFaces[nx, ny, z].back = false;
                                    }
                                    break;
                                }
                            }

                            verts.Add(new Vector3(xend+1, y, z+1));
                            verts.Add(new Vector3(xend+1, yend + 1, z+1));
                            verts.Add(new Vector3(x, yend + 1, z+1));
                            verts.Add(new Vector3(x, y, z+1));
                            numFaces++;

                            blockUsedFaces[x, y, z].back = true;
                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }

                        //left
                        if (blockUsedFaces[x, y, z].left == false && (x == 0 || chunk[x - 1, y, z] == 0))
                        {
                            int zend = chunkSize - 1;
                            int yend = chunkSize - 1;
                            for (int nz = z + 1; nz < chunkSize; nz++)
                            {
                                if (chunk[x, y, nz] == chunk[x, y, z] && (x == 0 || chunk[x - 1, y, nz] == 0) && blockUsedFaces[x, y, nz].left == false)
                                {
                                    blockUsedFaces[x, y, nz].left = true;
                                }
                                else
                                {
                                    zend = nz - 1;
                                    break;
                                }
                            }
                            int failed = -1;
                            for (int ny = y + 1; ny < chunkSize; ny++)
                            {
                                for (int nz = z; nz <= zend; nz++)
                                {
                                    if (chunk[x, ny, nz] == chunk[x, y, z] && (x == 0 || chunk[x - 1, ny, nz] == 0) && blockUsedFaces[x, ny, nz].left == false)
                                    {
                                        blockUsedFaces[x, ny, nz].left = true;
                                    }
                                    else
                                    {
                                        failed = nz;
                                        yend = ny - 1;
                                        break;
                                    }
                                }
                                if (failed != -1)
                                {
                                    for (int nz = z; nz <= failed; nz++)
                                    {
                                        blockUsedFaces[x, ny, nz].left = false;
                                    }
                                    break;
                                }
                            }
                            verts.Add(new Vector3(x, y, zend+1));
                            verts.Add(new Vector3(x, yend + 1, zend+1));
                            verts.Add(new Vector3(x, yend + 1, z));
                            verts.Add(new Vector3(x, y, z));
                            numFaces++;

                            blockUsedFaces[x, y, z].left = true;
                            uvs.AddRange(blockUVs(chunk[x, y, z]));
                        }

                        int tl = verts.Count - 4 * numFaces;
                        for (int i = 0; i < numFaces; i++)
                        {
                            tris.AddRange(new int[] { tl + i * 4, tl + i * 4 + 1, tl + i * 4 + 2, tl + i * 4, tl + i * 4 + 2, tl + i * 4 + 3 });
                        }
                    }
                }
        chunkMesh thisChunkMesh;
        thisChunkMesh.chunkCords = chunkCords;
        thisChunkMesh.tris = tris;
        thisChunkMesh.verts = verts;
        thisChunkMesh.uvs = uvs;

        lock (ChunkManager.meshToDraw)
        {
            ChunkManager.meshToDraw.Enqueue(thisChunkMesh);
        }

    }

    public static Vector2[] blockUVs(char id)
    {
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(.01f,(id-1)/16f + .01f),
            new Vector2(.01f,(id)/16f - .01f),
            new Vector2(1/16f - .01f,(id)/16f - .01f),
            new Vector2(1/16f - .01f,(id-1)/16f + .01f),
        };
        return uvs;
    }
}

[System.Serializable]
public struct BlockType
{
    public char id;
    public string name;
    public float height;
}

public struct chunkMesh
{
    public Vector3 chunkCords;
    public List<Vector3> verts;
    public List<int> tris;
    public List<Vector2> uvs;
}

public struct chunkData
{
    public char[,,] chunk;
    public Vector3 chunkCords;
}

public struct usedFaces
{
    public bool top;
    public bool bottom;
    public bool front;
    public bool back;
    public bool right;
    public bool left;
}

public struct vegetation
{
    public int id;
    public Vector3 position;
}