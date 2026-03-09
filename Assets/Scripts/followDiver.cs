using UnityEngine;

public class followDiver : MonoBehaviour
{
    [SerializeField] private Transform diver;

    // Update is called once per frame
    void Update()
    {
        this.transform.position = diver.position;
    }
}
