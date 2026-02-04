using UnityEngine;
using AbyssalReach.Core;

public class InputTester : MonoBehaviour
{
    private AbyssalReachControls controls;

    private void Awake()
    {
        controls = new AbyssalReachControls();
    }

    private void OnEnable()
    {
        controls.Enable();

        // Test barco
        controls.BoatControls.Movement.performed += ctx =>
        {
            float value = ctx.ReadValue<float>();
            Debug.Log("Boat Movement: {"+value+"}");
        };

        // Test buceador
        controls.DiverControls.Move.performed += ctx =>
        {
            Vector2 value = ctx.ReadValue<Vector2>();
            Debug.Log("Diver Move: {"+value+"}");
        };
    }

    private void OnDisable()
    {
        controls.Disable();
    }
}