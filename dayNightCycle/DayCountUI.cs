using UnityEngine;
using TMPro; // Or use UnityEngine.UI if not using TextMeshPro

namespace DayNightCycleStuff { 
public class DayDisplayUI : MonoBehaviour
{
    public DayNight dayNightCycle;
    public TextMeshProUGUI textDisplay; // Or use Text if you're not using TMP

    void Update()
    {
        if (dayNightCycle != null && textDisplay != null)
        {
            textDisplay.text = "Day: " + (dayNightCycle.dayCount + 1); // +1 so Day 1 starts at 0
        }
    }
}
}
