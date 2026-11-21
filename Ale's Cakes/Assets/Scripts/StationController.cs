using UnityEngine;

public class StationController : MonoBehaviour
{
    public static StationController Instance { get; private set; }

    [SerializeField] CakeBuilder cake;
    [SerializeField] Transform plate;      // el plato que viaja por la plataforma
    [SerializeField] Transform plateHome;  // punto de inicio (normalmente PlatePoint1)
    [SerializeField] float plateSpeed = 3f;

    StationZone currentZone;
    Transform plateTarget;
    bool plateAtTeleportSpot;

    bool PlateArrived => plateTarget == null || plate.position == plateTarget.position;

    void Awake() { Instance = this; }

    void Start()
    {
        plateTarget = plateHome;
        plate.position = plateHome.position;
    }

    public void EnterZone(StationZone zone)
    {
        currentZone = zone;
        if (zone.platePoint == null) return;

        plateTarget = zone.platePoint;

        if (zone.teleportPlate || plateAtTeleportSpot)
            plate.position = zone.platePoint.position;

        plateAtTeleportSpot = zone.teleportPlate;
    }

    public void ExitZone(StationZone zone)
    {
        if (currentZone == zone) currentZone = null;
    }

    void Update()
    {
        if (plateTarget != null)
            plate.position = Vector3.MoveTowards(plate.position, plateTarget.position, plateSpeed * Time.deltaTime);
    }

    // La interfaz pregunta qué estación está activa (solo si el plato ya llegó)
    public bool TryGetActiveStation(out StationType type)
    {
        type = StationType.Layers;
        if (currentZone == null || !PlateArrived) return false;
        type = currentZone.type;
        return true;
    }

    // --- Acciones que llaman los botones ---

    public void AddLayer(int size) { cake.AddLayer(size); }

    public void RemoveLastLayer() { cake.RemoveLastLayer(); }

    public void SetPiece(int index) { cake.SetLastLayerPiece(index); }

    public void SetTopping(int index) { cake.SetTopping(index); }

    public void Deliver()
    {
        if (cake.LayerCount == 0) return;

        Debug.Log("Pastel entregado. Aquí irá la calificación.");
        cake.ResetCake();
        plateTarget = plateHome;
        plate.position = plateHome.position;
        plateAtTeleportSpot = false;
    }
}