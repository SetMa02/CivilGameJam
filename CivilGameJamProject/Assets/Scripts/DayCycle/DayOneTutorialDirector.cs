using System.Collections;
using UnityEngine;
using CivilGame.UI;
using CivilGame.Lighthouse;
using CivilGame.Book;
using CivilGame.Phone;
using CivilGame.Threats;
using CivilGame.Localization;

namespace CivilGame.DayCycle
{
    public class DayOneTutorialDirector : MonoBehaviour
    {
        public static DayOneTutorialDirector Instance { get; private set; }

        public const string TutorialPrefKey = "TutorialCompleted";

        private enum TutorialState
        {
            None,
            Intro1,
            Intro2,
            Intro3,
            WaitLeftWindow,
            LeftWindowReaction,
            WaitRightWindow,
            RightWindowReaction,
            WaitBook,
            BookReaction,
            WaitPhoneDispatch,
            DispatchReaction,
            WaitSleep,
            Day2MorningFarewell,
            Completed
        }

        [Header("Scene References")]
        [SerializeField] private LighthouseRoomController _roomCtrl;
        [SerializeField] private BookUIController _bookCtrl;
        [SerializeField] private PhoneUIController _phoneCtrl;
        [SerializeField] private DayCycleManager _dayCycle;

        [Header("Lucy World Presence")]
        [SerializeField] private GameObject _lucyStandPoint;
        [SerializeField] private SpriteRenderer _lucySpriteRenderer;
        [SerializeField] private Sprite _defaultLucySprite;

        [Header("Camera Focus on Lucy")]
        [SerializeField] private Vector3 _cameraDefaultPos = new Vector3(0f, 0f, -11.44f);
        [SerializeField] private float _cameraDefaultFov = 52f;
        [SerializeField] private Vector3 _cameraZoomedPos = new Vector3(0.29f, -1.1f, -10.2f);
        [SerializeField] private float _cameraZoomedFov = 44f;
        [SerializeField] private float _cameraTransitionDuration = 0.55f;

        [Header("Tutorial Dialogue Customization (Day 1 & Day 2)")]
        [Tooltip("Replica 1: Welcome aboard, new ranger...")]
        [SerializeField] private DialogueLineData _line1 = new DialogueLineData();
        [Tooltip("Replica 2: Our job is to keep an eye on nature...")]
        [SerializeField] private DialogueLineData _line2 = new DialogueLineData();
        [Tooltip("Replica 3: You can click on windows... check left window")]
        [SerializeField] private DialogueLineData _line3 = new DialogueLineData();
        [Tooltip("Replica 4: My-my! We have a little situation here... check right")]
        [SerializeField] private DialogueLineData _line4 = new DialogueLineData();
        [Tooltip("Replica 5: Looks calm... drought on left, open book")]
        [SerializeField] private DialogueLineData _line5 = new DialogueLineData();
        [Tooltip("Replica 6: As it says here, report by phone")]
        [SerializeField] private DialogueLineData _line6 = new DialogueLineData();
        [Tooltip("Replica 7: Nice job, pal! Shift is over, rest / shop")]
        [SerializeField] private DialogueLineData _line7 = new DialogueLineData();
        [Tooltip("Replica 8 (Day 2 morning): Hello-hello! Leaving for 7 days...")]
        [SerializeField] private DialogueLineData _line8 = new DialogueLineData();

        [Header("Tutorial State")]
        [SerializeField] private TutorialState _currentState = TutorialState.None;
        [SerializeField] private bool _isTutorialActive = false;

        public bool IsTutorialActive => _isTutorialActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _isTutorialActive = PlayerPrefs.GetInt(TutorialPrefKey, 0) == 0;

            FindReferences();
        }

