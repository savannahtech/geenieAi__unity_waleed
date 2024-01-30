using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine.Advertisements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum AdType
{
	TestAds, LiveAds
}

public enum AdPriority
{
	Admob,
	Unity
}

//public enum adaptiveBanner
//{
//	Adaptive, Banner
//}

public class MyAdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
	public static Action onRewardedVideoAdsCompleteAction, onRewardedInterstitialCompleteAction, onRewardedInterstitialSkipAction;

	public static MyAdsManager instance;

	public static MyAdsManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<MyAdsManager>();
				if (instance && instance.gameObject)
				{
					DontDestroyOnLoad(instance.gameObject);
				}
			}
			return instance;
		}
	}
	
	private void SetAdIds()
	{
		if (adType == AdType.TestAds)
		{
			appID = "ca-app-pub-3940256099942544~3347511713";
			admobInterstitalId = "ca-app-pub-3940256099942544/1033173712";
			admobBannerId = "ca-app-pub-3940256099942544/6300978111";
			admobRewardedId = "ca-app-pub-3940256099942544/5224354917";

			//admobOpenAppAdsId = "ca-app-pub-3940256099942544/3419835294";
			//admobAdaptiveId = "ca-app-pub-3940256099942544/6300978111";
			//admobRewardedInterstitalId = "ca-app-pub-3940256099942544/5354046379"; hamza
			//admobNativeId = "ca-app-pub-3940256099942544/2247696110";
		}
		else if (adType == AdType.LiveAds)
		{
			if (SystemInfo.systemMemorySize <= 2048)
			{
				admobInterstitalId = admobInterstitalId_Two_Gb;
				admobBannerId = admobBannerId_Two_Gb;
			//	admobAdaptiveId = admobAdaptiveTwoGb;
			
			}
		}
	}
	
	public AdType adType;
	public AdPriority adPriority;
	public AdPosition bannerPosition;
	public string TestAppId = "ca-app-pub-3940256099942544~3347511713";
	public string appID;
	public string admobRewardedId;

	//public string admobRewardedInterstitalId;hamza

	private InterstitialAd admobInterstitial;
	private BannerView admobBannerView;
	private RewardedAd admobRewardBasedVideo;
	private RewardedInterstitialAd rewardedInterstitialAd;
	private AppOpenAd admobOpenAppAd;

	public delegate void actionRewardedCompleted();
	public event actionRewardedCompleted onRewardedVideoCompleteEvent;

	[Header("Admob Banner")]
	public string admobBannerId;
	public string admobBannerId_Two_Gb;
	[Header("Admob Interstitial")]
	public string admobInterstitalId;
	public string admobInterstitalId_Two_Gb;
	
	internal bool isAdmobInitialized;

	public bool canShowAds;
	public int adBoolValue;

	//[Header("Admob Adaptive Banner")]
	//public string admobAdaptiveId;
	//public string admobAdaptiveTwoGb;
	//public AdPosition adaptiveBannerPosition = AdPosition.BottomLeft;
	//public adaptiveBanner _BannerType;
	//	private BannerView adaptiveBannerView;

	[Header("Admob App Ad")]
    public string admobOpenAppAdsId;
    public ScreenOrientation appOpenOrientation = ScreenOrientation.LandscapeLeft;

    [Header("UnityAds")]
	public string unityId;
	public string unityInterstitalId = "Interstitial_Android";
	public string unityRewardedId = "Rewarded_Android";

	internal bool isRewardedInterstitialShown = false;
	internal bool isShowBannerAfterInterCallBack = true,isShowAdaptiveAfterInterCallBack,isShowBannerAfterRewardedInter, isBannerShowing, isAdaptiveBannerShowing;
	// Start is called before the first frame update
	private bool isShowingAd = false;
	static bool AdsForTwoGB,AdsForThreeGB,AdsForAboveThree;
	WaitForSecondsRealtime constantDelay_Zero_Two = new WaitForSecondsRealtime(.2f);
	WaitForSecondsRealtime adsDelay_Zero_Six = new WaitForSecondsRealtime(.6f);

