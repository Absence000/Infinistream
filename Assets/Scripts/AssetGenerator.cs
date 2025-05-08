using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class AssetGenerator : MonoBehaviour
{
    int timer = 0;
    bool notCurrentlyGenerating = true;
    bool nextSceneReady = false;
    ScenePlayer sceneplayer;
    CharacterSpawner charSpawner;
    cameraController camctrlr;

    public GameObject logoCamera;
    string assetPath;

    public GameObject prompt;
    TMPro.TextMeshPro promptText;
    // Start is called before the first frame update
    void Start()
    {
        assetPath = Application.dataPath;
        sceneplayer = GetComponent<ScenePlayer>();
        charSpawner = GetComponent<CharacterSpawner>();
        camctrlr = GetComponent<cameraController>();
        charSpawner.spawnCharacters();

        promptText = prompt.GetComponent<TMPro.TextMeshPro>();
        promptText.text = System.IO.File.ReadAllText($"{assetPath}/Scripts/prompt.txt");
    }

    public async void generateScript()
    {
        await File.WriteAllTextAsync($"{assetPath}/Scripts/extControl.txt", "run");
        //makes extControl.txt say "run" for 5 seconds, and then erases it
        //that triggers main.py to generate a script
        await Task.Delay(5000);
        await File.WriteAllTextAsync($"{assetPath}/Scripts/extControl.txt", "don't");
    }

    // Update is called once per frame
    void Update()
    {
        if (timer == 60)
        {
            timer = 0;
            bool newScript = newScriptExists();
            if (sceneplayer.sceneDone)
            {
                logoCamera.SetActive(true);
            }
            if (newScript)
            {
                notCurrentlyGenerating = true;
                UnityEngine.Debug.Log("newScript exists! Waiting for scene to finish...");
                nextSceneReady = true;
                if (nextSceneReady && sceneplayer.sceneDone)
                {
                    UnityEngine.Debug.Log("Scene finished!");
                    charSpawner.despawnCharacters();

                    logoCamera.SetActive(true);

                    charSpawner.spawnCharacters();

                    string doneWithAudio = System.IO.File.ReadAllText($"{assetPath}/Scripts/audioInfo.txt");
                    if (doneWithAudio == "done" && newScript)
                    {
                        UnityEngine.Debug.Log("Shifting...");
                        Shift();
                        sceneplayer.setupAudio();
                        camctrlr.setupCameras();
                        promptText = prompt.GetComponent<TMPro.TextMeshPro>();
                        promptText.text = System.IO.File.ReadAllText($"{assetPath}/Scripts/prompt.txt");
                        logoCamera.SetActive(false);
                        nextSceneReady = false;
                        UnityEngine.Debug.Log("Playing next scene!");
                    }
                }
            }
            else
            {
                if (notCurrentlyGenerating)
                {
                    notCurrentlyGenerating = false;
                    generateScript();
                    UnityEngine.Debug.Log("All set! Generating new script...");
                }

            }
        }
        timer++;
    }
    async void Shift()
    {
        //for folders
        AssetDatabase.DeleteAsset(@"Assets\mp3Files");
        AssetDatabase.Refresh();
        AssetDatabase.MoveAsset(@"Assets\newMP3s", @"Assets\mp3Files");
        AssetDatabase.Refresh();
        //for the script
        await File.WriteAllTextAsync($"{assetPath}/Scripts/shift.txt", "shift");
        await Task.Delay(2000);
        await File.WriteAllTextAsync($"{assetPath}/Scripts/shift.txt", "don't");
    }

    public bool newScriptExists()
    {
        try
        {
            string text = System.IO.File.ReadAllText($"{assetPath}/Scripts/newScript.txt");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
