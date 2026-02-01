using UnityEngine;

public class Laser : MonoBehaviour
{
    [Header("Ayarlar")]
    public Transform respawnPoint; // Yanınca gidilecek yer

    void Start()
    {
        SetupLasersAutomatic();
    }

    void SetupLasersAutomatic()
    {
 
        Collider[] allColliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in allColliders)
        {
            
            if (col.gameObject == this.gameObject) continue;

          
            col.isTrigger = true;


            if (col.gameObject.GetComponent<LaserPart>() == null)
            {
                LaserPart part = col.gameObject.AddComponent<LaserPart>();
                part.manager = this; 
            }
        }
    }

   
    public void OnPlayerHit(GameObject player)
    {
        var maskScript = player.GetComponent<PlayerMaskController>();

        if (maskScript != null)
        {
            
            if (maskScript.currentMask == PlayerMaskController.MaskType.Thief)
            {
                Debug.Log("💀 CIZZZ! Lazer Izgarasına yakalandın.");
                TeleportPlayer(player);
            }
            else
            {
                Debug.Log("🛡️ Güvenli Geçiş.");
            }
        }
    }

    void TeleportPlayer(GameObject player)
    {
        var controller = player.GetComponent<CharacterController>();
        if (controller) controller.enabled = false;

        if (respawnPoint != null)
        {
            player.transform.position = respawnPoint.position;
            player.transform.rotation = respawnPoint.rotation;
        }

        if (controller) controller.enabled = true;
    }
}
public class LaserPart : MonoBehaviour
{
    [HideInInspector] public Laser manager; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            manager.OnPlayerHit(other.gameObject);
        }
    }
}