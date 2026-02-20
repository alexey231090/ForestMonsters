using UnityEngine;
using System.Collections;

public class VisitorSpawner : SignalBinder
{
    [Header("Subscribed Events")]
    [SerializeField] private GameEvent GET_onDayStarted;
    [SerializeField] private GameEvent GET_onNightStarted;

    [Header("Variables SO")]
    [SerializeField] private FloatVariable VAR_Money;

    [Header("Settings")]
    public int minVisitors = 1;
    public int maxVisitors = 15;

    private void Start()
    {
        Bind(GET_onDayStarted, StartNewDay);
        Bind(GET_onNightStarted, StopSpawning);
    }
    
    // Reference to the coroutine so it can be stopped at night
    private Coroutine dailyRoutine;

    public void StartNewDay()
    {
        // Stop previous processes if any
        StopSpawning();

        // Start a new day
        dailyRoutine = StartCoroutine(VirtualVisitorRoutine());
    }

    public void StopSpawning()
    {
        if (dailyRoutine != null)
        {
            StopCoroutine(dailyRoutine);
            dailyRoutine = null;
        }
    }

    IEnumerator VirtualVisitorRoutine()
    {
        // 1. Decide how many people will come today
        int visitorsCount = Random.Range(minVisitors, maxVisitors + 1);
        Debug.Log($"[Forecast] Today the park is expected to be visited by {visitorsCount} people.");

        for (int i = 0; i < visitorsCount; i++)
        {
            // Random delay between visitors (from 2 to 5 seconds)
            yield return new WaitForSeconds(Random.Range(2f, 5f));

            // 2. CHECK: Are there memes in the park?
            if (ParkManager.instance == null) continue;
            
            int memesCount = ParkManager.instance.activePlatforms.Count;

            if (memesCount > 0)
            {
                // Price calculation: number of memes * price
                float payAmount = memesCount * ParkManager.instance.pricePerMeme;

                Debug.Log($"[Visitor #{i + 1}] Viewed {memesCount} memes.");
                if (VAR_Money != null) VAR_Money.ApplyChange(payAmount);
            }
            else
            {
                // If there are no memes - visitor doesn't come or leaves quietly
                // You can uncomment the line below if you want to see complaints
                // Debug.Log($"[Visitor #{i+1}] Turned around at the entrance: \"The park is empty!\"");
            }
        }
    }
}
