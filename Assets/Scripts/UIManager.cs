using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UniRx;
using System;
using Cysharp.Threading.Tasks;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [Header("Resources UI")]
    [SerializeField] private TextMeshProUGUI wheatCountText;
    [SerializeField] private TextMeshProUGUI flourCountText;
    [SerializeField] private TextMeshProUGUI breadCountText;

    [Header("Building Header")]
    [SerializeField] private TextMeshProUGUI buildingNameText;

    [Header("Alert System")]
    [SerializeField] private GameObject alertPanel;
    [SerializeField] private TextMeshProUGUI alertText;
    [SerializeField] private float alertDuration = 2.5f;
    [SerializeField] private float alertFadeDuration = 0.5f;

    [Header("Common UI")]
    [SerializeField] private Button collectButton;

    [Header("Production Panel")]
    [SerializeField] private GameObject productionPanel;
    [SerializeField] private Button addProductionButton;
    [SerializeField] private Button removeProductionButton;
    [SerializeField] private TextMeshProUGUI productionCountText;
    [SerializeField] private TextMeshProUGUI productionTimeText;
    [SerializeField] private Image productResourceImage;
    [SerializeField] private TextMeshProUGUI storedAmountText;
    [SerializeField] private Slider productionProgressSlider;

    [Header("Info Slider")]
    [SerializeField] private GameObject infoSliderPanel;
    [SerializeField] private TextMeshProUGUI infoCountText;
    [SerializeField] private TextMeshProUGUI infoTimeText;
    [SerializeField] private Image infoResourceImage;
    [SerializeField] private Slider infoProgressSlider;
    [SerializeField] private TextMeshProUGUI infoStoredAmountText;

    [Header("Collection Animation")]
    [SerializeField] private Image collectionAnimationImage;
    [SerializeField] private GameObject collectionAnimationImageParent;
    [SerializeField] private Transform wheatUITransform;
    [SerializeField] private Transform flourUITransform;
    [SerializeField] private Transform breadUITransform;
    [SerializeField] private float animationDuration = 0.8f;
    [SerializeField] private int animationIconCount = 5;
    [SerializeField] private float animationDelay = 0.1f;

    private BuildingProduction selectedBuilding;
    private Building currentBuildingData;

    private float lastAlertTime = 0f;
    private const float ALERT_COOLDOWN = 3f;
    private string lastAlertMessage = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        GameManager.Instance.GetResourceObservable(ResourceType.Wheat)
            .Subscribe(count => UpdateResourceUI(ResourceType.Wheat, count));

        GameManager.Instance.GetResourceObservable(ResourceType.Flour)
            .Subscribe(count => UpdateResourceUI(ResourceType.Flour, count));

        GameManager.Instance.GetResourceObservable(ResourceType.Bread)
            .Subscribe(count => UpdateResourceUI(ResourceType.Bread, count));

        HideProductionPanel();
        HideInfoSlider();

        if (buildingNameText != null)
            buildingNameText.gameObject.SetActive(false);

        if (collectButton != null)
        {
            collectButton.onClick.AddListener(CollectProducts);
            collectButton.gameObject.SetActive(false);
        }

        if (addProductionButton != null)
            addProductionButton.onClick.AddListener(AddProductionOrder);

        if (removeProductionButton != null)
            removeProductionButton.onClick.AddListener(RemoveProductionOrder);

        if (alertPanel != null)
            alertPanel.SetActive(false);

        if (collectionAnimationImage != null)
            collectionAnimationImage.gameObject.SetActive(false);
    }

    private void UpdateResourceUI(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Wheat:
                if (wheatCountText != null) wheatCountText.text = amount.ToString();
                break;
            case ResourceType.Flour:
                if (flourCountText != null) flourCountText.text = amount.ToString();
                break;
            case ResourceType.Bread:
                if (breadCountText != null) breadCountText.text = amount.ToString();
                break;
        }
    }

    public void ShowBuildingUI(BuildingProduction building)
    {
        if (selectedBuilding != null)
        {
            selectedBuilding.onProductionUpdate.RemoveListener(UpdateProductionUI);
            selectedBuilding.onProductionUpdate.RemoveListener(UpdateInfoSlider);
        }

        selectedBuilding = building;
        currentBuildingData = building.buildingData;

        if (buildingNameText != null)
        {
            buildingNameText.text = currentBuildingData.buildingName;
            buildingNameText.gameObject.SetActive(true);
        }

        if (currentBuildingData.requiredAmount > 0)
        {
            HideInfoSlider();

            ShowProductionPanel();

            selectedBuilding.onProductionUpdate.AddListener(UpdateProductionUI);

            UpdateProductionUI();

            if (productResourceImage != null && currentBuildingData.producedResourceSprite != null)
            {
                productResourceImage.sprite = currentBuildingData.producedResourceSprite;
                productResourceImage.gameObject.SetActive(true);
            }
        }
        else
        {
            HideProductionPanel();
            ShowInfoSlider(building);

            selectedBuilding.onProductionUpdate.AddListener(UpdateInfoSlider);

            if (infoResourceImage != null && currentBuildingData.producedResourceSprite != null)
            {
                infoResourceImage.sprite = currentBuildingData.producedResourceSprite;
                infoResourceImage.gameObject.SetActive(true);
            }
        }

        UpdateCollectButton();
    }

    private void UpdateCollectButton()
    {
        if (collectButton != null && selectedBuilding != null)
        {
            int storedAmount = selectedBuilding.GetStoredAmount();
            collectButton.gameObject.SetActive(true);
            collectButton.interactable = storedAmount > 0;
        }
    }

    private void ShowProductionPanel()
    {
        if (productionPanel != null)
            productionPanel.SetActive(true);
    }

    public void HideProductionPanel()
    {
        if (productionPanel != null)
            productionPanel.SetActive(false);
    }

    private void ShowInfoSlider(BuildingProduction building)
    {
        if (infoSliderPanel != null)
        {
            infoSliderPanel.SetActive(true);

            if (infoStoredAmountText != null)
                infoStoredAmountText.text = building.GetStoredAmount().ToString();

            UpdateInfoSlider();
        }
        else
        {
            Debug.LogError("Info Slider Panel reference not set!");
        }
    }

    public void HideInfoSlider()
    {
        if (infoSliderPanel != null)
            infoSliderPanel.SetActive(false);
    }

    private void UpdateInfoSlider()
    {
        if (selectedBuilding == null || infoSliderPanel == null) return;

        int storedAmount = selectedBuilding.GetStoredAmount();
        int capacity = selectedBuilding.buildingData.capacity;

        if (infoCountText != null)
            infoCountText.text = $"{storedAmount}/{capacity}";

        if (infoStoredAmountText != null)
            infoStoredAmountText.text = storedAmount.ToString();

        float remainingTime = selectedBuilding.GetProductionTimeRemaining();
        bool isProducing = selectedBuilding.IsProducing();

        if (infoProgressSlider != null)
        {
            infoProgressSlider.gameObject.SetActive(true);

            if (isProducing && remainingTime > 0)
            {
                float progress = 1 - (remainingTime / selectedBuilding.buildingData.productionTime);
                infoProgressSlider.value = progress;
            }
            else
            {
                infoProgressSlider.value = 0;
            }
        }

        if (infoTimeText != null)
        {
            if (isProducing && remainingTime > 0)
            {
                infoTimeText.text = $"{remainingTime:F0} sn";
            }
            else if (storedAmount >= capacity)
            {
                infoTimeText.text = "FULL";
            }
            else if (storedAmount == 0 && !isProducing)
            {
                infoTimeText.text = "IDLE";
            }
            else
            {
                infoTimeText.text = "READY";
            }
        }

        UpdateCollectButton();
    }

    public void UpdateProductionUI()
    {
        if (selectedBuilding == null || currentBuildingData == null) return;

        int storedAmount = selectedBuilding.GetStoredAmount();
        int queuedAmount = selectedBuilding.GetQueuedProductionCount();
        int inProgressAmount = selectedBuilding.IsProducing() ? currentBuildingData.productionAmount : 0;
        int totalAmount = storedAmount + queuedAmount + inProgressAmount;
        int capacity = currentBuildingData.capacity;

        if (storedAmountText != null)
            storedAmountText.text = storedAmount.ToString();

        if (productionCountText != null)
            productionCountText.text = $"{totalAmount}/{capacity}";

        float remainingTime = selectedBuilding.GetProductionTimeRemaining();
        bool isProducing = selectedBuilding.IsProducing();

        if (productionProgressSlider != null)
        {
            productionProgressSlider.gameObject.SetActive(true);

            if (isProducing && remainingTime > 0)
            {
                float progress = 1 - (remainingTime / currentBuildingData.productionTime);
                productionProgressSlider.value = progress;
            }
            else
            {
                productionProgressSlider.value = 0;
            }
        }

        if (productionTimeText != null)
        {
            if (isProducing && remainingTime > 0)
            {
                productionTimeText.text = $"{remainingTime:F0} sn";
            }
            else if (totalAmount >= capacity)
            {
                productionTimeText.text = "FULL";
            }
            else if (queuedAmount == 0 && storedAmount == 0 && !isProducing)
            {
                productionTimeText.text = "IDLE";
            }
            else if (queuedAmount == 0 && !isProducing)
            {
                productionTimeText.text = "NO PRODUCT IN PRODUCTION";
            }
            else
            {
                productionTimeText.text = "READY";
            }
        }

        UpdateProductionButtons();

        UpdateCollectButton();
    }

    private void UpdateProductionButtons()
    {
        if (selectedBuilding == null || currentBuildingData == null) return;

        int currentProduction = selectedBuilding.GetQueuedProductionCount();
        int storedAmount = selectedBuilding.GetStoredAmount();
        int inProgressAmount = selectedBuilding.IsProducing() ? currentBuildingData.productionAmount : 0;
        int totalAmount = storedAmount + currentProduction + inProgressAmount;
        int capacity = currentBuildingData.capacity;

        bool canAddMore = totalAmount < capacity &&
                         GameManager.Instance.GetResourceAmount(currentBuildingData.requiredResource) >= currentBuildingData.requiredAmount;

        if (addProductionButton != null)
        {
            bool prevInteractable = addProductionButton.interactable;
            addProductionButton.interactable = canAddMore;

            if (prevInteractable && !canAddMore)
            {
                if (totalAmount >= capacity)
                {
                    ShowAlertWithCooldown("Building capacity is full!", true);
                }
                else if (!GameManager.Instance.HasEnoughResource(currentBuildingData.requiredResource, currentBuildingData.requiredAmount))
                {
                    ShowAlertWithCooldown($"Not enough {currentBuildingData.requiredResource}!", true);
                }
            }
        }

        if (removeProductionButton != null)
            removeProductionButton.interactable = currentProduction > 0;
    }

    private void AddProductionOrder()
    {
        if (selectedBuilding != null)
        {
            selectedBuilding.AddProductionOrder();
            UpdateProductionUI();
        }
    }

    private void RemoveProductionOrder()
    {
        if (selectedBuilding != null)
        {
            selectedBuilding.RemoveProductionOrder();
            UpdateProductionUI();
        }
    }

    private void CollectProducts()
    {
        if (selectedBuilding != null)
        {
            Transform startTransform = null;

            if (selectedBuilding.buildingData.requiredAmount > 0)
            {
                if (productResourceImage != null)
                    startTransform = productResourceImage.transform;
            }
            else
            {
                if (infoResourceImage != null)
                    startTransform = infoResourceImage.transform;
            }

            int amountToCollect = selectedBuilding.GetStoredAmount();

            ResourceType resourceType = selectedBuilding.buildingData.producedResource;

            selectedBuilding.CollectProducts();

            if (startTransform != null && amountToCollect > 0)
            {
                PlayCollectionAnimation(resourceType, amountToCollect, startTransform);
            }

            bool isAutoProducer = selectedBuilding.buildingData.requiredAmount == 0;
            if (isAutoProducer)
            {
                StartAutomaticProduction();
                UpdateInfoSlider();
            }
            else
            {
                UpdateProductionUI();
            }
        }
    }

    private void PlayCollectionAnimation(ResourceType resourceType, int amount, Transform startTransform)
    {
        if (collectionAnimationImage == null)
        {
            Debug.LogError("Collection animation image is not assigned!");
            return;
        }
        Transform targetTransform = null;
        switch (resourceType)
        {
            case ResourceType.Wheat:
                targetTransform = wheatUITransform;
                break;
            case ResourceType.Flour:
                targetTransform = flourUITransform;
                break;
            case ResourceType.Bread:
                targetTransform = breadUITransform;
                break;
        }
        if (targetTransform == null)
        {
            Debug.LogWarning($"No target transform assigned for {resourceType}!");
            return;
        }
        if (selectedBuilding != null && selectedBuilding.buildingData.producedResourceSprite != null)
        {
            collectionAnimationImage.sprite = selectedBuilding.buildingData.producedResourceSprite;
        }
        int iconsToAnimate = Mathf.Min(animationIconCount, amount);
        if (iconsToAnimate <= 0) iconsToAnimate = 1;

        for (int i = 0; i < iconsToAnimate; i++)
        {
            Image iconCopy = Instantiate(collectionAnimationImage, collectionAnimationImageParent.transform);
            iconCopy.gameObject.SetActive(true);

            RectTransform iconRectTransform = iconCopy.GetComponent<RectTransform>();
            RectTransform startRectTransform = startTransform.GetComponent<RectTransform>();
            RectTransform targetRectTransform = targetTransform.GetComponent<RectTransform>();

            iconCopy.transform.position = startTransform.position;
            iconCopy.transform.rotation = startTransform.rotation;

            if (startRectTransform != null)
            {
                iconRectTransform.sizeDelta = startRectTransform.sizeDelta;
            }

            Sequence sequence = DOTween.Sequence();

            sequence.AppendInterval(i * animationDelay);

            sequence.Append(iconCopy.transform.DOMove(targetTransform.position, animationDuration)
                .SetEase(Ease.OutQuad));

            sequence.Join(iconCopy.transform.DORotate(targetTransform.eulerAngles, animationDuration)
                .SetEase(Ease.OutQuad));

            sequence.Join(iconCopy.DOFade(1f, animationDuration * 0.2f).From(0.2f));

            if (targetRectTransform != null)
            {
                sequence.Join(DOTween.To(() => iconRectTransform.sizeDelta,
                    x => iconRectTransform.sizeDelta = x,
                    targetRectTransform.sizeDelta,
                    animationDuration).SetEase(Ease.OutQuad));
            }

            sequence.Insert(animationDuration * 0.5f, DOTween.To(() => iconRectTransform.sizeDelta,
                x => iconRectTransform.sizeDelta = x,
                iconRectTransform.sizeDelta * 1.2f,
                animationDuration * 0.2f).SetEase(Ease.OutBack));

            sequence.Insert(animationDuration * 0.7f, DOTween.To(() => iconRectTransform.sizeDelta,
                x => iconRectTransform.sizeDelta = x,
                targetRectTransform.sizeDelta,
                animationDuration * 0.2f).SetEase(Ease.OutQuad));

            sequence.Join(iconCopy.DOFade(0f, animationDuration * 0.3f));

            sequence.OnComplete(() => {
                Destroy(iconCopy.gameObject);
            });
        }
    }

    private void StartAutomaticProduction()
    {
        if (selectedBuilding != null && selectedBuilding.buildingData.requiredAmount == 0)
        {
            if (!selectedBuilding.IsProducing() && selectedBuilding.GetStoredAmount() < selectedBuilding.buildingData.capacity)
            {
                selectedBuilding.StartProductionPublic();
            }
        }
    }

    public void OnClickOutside()
    {
        HideProductionPanel();
        HideInfoSlider();

        if (collectButton != null)
            collectButton.gameObject.SetActive(false);

        if (buildingNameText != null)
            buildingNameText.gameObject.SetActive(false);

        if (selectedBuilding != null)
        {
            selectedBuilding.onProductionUpdate.RemoveListener(UpdateProductionUI);
            selectedBuilding.onProductionUpdate.RemoveListener(UpdateInfoSlider);
        }

        selectedBuilding = null;
        currentBuildingData = null;
    }

    public void ShowAlert(string message, bool isError = false)
    {
        if (alertPanel == null || alertText == null) return;

        DOTween.Kill(alertPanel.transform);
        DOTween.Kill(alertText.transform);

        alertPanel.SetActive(true);
        alertText.text = message;

        alertText.color = isError ? Color.red : new Color(0, 0.7f, 0);

        Sequence alertSequence = DOTween.Sequence();

        alertPanel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        CanvasGroup canvasGroup = alertPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = alertPanel.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0;

        alertSequence.Append(canvasGroup.DOFade(1, alertFadeDuration))
                    .Join(alertPanel.transform.DOScale(1, alertFadeDuration).SetEase(Ease.OutBack))
                    .AppendInterval(alertDuration)
                    .Append(canvasGroup.DOFade(0, alertFadeDuration))
                    .OnComplete(() => {
                        alertPanel.SetActive(false);
                    });

        lastAlertMessage = message;
        lastAlertTime = Time.time;
    }

    public void UpdateAllResourcesUI()
    {
        foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
        {
            int amount = GameManager.Instance.GetResourceAmount(resourceType);
            UpdateResourceUI(resourceType, amount);
        }

        if (selectedBuilding != null)
        {
            if (currentBuildingData.requiredAmount > 0)
            {
                UpdateProductionUI();
            }
            else
            {
                UpdateInfoSlider();
            }

            UpdateCollectButton();
        }

        Debug.Log("All resource UIs have been updated.");
    }

    private void ShowAlertWithCooldown(string message, bool isError = false)
    {
        if (message == lastAlertMessage && Time.time - lastAlertTime < ALERT_COOLDOWN)
            return;

        ShowAlert(message, isError);
    }

    public void ShowBuildingAlert(string buildingName, string message, bool isError = false)
    {
        ShowAlert($"{buildingName}{message}", isError);
    }
}