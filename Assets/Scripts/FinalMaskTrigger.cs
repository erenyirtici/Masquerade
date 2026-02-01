using UnityEngine;

public class FinalMaskTrigger : MonoBehaviour
{
    private bool _collected = false;

    private void OnTriggerEnter(Collider other)
    {
        // Sadece "Player" etiketi olan biri çarparsa ve daha önce alýnmadýysa
        if (other.CompareTag("Player") && !_collected)
        {
            _collected = true;

            // Maske alýndýðý için görselini kapatalým
            gameObject.SetActive(false);

            // Manager'a haber ver
            GameLevelManager manager = FindObjectOfType<GameLevelManager>();
            if (manager != null)
            {
                manager.WinGame(); // Birazdan bu fonksiyonu yazacaðýz
            }
        }
    }
}