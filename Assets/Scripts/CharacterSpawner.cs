using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Unity.VisualScripting;

public class CharacterSpawner : MonoBehaviour
{
    public List<GameObject> unknownPrefab;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void spawnCharacters()
    {
        string charStr = System.IO.File.ReadAllText(@"Assets/Scripts/characterList.txt");
        string[] charList = charStr.Split(' ')[..^1];
        List<GameObject> instantiatedCharList = new List<GameObject>();
        List<Transform> instantiatedTransformList = new List<Transform>();
        List<GameObject> uninstantiatedUnknownList = new List<GameObject>();

        foreach (string character in charList)
        {
            string name = character.Replace("_", " ");
            string[] assetGUIDList = AssetDatabase.FindAssets(name, new[] { "Assets/Character Prefabs" });
            if (assetGUIDList.Length != 0)
            {
                GameObject nameFixer = Instantiate(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetGUIDList[0]), typeof(GameObject)), transform) as GameObject;
                nameFixer.name = nameFixer.name.Replace("(Clone)", "");
                instantiatedCharList.Add(nameFixer);
                instantiatedTransformList.Add(nameFixer.transform);
            }
            else
            {
                if(uninstantiatedUnknownList.Count != 0)
                {
                    int unknownIndex = Random.Range(0, uninstantiatedUnknownList.Count);
                    GameObject unknown = Instantiate(uninstantiatedUnknownList[unknownIndex], transform);
                    unknown.name = name;
                    characterController txtchanger = unknown.GetComponent<characterController>();
                    txtchanger.fixText(name);
                    instantiatedCharList.Add(unknown);
                    instantiatedTransformList.Add(unknown.transform);
                    uninstantiatedUnknownList.RemoveAt(unknownIndex);
                }
                else
                {
                    GameObject unknown = Instantiate(unknownPrefab[Random.Range(0, unknownPrefab.Count)], transform);
                    unknown.name = name;
                    characterController txtchanger = unknown.GetComponent<characterController>();
                    txtchanger.fixText(name);
                    instantiatedCharList.Add(unknown);
                    instantiatedTransformList.Add(unknown.transform);
                }
            }
        }
        //now passes in the list
        foreach (GameObject character in instantiatedCharList)
        {
            character.GetComponent<characterController>().AllCharactersReady(instantiatedTransformList);
        }
    }

    public void despawnCharacters()
    {
        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
