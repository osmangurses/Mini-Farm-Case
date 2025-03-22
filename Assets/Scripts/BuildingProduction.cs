using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using System;

[RequireComponent(typeof(ObjectOutline))]
public class BuildingProduction : MonoBehaviour
{
    public Building buildingData;
    public ObjectOutline outline;
    [SerializeField] private int storedAmount = 0;
    private bool isProducing = false;

    private Queue<ProductionOrder> productionQueue = new Queue<ProductionOrder>();
    private float currentProductionTimeRemaining = 0f;

    public UnityEvent onProductionStart;
    public UnityEvent onProductionComplete;
    public UnityEvent onProductionUpdate;

    [Serializable]
    private class ProductionOrder
    {
        public ResourceType producedResource;
        public int amount;
    }

    private void Start()
    {
        storedAmount = 0;
        outline = GetComponent<ObjectOutline>();
        StartCoroutine(UpdateProductionStatus());
    }

    public void OnBuildingClicked()
    {
        if (buildingData == null) return;

        UIManager.Instance.ShowBuildingUI(this);

        if (buildingData.requiredAmount == 0)
        {
            if (!isProducing && storedAmount < buildingData.capacity)
            {
                StartProduction();
            }
        }
    }

    private void StartProduction()
    {
        if (!isProducing && storedAmount < buildingData.capacity)
        {
            StartCoroutine(Produce());
            onProductionStart?.Invoke();
        }
        else
        {
            UIManager.Instance.ShowBuildingAlert($"{buildingData.buildingName}", $": Capacity is full or production already in progress!");
        }
    }

    public void StartProductionPublic()
    {
        StartProduction();
    }

    private IEnumerator Produce()
    {
        isProducing = true;
        UIManager.Instance.ShowBuildingAlert($"{buildingData.buildingName}", $": Production started... will take {buildingData.productionTime} seconds.");

        currentProductionTimeRemaining = buildingData.productionTime;

        while (currentProductionTimeRemaining > 0)
        {
            yield return new WaitForSeconds(0.1f);
            currentProductionTimeRemaining -= 0.1f;
            onProductionUpdate?.Invoke();
        }

        currentProductionTimeRemaining = 0;
        storedAmount += buildingData.productionAmount;

        UIManager.Instance.ShowBuildingAlert($"{buildingData.buildingName}", $": {buildingData.productionAmount} {buildingData.producedResource} produced. Current stock: {storedAmount}");

        onProductionComplete?.Invoke();

        isProducing = false;

        if (buildingData.requiredAmount == 0 && storedAmount < buildingData.capacity)
        {
            StartProduction();
        }
        else if (productionQueue.Count > 0)
        {
            ProcessNextProductionOrder();
        }
    }

    public void CollectProducts()
    {
        if (storedAmount > 0)
        {
            GameManager.Instance.AddResource(buildingData.producedResource, storedAmount);
            UIManager.Instance.ShowBuildingAlert($"{buildingData.buildingName}", $": {storedAmount} {buildingData.producedResource} collected!");
            storedAmount = 0;

            onProductionUpdate?.Invoke();
        }
        else
        {
            UIManager.Instance.ShowBuildingAlert($"{buildingData.buildingName}", $": No product to collect!");
        }
    }

    private IEnumerator UpdateProductionStatus()
    {
        while (true)
        {
            onProductionUpdate?.Invoke();
            yield return new WaitForSeconds(1f);
        }
    }

    public void AddProductionOrder()
    {
        if (buildingData.requiredAmount <= 0) return;

        int totalProduction = storedAmount + productionQueue.Count;

        if (totalProduction >= buildingData.capacity)
        {
            UIManager.Instance.ShowBuildingAlert($"{buildingData.buildingName}", $": Capacity is full! Can't add more production.");
            return;
        }

        if (!GameManager.Instance.UseResource(buildingData.requiredResource, buildingData.requiredAmount))
        {
            UIManager.Instance.ShowBuildingAlert($"{buildingData.buildingName}", $": Not enough {buildingData.requiredResource}!");
            return;
        }

        ProductionOrder order = new ProductionOrder
        {
            producedResource = buildingData.producedResource,
            amount = buildingData.productionAmount
        };

        productionQueue.Enqueue(order);

        if (!isProducing)
        {
            ProcessNextProductionOrder();
        }
    }

    public void RemoveProductionOrder()
    {
        if (productionQueue.Count > 0)
        {
            productionQueue.Dequeue();
            GameManager.Instance.AddResource(buildingData.requiredResource, buildingData.requiredAmount);
            UIManager.Instance.ShowBuildingAlert($"{buildingData.buildingName}", $": Production order canceled. {buildingData.requiredResource} refunded.");
        }
    }

    private void ProcessNextProductionOrder()
    {
        if (productionQueue.Count > 0)
        {
            ProductionOrder order = productionQueue.Dequeue();
            StartCoroutine(ProduceOrdered(order));
        }
    }

