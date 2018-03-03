using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class PlaneManager : MonoBehaviour
{
    #region PUBLIC_MEMBERS
    public PlaneFinderBehaviour m_PlaneFinder;
    public GameObject m_PlaneAugmentation;
    public UnityEngine.UI.Text m_TitleMode;
    public UnityEngine.UI.Text m_OnScreenMessage;
    public UnityEngine.UI.Button m_ResetButton;
    public CanvasGroup m_GroundReticle;
    #endregion // PUBLIC_MEMBERS


    #region PRIVATE_MEMBERS
    PositionalDeviceTracker positionalDeviceTracker;
    ContentPositioningBehaviour contentPositioningBehaviour;
    StateManager stateManager;
    Camera mainCamera;
    int AutomaticHitTestFrameCount;
    HitTestResult currentResult;
    float maxTime = 20;
    float minTime = 5;
    bool shouldSpawn;
    AudioSource audio;
    #endregion // PRIVATE_MEMBERS


    #region MONOBEHAVIOUR_METHODS

    void Start()
    {
        Debug.Log("Start() called.");
        VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);
        VuforiaARController.Instance.RegisterOnPauseCallback(OnVuforiaPaused);
        DeviceTrackerARController.Instance.RegisterTrackerStartedCallback(OnTrackerStarted);
        DeviceTrackerARController.Instance.RegisterDevicePoseStatusChangedCallback(OnDevicePoseStatusChanged);

        m_PlaneFinder.HitTestMode = HitTestMode.AUTOMATIC;

        m_ResetButton.interactable = false;

        shouldSpawn = true;

        audio = GetComponent<AudioSource>();

        mainCamera = Camera.main;

    }

    void LateUpdate()
    {
        if (AutomaticHitTestFrameCount == Time.frameCount)
        {
            // We got an automatic hit test this frame
            m_GroundReticle.alpha = 0; // hide the onscreen reticle
            m_OnScreenMessage.enabled = false; // hide the onscreen message
            SetSurfaceIndicatorVisible(true); // display the surface indicator
            CheckAndSpawn();
        }
        else
        {
            // No automatic hit test, so set alpha based on which plane mode is active
            m_GroundReticle.alpha = 1;

            m_OnScreenMessage.enabled = true;
            m_OnScreenMessage.text = "Point device towards ground";


            SetSurfaceIndicatorVisible(false);
        }
    }

    void OnDestroy()
    {
        Debug.Log("OnDestroy() called.");

        VuforiaARController.Instance.UnregisterVuforiaStartedCallback(OnVuforiaStarted);
        VuforiaARController.Instance.UnregisterOnPauseCallback(OnVuforiaPaused);
        DeviceTrackerARController.Instance.UnregisterTrackerStartedCallback(OnTrackerStarted);
        DeviceTrackerARController.Instance.UnregisterDevicePoseStatusChangedCallback(OnDevicePoseStatusChanged);
    }

    #endregion // MONOBEHAVIOUR_METHODS

    #region VUFORIA_CALLBACKS

    void OnVuforiaStarted()
    {
        Debug.Log("OnVuforiaStarted() called.");

        stateManager = TrackerManager.Instance.GetStateManager();
    }

    void OnVuforiaPaused(bool paused)
    {
        Debug.Log("OnVuforiaPaused(" + paused.ToString() + ") called.");

        if (paused)
            Reset();
    }

    #endregion // VUFORIA_CALLBACKS


    #region PRIVATE_METHODS

    void SetSurfaceIndicatorVisible(bool isVisible)
    {
        Renderer[] renderers = m_PlaneFinder.PlaneIndicator.GetComponentsInChildren<Renderer>(true);
        Canvas[] canvas = m_PlaneFinder.PlaneIndicator.GetComponentsInChildren<Canvas>(true);

        foreach (Canvas c in canvas)
            c.enabled = isVisible;

        foreach (Renderer r in renderers)
            r.enabled = isVisible;
    }

    private void DestroyAnchors()
    {
        IEnumerable<TrackableBehaviour> trackableBehaviours = stateManager.GetActiveTrackableBehaviours();

        string destroyed = "Destroying: ";

        foreach (TrackableBehaviour behaviour in trackableBehaviours)
        {
            if (behaviour is AnchorBehaviour)
            {
                // First determine which mode (Plane or MidAir) and then delete only the anchors for that mode
                // Leave the other mode's anchors intact
                // PlaneAnchor_<GUID>
                // Mid AirAnchor_<GUID>

                if (behaviour.Trackable.Name.Contains("PlaneAnchor"))
                {
                    destroyed +=
                        "\nGObj Name: " + behaviour.name +
                       "\nTrackable Name: " + behaviour.Trackable.Name +
                       "\nTrackable ID: " + behaviour.Trackable.ID +
                       "\nPosition: " + behaviour.transform.position.ToString();

                    stateManager.DestroyTrackableBehavioursForTrackable(behaviour.Trackable);
                    stateManager.ReassociateTrackables();
                }
            }
        }

        Debug.Log(destroyed);
    }

    private void RotateTowardCamera(GameObject augmentation)
    {
        var lookAtPosition = mainCamera.transform.position - augmentation.transform.position;
        lookAtPosition.y = 0;
        var rotation = Quaternion.LookRotation(lookAtPosition);
        augmentation.transform.rotation = rotation;
    }

    private void CheckAndSpawn() {
        Debug.Log("CheckAndSpawn " + shouldSpawn);
        float spawnTime = Random.Range(minTime, maxTime);
        if ( shouldSpawn ) {
            Debug.Log("CheckAndSpawn coroutines");
            shouldSpawn = false;
            StartCoroutine(WaitAndSpawn(spawnTime));
        }
    }

    private IEnumerator WaitAndSpawn(float waitTime)
    {
        Debug.Log("WaitAndSpawn " + waitTime);
        yield return new WaitForSeconds(waitTime);
        SpawnDroid();
    }

    private void SpawnDroid() {
        Debug.Log("SpawnDroid Setting Plane Augmentation to Active");

        if (positionalDeviceTracker != null && positionalDeviceTracker.IsActive)
        {
            DestroyAnchors();

            contentPositioningBehaviour = m_PlaneFinder.GetComponent<ContentPositioningBehaviour>();
            contentPositioningBehaviour.PositionContentAtPlaneAnchor(currentResult);
            m_ResetButton.interactable = true;

        }

        if (!m_PlaneAugmentation.activeInHierarchy)
        {
            Debug.Log("Setting Plane Augmentation to Active");
            // On initial run, unhide the augmentation
            m_PlaneAugmentation.SetActive(true);
        }

        Debug.Log("Positioning Plane Augmentation at: " + currentResult.Position);
        m_PlaneAugmentation.PositionAt(currentResult.Position);
        RotateTowardCamera(m_PlaneAugmentation);
    }

 

    #endregion // PRIVATE_METHODS

    #region PUBLIC_METHODS

    public void HandleAutomaticHitTest(HitTestResult result)
    {
        currentResult = result;

        AutomaticHitTestFrameCount = Time.frameCount;

    }

    public void HandleInteractiveHitTest(HitTestResult result)
    {
        Debug.Log("HandleInteractiveHitTest() called.");

        if (result == null)
        {
            Debug.LogError("Invalid hit test result!");
            return;
        }


        if (positionalDeviceTracker != null && positionalDeviceTracker.IsActive)
        {
            DestroyAnchors();

            contentPositioningBehaviour = m_PlaneFinder.GetComponent<ContentPositioningBehaviour>();
            contentPositioningBehaviour.PositionContentAtPlaneAnchor(result);
            m_ResetButton.interactable = true;

        }

        if (!m_PlaneAugmentation.activeInHierarchy)
        {
            Debug.Log("Setting Plane Augmentation to Active");
            // On initial run, unhide the augmentation
            m_PlaneAugmentation.SetActive(true);
        }

        Debug.Log("Positioning Plane Augmentation at: " + result.Position);
        m_PlaneAugmentation.PositionAt(result.Position);
        RotateTowardCamera(m_PlaneAugmentation);


    }

    public void Reset()
    {
        Debug.Log("Reset() called.");

        audio.Play();

        shouldSpawn = true;

        // reset augmentations
        m_PlaneAugmentation.transform.position = Vector3.zero;
        m_PlaneAugmentation.transform.localEulerAngles = Vector3.zero;
        m_PlaneAugmentation.SetActive(false);

        // reset buttons
        m_ResetButton.interactable = false;
    }

    #endregion // PUBLIC_METHODS


    #region DEVICE_TRACKER_CALLBACKS

    void OnTrackerStarted()
    {
        Debug.Log("OnTrackerStarted() called.");

        positionalDeviceTracker = TrackerManager.Instance.GetTracker<PositionalDeviceTracker>();

        if (positionalDeviceTracker != null)
        {
            if (!positionalDeviceTracker.IsActive)
                positionalDeviceTracker.Start();

            Debug.Log("PositionalDeviceTracker is Active?: " + positionalDeviceTracker.IsActive);
        }
    }

    void OnDevicePoseStatusChanged(TrackableBehaviour.Status status)
    {
        Debug.Log("OnDevicePoseStatusChanged(" + status.ToString() + ")");
    }

    #endregion // DEVICE_TRACKER_CALLBACK_METHODS
}
