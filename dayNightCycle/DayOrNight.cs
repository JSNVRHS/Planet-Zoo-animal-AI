using UnityEngine;
using TMPro; // Or use UnityEngine.UI if not using TextMeshPro

namespace DayNightCycleStuff { 
public class DayOrNightUI : MonoBehaviour
{
    public DayNight dayNightCycle;
    public TextMeshProUGUI textDisplay; // Or use Text if you're not using TMP

    void Update()
    {
        if (dayNightCycle != null && textDisplay != null)
        {
            if (dayNightCycle.isDaytime)
            {
                textDisplay.text = "Day";
            }
            else
            {
                textDisplay.text = "Night";
            }
        }
    }
}
}
