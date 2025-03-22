using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string savePath;
    private float saveInterval = 30f;
    private float timeSinceLastSave = 0f;
    private string encryptionKey = "minifarm_2025_key";

    [Serializable]
    public class SerializableResourceData
    {
        public string resourceType;
        public int amount;
    }

    [Serializable]
    public class GameData
    {
        public List<SerializableResourceData> resourcesList = new List<SerializableResourceData>();
        public List<BuildingData> buildings = new List<BuildingData>();
        public long lastSaveTime;
    }

    [Serializable]
    public class BuildingData
    {
        public string buildingId;
        public int storedAmount;
        public float remainingProductionTime;
        public int queuedProductionCount;
        public bool isProducing;
    }

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

        savePath = Path.Combine(Application.persistentDataPath, "minifarm_save.dat");
    }

    private void Start()
    {
        LoadGame();
    }

    private void Update()
    {
        timeSinceLastSave += Time.deltaTime;
        if (timeSinceLastSave >= saveInterval)
        {
            SaveGame();
            timeSinceLastSave = 0f;
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    public void SaveGame()
    {
        try
        {
            GameData gameData = new GameData();

            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                gameData.resourcesList.Add(new SerializableResourceData
                {
                    resourceType = resourceType.ToString(),
                    amount = GameManager.Instance.GetResourceAmount(resourceType)
                });
            }

            BuildingProduction[] buildings = FindObjectsOfType<BuildingProduction>();
            foreach (BuildingProduction building in buildings)
            {
                BuildingData buildingData = new BuildingData
                {
                    buildingId = building.buildingData.name,
                    storedAmount = building.GetStoredAmount(),
                    remainingProductionTime = building.GetProductionTimeRemaining(),
                    queuedProductionCount = building.GetQueuedProductionCount(),
                    isProducing = building.IsProducing()
                };
                gameData.buildings.Add(buildingData);
            }

            gameData.lastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string jsonData = JsonUtility.ToJson(gameData, true);
            string encryptedData = EncryptData(jsonData);
            File.WriteAllText(savePath, encryptedData);

            Debug.Log("Game saved: " + savePath);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAlert("The game was saved successfully.", false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("The game was saved successfully: " + e.Message);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAlert("Game could not be saved: " + e.Message, true);
            }
        }
    }

    public void LoadGame()
    {
        try
        {
            if (!File.Exists(savePath))
            {
                Debug.Log("Save file not found, starting new game.");
                return;
            }

            string encryptedData = File.ReadAllText(savePath);
            string jsonData = DecryptData(encryptedData);
            GameData gameData = JsonUtility.FromJson<GameData>(jsonData);

            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsedSeconds = currentTime - gameData.lastSaveTime;

            foreach (var resourceData in gameData.resourcesList)
            {
                ResourceType resourceType = (ResourceType)Enum.Parse(typeof(ResourceType), resourceData.resourceType);
                GameManager.Instance.SetResourceAmount(resourceType, resourceData.amount);
            }

            BuildingProduction[] buildings = FindObjectsOfType<BuildingProduction>();
            foreach (BuildingProduction building in buildings)
            {
                BuildingData savedData = gameData.buildings.Find(b => b.buildingId == building.buildingData.name);
                if (savedData != null)
                {
                    building.LoadState(savedData, (float)elapsedSeconds);
                }
            }

            UpdateUI();

            Debug.Log($"Game loaded. Elapsed time: {elapsedSeconds} seconds.");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAlert("The game has been successfully loaded.", false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Game could not be loaded: " + e.Message);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAlert("Game could not be loaded: " + e.Message, true);
            }
        }
    }

    private void UpdateUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateAllResourcesUI();
        }
    }

    private string EncryptData(string data)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
        byte[] resultBytes = new byte[dataBytes.Length];

        for (int i = 0; i < dataBytes.Length; i++)
        {
            resultBytes[i] = (byte)(dataBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return Convert.ToBase64String(resultBytes);
    }

    private string DecryptData(string encryptedData)
    {
        byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
        byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
        byte[] resultBytes = new byte[encryptedBytes.Length];

        for (int i = 0; i < encryptedBytes.Length; i++)
        {
            resultBytes[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return Encoding.UTF8.GetString(resultBytes);
    }

    public void SaveGameManually()
    {
        SaveGame();
        timeSinceLastSave = 0f;
    }
}