using UnityEngine;
using AbyssalReach.Core;

public class BagFillVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bagCircle;
    [SerializeField] private Transform bagTriangle;

    [Header("Circle Scale")]
    [SerializeField] private Vector2 emptyCircle = new Vector2(1f, 1f);
    [SerializeField] private Vector2 fullCircle = new Vector2(1.6f, 1.6f);

    [Header("Triangle Width")]
    [SerializeField] private float triangleMin = 1f;
    [SerializeField] private float triangleMax = 1.7f;

    [Header("Animation")]
    [SerializeField] private float animationSpeed = 5f;

    void Update()
    {
        if (InventoryManager.Instance == null)
            return;

        var diverInventory = InventoryManager.Instance.GetDiverInventory();

        float percent = diverInventory.GetCurrentWeight() / diverInventory.GetMaxWeight();

        // Tamaño objetivo del círculo
        Vector2 targetCircle = Vector2.Lerp(emptyCircle, fullCircle, percent);
        Vector3 targetCircleScale = new Vector3(targetCircle.x, targetCircle.y, 1);

        // Interpolación suave del círculo
        bagCircle.localScale = Vector3.Lerp(
            bagCircle.localScale,
            targetCircleScale,
            Time.deltaTime * animationSpeed
        );

        // Tamaño objetivo del triángulo
        float targetWidth = Mathf.Lerp(triangleMin, triangleMax, percent);

        Vector3 currentTri = bagTriangle.localScale;
        currentTri.x = Mathf.Lerp(
            currentTri.x,
            targetWidth,
            Time.deltaTime * animationSpeed
        );

        bagTriangle.localScale = currentTri;
    }
}