namespace Demo;

// Quick data-only lookup for sizes, orbits, and basic orbital parameters.
// No references to Seaharp geometry or world types.
internal static class Planets
{
    internal readonly struct Planet
    {
        public readonly string Name;
        public readonly long OrbitRadius;
        public readonly int Radius;
        public readonly double InclinationDeg;
        public readonly double AscendingNodeDeg;
        public readonly long[] MoonOrbits; // radii
        public readonly int MoonRadius;
        public readonly double MoonExtraInclineDeg;

        public Planet(string name, long orbitRadius, int radius,
                      double inclinationDeg, double ascendingNodeDeg,
                      long[]? moonOrbits = null, int moonRadius = 10,
                      double moonExtraInclineDeg = 8.0)
        {
            Name = name;
            OrbitRadius = orbitRadius;
            Radius = radius;
            InclinationDeg = inclinationDeg;
            AscendingNodeDeg = ascendingNodeDeg;
            MoonOrbits = moonOrbits ?? Array.Empty<long>();
            MoonRadius = moonRadius;
            MoonExtraInclineDeg = moonExtraInclineDeg;
        }
    }

    // Named planets with rough orbital tilts and nodes; orbit radii are in demo units.
    public static readonly Planet Mercury = new(
        name: "Mercury", orbitRadius: 350, radius: 22,
        inclinationDeg: 7.0, ascendingNodeDeg: 48.3);

    public static readonly Planet Venus = new(
        name: "Venus", orbitRadius: 520, radius: 32,
        inclinationDeg: 3.4, ascendingNodeDeg: 76.7);

    public static readonly Planet Earth = new(
        name: "Earth", orbitRadius: 720, radius: 34,
        inclinationDeg: 0.0, ascendingNodeDeg: 0.0,
        moonOrbits: new long[] { 70 }, moonRadius: 12);

    public static readonly Planet Mars = new(
        name: "Mars", orbitRadius: 920, radius: 26,
        inclinationDeg: 1.85, ascendingNodeDeg: 49.6,
        moonOrbits: new long[] { 55, 80 }, moonRadius: 9);

    public static readonly Planet Jupiter = new(
        name: "Jupiter", orbitRadius: 1250, radius: 78,
        inclinationDeg: 1.3, ascendingNodeDeg: 100.6,
        moonOrbits: new long[] { 140, 190, 260, 340 }, moonRadius: 15,
        moonExtraInclineDeg: 12);

    public static readonly Planet Saturn = new(
        name: "Saturn", orbitRadius: 1600, radius: 66,
        inclinationDeg: 2.49, ascendingNodeDeg: 113.7,
        moonOrbits: new long[] { 160, 240, 320 }, moonRadius: 13,
        moonExtraInclineDeg: 10);

    public static readonly Planet Uranus = new(
        name: "Uranus", orbitRadius: 1950, radius: 54,
        inclinationDeg: 0.77, ascendingNodeDeg: 74.0,
        moonOrbits: new long[] { 120, 180 }, moonRadius: 11);

    public static readonly Planet Neptune = new(
        name: "Neptune", orbitRadius: 2280, radius: 52,
        inclinationDeg: 1.77, ascendingNodeDeg: 131.8,
        moonOrbits: new long[] { 140 }, moonRadius: 11);
}
