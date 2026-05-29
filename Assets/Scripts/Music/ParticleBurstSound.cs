using UnityEngine;
using UnityEngine.ParticleSystemJobs;
using AbyssalReach.Core;

public class ParticleBurstSound : MonoBehaviour
{
    [SerializeField] private string soundName;

    private ParticleSystem ps;
    private ParticleSystem.Burst[] bursts;

    private int nextBurstIndex;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        var emission = ps.emission;

        bursts = new ParticleSystem.Burst[emission.burstCount];
        emission.GetBursts(bursts);
    }

    void OnEnable()
    {
        nextBurstIndex = 0;
    }

    void Update()
    {
        if (!ps.isPlaying)
            return;

        float time = ps.time;

        if (nextBurstIndex < bursts.Length &&
            time >= bursts[nextBurstIndex].time)
        {
            AudioManager.Instance.PlaySFX("Breathe");

            nextBurstIndex++;
        }
    }
}