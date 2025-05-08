using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting.Dependencies.NCalc;

public class ScenePlayer : MonoBehaviour
{
    public bool startScene;
    string[] audioGUIDs;
    public string audioPath;
    public AudioClip clip;
    public AudioSource audio;
    int index;
    int fileAmount;
    int timer = 0;
    public string characterName;
    public bool sceneDone;
    cameraController camctrlr;

    //makes Jesse say bitch randomly
    public AudioClip bitch;
    bool toggleBitch = false;
    int bitchTimer = 0;
    int bitchOclock = 0;

    public string name;
    GameObject subtitle;
    string[] subtitleList;
    TMPro.TextMeshPro text;


    // Start is called before the first frame update
    void Start()
    {
        subtitle = GameObject.Find("Subtitles");
        text = subtitle.GetComponent<TMPro.TextMeshPro>();

        camctrlr = GetComponent<cameraController>();
        setupAudio();
        audio = GetComponent<AudioSource>();
        Application.targetFrameRate = 60;
    }

    public void setupAudio()
    {
        camctrlr.setupCameras();
        string subtitles = System.IO.File.ReadAllText($"{Application.dataPath}/Scripts/subtitles.txt");
        subtitleList = subtitles.Split(System.Environment.NewLine, System.StringSplitOptions.RemoveEmptyEntries);

        audioGUIDs = AssetDatabase.FindAssets("", new[] { "Assets/mp3Files" });
        fileAmount = audioGUIDs.Length;
        index = 0;
        sceneDone = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (startScene)
        {
            //checks if audio's not playing and it's been over 1 second
            if (!audio.isPlaying && timer > 60)
            {
                //makes sure the scene is still going
                if (index < fileAmount && !sceneDone)
                {
                    if (toggleBitch)
                    {
                        if (bitchTimer == 1)
                        {
                            //randomly selects how long jesse waits to say "bitch"
                            var bitchTimeSelector = Random.Range(1, 10);
                            if (bitchTimeSelector >= 5)
                            {
                                bitchOclock = 30;
                            }
                            else if (bitchTimeSelector >= 3)
                            {
                                bitchOclock = 180;
                            }
                            else if (bitchTimeSelector == 1)
                            {
                                bitchOclock = 600;
                            }
                        }
                        else if (bitchTimer > bitchOclock)
                        {
                            audio.clip = bitch;
                            audio.Play();
                            toggleBitch = false;
                            bitchTimer = 0;
                        }
                        bitchTimer++;
                    }
                    else
                    {
                        //plays back the audio
                        timer = 0;
                        audioPath = AssetDatabase.GUIDToAssetPath(audioGUIDs[index]);
                        clip = AssetDatabase.LoadAssetAtPath(audioPath, typeof(AudioClip)) as AudioClip;
                        if (clip != null)
                        {
                            audio.clip = clip;
                            audio.clip = clip;
                            if (clip.name.Contains("JESSE"))
                            {
                                if (Random.Range(1, 5) == 3)
                                {
                                    toggleBitch = true;
                                }
                            }
                            //every character continually looks at the character name to figure out
                            //who's speaking
                            name = clip.name;
                            int hyphen = name.IndexOf('-');
                            characterName = name.Substring(hyphen, name.Length - hyphen).Remove(0, 2);
                            if (subtitleList.Length > index)
                            {
                                text.text = subtitleList[index];
                            }
                            audio.Play();
                        }
                        index++;
                    }
                }
                else
                {
                    sceneDone = true;
                }
            }
            if (timer <= 60)
            {
                timer++;
            }
        }
    }
}
