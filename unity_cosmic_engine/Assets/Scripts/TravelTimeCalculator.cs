using System;
using UnityEngine;

namespace CosmicZoom
{
    public enum UnitSystem
    {
        Kilometers = 0,
        Miles = 1,
        Dual = 2
    }

    /// <summary>
    /// Computes relativistic travel durations based on the universal constant c (299,792.458 km/s).
    /// Handles scales spanning from planetary orbits (Light-Hours) to the cosmological horizon (Giga-Light-Years).
    /// Supports metric (km), imperial (miles), and dual (both) measurement systems.
    /// </summary>
    public static class TravelTimeCalculator
    {
        // Unit Conversion Constants
        public const double KM_TO_MILES = 0.621371192237334;

        // Fundamental Astronomical & Physical Constants
        public const double C_KM_S = 299792.458; // Speed of light in km/s (186,282.4 mi/s)
        public const double AU_KM = 149597870.7; // 1 Astronomical Unit in km (92.96 million miles)
        public const double LY_KM = 9.4607304725808e12; // 1 Light-Year in km (5.879e12 miles)
        public const double LH_KM = C_KM_S * 3600.0; // 1 Light-Hour in km (~1.07925e9 km)
        public const double MLY_KM = LY_KM * 1e6; // 1 Mega-Light-Year in km
        public const double GLY_KM = LY_KM * 1e9; // 1 Giga-Light-Year in km

        // Benchmark stage diameters in kilometers
        public const double STAGE_1_SOLAR_KM = 60.14 * AU_KM; // Neptune orbital diameter (~8.996e9 km)
        public const double STAGE_2_GALAXY_KM = 100000.0 * LY_KM; // Milky Way disk (~9.461e17 km)
        public const double STAGE_3_LOCAL_KM = 10.0 * MLY_KM; // Local Group cluster (~9.461e19 km)
        public const double STAGE_4_UNIVERSE_KM = 93.0 * GLY_KM; // Observable Universe diameter (~8.798e23 km)

        // Vehicle velocity benchmarks in km/s
        public const double SPEED_PHOTON = C_KM_S; // 1.0c
        public const double SPEED_RELATIVISTIC = 0.1 * C_KM_S; // 0.1c (~29,979 km/s)
        public const double SPEED_PARKER_SOLAR_PROBE = 192.0; // Max recorded heliocentric speed (km/s)
        public const double SPEED_VOYAGER_1 = 17.0; // Interstellar departure speed (km/s)
        public const double SPEED_JETLINER = 900.0 / 3600.0; // 0.25 km/s (900 km/h)

        public static double GetLightTransitSeconds(double distanceKm)
        {
            return distanceKm / C_KM_S;
        }

        public static double GetTransitSeconds(double distanceKm, double velocityKmS)
        {
            if (velocityKmS <= 0) return double.PositiveInfinity;
            return distanceKm / velocityKmS;
        }

        public static double GetSpanKmFromZoom(float z)
        {
            z = Mathf.Clamp(z, 1.0f, 4.0f);
            if (z <= 2.0f)
            {
                float t = (z - 1.0f);
                double logMin = Math.Log10(STAGE_1_SOLAR_KM);
                double logMax = Math.Log10(STAGE_2_GALAXY_KM);
                return Math.Pow(10.0, logMin * (1.0 - t) + logMax * t);
            }
            else if (z <= 3.0f)
            {
                float t = (z - 2.0f);
                double logMin = Math.Log10(STAGE_2_GALAXY_KM);
                double logMax = Math.Log10(STAGE_3_LOCAL_KM);
                return Math.Pow(10.0, logMin * (1.0 - t) + logMax * t);
            }
            else
            {
                float t = (z - 3.0f);
                double logMin = Math.Log10(STAGE_3_LOCAL_KM);
                double logMax = Math.Log10(STAGE_4_UNIVERSE_KM);
                return Math.Pow(10.0, logMin * (1.0 - t) + logMax * t);
            }
        }

        public static string FormatTime(double seconds) => FormatDuration(seconds);
        public static string FormatSpan(double distanceKm, UnitSystem unitSystem = UnitSystem.Kilometers) => FormatDistanceSpan(distanceKm, unitSystem);

        public static string FormatDuration(double seconds)
        {
            if (double.IsInfinity(seconds) || seconds < 0) return "N/A";

            if (seconds < 60.0)
            {
                return $"{seconds:F2} Seconds";
            }
            if (seconds < 3600.0)
            {
                int m = (int)(seconds / 60.0);
                double s = seconds % 60.0;
                return $"{m}m {s:F1}s";
            }
            if (seconds < 86400.0)
            {
                int h = (int)(seconds / 3600.0);
                int m = (int)((seconds % 3600.0) / 60.0);
                int s = (int)(seconds % 60.0);
                return $"{h}h {m}m {s}s";
            }
            if (seconds < 365.25 * 86400.0)
            {
                double days = seconds / 86400.0;
                return $"{days:F1} Earth Days";
            }

            double years = seconds / (365.25 * 86400.0);
            if (years < 1e6)
            {
                return $"{years:N0} Years";
            }
            if (years < 1e9)
            {
                double myr = years / 1e6;
                return $"{myr:F2} Million Years";
            }

            double byr = years / 1e9;
            return $"{byr:F1} Billion Years";
        }

