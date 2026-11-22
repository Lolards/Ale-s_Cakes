using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public enum LayerType { Down, Up }

[System.Serializable]
public class PieceOption
{
    public string name;
    public Sprite icon;
    public LayerType type;      // Down o Up
    public GameObject prefab;   // opcional: si queda vacío usa la pieza base del tamaño
    public Material material;   // opcional: si queda vacío conserva el material del prefab
}

[System.Serializable]
public class ToppingOption
{
    public string name;
    public Sprite icon;
    public GameObject prefab;
    public Material material;   // opcional: si queda vacío conserva el material del prefab
}

[System.Serializable]
public class CakeLayerSet
{
    public string name;                 // "Grande", "Chico"
    public GameObject bottom;           // pieza base Down
    public GameObject top;              // pieza base Up
    public PieceOption[] pieces;        // las 19 piezas
    public ToppingOption[] toppings;    // los 12 toppings
}

public class LayerInstance
{
    public GameObject root;
    public int size;
    public LayerType type;
    public int pieceIndex = -1;         // pieza elegida (-1 = pieza base)
    public LayerDrop drop;              // solo si la capa cayó del dispensador
}

public class CakeBuilder : MonoBehaviour
{
    [SerializeField] Transform cakeRoot;
    [SerializeField] CakeLayerSet[] layerSets;   // 0 = grande, 1 = chico
    [SerializeField] int maxLayers = 3;

    [Header("Dispensador")]
    [SerializeField] Transform dropPoint;        // salida del dispensador
    [SerializeField] float dropGravity = 20f;    // más alto = cae más rápido
    public UnityEvent onLayerDropStarted;        // por ejemplo, animación o sonido del dispensador
    public UnityEvent onLayerLanded;             // por ejemplo, sonido al caer

    [Header("Efectos")]
    [SerializeField] ParticleSystem pieceFx;    // estación 2
    [SerializeField] ParticleSystem toppingFx;  // estación 3
    [SerializeField] ParticleSystem dispenserFx; // ráfaga en la salida al empezar a caer
    [SerializeField] ParticleSystem trailFx;     // estela que acompaña a la capa mientras cae
    [SerializeField] ParticleSystem landFx;      // ráfaga al aterrizar sobre el pastel

    [Header("Estaciones 2 y 3: efecto que viaja al pastel")]
    [SerializeField] Transform pieceSource;          // salida de la estación 2
    [SerializeField] Transform toppingSource;        // salida de la estación 3
    [SerializeField] ParticleSystem pieceTravelFx;   // estela que viaja a la capa
    [SerializeField] ParticleSystem toppingTravelFx; // estela que viaja a la parte de arriba
    [SerializeField] float travelTime = 0.4f;
    [SerializeField] float arcHeight = 0.3f;

    bool fxBusy; // true mientras viaja una estela

    readonly List<LayerInstance> layers = new List<LayerInstance>();
    GameObject currentTopping;
    int toppingIndex = -1;

    public int LayerCount => layers.Count;
    public int SizeCount => layerSets.Length;
    public IReadOnlyList<LayerInstance> Layers => layers;
    public int ToppingIndex => toppingIndex;
    public int MaxLayers => maxLayers;
    public bool IsFull => layers.Count >= maxLayers;

    // Tamaño de la primera capa; -1 si el pastel está vacío
    public int LockedSize => layers.Count > 0 ? layers[0].size : -1;

    // True mientras la última capa está cayendo
    public bool IsBusy
    {
        get
        {
            if (layers.Count == 0) return false;
            LayerDrop d = layers[layers.Count - 1].drop;
            return d != null && d.IsFalling;
        }
    }

    public CakeLayerSet GetSet(int sizeIndex) => layerSets[sizeIndex];

    // --- Capas ---

