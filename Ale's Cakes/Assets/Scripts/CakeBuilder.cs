using System.Collections.Generic;
using UnityEngine;

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
}

public class CakeBuilder : MonoBehaviour
{
    [SerializeField] Transform cakeRoot;
    [SerializeField] CakeLayerSet[] layerSets;   // 0 = grande, 1 = chico
    [SerializeField] int maxLayers = 3;

    readonly List<LayerInstance> layers = new List<LayerInstance>();
    GameObject currentTopping;
    int toppingIndex = -1;

    public int LayerCount => layers.Count;
    public IReadOnlyList<LayerInstance> Layers => layers;
    public int ToppingIndex => toppingIndex;
    public int MaxLayers => maxLayers;
    public bool IsFull => layers.Count >= maxLayers;

    // Tamaño de la primera capa; -1 si el pastel está vacío
    public int LockedSize => layers.Count > 0 ? layers[0].size : -1;

    public CakeLayerSet GetSet(int sizeIndex) => layerSets[sizeIndex];

    // --- Capas ---

    public bool AddLayer(int sizeIndex, int pieceIndex = -1)
    {
        if (layers.Count >= maxLayers) return false;
        if (layers.Count > 0 && sizeIndex != layers[0].size) return false; // no mezclar tamaños

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

        float topY = GetTopY();
        float bottomY = GetBounds(piece).min.y;
        piece.transform.position += Vector3.up * (topY - bottomY);

        layers.Add(new LayerInstance
        {
            root = piece,
            size = sizeIndex,
            type = type,
            pieceIndex = pieceIndex
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
        if (layers.Count == 0) return;
        int size = layers[layers.Count - 1].size;
        RemoveLastLayer();
        AddLayer(size, pieceIndex);
    }

    // --- Topping (estación 3) ---

    public void SetTopping(int index)
    {
        if (index == toppingIndex) index = -1; // clic en el mismo = quitarlo

        if (currentTopping != null) Destroy(currentTopping);
        currentTopping = null;
        toppingIndex = -1;

        if (index < 0 || layers.Count == 0) return;

        ToppingOption[] list = layerSets[LockedSize].toppings;
        if (index >= list.Length || list[index].prefab == null) return;
        toppingIndex = index;

        currentTopping = Instantiate(list[index].prefab, cakeRoot);

        Material mat = list[index].material;
        if (mat != null)
            foreach (Renderer r in currentTopping.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;
        
        Bounds cake = GetBounds(layers[layers.Count - 1].root);
        Bounds top = GetBounds(currentTopping);

        Vector3 pos = currentTopping.transform.position;
        pos.x = cake.center.x;
        pos.z = cake.center.z;
        pos.y += cake.max.y - top.min.y;
        currentTopping.transform.position = pos;
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
        return GetBounds(layers[layers.Count - 1].root).max.y;
    }

    static Bounds GetBounds(GameObject go)
    {
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }
}