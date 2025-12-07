using System;
using System.Collections.Generic;
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
    public AudioSource music;
    public GameObject intro;
    public List<float> strums = new List<float>();
    private float time;
    public GameObject strum;
    public RectTransform strumTransforms;
    private List<Strum> currentStrums = new List<Strum>();
    private int strumCount = 0;

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
            if (Input.GetKeyDown(KeyCode.Space))
            {
                startScreen = false;
                music.Play();
                intro.SetActive(false);
            }

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

        if (Input.GetKeyDown(KeyCode.Space) && !jumping && jump == 0)
        {
            // strums.Add(time);
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
                float newStars = (5.0f / strums.Count) * Math.Min(1, (1.4f / (5 * Math.Abs(closestStrum.time - time) + 1)));
                stars += newStars;
                closestStrum.strummed = true;
                closestStrum.circle.GetComponent<Image>().color = Color.green;
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