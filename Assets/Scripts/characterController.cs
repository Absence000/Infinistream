using OpenCover.Framework.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class characterController : MonoBehaviour
{
    public string charName;
    List<Transform> listofCharacters;
    ScenePlayer scenePlayer;
    Animator animator;
    Transform targetCharacter;

    //navmesh
    NavMeshAgent agent;
    bool currentlyMoving;
    int timer;
    int moveTime;
    Vector3 prevFramePos;
    TMPro.TextMeshPro text;
    public GameObject unk;

    void Awake()
    {
        scenePlayer = GameObject.Find("GameController").GetComponent<ScenePlayer>();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        moveTime = Random.Range(60, 600);
        if (charName == "UNKNOWN")
        {
            text = GetComponentInChildren(typeof(TMPro.TextMeshPro)) as TMPro.TextMeshPro;
        }
        agent.updateRotation = false;
    }
    
    //called by AssetGenerator when all the characters are loaded
    public void AllCharactersReady(List<Transform> listofCharacters)
    {
        ////populates listofCharacters with every gameObject but itself
        List<Transform> dummyList = new List<Transform>(listofCharacters.Count);
        foreach (Transform character in dummyList)
        {
            if (character.name == name)
            {
                listofCharacters.Remove(character);
            }
        }
        targetCharacter = listofCharacters[Random.Range(0, listofCharacters.Count)];
        transform.LookAt(targetCharacter);
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        transform.Rotate(new Vector3(0, 90, 0));
    }
    public void fixText(string txt)
    {
        text.text = txt;
        Texture2D newTex = new Texture2D(700, 700);
        newTex.LoadImage(System.IO.File.ReadAllBytes($"Assets/tempTextures/{gameObject.name}.png"));
        Material baseMaterial = unk.GetComponent<SkinnedMeshRenderer>().material;
        baseMaterial.SetTexture("_MainTex", newTex);
    }

    // Update is called once per frame
    void Update()
    {
        float sqrVel = (transform.position - prevFramePos).sqrMagnitude;
        //very simple character controller
        //chooses a random point on the navmesh and walks towards that
        if (scenePlayer.characterName == name)
        {
            animator.Play("speaking");
        }
        else if (prevFramePos == transform.position)
        {
            animator.Play("idle");
            if (currentlyMoving)
            {
                transform.LookAt(targetCharacter);
                transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                transform.Rotate(new Vector3(0, 90, 0));
            }
            if (timer == 180)
            {
                if(moveTime < 180)
                {
                    targetCharacter = listofCharacters[Random.Range(0, listofCharacters.Count)];
                }
            }
            if (timer == moveTime)
            {
                if (moveTime > 180)
                {
                    timer = 0;
                }
                timer = 0;
                moveTime = Random.Range(60, 600);
                Vector3 modifier = Random.insideUnitCircle;
                agent.destination = transform.position + modifier;
                animator.Play("walk");
            }
            timer++;
            currentlyMoving = false;
        }
        else
        {
            animator.Play("walk");
            transform.LookAt((transform.position - prevFramePos).normalized);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
            //transform.Rotate(new Vector3(0, 90, 0));
            currentlyMoving = true;
        }
        prevFramePos = transform.position;
    }
}
