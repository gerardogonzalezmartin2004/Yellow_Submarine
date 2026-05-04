using UnityEngine;
using TMPro;
using System.Collections;

public class GoldLossDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI tmpText;

    [Header("Display Settings")]
    [Tooltip("Segundos visible antes del fade")]
    [SerializeField] private float visibleDuration = 2f;

    [Tooltip("Duración del fade out")]
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Text Format")]
    [SerializeField] private Color textColor = new Color(1f, 0.3f, 0.3f, 1f);

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (tmpText == null)
            tmpText = GetComponent<TextMeshProUGUI>();

        if (tmpText == null) { Debug.LogError("[GoldLossDisplay] Falta TextMeshProUGUI"); return; }

        tmpText.color = textColor;
        SetAlpha(0f);
    }

    private void OnEnable() => ItemDamageTracker.OnDamageApplied += OnDamageApplied;
    private void OnDisable()
    {
        ItemDamageTracker.OnDamageApplied -= OnDamageApplied;

        // Matar la coroutine y resetear alpha al desactivarse
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        SetAlpha(0f);
    }

    // Ahora recibe solo el daño de este golpe
    private void OnDamageApplied(int goldLostThisHit)
    {
        if (tmpText == null || goldLostThisHit <= 0) return;

        // Muestra solo lo perdido en este golpe, varía según cuántos objetos tengas
        tmpText.text = "-" + goldLostThisHit + "G";

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(ShowAndFade());
    }

    private IEnumerator ShowAndFade()
    {
        SetAlpha(1f);
        yield return new WaitForSeconds(visibleDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(1f - Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        fadeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (tmpText == null) return;
        Color c = tmpText.color;
        c.a = alpha;
        tmpText.color = c;
    }
}