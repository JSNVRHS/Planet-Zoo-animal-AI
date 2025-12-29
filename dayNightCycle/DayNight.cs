using UnityEngine;

namespace DayNightCycleStuff
{
    public class DayNight : MonoBehaviour
    {
        public Light sun;
        public float dayDurationInSeconds = 60f; // Full 24h cycle in 60 real seconds
        public Material daySkybox;
        public Material nightSkybox;

        public float currentTime = 0f; // 0 to 1, where 0.5 is midnight
        public int dayCount = 0;
        public bool isDaytime;

        void Update()
        {
            currentTime += Time.deltaTime / dayDurationInSeconds;

            if (currentTime >= 1f)
            {
                currentTime = 0f;
                dayCount++;
            }

            // Rotate sun around to simulate movement
            float sunAngle = currentTime * 360f - 90f; // -90 so sun starts at horizon
            sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);

            // Update lighting based on time
            UpdateLighting();
        }

        void UpdateLighting()
        {
            if (currentTime >= 0.25f && currentTime <= 0.75f)
            {
                // Daytime
                if (!isDaytime)
                {
                    isDaytime = true;
                    if (daySkybox != null)
                        RenderSettings.skybox = daySkybox;
                    sun.intensity = 1.2f;
                }
            }
            else
            {
                // Nighttime
                if (isDaytime)
                {
                    isDaytime = false;
                    if (nightSkybox != null)
                        RenderSettings.skybox = nightSkybox;
                    sun.intensity = 0.2f;
                }
            }
        }
    }
}

