using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using Cysharp.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private Dictionary<ResourceType, IntReactiveProperty> resources = new Dictionary<ResourceType, IntReactiveProperty>();

    private bool isGamePaused = false;

    public event Action<ResourceType, int> OnResourceChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            resources[type] = new IntReactiveProperty(0);
        }
    }

    private void Start()
    {
    }

    public void TogglePause()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : 1f;
    }

    public void AddResource(ResourceType type, int amount)
    {
        if (resources.ContainsKey(type))
        {
            resources[type].Value += amount;
            OnResourceChanged?.Invoke(type, resources[type].Value);
        }
    }

    public bool UseResource(ResourceType type, int amount)
    {
        if (resources.ContainsKey(type) && resources[type].Value >= amount)
        {
            resources[type].Value -= amount;
            OnResourceChanged?.Invoke(type, resources[type].Value);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SetResourceAmount(ResourceType type, int amount)
    {
        if (resources.ContainsKey(type))
        {
            resources[type].Value = amount;
            OnResourceChanged?.Invoke(type, resources[type].Value);
        }
    }

    public void ModifyResource(ResourceType type, int amount)
    {
        if (resources.ContainsKey(type))
        {
            if (amount < 0 && resources[type].Value < Math.Abs(amount))
            {
                return;
            }

            resources[type].Value += amount;
            OnResourceChanged?.Invoke(type, resources[type].Value);
        }
    }

    public IReadOnlyReactiveProperty<int> GetResourceObservable(ResourceType type)
    {
        return resources[type];
    }

    public int GetResourceAmount(ResourceType type)
    {
        return resources.ContainsKey(type) ? resources[type].Value : 0;
    }

    public bool HasEnoughResource(ResourceType type, int amount)
    {
        return resources.ContainsKey(type) && resources[type].Value >= amount;
    }

    public void ClearAllResources()
    {
        foreach (var resource in resources)
        {
            resource.Value.Value = 0;
        }
    }

    public void LoadAllResources(Dictionary<string, int> resourceData)
    {
        foreach (var pair in resourceData)
        {
            if (Enum.TryParse<ResourceType>(pair.Key, out ResourceType type))
            {
                SetResourceAmount(type, pair.Value);
            }
        }
    }

    public Dictionary<string, int> GetAllResourcesData()
    {
        Dictionary<string, int> resourceData = new Dictionary<string, int>();

        foreach (var pair in resources)
        {
            resourceData[pair.Key.ToString()] = pair.Value.Value;
        }

        return resourceData;
    }

    public float GetGameTime()
    {
        return Time.time;
    }

    public string GetElapsedTimeText()
    {
        float elapsedTime = Time.time;
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        return $"{minutes:00}:{seconds:00}";
    }

    public void MonitorAllResourceChanges(Action<ResourceType, int> callback)
    {
        foreach (var pair in resources)
        {
            var resourceType = pair.Key;
            pair.Value.Subscribe(value => callback(resourceType, value));
        }
    }

    private void OnApplicationQuit()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
        }
    }
}

public enum ResourceType
{
    Wheat,
    Flour,
    Bread
}