using System.Collections;
using AnyThinkAds.Api;
using UnityEngine;

public class AnyThinkSDK : MonoBehaviour
{
    public static AnyThinkSDK instance;

    private const string PRIVACY_POLICY_ACCEPTED = "PRIVACY_POLICY_ACCEPTED";

    private const string APP_ID = "h698311296ccc1";
    private const string APP_KEY = "a031ff77f5ab0f973f2621993b30c2ffa";

    private const string PRIVACY_POLICY_URL =
        "https://www.sortiga.com/privacy_policy/";

    [Header("Privacy Policy UI")]
    [SerializeField] private GameObject _privacyPolicyPanel;

    [Header("UI To Enable After Agree")]
    [SerializeField] private GameObject _afterPrivacyUI;
[Header("Cameras")]
[SerializeField] private Camera privacyCamera;
[SerializeField] private Camera gameCamera;
    // Flow state
    private bool _privacyAccepted;

    public bool IsPrivacyPolicyAccepted()
    {
        return _privacyAccepted;
    }

    private bool _sdkInitialized;

    public bool IsSDKInitialized()
    {
        return _sdkInitialized;
    }

    // EU location async state
    private bool _locationResolved;
    private int _userLocation = -1;

    private bool isInEU = false;

    public bool IsInEU()
    {
        return isInEU;
    }

    private Coroutine _startupRoutine;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        _startupRoutine = StartCoroutine(StartupFlow());
    }


    private IEnumerator StartupFlow()
    {
        // ==========================================
        // 1. CHECK PRIVACY POLICY
        // ==========================================

        _privacyAccepted =
            PlayerPrefs.GetInt(PRIVACY_POLICY_ACCEPTED, 0) == 1;


        if (!_privacyAccepted)
        {
            // User has NOT accepted before

            ShowPrivacyPolicy();

            // Wait until user presses Agree
            yield return new WaitUntil(() => _privacyAccepted);
        }
        else
{
    Debug.Log("[AnyThinkSDK] Privacy policy was already accepted.");

    // Hide privacy UI
    if (_privacyPolicyPanel != null)
    {
        _privacyPolicyPanel.SetActive(false);
    }

    // Enable normal UI
    EnableAfterPrivacyUI();

    // Switch directly to game camera
    SwitchToGameCamera();
}


        // ==========================================
        // 2. EU LOCATION CHECK
        // ==========================================

#if UNITY_EDITOR

        Debug.Log(
            "[AnyThinkSDK] Unity Editor detected. " +
            "Skipping TopOn EU location lookup for Editor testing."
        );

        _locationResolved = true;
        _userLocation = -1;
        isInEU = false;

#else

        _locationResolved = false;
        _userLocation = -1;

        ATSDKAPI.getUserLocation(
            new GetLocationListener(this)
        );

        yield return new WaitUntil(
            () => _locationResolved
        );

        isInEU =
            (_userLocation == ATSDKAPI.kATUserLocationInEU);

#endif


        // ==========================================
        // 3. GDPR POPUP
        // ==========================================

        if (isInEU &&
            ATSDKAPI.getGDPRLevel() == ATSDKAPI.UNKNOWN)
        {
            ATSDKAPI.showGDPRAuth();

            const float timeoutSeconds = 120f;

            float t = 0f;

            while (
                ATSDKAPI.getGDPRLevel() ==
                ATSDKAPI.UNKNOWN &&
                t < timeoutSeconds
            )
            {
                t += Time.unscaledDeltaTime;

                yield return null;
            }


            if (
                ATSDKAPI.getGDPRLevel() ==
                ATSDKAPI.UNKNOWN
            )
            {
                Debug.LogWarning(
                    "[AnyThinkSDK] GDPR still UNKNOWN after timeout. " +
                    "Proceeding to init."
                );
            }
        }


        // ==========================================
        // 4. INITIALIZE TOPON
        // ==========================================

        InitSDKOnce();


        Debug.Log(
            "[AnyThinkSDK] Startup flow completed."
        );
    }


    private void InitSDKOnce()
    {
        if (_sdkInitialized)
            return;

        _sdkInitialized = true;

        ATSDKAPI.initSDK(
            APP_ID,
            APP_KEY
        );

        Debug.Log(
            "[AnyThinkSDK] TopOn initSDK called."
        );
    }


    // ==========================================
    // PRIVACY POLICY AGREE BUTTON
    // ==========================================

    public void OnPrivacyPolicyAccepted()
{
    _privacyAccepted = true;

    PlayerPrefs.SetInt(
        PRIVACY_POLICY_ACCEPTED,
        1
    );

    PlayerPrefs.Save();

    // Hide privacy policy
    if (_privacyPolicyPanel != null)
    {
        _privacyPolicyPanel.SetActive(false);
    }

    // Enable normal UI
    EnableAfterPrivacyUI();

    // Switch cameras
    SwitchToGameCamera();

    Debug.Log("[AnyThinkSDK] Privacy policy accepted.");
}


    // ==========================================
    // ENABLE UI AFTER PRIVACY ACCEPTANCE
    // ==========================================

    private void EnableAfterPrivacyUI()
    {
        if (_afterPrivacyUI != null)
        {
            _afterPrivacyUI.SetActive(true);

            Debug.Log(
                "[AnyThinkSDK] AfterPrivacyUI enabled."
            );
        }
        else
        {
            Debug.LogWarning(
                "[AnyThinkSDK] AfterPrivacyUI is not assigned."
            );
        }
    }


    // ==========================================
    // SHOW PRIVACY POLICY
    // ==========================================

    private void ShowPrivacyPolicy()
    {
        // Hide the normal UI
        if (_afterPrivacyUI != null)
        {
            _afterPrivacyUI.SetActive(false);
        }


        // Show privacy policy
        if (_privacyPolicyPanel != null)
        {
            _privacyPolicyPanel.SetActive(true);

            Debug.Log(
                "[AnyThinkSDK] Showing Privacy Policy."
            );
        }
        else
        {
            Debug.LogError(
                "[AnyThinkSDK] Privacy Policy Panel is not assigned!"
            );
        }
    }


    // ==========================================
    // PRIVACY POLICY LINK
    // ==========================================

    public void OpenPrivacyPolicyLink()
    {
        Application.OpenURL(
            PRIVACY_POLICY_URL
        );
    }


    // ==========================================
    // TOPON LOCATION LISTENER
    // ==========================================

    private class GetLocationListener :
        ATGetUserLocationListener
    {
        private readonly AnyThinkSDK _owner;


        public GetLocationListener(
            AnyThinkSDK owner
        )
        {
            _owner = owner;
        }


        public void didGetUserLocation(
            int location
        )
        {
            _owner._userLocation = location;

            _owner._locationResolved = true;


            Debug.Log(
                "[AnyThinkSDK] didGetUserLocation(): "
                + location
            );
        }
    }


private void SwitchToGameCamera()
{
    if (privacyCamera != null)
        privacyCamera.gameObject.SetActive(false);

    if (gameCamera != null)
        gameCamera.gameObject.SetActive(true);

    Debug.Log("[AnyThinkSDK] Switched to Game Camera.");
}

}