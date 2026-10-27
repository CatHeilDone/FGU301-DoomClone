using UnityEngine;
using UnityEngine.UI;

public class BubbleSpawner : MonoBehaviour
{
    public RectTransform container;          // leave empty to use this object
    public Sprite bubbleSprite;
    public int bubbleCount = 30;
    public Vector2 sizeRange = new Vector2(15f, 60f);
    public Vector2 speedRange = new Vector2(40f, 120f);
    public float wobbleAmount = 20f;
    [Range(0f, 1f)] public float maxAlpha = 0.5f;

    private RectTransform[] bubbles;
    private float[] speeds;
    private float[] offsets;
    private float[] baseX;

    void Start()
    {
        if (container == null) container = (RectTransform)transform;

        bubbles = new RectTransform[bubbleCount];
        speeds = new float[bubbleCount];
        offsets = new float[bubbleCount];
        baseX = new float[bubbleCount];

        for (int i = 0; i < bubbleCount; i++)
        {
            GameObject go = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(container, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = go.GetComponent<Image>();
            img.sprite = bubbleSprite;
            img.raycastTarget = false;   // so bubbles never block button clicks
            img.color = new Color(1f, 1f, 1f, Random.Range(0.15f, maxAlpha));

            bubbles[i] = rt;
            ResetBubble(i, true);
        }
    }

    void ResetBubble(int i, bool randomHeight)
    {
        Rect r = container.rect;
        float size = Random.Range(sizeRange.x, sizeRange.y);
        bubbles[i].sizeDelta = new Vector2(size, size);

        speeds[i] = Random.Range(speedRange.x, speedRange.y);
        offsets[i] = Random.Range(0f, 6.28f);
        baseX[i] = Random.Range(-r.width * 0.5f, r.width * 0.5f);

        float y = randomHeight
            ? Random.Range(-r.height * 0.5f, r.height * 0.5f)
            : -r.height * 0.5f - size;

        bubbles[i].anchoredPosition = new Vector2(baseX[i], y);
    }

    void Update()
    {
        float top = container.rect.height * 0.5f;

        for (int i = 0; i < bubbles.Length; i++)
        {
            float y = bubbles[i].anchoredPosition.y + speeds[i] * Time.unscaledDeltaTime;
            float x = baseX[i] + Mathf.Sin(Time.unscaledTime * 1.5f + offsets[i]) * wobbleAmount;
            bubbles[i].anchoredPosition = new Vector2(x, y);

            if (y > top + bubbles[i].sizeDelta.y)
            {
                ResetBubble(i, false);   // back to the bottom
            }
        }
    }
}