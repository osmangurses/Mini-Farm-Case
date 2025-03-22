using UnityEngine;
using UnityEngine.EventSystems;

public class TouchController : MonoBehaviour
{
    private Camera mainCamera;
    private BuildingProduction lastSelectedBuilding;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DetectTouch();
        }
    }

    private void DetectTouch()
    {
        if (IsPointerOverUIElement())
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            BuildingProduction building = hit.transform.GetComponent<BuildingProduction>();
            if (building != null)
            {
                if (lastSelectedBuilding != null && lastSelectedBuilding != building)
                {
                    if (lastSelectedBuilding.outline != null)
                        lastSelectedBuilding.outline.enabled = false;
                }

                if (building.outline != null)
                    building.outline.enabled = true;

                lastSelectedBuilding = building;

                building.OnBuildingClicked();
                return;
            }
        }

        if (lastSelectedBuilding != null && lastSelectedBuilding.outline != null)
        {
            lastSelectedBuilding.outline.enabled = false;
            lastSelectedBuilding = null;
        }

        UIManager.Instance.OnClickOutside();
    }

    private bool IsPointerOverUIElement()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}