        private void FindReferences()
        {
            if (_roomCtrl == null) _roomCtrl = FindAnyObjectByType<LighthouseRoomController>(FindObjectsInactive.Include);
            if (_bookCtrl == null) _bookCtrl = FindAnyObjectByType<BookUIController>(FindObjectsInactive.Include);
            if (_phoneCtrl == null) _phoneCtrl = FindAnyObjectByType<PhoneUIController>(FindObjectsInactive.Include);
            if (_dayCycle == null) _dayCycle = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>(FindObjectsInactive.Include);

            if (_lucyStandPoint == null)
            {
                var lspObj = GameObject.Find("LucyStandPoint");
                if (lspObj != null) _lucyStandPoint = lspObj;
            }

            if (_lucyStandPoint != null && _lucySpriteRenderer == null)
            {
                _lucySpriteRenderer = _lucyStandPoint.GetComponent<SpriteRenderer>();
            }

            if (_defaultLucySprite == null && _lucySpriteRenderer != null)
            {
                _defaultLucySprite = _lucySpriteRenderer.sprite;
            }
        }

        private void Start()
        {
            FindReferences();

            if (_isTutorialActive)
            {
                if (_lucyStandPoint != null) _lucyStandPoint.SetActive(true);
                StartCoroutine(RunDayOneSequenceRoutine());
            }
            else
            {
                if (_lucyStandPoint != null) _lucyStandPoint.SetActive(false);
                _currentState = TutorialState.Completed;
            }
        }

        private void SetLucyWorldSprite(Sprite customSprite)
        {
            if (_lucySpriteRenderer == null && _lucyStandPoint != null)
            {
                _lucySpriteRenderer = _lucyStandPoint.GetComponent<SpriteRenderer>();
            }

            if (_lucySpriteRenderer != null)
            {
                _lucySpriteRenderer.sprite = (customSprite != null) ? customSprite : _defaultLucySprite;
            }
        }

        private IEnumerator ZoomCameraToLucyRoutine()
        {
            yield return StartCoroutine(TransitionCameraRoutine(_cameraZoomedPos, _cameraZoomedFov, _cameraTransitionDuration));
        }

        private IEnumerator ResetCameraZoomRoutine()
        {
            yield return StartCoroutine(TransitionCameraRoutine(_cameraDefaultPos, _cameraDefaultFov, _cameraTransitionDuration));
        }

        private IEnumerator TransitionCameraRoutine(Vector3 targetPos, float targetFov, float duration)
        {
            var cam = Camera.main;
            if (cam == null) yield break;

            var parallaxCtrl = CivilGame.CameraEffects.WindowParallaxController.Instance != null 
                ? CivilGame.CameraEffects.WindowParallaxController.Instance 
                : FindAnyObjectByType<CivilGame.CameraEffects.WindowParallaxController>();

            if (parallaxCtrl != null)
            {
                parallaxCtrl.IsLockedBySequence = true;
            }

            Vector3 startPos = cam.transform.position;
            float startFov = cam.fieldOfView;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = t * t * (3f - 2f * t);

                cam.transform.position = Vector3.Lerp(startPos, targetPos, ease);
                cam.fieldOfView = Mathf.Lerp(startFov, targetFov, ease);
                yield return null;
            }

            cam.transform.position = targetPos;
            cam.fieldOfView = targetFov;

