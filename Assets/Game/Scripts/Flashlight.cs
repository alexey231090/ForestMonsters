using UnityEngine;

public class Flashlight : MonoBehaviour
{
    private Light myLight;
    public AudioSource clickSound; 

    void Start()
    {
        myLight = GetComponent<Light>();
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            myLight.enabled = !myLight.enabled;

            
            if (clickSound) clickSound.Play();
        }
    }
}
