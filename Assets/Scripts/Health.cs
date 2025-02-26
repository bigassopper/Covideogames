﻿using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    public GameObject body;
    MeshRenderer meshRenderer;
    Color startColor;
    Color endColor = Color.red;


    public float baseInfectionRate = 0;
    public float infectionMultiplier = 1;

    public float infectionLevel = 0;
    public float spreadMultiplier = 1;

    public float baseSpreadArea = 4f;
    public float spreadArea;

    public float diseaseSpread = 1f;

    ParticleSystem sickness;
    public ParticleSystem sicknessEffect;
    public bool isSick = false;
    public bool isContagious = false;

    PlayerController player;

    List<Health> peopleInRange;
    List<Health> contagiousPeopleInRange;
    Gradient gradient;

    AudioSource audioSource;
    public AudioClip deathSound;
    public ParticleSystem deathFX;
    //public GameObject tombstonePrefab;
    private void Awake()
    {

        meshRenderer = body.GetComponent<MeshRenderer>();
    
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        peopleInRange = new List<Health>();
        contagiousPeopleInRange = new List<Health>();
        audioSource = GetComponent<AudioSource>();

    }

    void Start()
    {
        SetupDiseaseGradient();
        /* tá comentado pois essa mudança causou muitos bugs
         * if(!this.gameObject.CompareTag("Player") && infectionLevel == 0)
        {
            if (Random.Range(0, 100) < 20)
                infectionLevel = UnityEngine.Random.Range(0, 50);
            else
                infectionLevel = UnityEngine.Random.Range(0, 20);


        }*/
    }

    private void SetupDiseaseGradient()
    {
        startColor = meshRenderer.material.color;
        gradient = new Gradient();

        // Populate the color keys at the relative time 0 and 1 (0 and 100%)
        GradientColorKey[] colorKey = new GradientColorKey[2];
        colorKey[0].color = startColor;
        colorKey[0].time = 0.0f;
        colorKey[1].color = endColor;
        colorKey[1].time = 1.0f;

        // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
        GradientAlphaKey[] alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 0.0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 1.0f;

        gradient.SetKeys(colorKey, alphaKey);

    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.gameManager.gameIsOver) return;

        spreadArea = baseSpreadArea * spreadMultiplier;
        ProcessDisease(FindPeopleInRange(), contagiousPeopleInRange);
        ProcessInfectionMilestones();
    }



    private float FindPeopleInRange()
    {
        peopleInRange.Clear();
        contagiousPeopleInRange.Clear();
        foreach (AiController npc in GameStats.gameStats.npcs)
        {
            if (npc.transform == this.transform) continue;
            if (Vector3.Distance(this.transform.position, npc.transform.position) < npc.GetComponent<Health>().spreadArea)
            {
                peopleInRange.Add(npc.GetComponent<Health>());
                if (npc.GetComponent<Health>().isContagious)
                {
                    contagiousPeopleInRange.Add(npc.GetComponent<Health>());
                }
            }
        }
        return peopleInRange.Count;
    }
    private void ProcessDisease(float peopleInRange, List<Health> contagiousPeopleInRange)
    {
        float originalRate = baseInfectionRate;
        baseInfectionRate = peopleInRange - contagiousPeopleInRange.Count;
        if (contagiousPeopleInRange != null)
        {
            foreach (Health npc in contagiousPeopleInRange)
            {
                baseInfectionRate += (npc.diseaseSpread * npc.spreadMultiplier);
            }
        }
        infectionLevel += baseInfectionRate * infectionMultiplier * Time.deltaTime;
        meshRenderer.material.color = gradient.Evaluate(infectionLevel / 100);
        baseInfectionRate = originalRate;
    }




    private void ProcessInfectionMilestones()
    {
        if (infectionLevel > 30)
        {
            if (!isSick)
            {
                GetSick();
            }

        }
        if (infectionLevel > 50)
        {
            if (!isContagious)
            {
                GetContagious();
            }
            else
            {
                IncreaseSicknessOverTime();
            }
        }
        if (infectionLevel > 100)
        {
            Die();
        }
    }

    private void IncreaseSicknessOverTime()
    {
        var emission = sickness.emission;
        emission.rateOverTime = 2 + (infectionLevel / 100) * 1.5f;
    }



    private void Die()
    {
        GameStats.gameStats.npcs.Remove(GetComponent<AiController>());
        GameStats.gameStats.sickPeopleList.Remove(this);
        GameStats.gameStats.peopleDead++;
        if (this.gameObject.GetComponent<AiController>())
        {
            if (this.gameObject.GetComponent<AiController>().wearingMask) GameStats.gameStats.peopleMasked--;
        }

        if (!GameManager.gameManager.ShouldWin())
        {
            AudioSource.PlayClipAtPoint(deathSound, Camera.main.transform.position);
        }

        //checar qtd de pontos pra ver qual áudio tocar.

        /* GameObject tombstone = Instantiate(tombstonePrefab, transform.position, Quaternion.identity, transform.parent);
        Destroy(tombstone, 20f);*/
        Destroy(this.gameObject);
    }

    private void GetSick()
    {
        isSick = true;
        if (!this.gameObject.CompareTag("Player")) GameStats.gameStats.sickPeopleList.Add(this);
    }

    private void GetContagious()
    {
        isContagious = true;
        sickness = Instantiate(sicknessEffect, this.transform);
        diseaseSpread *= 2f;
        baseSpreadArea *= 2f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(this.transform.position, spreadArea);
    }
}
