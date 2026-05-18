using UnityEngine;

public class UnderwaterEffects : MonoBehaviour
{
    [SerializeField] private GameObject WaterArriba;
    [SerializeField] private GameObject WaterAbajo;

    private void OnTriggerEnter(Collider other)
    {
        RenderSettings.fog = true;
        WaterArriba.SetActive(false);
        WaterAbajo.SetActive(true);
        Debug.Log("Debería activarse la fog");
    }

    private void OnTriggerExit(Collider other)
    {
        RenderSettings.fog = false;
        WaterArriba.SetActive(true);
        WaterAbajo.SetActive(false);
        Debug.Log("Debería desactivarse la fog");
    }
}
