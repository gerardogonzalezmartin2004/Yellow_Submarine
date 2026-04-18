using UnityEngine;

public class UnderwaterEffects : MonoBehaviour
{
    [SerializeField] private GameObject waterFx;

    private void OnTriggerEnter(Collider other)
    {
        //waterFx.SetActive(true);
        RenderSettings.fog = true;
        Debug.Log("Debería activarse la fog");
    }

    private void OnTriggerExit(Collider other)
    {
        //waterFx.SetActive(false);
        RenderSettings.fog = false;
        Debug.Log("Debería desactivarse la fog");
    }
}
