using AbyssalReach.Core;
using DG.Tweening;
using UnityEngine;

public class UnderwaterEffects : MonoBehaviour
{
    [SerializeField] private GameObject WaterArriba;
    [SerializeField] private GameObject WaterAbajo;
    [SerializeField] private ParticleSystem bubblesEffect;
    [SerializeField] private GameObject underWaterSource;


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
        underWaterSource.SetActive(true);
        bubblesEffect.Play();
        AudioManager.Instance.SetUnderwater(true);
        AudioManager.Instance.PlaySFX("Breathe");
        Debug.Log("Debería activarse la fog");
    }

    private void OnTriggerExit(Collider other)
    {
        RenderSettings.fog = false;
        WaterArriba.SetActive(true);
        WaterAbajo.SetActive(false);
        underWaterSource.SetActive(false);
        bubblesEffect.Stop();
        bubblesEffect.DORestart();
        AudioManager.Instance.PlaySFX("SalirAgua");
        AudioManager.Instance.SetUnderwater(false);
        Debug.Log("Debería desactivarse la fog");
    }
}
