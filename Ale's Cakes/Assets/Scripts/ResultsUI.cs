using UnityEngine;
using TMPro;

public class ResultsUI : MonoBehaviour
{
    [SerializeField] GameObject endPanel;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text detailsText;
    [SerializeField] TMP_Text hudText;      // opcional: progreso durante la partida

    bool filled;

    void Update()
    {
        CustomerManager cm = CustomerManager.Instance;

        endPanel.SetActive(cm.GameOver);

        // Texto de progreso
        if (hudText != null)
        {
            if (!GameSettings.EndsAfterOrders)
                hudText.text = "Modo libre";
            else
                hudText.text = "Pedido " + Mathf.Min(cm.OrdersStarted, cm.OrdersTotal) + "/" + cm.OrdersTotal
                             + "   Entregados: " + cm.Delivered
                             + "   Puntos: " + cm.TotalScore;
        }

        // Pantalla final (se llena una sola vez)
        if (!cm.GameOver || filled) return;
        filled = true;

        titleText.text = cm.Victory ? "¡Victoria!" : "Derrota";
        detailsText.text =
            "Pasteles entregados: " + cm.Delivered + "/" + cm.OrdersTotal + "\n" +
            "Estrellas: " + cm.TotalStars + "/" + (cm.OrdersTotal * 3) + "\n" +
            "Puntuación: " + cm.TotalScore + "/" + (cm.OrdersTotal * 100);
    }
}