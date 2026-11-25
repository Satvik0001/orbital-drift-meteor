using System.Collections;
using System.Collections.Generic;
using AnyThinkAds.Api;
using AnyThinkAds.ThirdParty.LitJson;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AdsController - Manages TopOn SDK ad loading and display
///
/// ADDED:
/// - Delayed retry for failed ad loads
/// - Retry coroutine protection to avoid duplicate reload spam
/// - Consistent "AdsController" prefix on every log for easy Logcat filtering
/// - Adapter/source level logging from callbackInfo
/// - SDK initialization logging
/// - Safer reward state cleanup
/// - Realtime wait for retry timers so Time.timeScale does not affect retry
/// </summary>
public class AdsController : MonoBehaviour
{
    public static AdsController Instance;

    private const string LOG_PREFIX = "AdsController";
    private const string INTERSTITIAL_AD_PLACEMENT_ID = "n69e60b1c0ced4";
    private const string REWARDED_VIDEO_AD_PLACEMENT_ID = "n69e60b41cceb9";

    [Header("Retry Settings")]
    [SerializeField] private float interstitialRetryDelay = 12f;
    [SerializeField] private float rewardedRetryDelay = 12f;

    // Stores the "current" reward action for the next rewarded ad
    private UnityAction _pendingRewardAction;
    private bool _rewardGrantedThisAd;

