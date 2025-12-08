using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Elephant : MonoBehaviour
{
    public Transform line;
    private List<Vector2> positions = new List<Vector2>();
    private int currentPos;
    private float lerp;
    public float speed = 1;
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer riderSpriteRenderer;
    public Sprite e1;
    public Sprite e2;
    public Sprite r1;
    public Sprite r2;
    public Vector2 rp1;
    public Vector2 rp2;
    private float stepTime;
    private float jump = 0;
    private bool jumping = false;
    private bool startScreen = true;
    public float stars = 0;
    public float allisonStars = 0;
    public RectTransform bar;
    public RectTransform allisonBar;
    public AudioSource music;
    public GameObject intro;
    public List<float> strums = new List<float>();
    private float time;
    public GameObject strum;
    public RectTransform strumTransforms;
    private List<Strum> currentStrums = new List<Strum>();
    private int strumCount = 0;
    public List<GameObject> starGamObjects = new List<GameObject>();
    private bool ended = false;
    private float endLerp = 0;
    public Camera camera;
    public GameObject track;
    public GameObject victory;
    public Transform playerFinalStars;
    public Transform allisonFinalStars;
    public TMP_Text playerPoints;
    public TMP_Text allisonPoints;

    class Strum
    {
        public int index;
        public float time;
        public float posLerp;
        public RectTransform circle;
        public bool strummed;

        public Strum(int index, float time, RectTransform circle)
        {
            this.index = index;
            this.time = time;
            this.circle = circle;
            this.posLerp = 1;
            strummed = false;
        }
    }

    void Start()
    {
        for (int i = 0; i < line.childCount; i++)
        {
            positions.Add(line.GetChild(i).position);
            line.GetChild(i).gameObject.SetActive(false);
        }

        transform.position = positions[0];
        spriteRenderer.sprite = e1;
        riderSpriteRenderer.sprite = r1;
        riderSpriteRenderer.transform.localPosition = rp1;
        SetRotation(GetAngleOutOfTwoPositions(positions[currentPos], positions[currentPos + 1]) +
                    (GetAngleOutOfTwoPositions(positions[currentPos + 1], positions[currentPos + 2]) -
                     GetAngleOutOfTwoPositions(positions[currentPos], positions[currentPos + 1])) * lerp + 90);
    }


    void Update()
    {
        if (startScreen)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) ||
                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                startScreen = false;
                music.Play();
                intro.SetActive(false);
            }

            return;
        }

        if (ended)
        {
            victory.SetActive(true);
            endLerp = Math.Min(1, endLerp + Time.deltaTime / 3);
            camera.orthographicSize = 10 + 20 * endLerp;
            track.SetActive(false);
            for (int i = 0; i < Math.Min(6, Math.Floor(stars)); i++)
            {
                playerFinalStars.transform.GetChild(i).gameObject.SetActive(true);
            }

            for (int i = 0; i < Math.Min(6, Math.Floor(allisonStars)); i++)
            {
                allisonFinalStars.transform.GetChild(i).gameObject.SetActive(true);
            }

            playerPoints.text = " " + Mathf.RoundToInt(stars * 200);
            allisonPoints.text = " " + Mathf.RoundToInt(allisonStars * 200);


            return;
        }

        time += Time.deltaTime;
        if (currentPos == positions.Count - 1)
        {
            spriteRenderer.sprite = e1;
            riderSpriteRenderer.sprite = r1;
            riderSpriteRenderer.transform.localPosition = rp1;
            return;
        }

        lerp += speed * Time.deltaTime / Vector2.Distance(positions[currentPos], positions[currentPos + 1]);
        transform.position = Vector2.MoveTowards(positions[currentPos], positions[currentPos + 1],
            Vector2.Distance(positions[currentPos], positions[currentPos + 1]) * lerp);

        if (currentPos != positions.Count - 2)
        {
            SetRotation(GetAngleOutOfTwoPositions(positions[currentPos], positions[currentPos + 1]) +
                        (GetAngleOutOfTwoPositions(positions[currentPos + 1], positions[currentPos + 2]) -
                         GetAngleOutOfTwoPositions(positions[currentPos], positions[currentPos + 1])) * lerp + 90);
        }
        else
        {
            SetRotation(GetAngleOutOfTwoPositions(positions[currentPos], positions[currentPos + 1]) + 90);
            ended = true;
        }

        if (jumping)
        {
            jump += Time.deltaTime * 8;
            if (jump > 2)
            {
                jumping = false;
                jump = 2;
            }
        }
        else if (!jumping && jump > 0)
        {
            jump -= Time.deltaTime * 4;
            if (jump < 0) jump = 0;
        }

        transform.position = (Vector2)transform.position +
                             GetPositionOutOfAngleAndDistance(transform.eulerAngles.z, jump);

        if (lerp >= 1)
        {
            currentPos++;
            lerp = 0;
            transform.position = positions[currentPos];
        }

        if (jump == 0)
            stepTime += Time.deltaTime;
        if (stepTime >= .3f)
        {
            spriteRenderer.sprite = spriteRenderer.sprite == e1 ? e2 : e1;
            riderSpriteRenderer.sprite = riderSpriteRenderer.sprite == r1 ? r2 : r1;
            riderSpriteRenderer.transform.localPosition = riderSpriteRenderer.sprite == r1 ? rp1 : rp2;
            stepTime = 0;
        }

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) ||
             (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) && !jumping && jump == 0)
        {
            jumping = true;
            Strum closestStrum = null;
            float dist = float.MaxValue;
            foreach (var currentStrum in currentStrums)
            {
                if (currentStrum.strummed) continue;
                if (Math.Abs(currentStrum.posLerp) <= .3f)
                {
                    if (Math.Abs(currentStrum.posLerp) < dist)
                    {
                        dist = Math.Abs(currentStrum.posLerp);
                        closestStrum = currentStrum;
                    }
                }
            }

            if (closestStrum != null)
            {
                float newStars = (5.0f / strums.Count) *
                                 Math.Min(1, (1.2f / (5 * Math.Abs(closestStrum.time - time) + 1)));
                stars += newStars;
                allisonStars += (float)(Math.Max(newStars * 1.2, 5.0f / strums.Count));
                closestStrum.strummed = true;
                closestStrum.circle.GetComponent<Image>().color = Color.green;
                bar.sizeDelta = new Vector2(10, 500 * stars / 5.0f);
                bar.anchoredPosition = new Vector2(20, -250 + bar.sizeDelta.y / 2);
                allisonBar.sizeDelta = new Vector2(10, 500 * allisonStars / 5.0f);
                allisonBar.anchoredPosition = new Vector2(-10, -250 + allisonBar.sizeDelta.y / 2);
                float maxStars = Math.Max(allisonStars, stars);
                for (int i = 0; i < Math.Min(6, Math.Floor(maxStars)); i++)
                {
                    starGamObjects[i].SetActive(true);
                }
            }
        }

        for (var i = strumCount; i < strums.Count; i++)
        {
            var strumTime = strums[i];
            if (strumTime <= time + 5)
            {
                currentStrums.Add(new Strum(i, strumTime,
                    Instantiate(strum, strumTransforms).GetComponent<RectTransform>()));
                strumCount++;
            }
        }

        for (var i = 0; i < currentStrums.Count; i++)
        {
            var strum = currentStrums[i];

            strum.posLerp -= Time.deltaTime / 5;
            strum.circle.anchoredPosition = new Vector2(strum.posLerp * strumTransforms.rect.width / 2, 0);
            if (strum.posLerp <= -1)
            {
                Destroy(strum.circle.gameObject);
                if (!strum.strummed)
                {
                    allisonStars += 5.0f / strums.Count;
                }

                currentStrums.RemoveAt(i);
                i--;
            }
        }
    }

    void SetRotation(float rot)
    {
        transform.eulerAngles = new Vector3(0, 0, rot);
    }

    /// <summary>
    ///     Returns 180 to -180 deg rotation where 0 is upwards and 90 is to the left.
    /// </summary>
    public static float GetAngleOutOfPosition(Vector2 pos)
    {
        float angleRad = Mathf.Atan2(pos.x, pos.y);
        float angleDeg = 180 / Mathf.PI * angleRad;
        return -angleDeg;
    }

    /// <summary>
    ///     Returns 180 to -180 deg rotation where 0 is upwards. and 90 is to the left.
    /// </summary>
    /// <param name="from">The from position</param>
    /// <param name="to">The to position</param>
    /// <returns></returns>
    public static float GetAngleOutOfTwoPositions(Vector2 from, Vector2 to)
    {
        return GetAngleOutOfPosition(to - from);
    }

    /// <summary>
    ///     Returns distance from an angle in degrees and a distance.
    /// </summary>
    /// <param name="angle">Angle in degrees from 0 to 360</param>
    /// <param name="distance">Distance to point</param>
    /// <returns></returns>
    public static Vector2 GetPositionOutOfAngleAndDistance(float angle, float distance)
    {
        return new Vector2(-Mathf.Sin(angle * Mathf.Deg2Rad) * distance, Mathf.Cos(angle * Mathf.Deg2Rad) * distance);
    }
}