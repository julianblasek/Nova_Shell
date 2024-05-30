using UnityEngine;
using System;

public class free_camera : MonoBehaviour
{
    public Transform target; // Zielobjekt, auf das die Kamera gerichtet sein soll
    private float speed = 5.1f; // Geschwindigkeit der Kamerabewegung, initial auf 5 gesetzt

    void Start()
    {
        // Setze die Hintergrundfarbe der Kamera auf dunkel
        Camera.main.backgroundColor = Color.black;
    }

    void Update()
    {
        
        
        if (Input.GetKeyDown(KeyCode.Q))
        {
            speed += 1f;
        }

        // Überprüfe, ob die "-" Taste gedrückt wurde und verringere die Geschwindigkeit
        if (Input.GetKeyDown(KeyCode.E)&&speed>1)
        {
            speed -= 1f;
        }

        // Berechne die Bewegungsrichtung basierend auf Tastatureingaben
        float horizontal = Input.GetAxis("Horizontal") * speed * Time.deltaTime; // "A" und "D" Tasten
        float vertical = Input.GetAxis("Vertical") * speed * Time.deltaTime; // "W" und "S" Tasten

        // Aktualisiere die Position der Kamera basierend auf der Eingabe
        transform.Translate(horizontal, 0, vertical);

        // Lasse die Kamera weiterhin auf das Zielobjekt blicken
        transform.LookAt(target);
    }

    // Methode zum Abrufen der Geschwindigkeit
    public float GetSpeed()
    {
        return speed;
    }
}
