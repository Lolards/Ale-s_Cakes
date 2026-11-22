using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// El pedido de un cliente
public class OrderData
{
    public int size;                            // 0 = grande, 1 = chico
    public List<int> pieces = new List<int>();  // pieza de cada capa, de abajo hacia arriba
    public int topping = -1;                    // -1 = sin topping
}

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [SerializeField] CakeBuilder cake;

    [Header("Clientes")]
    [SerializeField] GameObject[] customerPrefabs;
    [SerializeField] Transform spawnPoint;
    [SerializeField] Transform counterPoint;
    [SerializeField] Transform exitPoint;
    [SerializeField] float delayBetweenCustomers = 2f;
    [SerializeField] float resultTime = 2.5f;

    [Header("Pedidos")]
    [SerializeField] int minLayers = 1;
    [SerializeField] int maxLayers = 3;
    [SerializeField, Range(0f, 1f)] float toppingChance = 0.7f;
    [SerializeField] bool upOnlyOnTop = true;   // las piezas Up solo en la última capa

    [Header("Tiempo")]
    [SerializeField] float baseTime = 30f;
    [SerializeField] float timePerLayer = 20f;

    public OrderData CurrentOrder { get; private set; }
    public bool OrderActive { get; private set; }      // cliente esperando en el mostrador
    public bool ShowingResult { get; private set; }
    public float TimeLeft { get; private set; }
    public float TimeTotal { get; private set; }
    public int LastStars { get; private set; }         // 0 = se acabó el tiempo
    public int LastScore { get; private set; }

    [Header("Partida")]
    [SerializeField] int ordersPerGame = 5;

    public int OrdersTotal => ordersPerGame;
    public int OrdersStarted { get; private set; }
    public int Delivered { get; private set; }
    public int TotalScore { get; private set; }
    public int TotalStars { get; private set; }
    public bool GameOver { get; private set; }
    public bool Victory { get; private set; }

    void Awake() { Instance = this; }

    void Start()
    {
        if (customerPrefabs.Length == 0)
        {
            Debug.LogWarning("CustomerManager: no hay prefabs de clientes.");
            return;
        }
        StartCoroutine(CustomerLoop());
    }

    IEnumerator CustomerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(delayBetweenCustomers);

            // 1. Llega el cliente
            GameObject prefab = customerPrefabs[Random.Range(0, customerPrefabs.Length)];
            GameObject customer = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

            CustomerWalker walker = customer.GetComponent<CustomerWalker>();
            if (walker == null) walker = customer.AddComponent<CustomerWalker>();

            bool arrived = false;
            walker.WalkTo(counterPoint.position, () => arrived = true);
            while (!arrived) yield return null;
            customer.transform.rotation = counterPoint.rotation;

            // 2. Hace su pedido
            CurrentOrder = GenerateOrder();
            OrdersStarted++;
            LastStars = -1;

            if (GameSettings.HasTimer)
            {
                TimeTotal = (baseTime + timePerLayer * CurrentOrder.pieces.Count) * GameSettings.TimeMultiplier;
                TimeLeft = TimeTotal;
            }
            else
            {
                TimeTotal = 0f;   // modo libre: sin tiempo
                TimeLeft = 0f;
            }
            OrderActive = true;

            while (OrderActive && (!GameSettings.HasTimer || TimeLeft > 0f))
            {
                if (GameSettings.HasTimer) TimeLeft -= Time.deltaTime;
                yield return null;
            }

            // 3. Se acabó el tiempo sin entrega
            if (OrderActive)
            {
                OrderActive = false;
                LastStars = 0;
                LastScore = 0;
                StationController.Instance.ResetAll();
            }

            // 4. Muestra el resultado y se va
            ShowingResult = true;
            yield return new WaitForSeconds(resultTime);
            ShowingResult = false;
            CurrentOrder = null;

            bool left = false;
            walker.WalkTo(exitPoint.position, () => left = true);
            while (!left) yield return null;
            Destroy(customer);

            // 5. ¿Terminó la partida?
            if (GameSettings.EndsAfterOrders && OrdersStarted >= ordersPerGame)
            {
                Victory = Delivered >= GameSettings.RequiredCakes;
                GameOver = true;
                yield break;
            }
        }
    }

    public bool TryDeliver()
    {
        if (!OrderActive) return false;

        LastStars = Evaluate(CurrentOrder);
        Delivered++;
        TotalScore += LastScore;
        TotalStars += LastStars;
        OrderActive = false;
        return true;
    }

    // --- Pedidos ---

    OrderData GenerateOrder()
    {
        OrderData order = new OrderData();
        order.size = Random.Range(0, cake.SizeCount);
        CakeLayerSet set = cake.GetSet(order.size);

        int top = Mathf.Min(maxLayers, cake.MaxLayers);
        int layerCount = Random.Range(minLayers, top + 1);

        for (int i = 0; i < layerCount; i++)
        {
            bool isLast = i == layerCount - 1;
            bool allowUp = !upOnlyOnTop || isLast;
            order.pieces.Add(PickPiece(set, allowUp));
        }

        if (set.toppings.Length > 0 && Random.value < toppingChance)
            order.topping = Random.Range(0, set.toppings.Length);

        return order;
    }

    int PickPiece(CakeLayerSet set, bool allowUp)
    {
        List<int> valid = new List<int>();
        for (int i = 0; i < set.pieces.Length; i++)
            if (allowUp || set.pieces[i].type == LayerType.Down) valid.Add(i);

        if (valid.Count == 0) return 0;
        return valid[Random.Range(0, valid.Count)];
    }

    // --- Calificación ---

    int Evaluate(OrderData order)
    {
        int score = 0;

        if (cake.LockedSize == order.size) score += 20;
        if (cake.LayerCount == order.pieces.Count) score += 20;

        int correct = 0;
        for (int i = 0; i < order.pieces.Count; i++)
            if (i < cake.Layers.Count && cake.Layers[i].pieceIndex == order.pieces[i]) correct++;
        score += Mathf.RoundToInt(40f * correct / order.pieces.Count);

        if (cake.ToppingIndex == order.topping) score += 20;

        LastScore = score;
        return score >= 90 ? 3 : score >= 60 ? 2 : 1;
    }
}