using UnityEngine;
using AbyssalReach.Core;

public class GameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject boat;
    [SerializeField] private GameObject diver;
    [SerializeField] private GameObject tetherSystem;

    private AbyssalReachControls controls;
    private bool isDiving = false;

    private void Awake()
    {
        controls = new AbyssalReachControls();
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.BoatControls.StartDive.performed += _ => ToggleDiving(); // Esto es como una llamada a la función ToggleDiving cada vez que se presiona el botón de bucear. Si ya estás buceando, te saca a navegar, y si estás navegando, te mete a bucear. Me lo dijo Serg
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        // Empezar en modo navegación
        SetSailingMode();
    }

    private void ToggleDiving()
    {
        if (isDiving)
        {
            SetSailingMode();
        }
        else
        {
            SetDivingMode();
        }
    }

    private void SetSailingMode()
    {
        isDiving = false;

        boat.SetActive(true);
        diver.SetActive(false);
        tetherSystem.SetActive(false);

        // Activar controles del barco
        controls.BoatControls.Enable();
        controls.DiverControls.Disable();

        Debug.Log("[GameController] Saling Mode");
    }

    private void SetDivingMode()
    {
        isDiving = true;

        // Posicionar buceador bajo el barco
        Vector3 boatPos = boat.transform.position;
        diver.transform.position = new Vector3(boatPos.x, boatPos.y - 2f, 0f);

        boat.SetActive(true);
        diver.SetActive(true);
        tetherSystem.SetActive(true);

        // Activar controles del buceador
        controls.BoatControls.Disable();
        controls.DiverControls.Enable();

        Debug.Log("[GameController] Diving Mode");
    }
}