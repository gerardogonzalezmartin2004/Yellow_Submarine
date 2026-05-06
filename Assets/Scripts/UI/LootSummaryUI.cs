using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LootSummaryUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Prefab de tarjeta")]
    [SerializeField] private GameObject cardPrefab;

    [Header("Posición — margen desde la esquina superior derecha")]
    [SerializeField] private float marginRight = 20f;
    [SerializeField] private float marginTop = 20f;

    [Header("Tamaño de cada tarjeta")]
    [SerializeField] private Vector2 cardSize = new Vector2(200f, 50f);
    [SerializeField] private float cardSpacing = 6f;

    [Header("Animación")]
    [SerializeField] private float slideDistance = 80f;
    [SerializeField] private float slideInDuration = 0.18f;
    [SerializeField] private float staggerDelay = 0.08f;   // pequeño delay entre cada tarjeta
    [SerializeField] private float holdDuration = 3f;       // cuánto tiempo se quedan todas visibles
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region Private

    private RectTransform container;
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        CreateContainer();
    }

    #endregion

    #region Container Setup

    private void CreateContainer()
    {
        Canvas rootCanvas = FindRootCanvas();
        if (rootCanvas == null)
        {
            Debug.LogError("[LootSummaryUI] No se encontró Canvas en la escena.");
            return;
        }

        GameObject go = new GameObject("LootSummaryContainer");
        go.transform.SetParent(rootCanvas.transform, false);

        container = go.AddComponent<RectTransform>();
        container.anchorMin = new Vector2(1f, 1f);
        container.anchorMax = new Vector2(1f, 1f);
        container.pivot = new Vector2(1f, 1f);
        container.anchoredPosition = new Vector2(-marginRight, -marginTop);
        container.sizeDelta = new Vector2(cardSize.x, 0f);

        go.transform.SetAsLastSibling();
        LogDebug("Container creado en top-right del Canvas.");
    }

    private Canvas FindRootCanvas()
    {
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null) return c.rootCanvas;

        Canvas[] all = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in all)
            if (canvas.isRootCanvas) return canvas;

        return null;
    }

    #endregion

    #region Public API

    public void ShowLootSummary(List<InventoryItem> boatItems, float totalTime)
    {
        if (boatItems == null || boatItems.Count == 0) return;
        if (cardPrefab == null) { Debug.LogError("[LootSummaryUI] cardPrefab no asignado."); return; }
        if (container == null) { Debug.LogError("[LootSummaryUI] Container nulo."); return; }

        StopAll();

        LogDebug($"Mostrando {boatItems.Count} tarjetas apiladas.");

        // ── Lanzar todas las tarjetas a la vez con un pequeño stagger ──
        for (int i = 0; i < boatItems.Count; i++)
        {
            if (boatItems[i]?.itemData == null) continue;

            // Cada tarjeta ocupa su fila: 0 = la más arriba, 1 = debajo, etc.
            float yPos = -i * (cardSize.y + cardSpacing);
            float delay = i * staggerDelay;

            Coroutine c = StartCoroutine(ShowCard(boatItems[i].itemData, delay, yPos));
            activeCoroutines.Add(c);
        }
    }

    #endregion

    #region Card Coroutine

    private IEnumerator ShowCard(ItemData data, float delay, float yPos)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // ── Instanciar ──
        GameObject cardGO = Instantiate(cardPrefab, container);

        RectTransform cardRect = cardGO.GetComponent<RectTransform>();
        CanvasGroup cg = cardGO.GetComponent<CanvasGroup>()
                                 ?? cardGO.AddComponent<CanvasGroup>();

        if (cardRect == null) { Destroy(cardGO); yield break; }

        // ── Posición fija en la columna ──
        cardRect.anchorMin = new Vector2(1f, 1f);
        cardRect.anchorMax = new Vector2(1f, 1f);
        cardRect.pivot = new Vector2(1f, 1f);
        cardRect.sizeDelta = cardSize;

        Vector2 visiblePos = new Vector2(0f, yPos);
        Vector2 hiddenPos = visiblePos + new Vector2(slideDistance, 0f);

        cardRect.anchoredPosition = hiddenPos;
        cg.alpha = 0f;

        FillCard(cardGO, data);

        // ── Slide-in ──
        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / slideInDuration), 3f);
            cardRect.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, t);
            cg.alpha = t;
            yield return null;
        }
        cardRect.anchoredPosition = visiblePos;
        cg.alpha = 1f;

        // ── Todas visibles juntas hasta holdDuration ──
        yield return new WaitForSeconds(holdDuration);

        // ── Fade-out ──
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }

        Destroy(cardGO);
    }

    #endregion

    #region Fill Card

    private void FillCard(GameObject card, ItemData data)
    {
        Transform iconT = card.transform.Find("Icon");
        if (iconT != null)
        {
            Image img = iconT.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = data.itemIcon;
                img.color = data.itemIcon != null ? Color.white : Color.clear;
                img.enabled = data.itemIcon != null;
            }
        }

        Transform nameT = card.transform.Find("ItemName");
        if (nameT != null)
        {
            TextMeshProUGUI tmp = nameT.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = data.itemName;
        }

        Transform rarityT = card.transform.Find("Rarity");
        if (rarityT != null)
        {
            TextMeshProUGUI tmp = rarityT.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = data.rarity.ToString();
                tmp.color = data.GetAuraColor();
            }
        }
    }

    #endregion

    #region Helpers

    private void StopAll()
    {
        foreach (Coroutine c in activeCoroutines)
            if (c != null) StopCoroutine(c);
        activeCoroutines.Clear();

        if (container != null)
            foreach (Transform child in container)
                Destroy(child.gameObject);
    }

    private void LogDebug(string msg)
    {
        if (showDebugLogs) Debug.Log($"[LootSummaryUI] {msg}");
    }

    #endregion
}