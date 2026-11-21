using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StationUI : MonoBehaviour
{
    [SerializeField] CakeBuilder cake;

    [Header("Paneles")]
    [SerializeField] GameObject layersPanel;
    [SerializeField] GameObject piecesPanel;
    [SerializeField] GameObject toppingPanel;
    [SerializeField] GameObject deliverPanel;

    [Header("Estación de capas")]
    [SerializeField] Button bigButton;
    [SerializeField] Button smallButton;
    [SerializeField] Button removeButton;
    [SerializeField] TMP_Text layersInfo;

    [Header("Botones generados")]
    [SerializeField] Button optionButtonPrefab;  // botón con un hijo Image llamado "Icon"
    [SerializeField] Transform piecesContainer;  // dentro de PiecesPanel
    [SerializeField] Transform toppingsContainer; // dentro de ToppingPanel

    int builtPiecesSize = -2;
    int builtToppingsSize = -2;

    void Update()
    {
        StationType type;
        bool active = StationController.Instance.TryGetActiveStation(out type);

        layersPanel.SetActive(active && type == StationType.Layers);
        piecesPanel.SetActive(active && type == StationType.Colors);
        toppingPanel.SetActive(active && type == StationType.Topping);
        deliverPanel.SetActive(active && type == StationType.Deliver);

        if (!active) return;

        if (type == StationType.Layers) RefreshLayers();
        if (type == StationType.Colors) BuildPieces();
        if (type == StationType.Topping) BuildToppings();
    }

    void RefreshLayers()
    {
        int locked = cake.LockedSize; // -1 = pastel vacío
        bool full = cake.IsFull;

        bigButton.interactable = !full && (locked == -1 || locked == 0);
        smallButton.interactable = !full && (locked == -1 || locked == 1);
        removeButton.interactable = cake.LayerCount > 0;

        if (layersInfo != null)
            layersInfo.text = "Capas: " + cake.LayerCount + "/" + cake.MaxLayers;
    }

    void BuildPieces()
    {
        int size = cake.LockedSize;
        if (size == builtPiecesSize) return; // ya están hechos para este tamaño
        builtPiecesSize = size;

        Clear(piecesContainer);
        if (size < 0) return;

        PieceOption[] pieces = cake.GetSet(size).pieces;
        for (int i = 0; i < pieces.Length; i++)
        {
            int index = i; // copia, para que cada botón recuerde su número
            Button b = CreateButton(piecesContainer, pieces[i].icon);
            b.onClick.AddListener(() => StationController.Instance.SetPiece(index));
        }
    }

    void BuildToppings()
    {
        int size = cake.LockedSize;
        if (size == builtToppingsSize) return;
        builtToppingsSize = size;

        Clear(toppingsContainer);
        if (size < 0) return;

        ToppingOption[] toppings = cake.GetSet(size).toppings;
        for (int i = 0; i < toppings.Length; i++)
        {
            int index = i;
            Button b = CreateButton(toppingsContainer, toppings[i].icon);
            b.onClick.AddListener(() => StationController.Instance.SetTopping(index));
        }
    }

    Button CreateButton(Transform parent, Sprite icon)
    {
        Button b = Instantiate(optionButtonPrefab, parent);
        Transform iconT = b.transform.Find("Icon");
        Image img = iconT != null ? iconT.GetComponent<Image>() : b.image;
        img.sprite = icon;
        return b;
    }

    static void Clear(Transform container)
    {
        foreach (Transform child in container)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}