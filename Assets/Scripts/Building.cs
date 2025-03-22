using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Buildings/Building")]
public class Building : ScriptableObject
{
    public string buildingName;
    public ResourceType producedResource;
    public Sprite producedResourceSprite;
    public int productionAmount;
    public ResourceType requiredResource;
    public int requiredAmount;
    public float productionTime;
    public int capacity;
}