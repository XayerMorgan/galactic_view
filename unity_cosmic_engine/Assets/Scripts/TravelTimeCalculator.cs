using System;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Computes relativistic travel durations based on the universal constant c (299,792.458 km/s).
    /// Handles scales spanning from planetary orbits (Light-Hours) to the cosmological horizon (Giga-Light-Years).
    /// </summary>
    public static class TravelTimeCalculator
    {
        // Fundamental Astronomical & Physical Constants
        public const double C_KM_S = 299792.458; // Speed of light in km/s
        public const double AU_KM = 149597870.7; // 1 Astronomical Unit in km
        public const double LY_KM = 9.4607304725808e12; // 1 Light-Year in km
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
        public static string FormatSpan(double distanceKm) => FormatDistanceSpan(distanceKm);

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

        public static string FormatDistanceSpan(double distanceKm)
        {
            if (distanceKm < LY_KM * 0.1)
            {
                double lh = distanceKm / LH_KM;
                double au = distanceKm / AU_KM;
                return $"{lh:F2} Light-Hours ({au:F1} AU)";
            }
            if (distanceKm < MLY_KM * 0.1)
            {
                double ly = distanceKm / LY_KM;
                return $"{ly:N0} Light-Years";
            }
            if (distanceKm < GLY_KM * 0.1)
            {
                double mly = distanceKm / MLY_KM;
                return $"{mly:F2} Million Light-Years";
            }

            double gly = distanceKm / GLY_KM;
            return $"{gly:F1} Billion Light-Years";
        }
    }
}
