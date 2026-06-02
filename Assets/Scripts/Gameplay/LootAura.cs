using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LootAura : MonoBehaviour
{
    [Header("Item Source ")]
    [Tooltip("Si lo dejas vacio, busca el LootPickup en el padre automaticamente.")]
    [SerializeField] private LootPickup lootPickup;

    [Header("Opacidad")]
    [Tooltip("Opacidad global del aura (0 = invisible, 1 = opaco).")]
    [Range(0f, 1f)]
    [SerializeField] private float opacity = 1f;

    [Header("Tinte del objeto principal")]
    [Tooltip("Si esta activo, tine el sprite del objeto con el color de su rareza.")]
    [SerializeField] private bool tintMainObject = true;
    [Tooltip("Cuanto color de rareza se mezcla en el objeto (0 = nada, 1 = todo).")]
    [Range(0f, 1f)]
    [SerializeField] private float tintStrength = 0.35f;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minAlpha   = 0.1f;
    [SerializeField] private float maxAlpha   = 0.5f;

    [Header("Color")]
    [Tooltip("Sube la saturacion del color para que no se vea lavado/clarito. 0 = color original.")]
    [Range(0f, 1f)]
    [SerializeField] private float extraSaturation = 0.4f;

    [Header("Sorting")]
    [Tooltip("Orden de dibujado del aura. Debe ser menor que el del objeto para quedar detras.")]
    [SerializeField] private int sortingOrder = -1;

    [Header("Brillo")]
    [Tooltip("Si esta activo usa additive (brilla pero lava el color). Desactivado = color solido.")]
    [SerializeField] private bool useAdditive = false;

    private SpriteRenderer auraRenderer;
    private Color baseColor;
    private bool initialized = false;

    private static Sprite sharedGlowSprite;
    private static Material additiveMaterial;

    private void Awake()
    {
        auraRenderer = GetComponent<SpriteRenderer>();
        auraRenderer.sprite = GetGlowSprite();
        auraRenderer.sortingOrder = sortingOrder;

        if (useAdditive)
        {
            Material mat = GetAdditiveMaterial();
            if (mat != null)
                auraRenderer.material = mat;
        }
    }

    private void Start()
    {
        if (lootPickup == null)
            lootPickup = GetComponentInParent<LootPickup>();

        if (lootPickup != null && lootPickup.itemData != null)
            baseColor = lootPickup.itemData.GetAuraColor();
        else
            baseColor = Color.white;

        float maxChannel = Mathf.Max(baseColor.r, baseColor.g, baseColor.b);
        float minChannel = Mathf.Min(baseColor.r, baseColor.g, baseColor.b);
        bool esGris = (maxChannel - minChannel) < 0.1f;

        if (!esGris)
        {
            float h, s, v;
            Color.RGBToHSV(baseColor, out h, out s, out v);
            s = Mathf.Clamp01(s + extraSaturation);
            baseColor = Color.HSVToRGB(h, s, v);
        }

        if (tintMainObject && lootPickup != null)
        {
            SpriteRenderer mainSprite = FindMainSprite();
            if (mainSprite != null)
            {
                Color tint = Color.Lerp(Color.white, baseColor, tintStrength);
                tint.a = mainSprite.color.a;
                mainSprite.color = tint;
            }
            else
            {
                Debug.LogWarning("[LootAura] No se encontro el SpriteRenderer del prop para tintar.");
            }
        }

        baseColor.a = maxAlpha;
        auraRenderer.color = baseColor;
        initialized = true;
    }

    private SpriteRenderer FindMainSprite()
    {
        SpriteRenderer[] all = lootPickup.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in all)
        {
            if (sr != auraRenderer)
                return sr;
        }
        return null;
    }

    private void Update()
    {
        if (!initialized) return;

        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        baseColor.a = Mathf.Lerp(minAlpha, maxAlpha, t) * opacity;
        auraRenderer.color = baseColor;
    }

    private static Material GetAdditiveMaterial()
    {
        if (additiveMaterial != null) return additiveMaterial;

        string[] shaderNames =
        {
            "Legacy Shaders/Particles/Additive",
            "Mobile/Particles/Additive",
            "Particles/Additive"
        };

        foreach (string shaderName in shaderNames)
        {
            Shader sh = Shader.Find(shaderName);
            if (sh != null)
            {
                additiveMaterial = new Material(sh);
                if (additiveMaterial.HasProperty("_TintColor"))
                    additiveMaterial.SetColor("_TintColor", Color.white);
                return additiveMaterial;
            }
        }

        Debug.LogWarning("[LootAura] No se encontro shader additive. El aura usara blend normal.");
        return null;
    }

    private static Sprite GetGlowSprite()
    {
        if (sharedGlowSprite != null) return sharedGlowSprite;

        int size = 256;
        float radius = size / 2f;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / radius;

                float glow = Mathf.Exp(-(dist * dist) * 4f);
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - dist));
                float alpha = glow * edge;

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();

        sharedGlowSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return sharedGlowSprite;
    }
}
