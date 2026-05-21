using DG.Tweening;
using UnityEngine;

public class UnderwaterEffects : MonoBehaviour
{
    [SerializeField] private GameObject WaterArriba;
    [SerializeField] private GameObject WaterAbajo;
    [SerializeField] private ParticleSystem bubblesEffect;

    private void Start()
    {
        RenderSettings.fog = false;
        WaterArriba.SetActive(true);
        WaterAbajo.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        RenderSettings.fog = true;
        WaterArriba.SetActive(false);
        WaterAbajo.SetActive(true);
        bubblesEffect.Play();
        Debug.Log("Debería activarse la fog");
    }

    private void OnTriggerExit(Collider other)
    {
        RenderSettings.fog = false;
        WaterArriba.SetActive(true);
        WaterAbajo.SetActive(false);
        bubblesEffect.Stop();
        bubblesEffect.DORestart();
        Debug.Log("Debería desactivarse la fog");
    }
}