        public static string FormatRawDistance(double distanceKm, UnitSystem unitSystem)
        {
            double mi = distanceKm * KM_TO_MILES;
            return unitSystem switch
            {
                UnitSystem.Miles => mi < 1e7 ? $"{mi:N0} mi" : $"{mi:E3} mi",
                UnitSystem.Dual => distanceKm < 1e7 ? $"{distanceKm:N0} km ({mi:N0} mi)" : $"{distanceKm:E3} km [{mi:E3} mi]",
                _ => distanceKm < 1e7 ? $"{distanceKm:N0} km" : $"{distanceKm:E3} km"
            };
        }

        public static string FormatSpeed(double speedKmS, UnitSystem unitSystem, string craftType)
        {
            double miS = speedKmS * KM_TO_MILES;
            double kmH = speedKmS * 3600.0;
            double mph = miS * 3600.0;

            if (craftType == "photon")
            {
                return unitSystem switch
                {
                    UnitSystem.Miles => "Photon (Speed of Light, 1.0c • 186,282 mi/s)",
                    UnitSystem.Dual => "Photon (Speed of Light, 1.0c • 299,792 km/s | 186,282 mi/s)",
                    _ => "Photon (Speed of Light, 1.0c • 299,792 km/s)"
                };
            }
            else if (craftType == "relativistic")
            {
                return unitSystem switch
                {
                    UnitSystem.Miles => "Relativistic Craft (0.10c • 18,628 mi/s)",
                    UnitSystem.Dual => "Relativistic Craft (0.10c • 29,979 km/s | 18,628 mi/s)",
                    _ => "Relativistic Craft (0.10c • 29,979 km/s)"
                };
            }
            else if (craftType == "parker")
            {
                return unitSystem switch
                {
                    UnitSystem.Miles => $"Parker Solar Probe ({mph:N0} mph / {miS:F1} mi/s)",
                    UnitSystem.Dual => $"Parker Solar Probe (192 km/s [{mph:N0} mph])",
                    _ => "Parker Solar Probe (192 km/s • 691,200 km/h)"
                };
            }
            else if (craftType == "voyager")
            {
                return unitSystem switch
                {
                    UnitSystem.Miles => $"Voyager 1 ({mph:N0} mph / {miS:F1} mi/s)",
                    UnitSystem.Dual => $"Voyager 1 (17 km/s [{mph:N0} mph])",
                    _ => "Voyager 1 (17 km/s • 61,200 km/h)"
                };
            }
            else if (craftType == "jetliner")
            {
                return unitSystem switch
                {
                    UnitSystem.Miles => "Commercial Airliner (560 mph)",
                    UnitSystem.Dual => "Commercial Airliner (900 km/h [560 mph])",
                    _ => "Commercial Airliner (900 km/h • 250 m/s)"
                };
            }

            return craftType;
        }

        public static string FormatDistanceSpan(double distanceKm, UnitSystem unitSystem = UnitSystem.Kilometers)
        {
            double mi = distanceKm * KM_TO_MILES;

            if (distanceKm < LY_KM * 0.1)
            {
                double lh = distanceKm / LH_KM;
                double au = distanceKm / AU_KM;
                return unitSystem switch
                {
                    UnitSystem.Miles => $"{lh:F2} Light-Hours ({au:F1} AU • {mi:E2} mi)",
                    UnitSystem.Dual => $"{lh:F2} Light-Hours ({au:F1} AU • {distanceKm:E2} km / {mi:E2} mi)",
                    _ => $"{lh:F2} Light-Hours ({au:F1} AU • {distanceKm:E2} km)"
                };
            }
            if (distanceKm < MLY_KM * 0.1)
            {
                double ly = distanceKm / LY_KM;
                return unitSystem switch
                {
                    UnitSystem.Miles => $"{ly:N0} Light-Years ({mi:E2} mi)",
                    UnitSystem.Dual => $"{ly:N0} Light-Years ({distanceKm:E2} km | {mi:E2} mi)",
                    _ => $"{ly:N0} Light-Years ({distanceKm:E2} km)"
                };
            }
            if (distanceKm < GLY_KM * 0.1)
            {
                double mly = distanceKm / MLY_KM;
                return unitSystem switch
                {
                    UnitSystem.Miles => $"{mly:F2} Million Light-Years ({mi:E2} mi)",
                    UnitSystem.Dual => $"{mly:F2} Million Light-Years ({distanceKm:E2} km | {mi:E2} mi)",
                    _ => $"{mly:F2} Million Light-Years ({distanceKm:E2} km)"
                };
            }

            double gly = distanceKm / GLY_KM;
            return unitSystem switch
            {
                UnitSystem.Miles => $"{gly:F1} Billion Light-Years ({mi:E2} mi)",
                UnitSystem.Dual => $"{gly:F1} Billion Light-Years ({distanceKm:E2} km | {mi:E2} mi)",
                _ => $"{gly:F1} Billion Light-Years ({distanceKm:E2} km)"
            };
        }
    }
}