    public bool AddLayer(int sizeIndex, int pieceIndex = -1, bool drop = false)
    {
        if (layers.Count >= maxLayers) return false;
        if (layers.Count > 0 && sizeIndex != layers[0].size) return false; // no mezclar tamaños
        if (IsBusy) return false;                                          // espera a que caiga la anterior

        ClearTopping(); // el topping va siempre al final

        CakeLayerSet set = layerSets[sizeIndex];
        LayerType type = LayerType.Down;
        GameObject prefab = set.bottom;
        Material material = null;

        if (pieceIndex >= 0 && pieceIndex < set.pieces.Length)
        {
            PieceOption option = set.pieces[pieceIndex];
            type = option.type;
            prefab = option.prefab != null
                ? option.prefab
                : (type == LayerType.Down ? set.bottom : set.top);
            material = option.material;
        }
        else
        {
            pieceIndex = -1;
        }

        GameObject piece = Instantiate(prefab, cakeRoot);
        piece.transform.localPosition = Vector3.zero;
        piece.transform.localRotation = Quaternion.identity;

        if (material != null)
            foreach (Renderer r in piece.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = material;

        // Posición final sobre la capa anterior
        float topY = GetTopY();
        float bottomY = GetBounds(piece).min.y;
        piece.transform.position += Vector3.up * (topY - bottomY);

        // Caída desde el dispensador
        LayerDrop dropper = null;
        if (drop && dropPoint != null)
        {
            Vector3 targetLocal = piece.transform.localPosition;
            Vector3 startLocal = cakeRoot.InverseTransformPoint(dropPoint.position);
            startLocal.x = targetLocal.x;
            startLocal.z = targetLocal.z;

            if (startLocal.y > targetLocal.y)
            {
                dropper = piece.AddComponent<LayerDrop>();
                dropper.Begin(startLocal, targetLocal, dropGravity, trailFx, () =>
                {
                    onLayerLanded.Invoke();
                    Bounds landed = GetBounds(piece);
                    PlayFx(landFx, new Vector3(landed.center.x, landed.max.y, landed.center.z));
                });
                onLayerDropStarted.Invoke();
                PlayFx(dispenserFx, dropPoint.position);
            }
        }

        layers.Add(new LayerInstance
        {
            root = piece,
            size = sizeIndex,
            type = type,
            pieceIndex = pieceIndex,
            drop = dropper
        });
        return true;
    }

    public void RemoveLastLayer()
    {
        if (layers.Count == 0) return;
        ClearTopping();
        Destroy(layers[layers.Count - 1].root);
        layers.RemoveAt(layers.Count - 1);
    }

    public void ResetCake()
    {
        foreach (LayerInstance l in layers) Destroy(l.root);
        layers.Clear();
        ClearTopping();
    }

    // Estación 2: reemplaza la última capa por la pieza elegida (del mismo tamaño)
    public void SetLastLayerPiece(int pieceIndex)
    {
        if (layers.Count == 0 || fxBusy) return;
        StartCoroutine(PieceRoutine(pieceIndex));
    }

    IEnumerator PieceRoutine(int pieceIndex)
    {
        fxBusy = true;
        LayerInstance target = layers[layers.Count - 1];

        if (pieceSource != null && pieceTravelFx != null)
        {
            Vector3 end = GetFinalBounds(target).center;
            yield return StartCoroutine(Travel(pieceTravelFx, pieceSource.position, end));
        }

        // el pastel pudo cambiar mientras viajaba la estela
        if (layers.Count > 0 && layers[layers.Count - 1] == target)
        {
            int size = target.size;
            RemoveLastLayer();
            if (AddLayer(size, pieceIndex))
                PlayFx(pieceFx, GetBounds(layers[layers.Count - 1].root).center);
        }

        fxBusy = false;
    }

    // --- Topping (estación 3) ---

    public void SetTopping(int index)
    {
        if (fxBusy) return;

        if (index == toppingIndex) index = -1; // clic en el mismo = quitarlo

        if (index < 0)
        {
            ClearTopping();
            return;
        }

        if (layers.Count == 0) return;
        StartCoroutine(ToppingRoutine(index));
    }

    IEnumerator ToppingRoutine(int index)
    {
        fxBusy = true;
        LayerInstance topLayer = layers[layers.Count - 1];

        if (toppingSource != null && toppingTravelFx != null)
        {
            Bounds b = GetFinalBounds(topLayer);
            Vector3 end = new Vector3(b.center.x, b.max.y, b.center.z);
            yield return StartCoroutine(Travel(toppingTravelFx, toppingSource.position, end));
        }

        // el pastel pudo cambiar mientras viajaba la estela
        if (layers.Count > 0 && layers[layers.Count - 1] == topLayer)
            PlaceTopping(index);

        fxBusy = false;
    }

    void PlaceTopping(int index)
    {
        ClearTopping();

        ToppingOption[] list = layerSets[LockedSize].toppings;
        if (index >= list.Length || list[index].prefab == null) return;
        toppingIndex = index;

        currentTopping = Instantiate(list[index].prefab, cakeRoot);

        Material mat = list[index].material;
        if (mat != null)
            foreach (Renderer r in currentTopping.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;

        Bounds cake = GetFinalBounds(layers[layers.Count - 1]);
        Bounds top = GetBounds(currentTopping);

        Vector3 pos = currentTopping.transform.position;
        pos.x = cake.center.x;
        pos.z = cake.center.z;
        pos.y += cake.max.y - top.min.y;
        currentTopping.transform.position = pos;

        PlayFx(toppingFx, new Vector3(cake.center.x, cake.max.y + top.size.y * 0.5f, cake.center.z));
    }

    void ClearTopping()
    {
        if (currentTopping != null) Destroy(currentTopping);
        currentTopping = null;
        toppingIndex = -1;
    }

    // --- Utilidades ---

    float GetTopY()
    {
        if (layers.Count == 0) return cakeRoot.position.y;
        return GetFinalBounds(layers[layers.Count - 1]).max.y;
    }

    // Límites de la capa en su posición final (aunque todavía esté cayendo)
    Bounds GetFinalBounds(LayerInstance l)
    {
        Bounds b = GetBounds(l.root);
        if (l.drop != null && l.drop.IsFalling)
        {
            Vector3 delta = l.drop.TargetLocalPos - l.root.transform.localPosition;
            b.center += cakeRoot.TransformVector(delta);
        }
        return b;
    }

    static Bounds GetBounds(GameObject go)
    {
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }

    void PlayFx(ParticleSystem fx, Vector3 worldPos)
    {
        if (fx == null) return;
        fx.transform.position = worldPos;
        fx.Clear();
        fx.Play();
    }

    // Mueve una estela en arco desde "from" hasta "to"
    IEnumerator Travel(ParticleSystem fx, Vector3 from, Vector3 to)
    {
        fx.transform.position = from;
        fx.Clear();
        fx.Play();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / travelTime;
            float k = Mathf.Clamp01(t);
            Vector3 p = Vector3.Lerp(from, to, k);
            p.y += Mathf.Sin(k * Mathf.PI) * arcHeight;
            fx.transform.position = p;
            yield return null;
        }

        fx.transform.position = to;
        fx.Stop();
    }
}