    private Coroutine _interstitialReloadCoroutine;
    private Coroutine _rewardedReloadCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[{LOG_PREFIX}] Instance created and marked DontDestroyOnLoad.");
        }
        else
        {
            Debug.Log($"[{LOG_PREFIX}] Duplicate instance found. Destroying this object.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log($"[{LOG_PREFIX}] Start() called. Waiting for SDK initialization before loading ads.");
        StartCoroutine(WaitForSDKAndLoadAds());
    }

    private IEnumerator WaitForSDKAndLoadAds()
    {
        Debug.Log($"[{LOG_PREFIX}] Waiting for AnyThink SDK initialization...");

        while (AnyThinkSDK.instance == null)
        {
            Debug.Log($"[{LOG_PREFIX}] AnyThinkSDK.instance is NULL...");
            yield return new WaitForSecondsRealtime(0.5f);
        }

        while (!AnyThinkSDK.instance.IsSDKInitialized())
        {
            Debug.Log($"[{LOG_PREFIX}] AnyThink SDK exists but is not initialized yet...");
            yield return new WaitForSecondsRealtime(0.5f);
        }

        Debug.Log($"[{LOG_PREFIX}] ✅ AnyThink SDK initialized successfully.");
        Debug.Log($"[{LOG_PREFIX}] Adapter-level activity will be tracked through source attempt / filled / fail callbacks.");

        yield return new WaitForSecondsRealtime(0.5f);
        LoadAds();
    }

    public void LoadAds()
    {
        Debug.Log($"[{LOG_PREFIX}] LoadAds() called");

        if (!IsInterstitialAdReady())
        {
            Debug.Log($"[{LOG_PREFIX}] Interstitial ad not ready. Loading placement: {INTERSTITIAL_AD_PLACEMENT_ID}");
            LoadInterstitialAd(INTERSTITIAL_AD_PLACEMENT_ID);
        }
        else
        {
            Debug.Log($"[{LOG_PREFIX}] Interstitial ad already ready.");
        }

        if (!IsRewardedVideoAdReady())
        {
            Debug.Log($"[{LOG_PREFIX}] Rewarded video ad not ready. Loading placement: {REWARDED_VIDEO_AD_PLACEMENT_ID}");
            LoadRewardedVideoAd(REWARDED_VIDEO_AD_PLACEMENT_ID);
        }
        else
        {
            Debug.Log($"[{LOG_PREFIX}] Rewarded video ad already ready.");
        }
    }

    private void LoadInterstitialAd(string placementId)
    {
        if (!IsInterstitialAdReady())
        {
            Debug.Log($"[{LOG_PREFIX}] Calling TopOn interstitial load for placement: {placementId}");
            Debug.Log($"[{LOG_PREFIX}] >>> ABOUT TO CALL TopOn INTERSTITIAL LOAD");

ATInterstitialAd.Instance.loadInterstitialAd(placementId, null);

Debug.Log($"[{LOG_PREFIX}] >>> TopOn INTERSTITIAL LOAD CALL FINISHED");
        }
        else
        {
            Debug.Log($"[{LOG_PREFIX}] Interstitial already ready. Skipping load call.");
        }
    }

    private void LoadRewardedVideoAd(string placementId)
    {
        if (!IsRewardedVideoAdReady())
        {
            Debug.Log($"[{LOG_PREFIX}] Calling TopOn rewarded load for placement: {placementId}");
            Debug.Log($"[{LOG_PREFIX}] >>> ABOUT TO CALL TopOn REWARDED LOAD");

ATRewardedVideo.Instance.loadVideoAd(placementId, null);

Debug.Log($"[{LOG_PREFIX}] >>> TopOn REWARDED LOAD CALL FINISHED");
        }
        else
        {
            Debug.Log($"[{LOG_PREFIX}] Rewarded already ready. Skipping load call.");
        }
    }

    private void ScheduleInterstitialReload()
    {
        if (_interstitialReloadCoroutine != null)
        {
            Debug.Log($"[{LOG_PREFIX}] Stopping previous interstitial retry coroutine before scheduling a new one.");
            StopCoroutine(_interstitialReloadCoroutine);
        }

        _interstitialReloadCoroutine = StartCoroutine(LoadInterstitialAfterDelay());
    }

    private IEnumerator LoadInterstitialAfterDelay()
    {
        Debug.Log($"[{LOG_PREFIX}] Interstitial reload scheduled after {interstitialRetryDelay} seconds.");
        yield return new WaitForSecondsRealtime(interstitialRetryDelay);

        _interstitialReloadCoroutine = null;

        if (!IsInterstitialAdReady())
        {
            Debug.Log($"[{LOG_PREFIX}] Retrying interstitial load after delay...");
            LoadInterstitialAd(INTERSTITIAL_AD_PLACEMENT_ID);
        }
        else
        {
            Debug.Log($"[{LOG_PREFIX}] Interstitial became ready before retry. Skipping reload.");
        }
    }

    private void ScheduleRewardedReload()
    {
        if (_rewardedReloadCoroutine != null)
        {
            Debug.Log($"[{LOG_PREFIX}] Stopping previous rewarded retry coroutine before scheduling a new one.");
            StopCoroutine(_rewardedReloadCoroutine);
        }

        _rewardedReloadCoroutine = StartCoroutine(LoadRewardedAfterDelay());
    }

    private IEnumerator LoadRewardedAfterDelay()
    {
        Debug.Log($"[{LOG_PREFIX}] Rewarded reload scheduled after {rewardedRetryDelay} seconds.");
        yield return new WaitForSecondsRealtime(rewardedRetryDelay);

        _rewardedReloadCoroutine = null;

        if (!IsRewardedVideoAdReady())
        {
            Debug.Log($"[{LOG_PREFIX}] Retrying rewarded load after delay...");
            LoadRewardedVideoAd(REWARDED_VIDEO_AD_PLACEMENT_ID);
        }
        else
        {
            Debug.Log($"[{LOG_PREFIX}] Rewarded became ready before retry. Skipping reload.");
        }
    }

    public bool IsInterstitialAdReady()
    {
        return ATInterstitialAd.Instance.hasInterstitialAdReady(INTERSTITIAL_AD_PLACEMENT_ID);
    }

    public void ShowInterstitialAd()
    {
        Debug.Log($"[{LOG_PREFIX}] ShowInterstitialAd() called");

        if (IsInterstitialAdReady())
        {
            Debug.Log($"[{LOG_PREFIX}] Showing interstitial ad.");
            ATInterstitialAd.Instance.showInterstitialAd(INTERSTITIAL_AD_PLACEMENT_ID);
        }
        else
        {
            Debug.LogWarning($"[{LOG_PREFIX}] Interstitial not ready. Loading again instead of showing.");
            LoadInterstitialAd(INTERSTITIAL_AD_PLACEMENT_ID);
        }
    }

    public bool IsRewardedVideoAdReady()
    {
        return ATRewardedVideo.Instance.hasAdReady(REWARDED_VIDEO_AD_PLACEMENT_ID);
    }

    /// <summary>
    /// Show rewarded ad and run the provided action ONCE when reward event fires.
    /// </summary>
    public void ShowRewardedVideoAd(UnityAction onRewarded)
    {
        Debug.Log($"[{LOG_PREFIX}] ShowRewardedVideoAd() called");

        _pendingRewardAction = onRewarded;
        _rewardGrantedThisAd = false;

        if (IsRewardedVideoAdReady())
        {
            Debug.Log($"[{LOG_PREFIX}] Showing rewarded ad.");
            ATRewardedVideo.Instance.showAd(REWARDED_VIDEO_AD_PLACEMENT_ID);
        }
        else
        {
            Debug.LogWarning($"[{LOG_PREFIX}] Rewarded ad not ready. Loading ad now.");
            LoadRewardedVideoAd(REWARDED_VIDEO_AD_PLACEMENT_ID);
        }
    }

    public void GiveReward()
    {
        Debug.Log($"[{LOG_PREFIX}] GiveReward() called. Current Currency = {PlayerPrefs.GetInt("Currency")}");
        PlayerPrefs.SetInt("Currency", PlayerPrefs.GetInt("Currency") + 5000);
        PlayerPrefs.Save();
        Debug.Log($"[{LOG_PREFIX}] Reward applied. New Currency = {PlayerPrefs.GetInt("Currency")}");
    }

    public void ShowRewardedAdAndGive5000Currency()
    {
        Debug.Log($"[{LOG_PREFIX}] ShowRewardedAdAndGive5000Currency() called");
        ShowRewardedVideoAd(GiveReward);
    }

    public void ShowGdprPopup()
    {
        Debug.Log($"[{LOG_PREFIX}] ShowGdprPopup() called");
        ATSDKAPI.showGDPRAuth();
    }

    private void OnEnable()
{
    Debug.LogError("[AdsController TEST] ===== OnEnable START =====");

    // Interstitial
    Debug.LogError("[AdsController TEST] Registering INTERSTITIAL callbacks");

    ATInterstitialAd.Instance.client.onAdLoadEvent += OnInterstitialLoad;
    ATInterstitialAd.Instance.client.onAdLoadFailureEvent += OnInterstitialLoadFail;
    ATInterstitialAd.Instance.client.onAdShowEvent += OnInterstitialShow;
    ATInterstitialAd.Instance.client.onAdCloseEvent += OnInterstitialClose;
    ATInterstitialAd.Instance.client.onAdSourceAttemptEvent += OnInterstitialSourceAttempt;
    ATInterstitialAd.Instance.client.onAdSourceFilledEvent += OnInterstitialSourceFilled;
    ATInterstitialAd.Instance.client.onAdSourceLoadFailureEvent += OnInterstitialSourceLoadFail;

    // Rewarded
    Debug.LogError("[AdsController TEST] Registering REWARDED callbacks");

    ATRewardedVideo.Instance.client.onAdLoadEvent += OnRewardedVideoLoad;
    ATRewardedVideo.Instance.client.onAdLoadFailureEvent += OnRewardedVideoLoadFail;
    ATRewardedVideo.Instance.client.onAdVideoStartEvent += OnRewardedVideoStart;
    ATRewardedVideo.Instance.client.onAdVideoEndEvent += OnRewardedVideoEnd;
    ATRewardedVideo.Instance.client.onRewardEvent += OnRewardedVideoReward;
    ATRewardedVideo.Instance.client.onAdSourceAttemptEvent += OnRewardedVideoSourceAttempt;
    ATRewardedVideo.Instance.client.onAdSourceFilledEvent += OnRewardedVideoSourceFilled;
    ATRewardedVideo.Instance.client.onAdSourceLoadFailureEvent += OnRewardedVideoSourceLoadFail;

    Debug.LogError("[AdsController TEST] ===== OnEnable END =====");
}

    private void OnDisable()
    {
        Debug.Log($"[{LOG_PREFIX}] OnDisable() - Unregistering TopOn callbacks");

        // Interstitial
        ATInterstitialAd.Instance.client.onAdLoadEvent -= OnInterstitialLoad;
        ATInterstitialAd.Instance.client.onAdLoadFailureEvent -= OnInterstitialLoadFail;
        ATInterstitialAd.Instance.client.onAdShowEvent -= OnInterstitialShow;
        ATInterstitialAd.Instance.client.onAdCloseEvent -= OnInterstitialClose;
        ATInterstitialAd.Instance.client.onAdSourceAttemptEvent -= OnInterstitialSourceAttempt;
        ATInterstitialAd.Instance.client.onAdSourceFilledEvent -= OnInterstitialSourceFilled;
        ATInterstitialAd.Instance.client.onAdSourceLoadFailureEvent -= OnInterstitialSourceLoadFail;

        // Rewarded
        ATRewardedVideo.Instance.client.onAdLoadEvent -= OnRewardedVideoLoad;
        ATRewardedVideo.Instance.client.onAdLoadFailureEvent -= OnRewardedVideoLoadFail;
        ATRewardedVideo.Instance.client.onAdVideoStartEvent -= OnRewardedVideoStart;
        ATRewardedVideo.Instance.client.onAdVideoEndEvent -= OnRewardedVideoEnd;
        ATRewardedVideo.Instance.client.onRewardEvent -= OnRewardedVideoReward;
        ATRewardedVideo.Instance.client.onAdSourceAttemptEvent -= OnRewardedVideoSourceAttempt;
        ATRewardedVideo.Instance.client.onAdSourceFilledEvent -= OnRewardedVideoSourceFilled;
        ATRewardedVideo.Instance.client.onAdSourceLoadFailureEvent -= OnRewardedVideoSourceLoadFail;
    }

    private void OnDestroy()
    {
        Debug.Log($"[{LOG_PREFIX}] OnDestroy() called");

        if (_interstitialReloadCoroutine != null)
        {
            StopCoroutine(_interstitialReloadCoroutine);
            _interstitialReloadCoroutine = null;
        }

        if (_rewardedReloadCoroutine != null)
        {
            StopCoroutine(_rewardedReloadCoroutine);
            _rewardedReloadCoroutine = null;
        }
    }

    // ---------------- Helper Logging ----------------

    private void LogAdapterInfo(string section, Dictionary<string, object> dict)
    {
        if (dict == null)
        {
            Debug.Log($"[{LOG_PREFIX}] [{section}] No callback adapter/source info available.");
            return;
        }

        string[] possibleKeys =
        {
            "network_firm_id",
            "network_firm_name",
            "adsource_id",
            "adsource_index",
            "adsource_name",
            "ecpm",
            "currency",
            "format",
            "country",
            "publisher_revenue",
            "scenario_id",
            "scenario_reward_name",
            "scenario_reward_number",
            "channel",
            "sub_channel",
            "segment_id",
            "load_status"
        };

        string log = $"[{LOG_PREFIX}] [{section}] Adapter/Source Details:\n";

        foreach (string key in possibleKeys)
        {
            if (dict.ContainsKey(key) && dict[key] != null)
            {
                log += $"   {key}: {dict[key]}\n";
            }
        }

        log += $"   Full Callback Info: {JsonMapper.ToJson(dict)}";
        Debug.Log(log);
    }

    // ---------------- Interstitial Handlers ----------------

    private void OnInterstitialLoad(object sender, ATAdEventArgs args)
    {

 Debug.LogError(
        "[AdsController TEST] 🔥🔥🔥 INTERSTITIAL LOAD CALLBACK RECEIVED 🔥🔥🔥"
    );

    Debug.LogError(
        "[AdsController TEST] Placement = " + args.placementId
    );


        Debug.Log($"[{LOG_PREFIX}] ✅ Interstitial Ad LOADED successfully! Placement: {args.placementId}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            Debug.Log($"[{LOG_PREFIX}] Interstitial Load Callback Info: {JsonMapper.ToJson(dict)}");
            LogAdapterInfo("Interstitial Load", dict);
        }
    }

    private void OnInterstitialLoadFail(object sender, ATAdErrorEventArgs args)
    {

Debug.LogError(
        "[AdsController TEST] ❌❌❌ INTERSTITIAL LOAD FAILURE CALLBACK RECEIVED ❌❌❌"
    );

    Debug.LogError(
        "[AdsController TEST] Placement = " + args.placementId
    );

    Debug.LogError(
        "[AdsController TEST] Error Code = " + args.errorCode
    );

    Debug.LogError(
        "[AdsController TEST] Error Message = " + args.errorMessage
    );


        Debug.LogError($"[{LOG_PREFIX}] ❌ Interstitial Ad LOAD FAILED! Placement: {args.placementId}");
        Debug.LogError($"[{LOG_PREFIX}] Error Code: {args.errorCode}");
        Debug.LogError($"[{LOG_PREFIX}] Error Message: {args.errorMessage}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            Debug.LogError($"[{LOG_PREFIX}] Error Callback Info: {JsonMapper.ToJson(dict)}");
            LogAdapterInfo("Interstitial LoadFail", dict);
        }

        ScheduleInterstitialReload();
    }

    private void OnInterstitialSourceAttempt(object sender, ATAdEventArgs args)
    {
        Debug.Log($"[{LOG_PREFIX}] 🔄 Interstitial Source ATTEMPT - Placement: {args.placementId}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            LogAdapterInfo("Interstitial SourceAttempt", dict);
        }
    }

    private void OnInterstitialSourceFilled(object sender, ATAdEventArgs args)
    {
        Debug.Log($"[{LOG_PREFIX}] ✅ Interstitial Source FILLED - Placement: {args.placementId}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            LogAdapterInfo("Interstitial SourceFilled", dict);
        }
    }

    private void OnInterstitialSourceLoadFail(object sender, ATAdErrorEventArgs args)
    {
        Debug.LogWarning($"[{LOG_PREFIX}] ⚠️ Interstitial Source LOAD FAILED - Placement: {args.placementId}");
        Debug.LogWarning($"[{LOG_PREFIX}] Source Error Code: {args.errorCode}");
        Debug.LogWarning($"[{LOG_PREFIX}] Source Error Message: {args.errorMessage}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            LogAdapterInfo("Interstitial SourceLoadFail", dict);
        }
    }

    private void OnInterstitialShow(object sender, ATAdEventArgs args)
    {
        Debug.Log($"[{LOG_PREFIX}] Interstitial Ad SHOWN - Placement: {args.placementId}");
        Time.timeScale = 0;
    }

    private void OnInterstitialClose(object sender, ATAdEventArgs args)
    {
        Debug.Log($"[{LOG_PREFIX}] Interstitial Ad CLOSED - Placement: {args.placementId}");
        Time.timeScale = 1;

        if (!IsInterstitialAdReady())
        {
            Debug.Log($"[{LOG_PREFIX}] Interstitial not ready after close. Loading again...");
            LoadInterstitialAd(INTERSTITIAL_AD_PLACEMENT_ID);
        }
    }

    // ---------------- Rewarded Handlers ----------------

    private void OnRewardedVideoLoad(object sender, ATAdEventArgs args)
    {

Debug.LogError(
        "[AdsController TEST] 🔥🔥🔥 REWARDED LOAD CALLBACK RECEIVED 🔥🔥🔥"
    );

    Debug.LogError(
        "[AdsController TEST] Placement = " + args.placementId
    );



        Debug.Log($"[{LOG_PREFIX}] ✅ Rewarded Video Ad LOADED successfully! Placement: {args.placementId}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            Debug.Log($"[{LOG_PREFIX}] Rewarded Video Load Callback Info: {JsonMapper.ToJson(dict)}");
            LogAdapterInfo("Rewarded Load", dict);
        }
    }

    private void OnRewardedVideoLoadFail(object sender, ATAdErrorEventArgs args)
    {

Debug.LogError(
        "[AdsController TEST] ❌❌❌ REWARDED LOAD FAILURE CALLBACK RECEIVED ❌❌❌"
    );

    Debug.LogError(
        "[AdsController TEST] Placement = " + args.placementId
    );

    Debug.LogError(
        "[AdsController TEST] Error Code = " + args.errorCode
    );

    Debug.LogError(
        "[AdsController TEST] Error Message = " + args.errorMessage
    );



        Debug.LogError($"[{LOG_PREFIX}] ❌ Rewarded Video Ad LOAD FAILED! Placement: {args.placementId}");
        Debug.LogError($"[{LOG_PREFIX}] Error Code: {args.errorCode}");
        Debug.LogError($"[{LOG_PREFIX}] Error Message: {args.errorMessage}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            Debug.LogError($"[{LOG_PREFIX}] Error Callback Info: {JsonMapper.ToJson(dict)}");
            LogAdapterInfo("Rewarded LoadFail", dict);
        }

        ScheduleRewardedReload();
    }

    private void OnRewardedVideoSourceAttempt(object sender, ATAdEventArgs args)
    {
        Debug.Log($"[{LOG_PREFIX}] 🔄 Rewarded Source ATTEMPT - Placement: {args.placementId}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            LogAdapterInfo("Rewarded SourceAttempt", dict);
        }
    }

    private void OnRewardedVideoSourceFilled(object sender, ATAdEventArgs args)
    {
        Debug.Log($"[{LOG_PREFIX}] ✅ Rewarded Source FILLED - Placement: {args.placementId}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            LogAdapterInfo("Rewarded SourceFilled", dict);
        }
    }

    private void OnRewardedVideoSourceLoadFail(object sender, ATAdErrorEventArgs args)
    {
        Debug.LogWarning($"[{LOG_PREFIX}] ⚠️ Rewarded Source LOAD FAILED - Placement: {args.placementId}");
        Debug.LogWarning($"[{LOG_PREFIX}] Source Error Code: {args.errorCode}");
        Debug.LogWarning($"[{LOG_PREFIX}] Source Error Message: {args.errorMessage}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            LogAdapterInfo("Rewarded SourceLoadFail", dict);
        }
    }

    private void OnRewardedVideoStart(object sender, ATAdEventArgs args)
    {
        Debug.Log($"[{LOG_PREFIX}] Rewarded Video Ad STARTED - Placement: {args.placementId}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            Debug.Log($"[{LOG_PREFIX}] Video Start Info: {JsonMapper.ToJson(dict)}");
            LogAdapterInfo("Rewarded Start", dict);
        }

        Time.timeScale = 0;
    }

    private void OnRewardedVideoEnd(object sender, ATAdEventArgs args)
    {
        Debug.Log($"[{LOG_PREFIX}] Rewarded Video Ad ENDED - Placement: {args.placementId}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            Debug.Log($"[{LOG_PREFIX}] Video End Info: {JsonMapper.ToJson(dict)}");
            LogAdapterInfo("Rewarded End", dict);
        }

        Time.timeScale = 1;

        // NOTE: Do NOT reset _pendingRewardAction here. Many network adapters (Meta, Unity Ads,
        // ironSource) fire onAdVideoEndEvent BEFORE onRewardEvent. Clearing state here would
        // drop the reward before the reward callback arrives. State is reset inside
        // OnRewardedVideoReward, or at the start of the next ShowRewardedVideoAd() call.

        if (!IsRewardedVideoAdReady())
        {
            Debug.Log($"[{LOG_PREFIX}] Rewarded not ready after end. Loading again...");
            LoadRewardedVideoAd(REWARDED_VIDEO_AD_PLACEMENT_ID);
        }
    }

    private void OnRewardedVideoReward(object sender, ATAdEventArgs args)
    {
        Debug.Log($"[{LOG_PREFIX}] Rewarded Video Ad REWARD GRANTED - Placement: {args.placementId}");

        if (args.callbackInfo != null)
        {
            var dict = args.callbackInfo.toDictionary();
            Debug.Log($"[{LOG_PREFIX}] Reward Info: {JsonMapper.ToJson(dict)}");
            LogAdapterInfo("Rewarded Reward", dict);
        }

        // Guard: stray callback with no pending action (no ShowRewardedVideoAd was called)
        if (_pendingRewardAction == null && !_rewardGrantedThisAd)
        {
            Debug.LogWarning($"[{LOG_PREFIX}] Reward event received but no pending reward action exists. Ignoring stray callback.");
            return;
        }

        // Guard: duplicate reward event for the same ad show
        if (_rewardGrantedThisAd)
        {
            Debug.LogWarning($"[{LOG_PREFIX}] Reward already granted for this ad show. Ignoring duplicate reward event.");
            return;
        }

        _rewardGrantedThisAd = true;

        // Capture and clear before invoke to prevent re-entry issues
        UnityAction action = _pendingRewardAction;
        _pendingRewardAction = null;

        if (action != null)
        {
            Debug.Log($"[{LOG_PREFIX}] Invoking pending reward action...");
            action.Invoke();
        }

        // _rewardGrantedThisAd intentionally stays true until ShowRewardedVideoAd() is called
        // again. This prevents a second onRewardEvent (fired by some adapters) from double-granting
        // the reward before the end/close callback arrives.
    }
}