//	GameObject simpleBanner,adaptiveBannerScreen;

	protected void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(transform.gameObject);
		}
		else
		{
			if (this != instance)
			{
				Destroy(this.gameObject);
			}
		}

        SetAdIds();

		adBoolValue = PlayerPrefs.GetInt("CanShowAds", 1);
    }

	IEnumerator Start()
	{
		CanShowAds();
		yield return constantDelay_Zero_Two;
		setUpDeviceData();
		yield return new WaitForSecondsRealtime(2f);
		InitializeAds();
        StartCoroutine(ShowBannerRountine());
        //setBannerBlackScreen();

    }

	void CanShowAds()
    {
		if(adBoolValue != 1)
        {
			canShowAds = false;
        }
		else
        {
			canShowAds = true;
        }
    }

	bool isInternetAvailble()
	{
		return !(Application.internetReachability == NetworkReachability.NotReachable);
	}

	void setUpDeviceData() {

		//if(SystemInfo.systemMemorySize <= 2560)
		//      {
		//	AdsForTwoGB = true;
		//	AdsForThreeGB = false;
		//	AdsForAboveThree = false;
		//      }
		//else if(SystemInfo.systemMemorySize <= 3584)
		//      {
		//	AdsForThreeGB = true;
		//	AdsForTwoGB = true;
		//	AdsForAboveThree = false;
		//}
		//      else
		//      {
		//	AdsForThreeGB = true;
		//	AdsForTwoGB = true;
		//	AdsForAboveThree = true;
		//}
		AdsForAboveThree = true;
	}

	//public void setBannerBlackScreen()
	//{
	//	simpleBanner = transform.GetChild(0).transform.GetChild(0).gameObject;
	//	adaptiveBannerScreen = transform.GetChild(0).transform.GetChild(1).gameObject;
	//}

	public void InitializeAds()
	{
		if (!isInternetAvailble() || !canShowAds /*|| AdsRemoteConfigManager.Instance.IsHideAd()*/) 
			return;

		MobileAds.Initialize(initStatus => { Debug.Log("Initialized"); StartCoroutine(RequestAdmobAds()); isAdmobInitialized = true; });
	}

	IEnumerator RequestAdmobAds()
	{
		yield return constantDelay_Zero_Two;

        //      if (AdsForThreeGB)
        //      {

        LoadOpenAppAd();

        //	yield return adsDelay_Zero_Six;
        //	yield return adsDelay_Zero_Six;
        //	RequestAdmobRewardedInterstitial();
        //	yield return adsDelay_Zero_Six;
        //}

        RequestAdmobInterstitial();

		yield return adsDelay_Zero_Six;
		if (AdsForAboveThree)
		{
			yield return adsDelay_Zero_Six;
			//RequestAdmobRewardedVideo();
			yield return adsDelay_Zero_Six;
			if (adType == AdType.TestAds)
			{
				Advertisement.Initialize(unityId, true, this);
			}
			else
			{
				Advertisement.Initialize(unityId, false, this);
			}
		}

		if (AdsForThreeGB)
		{
			yield return adsDelay_Zero_Six;
			yield return adsDelay_Zero_Six;
            ShowOpenAppAdIfAvailable();
        }
	}

	//unity ads initialization complete failed
	public void OnInitializationComplete()
	{
		Debug.Log("Unity Ads initialization complete.");
		if (AdsForAboveThree)
			StartCoroutine(loadUnityAds());
	}

	public void OnInitializationFailed(UnityAdsInitializationError error, string message)
	{
		Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
	}

	#region RequestAdmobInterstitial

	IEnumerator DelayRequestAdmobInterstitial(float time)
    {
		yield return new WaitForSecondsRealtime(time);
		RequestAdmobInterstitial();

	}

	public void RequestAdmobInterstitial()
	{
		if (this.admobInterstitial != null)
			return;
		StartCoroutine(delayReqtAdmobInterstitial());
	}

	IEnumerator delayReqtAdmobInterstitial()
	{
		if (this.admobInterstitial != null)
		{

			this.admobInterstitial.Destroy();
			this.admobInterstitial = null;

		}
		yield return constantDelay_Zero_Two;
		// Initialize an InterstitialAd.
		this.admobInterstitial = new InterstitialAd(admobInterstitalId);
		this.admobInterstitial.OnAdLoaded += HandleOnInterstitalLoaded;
		// Called when an ad request failed to load.
		this.admobInterstitial.OnAdFailedToLoad += HandleOnInterstitialFailedToLoad;
		// Called when the ad is closed.
		this.admobInterstitial.OnAdClosed += HandleOnInterstitialClosed;
		this.admobInterstitial.OnAdFailedToShow += HandleOnInterstitialFailedToShow;
		// Create an empty ad request.
		AdRequest request = new AdRequest.Builder()
			.Build();
		// Load the interstitial with the request.
		this.admobInterstitial.LoadAd(request);
	}

	#endregion

	#region ShowAdmobInterstitial
	public void ShowAdmobInterstitial()
	{
		if (isadmobInterstitialLoaded())
		{
			HideAdmobBanner();
			StartCoroutine(ShowInterstitialDelay());
		}
		
	}

	IEnumerator ShowInterstitialDelay()
	{
		yield return new WaitForSecondsRealtime(.1f);

		this.admobInterstitial.Show();
	}
	#endregion

	#region IsInterstitialLoaded
	public bool isInterstitialAdsLoaded()
	{
		if (Application.internetReachability == NetworkReachability.NotReachable) return false;
		if (isUnityInterstitialReady() || isadmobInterstitialLoaded())
			return true;
		else return false;
	}



	public bool isadmobInterstitialLoaded()
	{
		if (this.admobInterstitial != null && this.admobInterstitial.IsLoaded())
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	#endregion

	#region RequestAndShowAdmobBanner
	public void RequestAndShowAdmobBanner(AdPosition _adPosition)
	{
		if (this.admobBannerView != null || !canShowAds) return;

		this.admobBannerView = new BannerView(admobBannerId, AdSize.Banner, _adPosition);
		// Called when an ad request has successfully loaded.
		this.admobBannerView.OnAdLoaded += this.HandleOnAdmobBannerLoaded;
		// Called when an ad request failed to load.
		this.admobBannerView.OnAdFailedToLoad += this.HandleOnAdmobBannerFailedToLoad;
		this.admobBannerView.OnAdClosed += this.HandleAdmobBannerClosed;
		// Create an empty ad request.
		AdRequest request = new AdRequest.Builder().Build();
		// Load the banner with the request.
		this.admobBannerView.LoadAd(request);
		isBannerShowing = true;
		//simpleBanner.SetActive(true);


	}

	#endregion

	#region ShowAdmobBanner

	public void RequestCallAdmobBanner()
	{
		RequestAndShowAdmobBanner(bannerPosition);

	}

	#endregion

	#region HideAdmobBanner
	public void HideAdmobBanner()
	{
		//simpleBanner.SetActive(false);

		if (Application.internetReachability == NetworkReachability.NotReachable) return;
		if (this.admobBannerView != null && isBannerShowing)
		{
			this.admobBannerView.Hide();
			isBannerShowing = false;

		}

	}


	IEnumerator ShowBannerRountine()
	{
		yield return constantDelay_Zero_Two;
		ShowAdmobBanner();
	}
	public void ShowAdmobBanner()
	{
		if (Application.internetReachability == NetworkReachability.NotReachable ||  (PlayerPrefs.GetInt("NoAds") == 1) || !canShowAds)
			return;

		if (this.admobBannerView != null && !isBannerShowing)
		{
			this.admobBannerView.Show();
			isBannerShowing = true;
			//simpleBanner.SetActive(true);
		}
		else
		{
			RequestAndShowAdmobBanner(bannerPosition);
		}
	}

	#endregion

	#region RequestAdmobRewardedVideo

	IEnumerator delayLoadAdmobVideo(float time)
    {
		yield return new WaitForSecondsRealtime(time);
		RequestAdmobRewardedVideo();

	}

	public void RequestAdmobRewardedVideo()
	{
		if (!canShowAds)
			return;

		if (!AdsForThreeGB) 
			return;

		if (this.admobRewardBasedVideo != null)
			return;

		StartCoroutine(delayRqtAdmobVideo());
	}

	IEnumerator delayRqtAdmobVideo()
	{

		yield return constantDelay_Zero_Two;
		this.admobRewardBasedVideo = new RewardedAd(admobRewardedId);
		// Called when an ad request has successfully loaded.
		this.admobRewardBasedVideo.OnAdLoaded += HandleRewardedAdLoaded;
        // Called when an ad request failed to load.
        this.admobRewardBasedVideo.OnAdFailedToLoad += HandleAdmobRewardedFailedToLoad;
        // Called when the user should be rewarded for interacting with the ad.
        this.admobRewardBasedVideo.OnUserEarnedReward += HandleAdmobEarnedReward;
		// Called when the ad is closed.
		this.admobRewardBasedVideo.OnAdClosed += HandleAdmobRewardedAdClosed;
		this.admobRewardBasedVideo.OnAdFailedToShow += HandleAdmobRewardedAdFailedToShow;
		
		// Create an empty ad request.
		AdRequest request = new AdRequest.Builder().Build();
		// Load the rewarded ad with the request.
		this.admobRewardBasedVideo.LoadAd(request);
	}
	#endregion

	#region ShowAdmobRewardedVideo
	IEnumerator ShowAdmobRewardedVideo()
	{
		HideAdmobBanner();
		//if (isAdaptiveBannerShowing)
		//	hideAdaptiveBanner();
		yield return constantDelay_Zero_Two;
		if (IsAdmobRewardedVideoAvailable())
			this.admobRewardBasedVideo.Show();
		else
		{
			yield return constantDelay_Zero_Two;
			RequestAdmobRewardedVideo();
		}

	}
	#endregion

	#region IsRewardedVideoLoaded
	public bool IsAdmobRewardedVideoAvailable()
	{
		if (AdsForAboveThree)
		{
			if (this.admobRewardBasedVideo != null && this.admobRewardBasedVideo.IsLoaded())
			{
				return true;
			}
			else
				return false;
		}
		else return false;

	}
	#endregion

	#region Banner callback handlers
	int bannerRecheckCounter = 0;
	public void HandleOnAdmobBannerLoaded(object sender, EventArgs args)
	{
		Debug.Log("Banner Loaded: " + args.ToString());
		bannerRecheckCounter = 0;
	}
	public void HandleOnAdmobBannerFailedToLoad(object sender, AdFailedToLoadEventArgs args)
	{
		//Debug.Log("Banner Failed to load: " + args.LoadAdError);
		// Handle the ad failed to load event.
		if (bannerRecheckCounter < 3)
		{
			Debug.Log("wwww");
			StartCoroutine(delayRequestBanner(.2f));
			bannerRecheckCounter++;
		}
		//else simpleBanner.SetActive(false);
	}
	
	public void HandleAdmobBannerClosed(object sender, EventArgs args)
	{
		Debug.Log("Banner Closed event received.");
		StartCoroutine(delayRequestBanner(.2f));
		
	}

	IEnumerator  delayRequestBanner(float time)
	{
		yield return new WaitForSecondsRealtime(time);
		if(isBannerShowing)
		RequestAndShowAdmobBanner(bannerPosition);
	}
	#endregion

	#region Interstitial callback handlers
	int interRecheckCounter = 0;
	public void HandleOnInterstitalLoaded(object sender, EventArgs args)
	{
		//Debug.Log("Interstitial Loaded " + args.ToString());
		interRecheckCounter = 0;
	}
	public void HandleOnInterstitialFailedToLoad(object sender, AdFailedToLoadEventArgs args)
	{
		//	Debug.Log("Interstitial Failed to load: " + args.LoadAdError);
		if (interRecheckCounter < 3)
		{
			StartCoroutine(ReqtInterstitialDelay());
			interRecheckCounter++;
		}
	}
	public void HandleOnInterstitialFailedToShow(object sender, EventArgs args)
	{
		StartCoroutine(ReqtInterstitialDelay());
	}
	public void HandleOnInterstitialClosed(object sender, EventArgs args)
	{
		//Debug.Log("Interstitial Closed event received.");
		StartCoroutine(ReqtInterstitialDelay());
	}

	IEnumerator ReqtInterstitialDelay()
	{
		yield return constantDelay_Zero_Two;
		if (this.admobInterstitial != null)
		{

			this.admobInterstitial.Destroy();
			this.admobInterstitial = null;

		}
		StartCoroutine(DelayRequestAdmobInterstitial(.6f));
		if (isShowBannerAfterInterCallBack/*&& !isAdaptiveBannerShowing*/ )
        {
			Debug.Log("ss");
			StartCoroutine(ShowBannerRountine());
			isShowBannerAfterInterCallBack = false;
		}
			
		//if (isShowAdaptiveAfterInterCallBack&&!isBannerShowing)
		//{
		//	//showAdaptiveBanner();
		//	isShowAdaptiveAfterInterCallBack = false;

		//}
	}
	#endregion

	#region Awarded-Video callback handlers


	int admobVedioCheckCount;
	public void HandleRewardedAdLoaded(object sender, EventArgs args)
	{
		//Debug.Log("HandleRewardedAdLoaded event received " + args.ToString());
	}

	

	public void HandleAdmobRewardedFailedToLoad(object sender, AdFailedToLoadEventArgs args)
	{
		//Debug.Log("Rewarded Failed To Load " + args.ToString());
		admobVedioCheckCount++;
		
		if (admobVedioCheckCount < 3)
        {
			StartCoroutine(delayLoadAdmobVideo(.6f));
			
		}

	}
	public void HandleAdmobRewardedAdFailedToShow(object sender, AdErrorEventArgs args)
	{
		StartCoroutine(delayUnloadCompleteAction(.5f));
		if (!isAdaptiveBannerShowing)
			StartCoroutine(ShowBannerRountine());
	}
	public void HandleAdmobRewardedAdClosed(object sender, EventArgs args)
	{
		if (onRewardedVideoCompleteEvent != null)
		{
			onRewardedVideoCompleteEvent();
		}
		StartCoroutine(DelayDestoryAdmobVideo());
		if (!isAdaptiveBannerShowing)
			StartCoroutine(ShowBannerRountine());
	}
	void delayCall()
	{
		if (onRewardedVideoCompleteEvent != null)
		{
			onRewardedVideoCompleteEvent();
		}
	}
	public void HandleAdmobEarnedReward(object sender, Reward args)
	{

		string type = args.Type;
		double amount = args.Amount;
		Invoke(nameof(delayCall), .1f);

		StartCoroutine(delayRewardOnComplete(.2f));
		

		StartCoroutine(delayLoadAdmobVideo(1f));
		//Debug.Log("HandleAdmobEarnedReward event rewarded " + args.ToString());
	}


	IEnumerator DelayDestoryAdmobVideo()
	{
		yield return constantDelay_Zero_Two;
		if (this.admobRewardBasedVideo != null)
		{
			this.admobRewardBasedVideo.Destroy();
			this.admobRewardBasedVideo = null;
		}


	}



	IEnumerator  delayRewardOnComplete(float time)
	{
		yield return new WaitForSecondsRealtime(time);
		// by Action Approch
		if (onRewardedVideoAdsCompleteAction != null)
		{
			onRewardedVideoAdsCompleteAction.Invoke();
			onRewardedVideoAdsCompleteAction = null;
		}
	}

	IEnumerator delayUnloadCompleteAction(float time)
	{
		// by Action Approch
		yield return new WaitForSecondsRealtime (time);
		if (onRewardedVideoAdsCompleteAction != null)
		{
			onRewardedVideoAdsCompleteAction = null;

		}

	}
	#endregion


	#region unity Ads
	// unitu ads info
	IEnumerator loadUnityAds()
	{
		yield return constantDelay_Zero_Two;
		loadUnityInterstitial();
		yield return adsDelay_Zero_Six;
		loadUnityRewardedVideo();
		
		
		
	}


	IEnumerator delayLoadUnityInterstitial(float time)
    {
		yield return new WaitForSecondsRealtime(time);
		loadUnityInterstitial();
    }
	IEnumerator delayLoadUnityVideo(float time)
	{
		yield return new WaitForSecondsRealtime(time);
		loadUnityRewardedVideo();
	}
	public void loadUnityInterstitial()
	{
		//Debug.Log("Loading Ad: " + unityInterstitalId);
		if (AdsForAboveThree)
			Advertisement.Load(unityInterstitalId, this);
	}
	public void loadUnityRewardedVideo()
	{
		//Debug.Log("Loading Ad: " + unityRewardedId);
		if(AdsForAboveThree)
		Advertisement.Load(unityRewardedId, this);
	}

	public bool IsRewardedVideoesAvailable()
	{
		if (!isInternetAvailble()) return false;
		if (AdsForAboveThree)
		{
			if (IsAdmobRewardedVideoAvailable() || isUnityVideoReady())
				return true;
		}
		//else if (AdsForThreeGB)
		//{
		//return isRewardedInterstitialAdReady();
  //      }
		return false;
	}

	public void OnUnityAdsAdLoaded(string adUnitId)
	{
		if (adUnitId == unityInterstitalId)
		{
			unityInterstitialLoadCount = 0;

		}
		if (adUnitId == unityRewardedId)
		{
			unityVideoLoadCount = 0;

		}
		//Debug.Log("Ad Loaded status: " + adUnitId+"_"+isunityInterstitialAdLoaded);
	}
	int unityInterstitialLoadCount = 0;
	int unityVideoLoadCount = 0;
	public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
	{
		Debug.Log($"Error loading Ad Unit: {adUnitId} - {error.ToString()} - {message}");
		// Optionally execute code if the Ad Unit fails to load, such as attempting to try again.

		if (adUnitId == unityInterstitalId)
		{
			unityInterstitialLoadCount++;
			if (unityInterstitialLoadCount < 3)
			{
				StartCoroutine(delayLoadUnityInterstitial(.2f));
				
			}
		}
		if (adUnitId == unityRewardedId)
		{
			unityVideoLoadCount++;
			if (unityVideoLoadCount < 3)
			{
				StartCoroutine(delayLoadUnityVideo(.2f));
				

			}
		}
	}

	public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
	{
		Debug.Log($"Error showing Ad Unit {adUnitId}: {error.ToString()} - {message}");
		// Optionally execute code if the Ad Unit fails to show, such as loading another ad.
		if (adUnitId.Equals(unityRewardedId))
		{
			//Debug.Log("Unity Ads Rewarded Ad failed to show");
			StartCoroutine(delayUnloadCompleteAction(.5f));
			StartCoroutine(ShowBannerRountine());
		}
		if (adUnitId.Equals(unityInterstitalId))
		{
			if (isShowBannerAfterInterCallBack && !isAdaptiveBannerShowing)
			{
				StartCoroutine(ShowBannerRountine());
				isShowBannerAfterInterCallBack = false;
			}

			if (isShowAdaptiveAfterInterCallBack && !isBannerShowing)
			{
				//showAdaptiveBanner();
				isShowAdaptiveAfterInterCallBack = false;

			}
		}
	}
	
	public void OnUnityAdsShowStart(string adUnitId) { }
	public void OnUnityAdsShowClick(string adUnitId) { }
	public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
	{
		Time.timeScale = 1;
		// interstitial wor
		if (adUnitId.Equals(unityInterstitalId) && (showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED) || showCompletionState.Equals(UnityAdsShowCompletionState.SKIPPED)))
		{
			Debug.Log("Unity Ads interstitial Completed");
			Invoke(nameof(delayCall), .1f);
			// Load another ad:
			StartCoroutine(delayLoadUnityInterstitial(.2f));

			if (isShowBannerAfterInterCallBack && !isAdaptiveBannerShowing)
			{
				StartCoroutine(ShowBannerRountine());
				isShowBannerAfterInterCallBack = false;
			}

			if (isShowAdaptiveAfterInterCallBack && !isBannerShowing)
			{
				//showAdaptiveBanner();
				isShowAdaptiveAfterInterCallBack = false;

			}


		}

		//rewarded word
		if (adUnitId.Equals(unityRewardedId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
		{
			Debug.Log("Unity Ads Rewarded Ad Completed");
			// Grant a reward.
			StartCoroutine(delayRewardOnComplete(.2f));
			// Load another ad:
			StartCoroutine(delayLoadUnityVideo(.6f));
			if (isShowBannerAfterInterCallBack && !isBannerShowing)
            {
				StartCoroutine(ShowBannerRountine());
				isShowBannerAfterInterCallBack = false;
			}
				
			
		}


	}

	public bool isUnityInterstitialReady()
	{
		return unityInterstitalId.Equals(unityInterstitalId);
	}
	public bool isUnityVideoReady()
	{
		
		if (AdsForAboveThree)
			return unityRewardedId.Equals(unityRewardedId);
		else return false;
	}
	IEnumerator ShowUnityRewardedVideo()
	{
		HideAdmobBanner();
		//if (isAdaptiveBannerShowing)
		//	hideAdaptiveBanner();
		yield return constantDelay_Zero_Two;
		Advertisement.Show(unityRewardedId, this);
	}

	IEnumerator ShowUnityInterstitial()
	{
		HideAdmobBanner();
		yield return constantDelay_Zero_Two;
		Advertisement.Show(unityInterstitalId, this);


	}
	#endregion

	#region ShowAds
	public void ShowInterstitialAds()
	{
		if (Application.internetReachability == NetworkReachability.NotReachable || (PlayerPrefs.GetInt("NoAds") ==1) || !canShowAds) return;
		if (adPriority == AdPriority.Admob)
		{
			if (isadmobInterstitialLoaded())
			{
				ShowAdmobInterstitial();
			}
			else
			{
				if (isUnityInterstitialReady())
					StartCoroutine(ShowUnityInterstitial());
				else
				{
					StartCoroutine(DelayRequestAdmobInterstitial(.2f));
					StartCoroutine(delayLoadUnityInterstitial(.6f));
					
				}


			}
		}
		else if (adPriority == AdPriority.Unity)
		{

			if (isUnityInterstitialReady())
			{
				StartCoroutine(ShowUnityInterstitial());
			}
			else
			{
				if (isadmobInterstitialLoaded())
				{
					ShowAdmobInterstitial();
				}
				else
				{
					StartCoroutine(DelayRequestAdmobInterstitial(.6f));
					StartCoroutine(delayLoadUnityInterstitial(.2f));
				}
			}
		}
	}
	public void ShowRewardedVideo(Action rewardedFuction)
	{

		if (Application.internetReachability == NetworkReachability.NotReachable) return;
        if (AdsForAboveThree)
        {
			//binding
			onRewardedVideoAdsCompleteAction = null;
			onRewardedVideoAdsCompleteAction = rewardedFuction;



			if (adPriority == AdPriority.Unity)
			{
				if (isUnityVideoReady())
					StartCoroutine(ShowUnityRewardedVideo());
				else if (IsAdmobRewardedVideoAvailable())
					StartCoroutine(ShowAdmobRewardedVideo());

				else
				{

					StartCoroutine(delayLoadAdmobVideo(.6f));
					StartCoroutine(delayLoadUnityVideo(.3f));
					

				}


			}
			else if (adPriority == AdPriority.Admob)
			{
				if (IsAdmobRewardedVideoAvailable())
					StartCoroutine(ShowAdmobRewardedVideo());
				else
				{
					if (isUnityVideoReady())
						StartCoroutine(ShowUnityRewardedVideo());
                    else
                    {
						StartCoroutine(delayLoadAdmobVideo(.3f));
						StartCoroutine(delayLoadUnityVideo(.6f));
					}
					
				}
			}
		}
  //      else if (AdsForThreeGB)
		//{
		//	if (isRewardedInterstitialAdReady())
  //          {
		//		isShowBannerAfterRewardedInter = true;
		//		ShowRewardedInterstitialAd(rewardedFuction, unloadingActionsOnSKip);
		//	}
				
  //      }
		else
        {
			Debug.Log("no video ad to show");
        }
		//start working form here   call function of earn rerward in parameter as action
	}
	void unloadingActionsOnSKip()
    {
		StartCoroutine(unloadRewardedInterstitialAction(0f));
	}

	IEnumerator unloadRewardedInterstitialAction(float time)
    {
		yield return new WaitForSecondsRealtime(time);
		if(onRewardedInterstitialCompleteAction!=null)
		onRewardedInterstitialCompleteAction = null;
		if(onRewardedInterstitialSkipAction != null)
		onRewardedInterstitialSkipAction = null;
	}
	#endregion


	#region Adaptive
	//private void RequestAdaptiveBanner()
	//{
	//	if (Application.internetReachability == NetworkReachability.NotReachable) return;
	//	// These ad units are configured to always serve test ads.



	//	// Clean up banner ad before creating a new one.
	//	if (this.adaptiveBannerView == null)
	//	{
	//		AdSize adaptiveSize;
	//		if (_BannerType == adaptiveBanner.Adaptive)
	//			adaptiveSize = new AdSize(300, 250);
	//		else adaptiveSize = new AdSize(320, 50);



	//		this.adaptiveBannerView = new BannerView(admobAdaptiveId, adaptiveSize, adaptiveBannerPosition);

	//		// Register for ad events.
	//		this.adaptiveBannerView.OnAdLoaded += this.HandleAdLoaded;
	//		this.adaptiveBannerView.OnAdFailedToLoad += this.HandleAdFailedToLoad;
	//		this.adaptiveBannerView.OnAdOpening += this.HandleAdOpened;
	//		this.adaptiveBannerView.OnAdClosed += this.HandleAdClosed;

	//		AdRequest adRequest = new AdRequest.Builder().Build();

	//		// Load a banner ad.
	//		this.adaptiveBannerView.LoadAd(adRequest);

	//		isAdaptiveBannerShowing = true;
	//	}
	//	else
	//	{

	//	}


	//}

	//public void showAdaptiveBanner()
	//{
	//	if (Application.internetReachability == NetworkReachability.NotReachable) return;
		
	//	if (this.adaptiveBannerView != null && !isAdaptiveBannerShowing)
	//	{
	//		this.adaptiveBannerView.Show();
	//		isAdaptiveBannerShowing = true;
	//		adaptiveBannerScreen.SetActive(true);
	//	}
	//	else
	//	{
	//		RequestAdaptiveBanner();
	//		adaptiveBannerScreen.SetActive(true);
	//	}

	//}
	//public void hideAdaptiveBanner()
	//{
	//	adaptiveBannerScreen.SetActive(false);
	//	if (Application.internetReachability == NetworkReachability.NotReachable) return;
	//	if (this.adaptiveBannerView != null && isAdaptiveBannerShowing)
	//	{
	//		this.adaptiveBannerView.Hide();
	//		isAdaptiveBannerShowing = false;
	//	}
	//}

	//public void HandleAdLoaded(object sender, EventArgs args)
	//{
	//	print("HandleAdLoaded event received");
	//	//print(String.Format("Ad Height: {0}, width: {1}",
	//	//	this.adaptiveBannerView.GetHeightInPixels(),
	//	//	this.adaptiveBannerView.GetWidthInPixels()));
	//}
	//int adaptiveCount;
	//public void HandleAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
	//{
	//	print("HandleFailedToReceiveAd event received with message: ");
	//	if (adaptiveCount < 1)
	//	{
	//		adaptiveCount++;
	//		StartCoroutine(delayRquestAdaptiveOnCallBacks());
	//	}
	//	else adaptiveBannerScreen.SetActive(false);
	//}

	//public void HandleAdOpened(object sender, EventArgs args)
	//{
	//	print("HandleAdOpened event received");
	//}

	//public void HandleAdClosed(object sender, EventArgs args)
	//{
	//	print("HandleAdClosed event received");
	//	StartCoroutine(delayRquestAdaptiveOnCallBacks());
	//}

	//public void HandleAdLeftApplication(object sender, EventArgs args)
	//{
	//	print("HandleAdLeftApplication event received");
	//}

	//IEnumerator delayRquestAdaptiveOnCallBacks()
	//{
	//	yield return new WaitForSecondsRealtime(.1f);
	//	if (adaptiveBannerView != null)
	//	{
	//		adaptiveBannerView.Destroy();
	//		adaptiveBannerView = null;
	//	}
	//	yield return new WaitForSecondsRealtime(.1f);
	//	if(isAdaptiveBannerShowing)
	//	RequestAdaptiveBanner();
	//}
	#endregion


	#region rewardedInterstitial
	//bool isRewardedInterstitialGranted = false;
	//int rewardedInterstitialRequestCount;
	//private void adLoadCallback(RewardedInterstitialAd ad, AdFailedToLoadEventArgs error)
	//{
	//	if (error == null)
	//	{
	//		rewardedInterstitialAd = ad;
	//	}
	//}

	//private void userEarnedRewardCallback(Reward reward)
	//{
		
	//	if (onRewardedInterstitialCompleteAction != null)
	//	{
			
	//		isRewardedInterstitialGranted = true;
	//		StartCoroutine(onRewardedInterstitialCompleteEventDelayed(.1f));
		
	//	}
	//	// TODO: Reward the user.
	//}

	//IEnumerator onRewardedInterstitialCompleteEventDelayed(float time)
	//{
	//	yield return new WaitForSecondsRealtime(time);
	//	if (onRewardedInterstitialCompleteAction != null)
	//	{
	//		onRewardedInterstitialCompleteAction.Invoke();
	//		onRewardedInterstitialCompleteAction = null;
	//		if (onRewardedInterstitialSkipAction != null)
	//			onRewardedInterstitialSkipAction = null;
	//	}
 //       if (isShowBannerAfterRewardedInter)
 //       {
	//		StartCoroutine(ShowBannerRountine());
			
	//		isShowBannerAfterRewardedInter = false;

	//	}
	//	StartCoroutine(unloadRewardedInterstitialAction(0.2f));
	//	StartCoroutine(RewardedInterstitialDelayLoad(.6f));
		

	//}
	//IEnumerator onRewardedInterstitialSkippedEventDelayed(float time)
	//{
	//	yield return new WaitForSecondsRealtime(time);
	//	if (onRewardedInterstitialSkipAction != null)
 //       {
	//		onRewardedInterstitialSkipAction.Invoke();
	//		onRewardedInterstitialSkipAction = null;
	//		if (onRewardedInterstitialCompleteAction != null)
	//			onRewardedInterstitialCompleteAction = null;

	//	}
	//	StartCoroutine(unloadRewardedInterstitialAction(0.2f));
	//	StartCoroutine(RewardedInterstitialDelayLoad(.6f));
	//}

	//private void HandleAdFailedToPresent(object sender, AdErrorEventArgs args)
	//{
	//	rewardedInterstitialRequestCount++;
	//	if (rewardedInterstitialRequestCount < 4)
	//	{
			
	//	StartCoroutine(onRewardedInterstitialSkippedEventDelayed(.2f));
	//	}

		
	//	isRewardedInterstitialGranted = false;
	//	isRewardedInterstitialShown = false;
	//	print("Rewarded interstitial ad has failed to present.");
	//}
	//private void HandleAdDidPresent(object sender, EventArgs args)
	//{
	//	//RequestAdmobRewardedInterstitial();
	//	print("Rewarded interstitial ad has presented.");
	//}
	//private void HandleAdDidDismiss(object sender, EventArgs args)
	//{
		
	//	if (onRewardedInterstitialSkipAction != null)
	//	{

	//		StartCoroutine(onRewardedInterstitialSkippedEventDelayed(.2f));
			
			
	//	}
	//	isRewardedInterstitialGranted = false;
	//	isRewardedInterstitialShown = false;

 //   }
	//private void HandleRewardedPaidEvent(object sender, AdValueEventArgs args)
	//{
	//	print("Rewarded interstitial ad has received a paid event.");
	//}


	//IEnumerator RewardedInterstitialDelayLoad(float time)
 //   {
	//	yield return new WaitForSecondsRealtime(time);
	//	RequestAdmobRewardedInterstitial();


	//}

	//public void RequestAdmobRewardedInterstitial()
	//{
	//	if (!AdsForThreeGB) return;
	//	if (rewardedInterstitialAd != null)
	//	{
	//		rewardedInterstitialAd.Destroy();
	//		rewardedInterstitialAd = null;
	//	}
	//	StartCoroutine(delayLoadRewardedInterstitial());
		
	//}

	//IEnumerator delayLoadRewardedInterstitial()
 //   {
	//	yield return constantDelay_Zero_Two;
	//	// Create an empty ad request.
	//	AdRequest request = new AdRequest.Builder().Build();
	//	// Load the rewarded ad with the request.
	//	RewardedInterstitialAd.LoadAd(admobRewardedInterstitalId, request, adLoadCallback);
	//}


	//public void ShowRewardedInterstitialAd(Action RewardFuction,Action skipFunction)
	//{
	//	HideAdmobBanner();
	//	if (isAdaptiveBannerShowing)
	//		hideAdaptiveBanner();
	//	if (onRewardedInterstitialCompleteAction != null)
	//		onRewardedInterstitialCompleteAction = null;
	//	if (onRewardedInterstitialSkipAction != null)
	//		onRewardedInterstitialSkipAction = null;
	//	onRewardedInterstitialCompleteAction = RewardFuction;
	//	onRewardedInterstitialSkipAction = skipFunction;
	//	StartCoroutine(delayShowRewardInterstitial());
		
	//}
	//IEnumerator delayShowRewardInterstitial()
 //   {
	//	yield return constantDelay_Zero_Two;
	//	if (rewardedInterstitialAd != null)
	//	{
	//		isRewardedInterstitialGranted = false;
	//		rewardedInterstitialAd.Show(userEarnedRewardCallback);
	//		rewardedInterstitialAd.OnAdFailedToPresentFullScreenContent += HandleAdFailedToPresent;
	//		rewardedInterstitialAd.OnAdDidPresentFullScreenContent += HandleAdDidPresent;
	//		rewardedInterstitialAd.OnAdDidDismissFullScreenContent += HandleAdDidDismiss;
	//		rewardedInterstitialAd.OnPaidEvent += HandleRewardedPaidEvent;
	//	}
	//}

	//public bool isRewardedInterstitialAdReady()
	//{

	//	if (Application.internetReachability == NetworkReachability.NotReachable) return false;
	//	if (rewardedInterstitialAd != null)
	//	{
	//		return true;
	//	}
	//	else
	//	{
	//		return false;
	//	}
	//}
	#endregion
	#region appOpen Ad
	public bool IsOpenAppAdAvailable
	{
		get
		{
			return admobOpenAppAd != null;
		}
	}
	public void LoadOpenAppAd()
	{
        //if (!AdsRemoteConfigManager.Instance.isShowAppOpen() || AdsRemoteConfigManager.Instance.IsHideAd()) return;
        AdRequest request = new AdRequest.Builder().Build();
        // Load an app open ad for portrait orientation
        AppOpenAd.LoadAd(admobOpenAppAdsId, appOpenOrientation, request, ((appOpenAd, error) =>
        {
            if (error != null)
            {
                // Handle the error.
                //Debug.LogFormat("Failed to load the ad. (reason: {0})", error.LoadAdError.GetMessage());
                return;
            }
            // App open ad is loaded.
            admobOpenAppAd = appOpenAd;
        }));
    }
	public void ShowOpenAppAdIfAvailable()
	{
		if ((PlayerPrefs.GetInt("NoAds") == 1)) return;
        //if (!AdsRemoteConfigManager.Instance.isShowAppOpen()|| AdsRemoteConfigManager.Instance.IsHideAd()) return;
        if (!IsOpenAppAdAvailable || isShowingAd)
        {
            return;
        }
        admobOpenAppAd.OnAdDidDismissFullScreenContent += HandleAdDidDismissFullScreenContent;
        admobOpenAppAd.OnAdFailedToPresentFullScreenContent += HandleAdFailedToPresentFullScreenContent;
        admobOpenAppAd.OnAdDidPresentFullScreenContent += HandleAdDidPresentFullScreenContent;
        admobOpenAppAd.OnAdDidRecordImpression += HandleAdDidRecordImpression;
        admobOpenAppAd.OnPaidEvent += HandlePaidEvent;

        admobOpenAppAd.Show();
    }
	private void HandleAdDidDismissFullScreenContent(object sender, EventArgs args)
	{
		// Set the ad to null to indicate that AppOpenAdManager no longer has another ad to show.
		admobOpenAppAd = null;
		isShowingAd = false;
	}
	int appOpenAdLoadCount;
	private void HandleAdFailedToPresentFullScreenContent(object sender, AdErrorEventArgs args)
	{
		//Debug.LogFormat("Failed to present the ad (reason: {0})", args.AdError.GetMessage());
		// Set the ad to null to indicate that AppOpenAdManager no longer has another ad to show.
		admobOpenAppAd = null;
        if (appOpenAdLoadCount < 3)
        {
			StartCoroutine(delayLoadAppOpen());
        }
    }
	IEnumerator delayLoadAppOpen()
    {
		yield return constantDelay_Zero_Two;
		LoadOpenAppAd();
		appOpenAdLoadCount++;

	}
	private void HandleAdDidPresentFullScreenContent(object sender, EventArgs args)
	{
		//Debug.Log("Displayed app open ad");
		isShowingAd = true;
	}
	private void HandleAdDidRecordImpression(object sender, EventArgs args)
	{
		//Debug.Log("Recorded ad impression");
	}
	private void HandlePaidEvent(object sender, AdValueEventArgs args)
	{
		//Debug.LogFormat("Received paid event. (currency: {0}, value: {1}",
		//		args.AdValue.CurrencyCode, args.AdValue.Value);
	}


	#endregion
}
