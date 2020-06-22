using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public LayerMask groundLayer;
    public string chunkTag;
    public float maxDist = 100;
    public Transform crosshair;
    public GameObject constructionBlock;
    ConstructionBlock script;
    // Update is called once per frame
    private void Start()
    {
        script = constructionBlock.GetComponent<ConstructionBlock>();
    }
    void Update()
    {
        bool leftClick = Input.GetMouseButtonDown(0);
        bool rightClick = Input.GetMouseButtonDown(1);
        if (leftClick || rightClick)
        {
            Vector3 direction = Camera.main.ScreenPointToRay(crosshair.position).direction;
            RaycastHit hitInfo;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(crosshair.position), out hitInfo, maxDist, groundLayer))
            {
                if (hitInfo.transform.CompareTag(chunkTag))
                {
                    
                    Vector3 pointInTargetBlock;
                    //destroy



                    if (rightClick)
                        pointInTargetBlock = hitInfo.point + Vector3.Normalize(direction) * .01f;//move a little inside the block
                    else
                        pointInTargetBlock = hitInfo.point - Vector3.Normalize(direction) * .01f; 
                     //Debug.Log(hitInfo.point);

                     //get the terrain chunk (can't just use collider)

                     int chunkPosX = Mathf.FloorToInt(pointInTargetBlock.x / 16f) * 16;
                    int chunkPosY = Mathf.FloorToInt(pointInTargetBlock.y / 16f) * 16;
                    int chunkPosZ = Mathf.FloorToInt(pointInTargetBlock.z / 16f) * 16;

                    GameObject chunk = ChunkManager.loadedChunks[ChunkManager.floatToIntCords(pointInTargetBlock)];
                    Chunk realChunk = chunk.GetComponent<Chunk>();
                    //index of the target block
                    int bix = Mathf.FloorToInt(pointInTargetBlock.x) - chunkPosX;
                    int biy = Mathf.FloorToInt(pointInTargetBlock.y) - chunkPosY;
                    int biz = Mathf.FloorToInt(pointInTargetBlock.z) - chunkPosZ;

                    if (rightClick)//replace block with air
                    {
                        realChunk.chunk[bix, biy, biz] = (char)0;
                        realChunk.quickGenerateMesh();
                    }
                    else if (Vector3Int.FloorToInt(pointInTargetBlock) != Vector3Int.FloorToInt(this.transform.position))
                    {
                        
                        if(script.constructionBlocks[script.i].cube)
                        {
                            realChunk.chunk[bix, biy, biz] = script.constructionBlocks[script.i].id;
                            realChunk.quickGenerateMesh();
                        }
                        else
                        {
                            GameObject block = Instantiate(script.constructionBlocks[script.i].prefab, Vector3Int.FloorToInt(pointInTargetBlock), Quaternion.identity);
                            block.transform.SetParent(chunk.transform, true);
                        }
                    }

                }
                else if(rightClick)
                {
                    Destroy(hitInfo.transform.gameObject);
                }
            }
        }
    }
}
