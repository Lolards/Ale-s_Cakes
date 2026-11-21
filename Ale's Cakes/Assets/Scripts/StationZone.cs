using UnityEngine;

public enum StationType { Layers, Colors, Topping, Deliver }

[RequireComponent(typeof(BoxCollider))]
public class StationZone : MonoBehaviour
{
    public StationType type;
    public Transform platePoint; // a dónde va el plato cuando el personaje entra aquí
    public bool teleportPlate; // El plato aparece directo, sin deslizarse

    void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) StationController.Instance.EnterZone(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) StationController.Instance.ExitZone(this);
    }
}