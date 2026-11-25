using UnityEngine;

public class AdTimer : MonoBehaviour
{
    [Header("Interstitial Settings")]
    [SerializeField] private float adDelay = 60f;

    private bool adShown = false;

    private void Start()
    {
        Invoke(nameof(ShowInterstitial), adDelay);
    }

    private void ShowInterstitial()
    {
        if (adShown)
            return;

        adShown = true;

        Debug.Log("[AdTimer] 60 seconds reached. Showing interstitial.");

        if (AdsController.Instance != null)
        {
            AdsController.Instance.ShowInterstitialAd();
        }
        else
        {
            Debug.LogWarning("[AdTimer] AdsController.Instance is NULL.");
        }
    }
}