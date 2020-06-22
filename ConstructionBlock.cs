using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConstructionBlock : MonoBehaviour
{
    public constructionBlock[] constructionBlocks;
    public int i = 0;
    public int lenght;
    public Text blockName;
    // Start is called before the first frame update
    void Start()
    {
        lenght = constructionBlocks.Length;
        blockName.text = constructionBlocks[i].name;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.mouseScrollDelta.y != 0f)
        {
            i += (int)Input.mouseScrollDelta.y;
            i = (Mathf.Abs(i * lenght) + i) % lenght;
            blockName.text = constructionBlocks[i].name;
        }

    }
}

[System.Serializable]
public struct constructionBlock
{
    public bool cube;
    public char id;
    public GameObject prefab;
    public string name;
}