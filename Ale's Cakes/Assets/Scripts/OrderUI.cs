using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderUI : MonoBehaviour
{
    [SerializeField] CakeBuilder cake;
    [SerializeField] GameObject orderPanel;
    [SerializeField] TMP_Text sizeText;
    [SerializeField] Transform iconsContainer;
    [SerializeField] Image iconPrefab;
    [SerializeField] Image timerFill;      // Image de tipo Filled
    [SerializeField] TMP_Text resultText;
    [SerializeField] GameObject timerBar;   // el objeto TimerBack

    OrderData shownOrder;

    void Update()
    {
        CustomerManager cm = CustomerManager.Instance;
        bool show = cm.OrderActive || cm.ShowingResult;

        orderPanel.SetActive(show);
        if (!show)
        {
            shownOrder = null;
            return;
        }

        timerBar.SetActive(GameSettings.HasTimer);

        if (cm.CurrentOrder != shownOrder) Build(cm.CurrentOrder);

        timerFill.fillAmount = cm.TimeTotal > 0f ? Mathf.Clamp01(cm.TimeLeft / cm.TimeTotal) : 0f;

        if (cm.ShowingResult)
            resultText.text = cm.LastStars <= 0
                ? "¡Se acabó el tiempo!"
                : "Estrellas: " + cm.LastStars + "  (" + cm.LastScore + " pts)";
        else
            resultText.text = "";
    }

    void Build(OrderData order)
    {
        shownOrder = order;
        if (order == null) return;

        CakeLayerSet set = cake.GetSet(order.size);
        sizeText.text = "Pastel " + set.name;

        foreach (Transform child in iconsContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        // Capas de abajo hacia arriba, y el topping al final
        foreach (int pieceIndex in order.pieces)
            AddIcon(set.pieces[pieceIndex].icon);

        if (order.topping >= 0)
            AddIcon(set.toppings[order.topping].icon);
    }

    void AddIcon(Sprite sprite)
    {
        Image img = Instantiate(iconPrefab, iconsContainer);
        img.sprite = sprite;
    }
}