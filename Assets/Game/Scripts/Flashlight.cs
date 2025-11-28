using UnityEngine;

public class Flashlight : MonoBehaviour
{
    private Light myLight;
    public AudioSource clickSound; // Можно добавить звук щелчка

    void Start()
    {
        myLight = GetComponent<Light>();
    }

    void Update()
    {
        // Переключение на F
        if (Input.GetKeyDown(KeyCode.F))
        {
            myLight.enabled = !myLight.enabled;

            // Если есть звук
            if (clickSound) clickSound.Play();
        }
    }
}
