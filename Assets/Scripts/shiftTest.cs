using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class shiftTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Shift();
        Debug.Log("shifted!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void Shift()
    {
        //for folders
        //AssetDatabase.DeleteAsset(@"Assets\mp3Files");
        AssetDatabase.MoveAsset(@"Assets\newMP3s", @"Assets\mp3Files");
        AssetDatabase.Refresh();
        //for the script
        System.Diagnostics.Process.Start("CMD.exe", "/C py \"C:\\Users\\Cameron\\watchmealways\\Assets\\Scripts\\shift.py\"");
        //debug: py "C:\Users\Cameron\watchmealways\Assets\Scripts\shift.py"
    }
}
