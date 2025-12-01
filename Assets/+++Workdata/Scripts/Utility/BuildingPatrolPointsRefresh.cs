using UnityEngine;

public class BuildingPatrolPointsRefresh : MonoBehaviour
{
    [SerializeField] private Door[] doors;
    [SerializeField] private GameObject buildingPatrolPointsParent;
    private PatrolPoint[] buildingPatrolPoints;

    private PatrolPointManager _patrolPointManager;
    EnemyManager enemy;

    bool isRepaired = false;

    private void Awake()
    {
        _patrolPointManager = FindFirstObjectByType<PatrolPointManager>();
        enemy = FindFirstObjectByType<EnemyManager>();
    }

    private void Start()
    {
        if(gameObject.scene.name == "MainMenuScene") return;

        buildingPatrolPointsParent.SetActive(false);

        int childCount = buildingPatrolPointsParent.transform.childCount;
        buildingPatrolPoints = new PatrolPoint[childCount];
        for (int i = 0; i < childCount; i++)
        {
            buildingPatrolPoints[i] = buildingPatrolPointsParent.transform.GetChild(i).GetComponent<PatrolPoint>();
        }
        Debug.Log("Building Patrol Points found: " + buildingPatrolPoints.Length);

        foreach (Door door in doors)
        {
            if (door != null)
            {
                door.refreshBuildingPatrolPoints.AddListener(RefreshPatrolPoints);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (Door door in doors)
        {
            if (door != null)
            {
                door.refreshBuildingPatrolPoints.RemoveListener(RefreshPatrolPoints);
            }
        }
    }

    public void RefreshPatrolPoints()
    {
        if (isRepaired) return;

        CheckIfAtLeastOneDoorIsOpen();
        _patrolPointManager.RefreshPatrolPoints();
    }

    private void CheckIfAtLeastOneDoorIsOpen()
    {
        bool oneDoorIsOpen = false;

        foreach (Door door in doors)
        {
            if (door != null && door.isOpen)
            {
                oneDoorIsOpen = true;
                break;
            }
        }

        TogglePatrolPoints(oneDoorIsOpen);
        Debug.Log("At least one Door is open = " + oneDoorIsOpen);
    }

    private void TogglePatrolPoints(bool oneDoorIsOpen)
    {
        if (isRepaired) return;

        if (oneDoorIsOpen)
        {
            buildingPatrolPointsParent.SetActive(true);
        }
        else
        {
            buildingPatrolPointsParent.SetActive(false);
        }
    }

    public void OfficeSubSystemRepaired()
    {
        buildingPatrolPointsParent.SetActive(false);
        _patrolPointManager.RefreshPatrolPoints();
        isRepaired = true;
        enemy.TeleportEnemy();
        enemy.playerTarget = null;
        enemy.soundTarget = null;
        enemy.lastPlayerPosTarget = null;
    }
}