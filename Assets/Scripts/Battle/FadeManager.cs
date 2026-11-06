using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    [SerializeField] float speed = 1f;

    private Image fadeImage;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        // 親オブジェクト(FadeImage)のImageを取得
        fadeImage = GetComponentInParent<Image>();

        if (fadeImage == null)
        {
            Debug.LogError("親オブジェクトにImageが見つかりません");
            return;
        }

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        fadeImage.enabled = false;
    }

    public IEnumerator FadeOutStart()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeImage.enabled = true;
        fadeCoroutine = StartCoroutine(Fade(1f));

        yield return fadeCoroutine;
    }

    public IEnumerator FadeInStart()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeImage.enabled = true;
        fadeCoroutine = StartCoroutine(Fade(0f));

        yield return fadeCoroutine;
    }

    IEnumerator Fade(float targetAlpha)
    {
        float alpha = fadeImage.color.a;

        while (!Mathf.Approximately(alpha, targetAlpha))
        {
            alpha = Mathf.MoveTowards(
                alpha,
                targetAlpha,
                speed * Time.deltaTime
            );

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(targetAlpha);

        if (targetAlpha == 0f)
        {
            fadeImage.enabled = false;
        }
    }

    void SetAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}