            if (parallaxCtrl != null && targetPos == _cameraDefaultPos)
            {
                parallaxCtrl.IsLockedBySequence = false;
            }
        }

        private IEnumerator RunDayOneSequenceRoutine()
        {
            FindReferences();

            // Wait brief moment for scene to initialize
            yield return new WaitForSeconds(0.6f);

            // Setup scripted Day 1: Clear any random threats, spawn only scripted Drought on Field
            if (_dayCycle != null)
            {
                var field = _dayCycle.GetZoneModel(ZoneType.Field);
                if (field != null)
                {
                    field.activeThreat = new ActiveThreat(new ThreatDefinition(ThreatType.Drought, "Drought", 3, ZoneType.Field), 1);
                }
                var forest = _dayCycle.GetZoneModel(ZoneType.Forest);
                if (forest != null) forest.activeThreat = null;
                var sea = _dayCycle.GetZoneModel(ZoneType.Sea);
                if (sea != null) sea.activeThreat = null;
                var mt = _dayCycle.GetZoneModel(ZoneType.Mountains);
                if (mt != null) mt.activeThreat = null;

                _dayCycle.UpdateUIAndViews();
            }

            // --- REPLICA 1 ---
            _currentState = TutorialState.Intro1;
            SetLucyWorldSprite(_line1.portrait);
            yield return StartCoroutine(ZoomCameraToLucyRoutine());

            bool advance = false;
            GuideDialogueUI.Instance.ShowDialogue(
                LocalizationManager.Get("guide.name"),
                LocalizationManager.Get("guide.line1"),
                () => advance = true,
                _line1,
                showUiPortrait: false);
            while (!advance) yield return null;

            // --- REPLICA 2 ---
            _currentState = TutorialState.Intro2;
            SetLucyWorldSprite(_line2.portrait);
            advance = false;
            GuideDialogueUI.Instance.ShowDialogue(
                LocalizationManager.Get("guide.name"),
                LocalizationManager.Get("guide.line2"),
                () => advance = true,
                _line2,
                showUiPortrait: false);
            while (!advance) yield return null;

            // --- REPLICA 3 (Prompt Left Window) ---
            _currentState = TutorialState.Intro3;
            SetLucyWorldSprite(_line3.portrait);
            advance = false;
            GuideDialogueUI.Instance.ShowDialogue(
                LocalizationManager.Get("guide.name"),
                LocalizationManager.Get("guide.line3"),
                () => advance = true,
                _line3,
                showUiPortrait: false);
            while (!advance) yield return null;

            GuideDialogueUI.Instance.HideDialogue();

            // Zoom camera back out so player sees window arrows and hall
            yield return StartCoroutine(ResetCameraZoomRoutine());

            // --- WAIT FOR PLAYER TO ENTER LEFT WINDOW ---
            _currentState = TutorialState.WaitLeftWindow;
            while (_roomCtrl == null || _roomCtrl.CurrentView != LighthouseView.LeftWindow)
            {
                yield return null;
            }

            // Small pause so player sees the left window view
            yield return new WaitForSeconds(0.8f);

            // --- REPLICA 4 (React to Left Window & Prompt Right Window) ---
            _currentState = TutorialState.LeftWindowReaction;
            SetLucyWorldSprite(_line4.portrait);
            advance = false;
            GuideDialogueUI.Instance.ShowDialogue(
                LocalizationManager.Get("guide.name"),
                LocalizationManager.Get("guide.line4"),
                () => advance = true,
                _line4,
                showUiPortrait: true);
            while (!advance) yield return null;

            GuideDialogueUI.Instance.HideDialogue();

            // --- WAIT FOR PLAYER TO ENTER RIGHT WINDOW ---
            _currentState = TutorialState.WaitRightWindow;
            while (_roomCtrl == null || _roomCtrl.CurrentView != LighthouseView.RightWindow)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.8f);

            // --- REPLICA 5 (React to Right Window & Prompt Book) ---
            _currentState = TutorialState.RightWindowReaction;
            SetLucyWorldSprite(_line5.portrait);
            advance = false;
            GuideDialogueUI.Instance.ShowDialogue(
                LocalizationManager.Get("guide.name"),
                LocalizationManager.Get("guide.line5"),
                () => advance = true,
                _line5,
                showUiPortrait: true);
            while (!advance) yield return null;

            GuideDialogueUI.Instance.HideDialogue();

            // Safety delay so dialogue advance click does not bleed into objects
            yield return new WaitForSeconds(0.2f);

            // --- WAIT FOR PLAYER TO OPEN BOOK AND THEN CLOSE IT ---
            _currentState = TutorialState.WaitBook;
            FindReferences();

            bool bookWasOpened = false;
            System.Action onOpened = () => bookWasOpened = true;
            if (_bookCtrl != null)
            {
                _bookCtrl.OnBookOpened += onOpened;
            }

            // 1. Wait until book is opened
            while (!bookWasOpened && (_bookCtrl == null || !_bookCtrl.IsOpen))
            {
                yield return null;
            }

            if (_bookCtrl != null)
            {
                _bookCtrl.OnBookOpened -= onOpened;
            }

            Debug.Log("[Tutorial] Book was opened by player. Now waiting for player to close book...");

            // 2. Wait until book is closed
            while (_bookCtrl != null && _bookCtrl.IsOpen)
            {
                yield return null;
            }

            Debug.Log("[Tutorial] Book closed. Proceeding to phone prompt...");
            yield return new WaitForSeconds(0.3f);

            // --- REPLICA 6 (Prompt Phone Call) ---
            _currentState = TutorialState.BookReaction;
            SetLucyWorldSprite(_line6.portrait);
            yield return StartCoroutine(ZoomCameraToLucyRoutine());

            advance = false;
            GuideDialogueUI.Instance.ShowDialogue(
                LocalizationManager.Get("guide.name"),
                LocalizationManager.Get("guide.line6"),
                () => advance = true,
                _line6,
                showUiPortrait: false);
            while (!advance) yield return null;

            GuideDialogueUI.Instance.HideDialogue();

            // Zoom camera back out so player can see and click phone
            yield return StartCoroutine(ResetCameraZoomRoutine());

            // Safety delay so dialogue advance click does not bleed into objects
            yield return new WaitForSeconds(0.2f);

            // --- WAIT FOR PHONE DISPATCH ---
            _currentState = TutorialState.WaitPhoneDispatch;
            FindReferences();

            bool dispatched = false;
            System.Action<ZoneType, SquadType> onDispatch = (z, s) => dispatched = true;
            if (_phoneCtrl != null)
            {
                _phoneCtrl.OnSquadDispatchedSuccess += onDispatch;
            }

            while (!dispatched && (_dayCycle == null || !_dayCycle.HasDispatchedToday))
            {
                yield return null;
            }

            if (_phoneCtrl != null)
            {
                _phoneCtrl.OnSquadDispatchedSuccess -= onDispatch;
            }

            Debug.Log("[Tutorial] Squad dispatched. Waiting for phone UI to close...");

            // Wait until phone UI is closed so dialogue doesn't overlap
            while (_phoneCtrl != null && _phoneCtrl.IsOpen)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            // --- REPLICA 7 (Nice job & Nighty-Night) ---
            _currentState = TutorialState.DispatchReaction;
            SetLucyWorldSprite(_line7.portrait);
            yield return StartCoroutine(ZoomCameraToLucyRoutine());

            advance = false;
            GuideDialogueUI.Instance.ShowDialogue(
                LocalizationManager.Get("guide.name"),
                LocalizationManager.Get("guide.line7"),
                () => advance = true,
                _line7,
                showUiPortrait: false);
            while (!advance) yield return null;

            GuideDialogueUI.Instance.HideDialogue();

            // Zoom camera back out so player can click bed to sleep
            yield return StartCoroutine(ResetCameraZoomRoutine());

            // --- WAIT FOR BED SLEEP TO START ---
            _currentState = TutorialState.WaitSleep;
            while (_dayCycle == null || _dayCycle.CurrentDay < 2)
            {
                yield return null;
            }

            // --- DAY 2 MORNING: REPLICA 8 (Farewell for 7 days) ---
            yield return new WaitForSeconds(1.2f);

            _currentState = TutorialState.Day2MorningFarewell;
            SetLucyWorldSprite(_line8.portrait);
            yield return StartCoroutine(ZoomCameraToLucyRoutine());

            advance = false;
            GuideDialogueUI.Instance.ShowDialogue(
                LocalizationManager.Get("guide.name"),
                LocalizationManager.Get("guide.line8"),
                () => advance = true,
                _line8,
                showUiPortrait: false);
            while (!advance) yield return null;

            GuideDialogueUI.Instance.HideDialogue();

            // Zoom back out and Lucy leaves
            yield return StartCoroutine(ResetCameraZoomRoutine());
            if (_lucyStandPoint != null)
            {
                _lucyStandPoint.SetActive(false);
            }

            // Tutorial successfully completed!
            PlayerPrefs.SetInt(TutorialPrefKey, 1);
            PlayerPrefs.Save();
            _isTutorialActive = false;
            _currentState = TutorialState.Completed;
            Debug.Log("[Tutorial] Day 1 Tutorial fully completed. Standard gameplay active!");
        }

        public bool CanSleepNow()
        {
            if (!_isTutorialActive) return true;
            // During Day 1 tutorial, player can only sleep after dispatching
            return _currentState == TutorialState.WaitSleep || _currentState == TutorialState.DispatchReaction;
        }
    }
}
