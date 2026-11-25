using UnityEngine;

public class ReviveManager : MonoBehaviour
{
    [Header("Revive Settings")]
    [SerializeField] private int reviveCount = 1;

    private bool isReviving = false;

    public void Revive()
    {
        if (isReviving)
            return;

        if (reviveCount <= 0)
        {
            Debug.Log("[ReviveManager] Revive already used.");
            return;
        }

        if (AdsController.Instance == null)
        {
            Debug.LogWarning("[ReviveManager] AdsController.Instance is NULL.");
            return;
        }

        isReviving = true;

        Debug.Log("[ReviveManager] Requesting rewarded ad for revive.");

        AdsController.Instance.ShowRewardedVideoAd(OnReviveRewarded);
    }

    private void OnReviveRewarded()
    {
        Debug.Log("[ReviveManager] Rewarded ad completed. Reviving player.");

        reviveCount--;

        isReviving = false;

        RevivePlayer();
    }

    private void RevivePlayer()
    {
        Debug.Log("[ReviveManager] Player revived.");

        // We will connect your actual player/game-over system here.
    }
}