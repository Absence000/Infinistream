using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
    GameObject cameraEmpty;
    GameObject previouslyActiveCamera;
    Transform[] camTransforms;
    List<GameObject> cameras = new List<GameObject>();
    List<Vector3> initCamPosList;
    List<Quaternion> initCamRotList;
    int camTimer = 0;
    ScenePlayer sceneplayer;
    int camIndex;

    GameObject subtitle;
    GameObject prompt;

    Transform activeSpeaker;
    public void setupCameras()
    {
        //this is dumb but idk how to use c# at all
        cameraEmpty = GameObject.Find("Cameras");
        camTransforms = cameraEmpty.GetComponentsInChildren<Transform>(includeInactive: true);
        cameras = new List<GameObject>();
        initCamPosList = new List<Vector3>();
        initCamRotList = new List<Quaternion>();

        foreach (Transform child in camTransforms)
        {
            if (child.name != "Cameras")
            {
                cameras.Add(child.gameObject);
                initCamPosList.Add(child.transform.position);
                initCamRotList.Add(child.transform.rotation);
            }
        }
        camIndex = Random.Range(0, cameras.Count - 1);
        previouslyActiveCamera = cameras[camIndex];
        previouslyActiveCamera.SetActive(true);
        changeSubtitlePosition();
    }
    // Start is called before the first frame update
    void Awake()
    {
        //subtitle setup
        subtitle = GameObject.Find("Subtitles");
        prompt = GameObject.Find("Summary");
        sceneplayer = GetComponent<ScenePlayer>();
        //setupCameras();
    }

    // Update is called once per frame
    void Update()
    {
        if (!sceneplayer.sceneDone)
        {
            if (camTimer >= 180)
            {
                //decides whether to do a close up or not
                if (Random.Range(0, 2) == 1)
                {
                    try
                    {
                        string speaker = sceneplayer.characterName;
                        UnityEngine.Debug.Log(speaker);
                        activeSpeaker = GameObject.Find(speaker).transform;
                        int shotDecider = Random.Range(1, 11);
                        if (shotDecider <= 4)
                        {
                            //kind of close close up (left)
                            previouslyActiveCamera.transform.position = activeSpeaker.position + activeSpeaker.forward * 1f + activeSpeaker.up * 1f + activeSpeaker.right * -1f;
                        }
                        else if (shotDecider <= 8)
                        {
                            //kind of close close up (right)
                            previouslyActiveCamera.transform.position = activeSpeaker.position + activeSpeaker.forward * -1f + activeSpeaker.up * 1f + activeSpeaker.right * -1f;
                        }
                        else if (shotDecider == 9)
                        {
                            //extreme close up
                            previouslyActiveCamera.transform.position = activeSpeaker.position + activeSpeaker.right * -1f + activeSpeaker.up * 2f;
                        }
                        else
                        {
                            //low angle
                            previouslyActiveCamera.transform.position = activeSpeaker.position + activeSpeaker.right * -1f + activeSpeaker.up * 1f;
                        }
                        previouslyActiveCamera.transform.LookAt(activeSpeaker.position + activeSpeaker.up);
                        changeSubtitlePosition();

                    }
                    catch
                    {
                        //UnityEngine.Debug.Log("Error");
                        //switches angles
                        previouslyActiveCamera.transform.position = initCamPosList[camIndex];
                        previouslyActiveCamera.transform.rotation = initCamRotList[camIndex];
                        previouslyActiveCamera.SetActive(false);
                        camIndex = Random.Range(0, cameras.Count - 1);
                        previouslyActiveCamera = cameras[camIndex];
                        previouslyActiveCamera.SetActive(true);
                        changeSubtitlePosition();
                    }
                }
                else
                {
                    //switches angles
                    previouslyActiveCamera.transform.position = initCamPosList[camIndex];
                    previouslyActiveCamera.transform.rotation = initCamRotList[camIndex];
                    previouslyActiveCamera.SetActive(false);
                    camIndex = Random.Range(0, cameras.Count - 1);
                    previouslyActiveCamera = cameras[camIndex];
                    previouslyActiveCamera.SetActive(true);
                    changeSubtitlePosition();
                }
                camTimer = 0;
            }
            camTimer++;
        }
        else
        {
            previouslyActiveCamera.transform.position = initCamPosList[camIndex];
            previouslyActiveCamera.transform.rotation = initCamRotList[camIndex];
            previouslyActiveCamera.SetActive(false);
        }
    }
    public void changeSubtitlePosition()
    {
        subtitle.transform.position = previouslyActiveCamera.transform.position + previouslyActiveCamera.transform.forward - 0.2f * previouslyActiveCamera.transform.up;
        subtitle.transform.LookAt(previouslyActiveCamera.transform.position);
        prompt.transform.position = previouslyActiveCamera.transform.position + previouslyActiveCamera.transform.forward + 0.4f * previouslyActiveCamera.transform.up - 0.5f * previouslyActiveCamera.transform.right;
        prompt.transform.LookAt(previouslyActiveCamera.transform.position);
    }
}
