using UnityEngine;
using System.Collections.Generic;

namespace AbyssalReach.Gameplay
{
    // Sistema de bolsa deformable usando partículas conectadas con SpringJoints
    // Simula comportamiento de tela/softbody sin necesidad de plugins
    public class DiverBagSoftbody : MonoBehaviour
    {
        [Header("Bag Structure")]
        [Tooltip("Ancho de la bolsa (número de partículas horizontales)")]
        [SerializeField] private int width = 5;

        [Tooltip("Alto de la bolsa (número de partículas verticales)")]
        [SerializeField] private int height = 7;

        [Tooltip("Separación entre partículas")]
        [SerializeField] private float particleSpacing = 0.3f;

        [Header("Physics")]
        [Tooltip("Masa de cada partícula")]
        [SerializeField] private float particleMass = 0.1f;

        [Tooltip("Fuerza de los springs estructurales")]
        [SerializeField] private float structuralSpring = 50f;

        [Tooltip("Amortiguación de los springs")]
        [SerializeField] private float springDamper = 2f;

        [Tooltip("Gravedad aplicada a las partículas")]
        [SerializeField] private float particleGravity = 0.5f;

        [Header("Anchor")]
        [Tooltip("Número de partículas superiores fijas al diver")]
        [SerializeField] private int anchoredParticles = 3;

        [Tooltip("Distancia del diver a la boca de la bolsa")]
        [SerializeField] private float anchorDistance = 1f;

        [Header("Storage")]
        [Tooltip("Capacidad máxima de tesoros")]
        [SerializeField] private int maxCapacity = 15;

        [Tooltip("Radio del área de almacenamiento")]
        [SerializeField] private float storageRadius = 1.5f;

        [Header("Internal Physics")]
        [Tooltip("Drag de tesoros dentro")]
        [SerializeField] private float internalDrag = 3f;

        [Tooltip("Gravedad de tesoros dentro")]
        [SerializeField] private float internalGravity = 0.8f;

        [Header("Visual")]
        [Tooltip("Material para el mesh de la bolsa")]
        [SerializeField] private Material bagMaterial;

        [Tooltip("Color de la bolsa")]
        [SerializeField] private Color bagColor = new Color(0.6f, 0.4f, 0.2f, 0.8f);

        [Header("References")]
        [Tooltip("Transform del diver")]
        [SerializeField] private Transform diverTransform;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;
        [SerializeField] private bool showParticles = true;
        [SerializeField] private bool showSprings = true;

        // Sistema de partículas
        private class BagParticle
        {
            public GameObject gameObject;
            public Rigidbody2D rigidbody;
            public CircleCollider2D collider;
            public bool isAnchored;
            public Vector2 localOffset; // Offset respecto al diver cuando está anclada
        }

        private BagParticle[,] particles;
        private List<SpringJoint2D> springs = new List<SpringJoint2D>();
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh bagMesh;

        // Storage
        private List<LootObject> storedTreasures = new List<LootObject>();
        private Transform storageCenter;

        #region Unity Lifecycle

        private void Awake()
        {
            if (diverTransform == null)
            {
                diverTransform = transform.parent;
            }

            CreateBagStructure();
            CreateVisualMesh();
        }

        private void Update()
        {
            UpdateAnchoredParticles();
            UpdateVisualMesh();
        }

        #endregion

        #region Bag Structure Creation

        private void CreateBagStructure()
        {
            particles = new BagParticle[width, height];

            Vector2 startPos = transform.position;

            // Crear partículas
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 pos = startPos + new Vector2(
                        (x - width / 2f) * particleSpacing,
                        -y * particleSpacing
                    );

                    particles[x, y] = CreateParticle(pos, x, y);
                }
            }

            // Crear springs estructurales
            CreateSprings();

