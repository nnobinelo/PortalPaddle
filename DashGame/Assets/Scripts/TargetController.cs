﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetController : MonoBehaviour
{
    public GameObject spawnArea;
    RectTransform spawnAreaRect;
    Vector3[] spawnAreaCorners = new Vector3[4];
    CircleCollider2D collider; //used to account for the offset needed for the radius of the target
    public GameObject TargetPrefab;
    public EnemyBehavior ball;
    LevelGenerator LG;
    GameManager game;
    float randomSize;
    Vector3 defaultTargetSize = new Vector3(0.2636f, 0.2636f, 1);
    bool target1Travel;
    bool target2Travel;
    bool target1Hit, target2Hit;
    public float travelSpeed;
    int pointCounter1, pointCounter2;
    Vector3[] travelPath1;
    Vector3[] travelPath2;
    Vector3 currentPoint1, currentPoint2;
    Vector3 nextPoint1, nextPoint2;
    Transform currentTargetInUse;
    Vector3[] tempPath1,tempPath2;

    public static TargetController Instance;

    public delegate void TargetDelegate();
    public static event TargetDelegate TargetHit;
    public static event TargetDelegate TargetHitAndRichochet;

    float size; //size of the target

    class Target
    {
        public bool inUse;
        public Transform transform;
        public Animator animator;

        public Target(Transform t, Animator anim)
        {
            transform = t;
            animator = anim;
        }

        public void StopUsing()
        {
            inUse = false;
        }

        public void Use()
        {
            inUse = true;
        }
    }

    Target[] targets;

    private void Awake()
    {
        Instance = this;
        ball = EnemyBehavior.Instance;
        collider = TargetPrefab.GetComponent<CircleCollider2D>();
        spawnAreaRect = spawnArea.transform as RectTransform;
        spawnAreaRect.GetWorldCorners(spawnAreaCorners);
        target1Travel = false;
        target2Travel = false;
        target1Hit = false;
        target2Hit = false;
        pointCounter1 = 0;
        pointCounter2 = 0;

        targets = new Target[2];

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject go = Instantiate(TargetPrefab, Vector2.right * -1000, Quaternion.identity);
            go.name = "Target" + i;
            Animator anim = go.GetComponent<Animator>();
            targets[i] = new Target(go.transform, anim);
        }
        targets[0].StopUsing();
        targets[1].Use();
    }

    private void Start()
    {
        LG = LevelGenerator.Instance;
        game = GameManager.Instance;
    }

    private void OnEnable()
    {
        EnemyBehavior.AbsorbDoneAndRichochet += AbsorbDoneAndRichochet;
        GameManager.GameStarted += GameStarted;
        EnemyBehavior.AbsorbDone += AbsorbDone;
        GameManager.GameOverConfirmed += GameOverConfirmed;
        LevelGenerator.NextLvlGenerated += NextLvlGenerated;
        GameManager.MoveToNextLvl += MoveToNextLvl;
        LevelGenerator.TransitionDone += TransitionDone;
    }

    private void OnDisable()
    {
        EnemyBehavior.AbsorbDoneAndRichochet -= AbsorbDoneAndRichochet;
        GameManager.GameStarted -= GameStarted;
        EnemyBehavior.AbsorbDone -= AbsorbDone;
        GameManager.GameOverConfirmed -= GameOverConfirmed;
        LevelGenerator.NextLvlGenerated -= NextLvlGenerated;
        GameManager.MoveToNextLvl -= MoveToNextLvl;
        LevelGenerator.TransitionDone -= TransitionDone;
    }

    private void Update()
    {
        targets[0].animator.SetBool("InUse", targets[0].inUse);
        targets[1].animator.SetBool("InUse", targets[1].inUse);

        if (target1Hit)
        {
            target1Travel = false;
        }
        if (target2Hit)
        {
            target2Travel = false;
        }

        if (target1Travel)
        {
            nextPoint1 = PointOnPath(travelPath1, pointCounter1);
            targets[0].transform.localPosition = Vector2.MoveTowards(targets[0].transform.localPosition, nextPoint1, Time.deltaTime * travelSpeed);
            if (targets[0].transform.localPosition == nextPoint1)
            {
                if (pointCounter1 == travelPath1.Length - 1)
                {
                    pointCounter1 = 0;
                }
                pointCounter1 += 1;
            }
        }

        if (target2Travel)
        {
            targets[1].transform.localPosition = Vector2.MoveTowards(targets[1].transform.localPosition, nextPoint2, Time.deltaTime * travelSpeed);
            nextPoint2 = PointOnPath(travelPath2, pointCounter2);
            if (targets[1].transform.localPosition == nextPoint2)
            {
                if (pointCounter2 + 1 == travelPath2.Length - 1)
                {
                    pointCounter2 = 0;
                }
                pointCounter2 += 1;
            }
        }
    }

    void SelectTargetToTravel(Target target)
    {
        int aRandomNum = Random.Range(0, 1); //if equal to 0 will travel in normal order if 1 will travel in reverse order

        if (target == targets[0])
        {
            target1Hit = false;
            target1Travel = true;

            if (aRandomNum == 1)
            {
                tempPath1 = LG.GetNextObstaclePath;
                System.Array.Reverse(tempPath1);
                travelPath1 = tempPath1;
            }
            else
            {
                travelPath1 = LG.GetNextObstaclePath;
            }

            pointCounter1 = Random.Range(0,travelPath1.Length-1);
        }

        if (target == targets[1])
        {
            target2Hit = false;
            target2Travel = true;
            if (aRandomNum == 1)
            {
                tempPath2 = LG.GetNextObstaclePath;
                System.Array.Reverse(tempPath2);
                travelPath2 = tempPath2;
            }
            else
            {
                travelPath2 = LG.GetNextObstaclePath;
            }
            pointCounter2 = Random.Range(0, travelPath2.Length - 1);
        }
    }

    Vector3 PointOnPath(Vector3[] path, int iterator)
    { 
        return path[iterator];
    }

    void MoveToNextLvl()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i].inUse)
            {
                targets[i].StopUsing();
                if (i == 0)
                {
                    target1Hit = true;
                }
                if (i == 1)
                {
                    target2Hit = true;
                }
            }
            else
            {
                targets[i].Use();
            }
        }
    }

    void AbsorbDone()
    {
        TargetHit();

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i].inUse)
            {
                targets[i].StopUsing();
                if (i == 0)
                {
                    target1Hit = true;
                }
                if (i == 1)
                {
                    target2Hit = true;
                }
            }
            else
            {
                targets[i].Use();
            }
        }

    }

    void AbsorbDoneAndRichochet()
    {
        TargetHitAndRichochet();

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i].inUse)
            {
                targets[i].StopUsing();
                if (i == 0)
                {
                    target1Hit = true;
                }
                if (i == 1)
                {
                    target2Hit = true;
                }
            }
            else
            {
                targets[i].Use();
            }
        }

    }

    void TransitionDone()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i].inUse)
            {
                currentTargetInUse = targets[i].transform;
            }
        }
    }

    void NextLvlGenerated()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (!targets[i].inUse)
            {
                /*
                if (game.GetScore < -1)
                {
                    targets[i].transform.parent = LG.GetNextLvl;
                    targets[i].transform.localScale = defaultTargetSize;
                    targets[i].transform.localPosition = RandomPos();
                }
                
                if (game.GetScore >= -1 && game.GetScore < -1)
                {
                    targets[i].transform.parent = LG.GetNextLvl;
                    randomSize = Random.Range(.06f, defaultTargetSize.x);
                    Debug.Log(randomSize);
                    targets[i].transform.localScale = new Vector3(randomSize, randomSize, 1);
                    targets[i].transform.localPosition = RandomPos();
                }
                */

                //if(game.GetScore > 0)
                {
                    targets[i].transform.parent = LG.GetNextLvl;
                    targets[i].transform.localScale = defaultTargetSize;
                    targets[i].transform.localPosition = LG.GetNextObstaclePath[Random. Range(0,LG.GetNextObstaclePath.Length-1)];
                    SelectTargetToTravel(targets[i]);
                }

            }
        }
    }

    void GameOverConfirmed()
    {
        targets[0].transform.position = Vector2.right * -1000;
        targets[1].transform.position = Vector2.right * -1000;

        target1Travel = false;
        target2Travel = false;
    }

    void GameStarted()
    {
        targets[0].StopUsing();
        targets[1].Use();
        targets[1].transform.parent = LG.GetCurrentLvl;
        targets[1].transform.localPosition = RandomPos();
        targets[1].animator.SetTrigger("GameStarted");
        currentTargetInUse = targets[1].transform;
    }

    public Vector2 RandomPos()
    {
        return new Vector2(Random.Range(spawnAreaCorners[0].x + (2.09f * TargetPrefab.transform.localScale.x), spawnAreaCorners[3].x - (2.09f * TargetPrefab.transform.localScale.x)), Random.Range(spawnAreaCorners[0].y + (2.09f * TargetPrefab.transform.localScale.x), spawnAreaCorners[2].y - (2.09f * TargetPrefab.transform.localScale.x)));
    }

    public int RandomSpawnAreaXRange
    {
        get
        {
            return (int)Random.Range(spawnAreaCorners[0].x + (2.09f * TargetPrefab.transform.localScale.x), spawnAreaCorners[3].x - (2.09f * TargetPrefab.transform.localScale.x));
        }
    }

    public Transform GetCurrentTargetTransform
    {
        get
        {
            return currentTargetInUse;
        }
    }
}