    private IEnumerator ProduceOrdered(ProductionOrder order)
    {
        isProducing = true;
        UIManager.Instance.ShowBuildingAlert($"{buildingData.buildingName}", $": Order production started... will take {buildingData.productionTime} seconds.");

        currentProductionTimeRemaining = buildingData.productionTime;

        while (currentProductionTimeRemaining > 0)
        {
            yield return new WaitForSeconds(0.1f);
            currentProductionTimeRemaining -= 0.1f;
            onProductionUpdate?.Invoke();
        }

        currentProductionTimeRemaining = 0;
        storedAmount += order.amount;

        UIManager.Instance.ShowBuildingAlert($"{buildingData.buildingName}", $": {order.amount} {order.producedResource} produced. Current stock: {storedAmount}");

        onProductionComplete?.Invoke();

        isProducing = false;

        if (productionQueue.Count > 0)
        {
            ProcessNextProductionOrder();
        }
    }

    public void LoadState(SaveManager.BuildingData savedData, float elapsedSeconds)
    {
        storedAmount = savedData.storedAmount;

        currentProductionTimeRemaining = savedData.remainingProductionTime;

        isProducing = savedData.isProducing;

        productionQueue.Clear();

        for (int i = 0; i < savedData.queuedProductionCount; i++)
        {
            ProductionOrder order = new ProductionOrder
            {
                producedResource = buildingData.producedResource,
                amount = buildingData.productionAmount
            };

            productionQueue.Enqueue(order);
        }

        storedAmount = Mathf.Min(storedAmount, buildingData.capacity);

        if (isProducing && currentProductionTimeRemaining > 0)
        {
            currentProductionTimeRemaining -= elapsedSeconds;

            if (currentProductionTimeRemaining <= 0)
            {
                FinishProduction(elapsedSeconds);
            }
            else
            {
                StartCoroutine(ResumeProduction());
            }
        }
        else if (productionQueue.Count > 0 && !isProducing)
        {
            ProcessNextProductionOrder();
        }
        else if (buildingData.requiredAmount == 0 && storedAmount < buildingData.capacity && !isProducing)
        {
            StartProduction();
        }

        onProductionUpdate?.Invoke();

        Debug.Log($"{buildingData.buildingName}: State loaded. Storage: {storedAmount}, Remaining time: {currentProductionTimeRemaining}, Queue: {productionQueue.Count}");
    }

    private void FinishProduction(float elapsedSeconds)
    {
        currentProductionTimeRemaining = 0;
        storedAmount += buildingData.productionAmount;

        storedAmount = Mathf.Min(storedAmount, buildingData.capacity);

        isProducing = false;
        onProductionComplete?.Invoke();

        float remainingTime = elapsedSeconds - buildingData.productionTime;
        int completedProductions = 0;

        if (buildingData.requiredAmount == 0)
        {
            while (remainingTime > 0 && storedAmount < buildingData.capacity)
            {
                int productionsToComplete = Mathf.FloorToInt(remainingTime / buildingData.productionTime);
                int spacesAvailable = buildingData.capacity - storedAmount;
                int actualProductions = Mathf.Min(productionsToComplete, spacesAvailable);

                storedAmount += actualProductions * buildingData.productionAmount;
                storedAmount = Mathf.Min(storedAmount, buildingData.capacity);

                completedProductions += actualProductions;
                remainingTime -= actualProductions * buildingData.productionTime;
            }
        }
        else
        {
            while (remainingTime > 0 && productionQueue.Count > 0)
            {
                if (remainingTime >= buildingData.productionTime && storedAmount < buildingData.capacity)
                {
                    ProductionOrder order = productionQueue.Dequeue();
                    storedAmount += order.amount;
                    storedAmount = Mathf.Min(storedAmount, buildingData.capacity);

                    completedProductions++;
                    remainingTime -= buildingData.productionTime;
                }
                else
                {
                    break;
                }
            }
        }

        if (completedProductions > 0)
        {
            Debug.Log($"{buildingData.buildingName}: {completedProductions} productions completed in elapsed time.");
        }

        if (productionQueue.Count > 0)
        {
            ProcessNextProductionOrder();
        }
        else if (buildingData.requiredAmount == 0 && storedAmount < buildingData.capacity)
        {
            StartProduction();
        }
    }

    private IEnumerator ResumeProduction()
    {
        Debug.Log($"{buildingData.buildingName}: Production continues with remaining time {currentProductionTimeRemaining:F1} seconds...");

        while (currentProductionTimeRemaining > 0)
        {
            yield return new WaitForSeconds(0.1f);
            currentProductionTimeRemaining -= 0.1f;
            onProductionUpdate?.Invoke();
        }

        currentProductionTimeRemaining = 0;
        storedAmount += buildingData.productionAmount;

        storedAmount = Mathf.Min(storedAmount, buildingData.capacity);

        isProducing = false;
        onProductionComplete?.Invoke();

        if (productionQueue.Count > 0)
        {
            ProcessNextProductionOrder();
        }
        else if (buildingData.requiredAmount == 0 && storedAmount < buildingData.capacity)
        {
            StartProduction();
        }
    }

    public int GetStoredAmount() => storedAmount;
    public bool IsProducing() => isProducing;
    public float GetProductionTimeRemaining() => currentProductionTimeRemaining;
    public int GetQueuedProductionCount() => productionQueue.Count;
    public int GetTotalProduction() => storedAmount + productionQueue.Count;
}