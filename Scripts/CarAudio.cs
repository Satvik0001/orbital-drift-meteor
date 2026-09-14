using UnityEngine;
using UnityEngine.InputSystem;

public class CarAudio : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if(audioSource == null)
        {
            Debug.LogError(
                "No AudioSource found on Car!"
            );
        }
    }

    void Update()
    {
        if(audioSource == null)
            return;

        bool moving =
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.sKey.isPressed;

        if(moving)
        {
            if(!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            if(audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}