            // Crear centro de almacenamiento
            CreateStorageCenter();
        }

        private BagParticle CreateParticle(Vector2 position, int x, int y)
        {
            GameObject particleObj = new GameObject($"Particle_{x}_{y}");
            particleObj.transform.parent = transform;
            particleObj.transform.position = position;
            particleObj.layer = LayerMask.NameToLayer("Default");

            BagParticle particle = new BagParticle();
            particle.gameObject = particleObj;

            // Rigidbody
            Rigidbody2D rb = particleObj.AddComponent<Rigidbody2D>();
            rb.mass = particleMass;
            rb.linearDamping = 2f;
            rb.gravityScale = particleGravity;
            particle.rigidbody = rb;

            // Collider pequeño
            CircleCollider2D col = particleObj.AddComponent<CircleCollider2D>();
            col.radius = particleSpacing * 0.3f;
            particle.collider = col;

            // Verificar si es partícula anclada (fila superior, centradas)
            if (y == 0)
            {
                int centerX = width / 2;
                int halfAnchored = anchoredParticles / 2;

                if (x >= centerX - halfAnchored && x <= centerX + halfAnchored)
                {
                    particle.isAnchored = true;
                    rb.bodyType = RigidbodyType2D.Kinematic;

                    // Calcular offset desde el diver
                    if (diverTransform != null)
                    {
                        particle.localOffset = (Vector2)particleObj.transform.position - (Vector2)diverTransform.position;
                    }
                }
            }

            return particle;
        }

        private void CreateSprings()
        {
            // Springs horizontales
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    CreateSpring(particles[x, y], particles[x + 1, y]);
                }
            }

            // Springs verticales
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    CreateSpring(particles[x, y], particles[x, y + 1]);
                }
            }

            // Springs diagonales (para rigidez)
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    CreateSpring(particles[x, y], particles[x + 1, y + 1]);
                    CreateSpring(particles[x + 1, y], particles[x, y + 1]);
                }
            }
        }

        private void CreateSpring(BagParticle p1, BagParticle p2)
        {
            if (p1.isAnchored && p2.isAnchored) return;

            SpringJoint2D spring = p1.gameObject.AddComponent<SpringJoint2D>();
            spring.connectedBody = p2.rigidbody;
            spring.autoConfigureDistance = false;
            spring.distance = Vector2.Distance(p1.gameObject.transform.position, p2.gameObject.transform.position);
            spring.frequency = structuralSpring;
            spring.dampingRatio = springDamper;

            springs.Add(spring);
        }

        private void CreateStorageCenter()
        {
            GameObject centerObj = new GameObject("StorageCenter");
            centerObj.transform.parent = transform;

            // Posicionar en el centro de la bolsa
            float centerX = width / 2f * particleSpacing;
            float centerY = -height / 2f * particleSpacing;
            centerObj.transform.localPosition = new Vector3(centerX - (width / 2f * particleSpacing), centerY, 0f);

            storageCenter = centerObj.transform;
        }

        #endregion

        #region Visual Mesh

        private void CreateVisualMesh()
        {
            GameObject meshObj = new GameObject("BagMesh");
            meshObj.transform.parent = transform;
            meshObj.transform.localPosition = Vector3.zero;

            meshFilter = meshObj.AddComponent<MeshFilter>();
            meshRenderer = meshObj.AddComponent<MeshRenderer>();

            bagMesh = new Mesh();
            bagMesh.name = "BagMesh";
            meshFilter.mesh = bagMesh;

            if (bagMaterial != null)
            {
                meshRenderer.material = bagMaterial;
            }
            else
            {
                meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            meshRenderer.material.color = bagColor;
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = -1; // Detrás de tesoros
        }

        private void UpdateVisualMesh()
        {
            if (bagMesh == null || particles == null) return;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // Generar vertices desde las partículas
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 worldPos = particles[x, y].gameObject.transform.position;
                    Vector3 localPos = transform.InverseTransformPoint(worldPos);
                    vertices.Add(localPos);

                    // UVs
                    uvs.Add(new Vector2((float)x / (width - 1), 1f - (float)y / (height - 1)));
                }
            }

            // Generar triángulos
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int topLeft = y * width + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = (y + 1) * width + x;
                    int bottomRight = bottomLeft + 1;

                    // Primer triángulo
                    triangles.Add(topLeft);
                    triangles.Add(bottomLeft);
                    triangles.Add(topRight);

                    // Segundo triángulo
                    triangles.Add(topRight);
                    triangles.Add(bottomLeft);
                    triangles.Add(bottomRight);
                }
            }

            bagMesh.Clear();
            bagMesh.vertices = vertices.ToArray();
            bagMesh.triangles = triangles.ToArray();
            bagMesh.uv = uvs.ToArray();
            bagMesh.RecalculateNormals();
            bagMesh.RecalculateBounds();
        }

        #endregion

        #region Anchored Particles Update

        private void UpdateAnchoredParticles()
        {
            if (diverTransform == null) return;

            // Actualizar posición de partículas ancladas
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    BagParticle particle = particles[x, y];

                    if (particle.isAnchored)
                    {
                        Vector2 targetPos = (Vector2)diverTransform.position +
                                          new Vector2(particle.localOffset.x, -anchorDistance + particle.localOffset.y);
                        particle.gameObject.transform.position = targetPos;
                    }
                }
            }
        }

        #endregion

        #region Storage System

        public bool CanStore()
        {
            return storedTreasures.Count < maxCapacity;
        }

        public void StoreTreasure(LootObject loot)
        {
            if (!CanStore())
            {
                if (showDebug)
                {
                    Debug.Log("[Bag] ¡Bolsa llena! " + storedTreasures.Count + "/" + maxCapacity);
                }
                return;
            }

            // Teleportar a posición aleatoria dentro de la bolsa
            Vector2 randomOffset = Random.insideUnitCircle * (storageRadius * 0.5f);
            Vector3 targetPos = storageCenter.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            loot.transform.position = targetPos;

            // Configurar físicas internas
            Rigidbody2D lootRb = loot.GetRigidbody();
            if (lootRb != null)
            {
                lootRb.linearVelocity = Vector2.zero;
                lootRb.angularVelocity = 0f;
                lootRb.linearDamping = internalDrag;
                lootRb.gravityScale = internalGravity;
            }

            // Añadir a lista
            storedTreasures.Add(loot);

            // Marcar como no agarrado
            loot.SetGrabbed(false);

            if (showDebug)
            {
                Debug.Log("[Bag] Almacenado: " + loot.GetItemData().itemName +
                         " (" + storedTreasures.Count + "/" + maxCapacity + ")");
            }
        }

        public List<LootObject> EmptyBag()
        {
            List<LootObject> treasures = new List<LootObject>(storedTreasures);

            // Restaurar físicas normales
            foreach (LootObject loot in storedTreasures)
            {
                Rigidbody2D lootRb = loot.GetRigidbody();
                if (lootRb != null)
                {
                    lootRb.linearDamping = 2f;
                    lootRb.gravityScale = 0f;
                }
            }

            storedTreasures.Clear();

            if (showDebug)
            {
                Debug.Log("[Bag] Bolsa vaciada: " + treasures.Count + " tesoros");
            }

            return treasures;
        }

        public Vector3 GetStoragePosition()
        {
            return storageCenter != null ? storageCenter.position : transform.position;
        }

        public float GetStorageRadius()
        {
            return storageRadius;
        }

        #endregion

        #region Public API

        public int GetStoredCount()
        {
            return storedTreasures.Count;
        }

        public bool IsFull()
        {
            return storedTreasures.Count >= maxCapacity;
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            // Área de almacenamiento
            if (storageCenter != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.DrawWireSphere(storageCenter.position, storageRadius);
            }

            // Partículas
            if (showParticles && particles != null)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (particles[x, y] != null && particles[x, y].gameObject != null)
                        {
                            Vector3 pos = particles[x, y].gameObject.transform.position;

                            if (particles[x, y].isAnchored)
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawSphere(pos, 0.1f);
                            }
                            else
                            {
                                Gizmos.color = Color.yellow;
                                Gizmos.DrawWireSphere(pos, 0.05f);
                            }
                        }
                    }
                }
            }

            // Springs
            if (showSprings && particles != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width - 1; x++)
                    {
                        if (particles[x, y] != null && particles[x + 1, y] != null)
                        {
                            Gizmos.DrawLine(
                                particles[x, y].gameObject.transform.position,
                                particles[x + 1, y].gameObject.transform.position
                            );
                        }
                    }
                }

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height - 1; y++)
                    {
                        if (particles[x, y] != null && particles[x, y + 1] != null)
                        {
                            Gizmos.DrawLine(
                                particles[x, y].gameObject.transform.position,
                                particles[x, y + 1].gameObject.transform.position
                            );
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebug) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 12;
            style.normal.textColor = Color.cyan;

            GUI.Label(new Rect(10, 690, 300, 20),
                     "[Bag] Tesoros: " + storedTreasures.Count + "/" + maxCapacity, style);
        }

        #endregion
    }
}