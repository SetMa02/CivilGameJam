using System.Collections;
using UnityEngine;
using TMPro;

public class MoneySystem : MonoBehaviour
{
    private static MoneySystem _instance;
    public static MoneySystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<MoneySystem>();
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    [Header("Balance")]
    public int money = 500; // Starting money for testing and gameplay
    public int moneyPerClick = 100;
    public int rewardPerResolvedThreat = 200;

    [Header("UI References")]
    public TMP_Text moneyText;
    public TMP_Text floatingDeltaText;
    public RectTransform moneyContainer;

    private Coroutine _animateCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateMoneyText();
        if (floatingDeltaText != null)
        {
            floatingDeltaText.gameObject.SetActive(false);
        }
    }

    public void AddMoney()
    {
        AddMoneyAmount(moneyPerClick);
    }

    public void AddMoneyAmount(int amount)
    {
        if (amount <= 0) return;
        money += amount;
        UpdateMoneyText();
        PlayRewardAnimation(amount);
    }

    public bool SpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            UpdateMoneyText();
            PlaySpendAnimation(amount);
            return true;
        }
        return false;
    }

    private void UpdateMoneyText()
    {
        if (moneyText != null)
        {
            moneyText.text = "$" + money;
        }
    }

    private void PlayRewardAnimation(int delta)
    {
        if (_animateCoroutine != null) StopCoroutine(_animateCoroutine);
        _animateCoroutine = StartCoroutine(AnimateMoneyDeltaRoutine(delta, true));
    }

    private void PlaySpendAnimation(int delta)
    {
        if (_animateCoroutine != null) StopCoroutine(_animateCoroutine);
        _animateCoroutine = StartCoroutine(AnimateMoneyDeltaRoutine(delta, false));
    }

    private IEnumerator AnimateMoneyDeltaRoutine(int delta, bool isPositive)
    {
        // Bounce money container
        if (moneyContainer != null)
        {
            Vector3 originalScale = Vector3.one;
            float punch = isPositive ? 1.25f : 0.9f;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float currentPunch = Mathf.Sin(progress * Mathf.PI) * (punch - 1f);
                moneyContainer.localScale = originalScale * (1f + currentPunch);
                yield return null;
            }
            moneyContainer.localScale = originalScale;
        }

        // Show floating text "+$200"
        if (floatingDeltaText != null)
        {
            floatingDeltaText.gameObject.SetActive(true);
            floatingDeltaText.text = isPositive ? $"+${delta}" : $"-${delta}";
            floatingDeltaText.color = isPositive ? new Color(0.29f, 0.87f, 0.5f, 1f) : new Color(0.97f, 0.44f, 0.44f, 1f);

            var rt = floatingDeltaText.GetComponent<RectTransform>();
            Vector2 startPos = new Vector2(0f, -25f);
            Vector2 endPos = new Vector2(0f, 15f);

            float duration = 0.8f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
                Color c = floatingDeltaText.color;
                c.a = Mathf.Lerp(1f, 0f, progress);
                floatingDeltaText.color = c;
                yield return null;
            }

            floatingDeltaText.gameObject.SetActive(false);
        }

        _animateCoroutine = null;
    }
}