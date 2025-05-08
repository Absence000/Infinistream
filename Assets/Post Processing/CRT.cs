using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CRT : MonoBehaviour
{
    public Shader shader;
    float bend = 4f;
    float scanlineSize1 = 290;
    float scanlineSpeed1 = 1;
    float scanlineSize2 = 300;
    float scanlineSpeed2 = -1;
    float scanlineAmount = 0.002f;
    float vignetteSize = 1.9f;
    float vignetteSmoothness = 0.6f;
    float vignetteEdgeRound = 8f;
    float noiseSize = 75f;
    float noiseAmount = 0.01f;

    // Chromatic aberration amounts
    Vector2 redOffset = new Vector2(0, -0.001f);
    Vector2 blueOffset = Vector2.zero;
    Vector2 greenOffset = new Vector2(0, 0.001f);

    private Material material;

    // Use this for initialization
    void Start()
    {
        material = new Material(shader);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        material.SetFloat("u_time", Time.fixedTime);
        material.SetFloat("u_bend", bend);
        material.SetFloat("u_scanline_size_1", scanlineSize1);
        material.SetFloat("u_scanline_speed_1", scanlineSpeed1);
        material.SetFloat("u_scanline_size_2", scanlineSize2);
        material.SetFloat("u_scanline_speed_2", scanlineSpeed2);
        material.SetFloat("u_scanline_amount", scanlineAmount);
        material.SetFloat("u_vignette_size", vignetteSize);
        material.SetFloat("u_vignette_smoothness", vignetteSmoothness);
        material.SetFloat("u_vignette_edge_round", vignetteEdgeRound);
        material.SetFloat("u_noise_size", noiseSize);
        material.SetFloat("u_noise_amount", noiseAmount);
        material.SetVector("u_red_offset", redOffset);
        material.SetVector("u_blue_offset", blueOffset);
        material.SetVector("u_green_offset", greenOffset);
        Graphics.Blit(source, destination, material);
    }
}